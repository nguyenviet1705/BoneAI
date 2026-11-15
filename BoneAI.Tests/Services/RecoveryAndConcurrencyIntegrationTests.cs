using System.Net;
using System.Net.Http.Json;
using BoneAI.Application.DTOs;
using BoneAI.Application.Interfaces;
using BoneAI.Domain.Entities;
using BoneAI.Domain.Enums;
using BoneAI.Infrastructure.BackgroundServices;
using BoneAI.Infrastructure.Configuration;
using BoneAI.Infrastructure.Persistence;
using BoneAI.Tests.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace BoneAI.Tests.Services;

public class RecoveryAndConcurrencyIntegrationTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly string _testStorageDir;
    private readonly string _dbName;

    private readonly Guid _testImageId = Guid.NewGuid();
    private readonly Guid _testModelId = Guid.NewGuid();

    public RecoveryAndConcurrencyIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _testStorageDir = Path.Combine(Path.GetTempPath(), "BoneAI_Recovery_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testStorageDir);

        _dbName = "RecoveryConcurrencyDb_" + Guid.NewGuid().ToString("N");

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");

            builder.ConfigureServices(services =>
            {
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<BoneAIDbContext>));
                if (descriptor != null)
                {
                    services.Remove(descriptor);
                }

                services.AddDbContext<BoneAIDbContext>(options =>
                {
                    options.UseInMemoryDatabase(_dbName)
                        .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
                });

                services.Configure<ImageStorageOptions>(options =>
                {
                    options.RootPath = _testStorageDir;
                });
            });
        });

        _client = _factory.CreateClient();

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BoneAIDbContext>();

        var imagePath = Path.Combine(_testStorageDir, "wrist_rec.png");
        File.WriteAllBytes(imagePath, TestImageGenerator.CreateValidPng(100, 100));

        db.XRayImages.Add(new XRayImage
        {
            Id = _testImageId,
            FileName = "wrist_rec.png",
            FilePath = "wrist_rec.png",
            ContentType = "image/png",
            FileSize = 100,
            CreatedAt = DateTime.UtcNow
        });

        db.AIModels.Add(new AIModel
        {
            Id = _testModelId,
            Name = "Model Recovery",
            ModelKey = "model-rec",
            Version = "1.0",
            IsActive = true,
            IsAvailable = true
        });

        db.SaveChanges();
    }

    [Fact]
    public async Task ClaimJob_ExpiredLeaseFromCrashedWorker_IsSuccessfullyReclaimed()
    {
        var runId = Guid.NewGuid();
        var analysisId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BoneAIDbContext>();

            var analysis = new Analysis
            {
                Id = analysisId,
                XRayImageId = _testImageId,
                Status = AnalysisStatus.Processing,
                CreatedAt = DateTime.UtcNow.AddMinutes(-10)
            };

            // Run was processing, but lease expired 5 minutes ago (simulating crashed worker)
            var abandonedRun = new AnalysisRun
            {
                Id = runId,
                AnalysisId = analysisId,
                AIModelId = _testModelId,
                Status = AnalysisRunStatus.Processing,
                StartedAt = DateTime.UtcNow.AddMinutes(-10),
                LeaseUntil = DateTime.UtcNow.AddMinutes(-5),
                RetryCount = 0
            };

            analysis.AnalysisRuns.Add(abandonedRun);
            db.Analyses.Add(analysis);
            db.SaveChanges();
        }

        // Now an active worker attempts to claim next job
        using (var scope = _factory.Services.CreateScope())
        {
            var runRepo = scope.ServiceProvider.GetRequiredService<IAnalysisRunRepository>();
            var claimedRun = await runRepo.ClaimNextPendingRunAsync(TimeSpan.FromMinutes(2));

            Assert.NotNull(claimedRun);
            Assert.Equal(runId, claimedRun.Id);
            Assert.Equal(AnalysisRunStatus.Processing, claimedRun.Status);
            Assert.NotNull(claimedRun.LeaseUntil);
            Assert.True(claimedRun.LeaseUntil > DateTime.UtcNow); // Lease renewed
        }
    }

    [Fact]
    public async Task TransientError_TriggersRetryWithExponentialBackoff()
    {
        var runId = Guid.NewGuid();
        var analysisId = Guid.NewGuid();

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BoneAIDbContext>();

            var analysis = new Analysis
            {
                Id = analysisId,
                XRayImageId = _testImageId,
                Status = AnalysisStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            var run = new AnalysisRun
            {
                Id = runId,
                AnalysisId = analysisId,
                AIModelId = _testModelId,
                Status = AnalysisRunStatus.Pending,
                RetryCount = 0
            };

            analysis.AnalysisRuns.Add(run);
            db.Analyses.Add(analysis);
            db.SaveChanges();
        }

        var mockAiClient = new Mock<IAIServiceClient>();
        // Transient error (HttpRequestException)
        mockAiClient.Setup(c => c.ExecuteInferenceAsync(It.IsAny<InferenceClientRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Connection reset by peer"));

        var mockStorage = new Mock<IImageStorageService>();
        mockStorage.Setup(s => s.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(new byte[] { 1, 2, 3 }));

        var services = new ServiceCollection();
        services.AddDbContext<BoneAIDbContext>(options =>
            options.UseInMemoryDatabase(_dbName)
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)));
        services.AddScoped<IAnalysisRunRepository, Infrastructure.Repositories.AnalysisRunRepository>();
        services.AddScoped<IAnalysisRepository, Infrastructure.Repositories.AnalysisRepository>();
        services.AddSingleton(mockStorage.Object);
        services.AddSingleton(mockAiClient.Object);

        var provider = services.BuildServiceProvider();
        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(f => f.CreateScope()).Returns(provider.CreateScope());

        var options = Options.Create(new AIServiceOptions
        {
            MaxRetryAttempts = 3,
            InitialRetryDelayMs = 100
        });

        var worker = new AnalysisBackgroundWorker(scopeFactory.Object, options, NullLogger<AnalysisBackgroundWorker>.Instance);

        var processed = await worker.ProcessNextJobAsync(CancellationToken.None);
        Assert.True(processed);

        // Verify run was scheduled for retry: RetryCount = 1, Status = Pending, LeaseUntil set in future
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BoneAIDbContext>();
            var run = await db.AnalysisRuns.FindAsync(runId);
            Assert.NotNull(run);
            Assert.Equal(1, run.RetryCount);
            Assert.Equal(AnalysisRunStatus.Pending, run.Status);
            Assert.Contains("Transient error on attempt 1", run.ErrorMessage);
            Assert.NotNull(run.LeaseUntil);
            Assert.True(run.LeaseUntil > DateTime.UtcNow);
        }
    }

    [Fact]
    public async Task IdempotencyKey_SameKeySubmittedMultipleTimes_ReturnsSameAnalysisWithoutDuplicates()
    {
        var idempotencyKey = "idem-" + Guid.NewGuid();

        var request1 = new SubmitAnalysisRequest
        {
            XRayImageId = _testImageId,
            AIModelId = _testModelId,
            IdempotencyKey = idempotencyKey
        };

        var res1 = await _client.PostAsJsonAsync("/api/analysis", request1);
        Assert.Equal(HttpStatusCode.Accepted, res1.StatusCode);
        var body1 = await res1.Content.ReadFromJsonAsync<AnalysisSubmissionResponse>();
        Assert.NotNull(body1);

        // Submit again with same idempotency key
        var request2 = new SubmitAnalysisRequest
        {
            XRayImageId = _testImageId,
            AIModelId = _testModelId,
            IdempotencyKey = idempotencyKey
        };

        var res2 = await _client.PostAsJsonAsync("/api/analysis", request2);
        Assert.Equal(HttpStatusCode.Accepted, res2.StatusCode);
        var body2 = await res2.Content.ReadFromJsonAsync<AnalysisSubmissionResponse>();
        Assert.NotNull(body2);

        // Verify identical AnalysisId and RunId returned
        Assert.Equal(body1.AnalysisId, body2.AnalysisId);
        Assert.Equal(body1.AnalysisRunId, body2.AnalysisRunId);

        // Verify in DB only 1 Analysis exists with that idempotency key
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BoneAIDbContext>();
        var count = await db.Analyses.CountAsync(a => a.IdempotencyKey == idempotencyKey);
        Assert.Equal(1, count);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testStorageDir))
            {
                Directory.Delete(_testStorageDir, true);
            }
        }
        catch { }
    }
}
