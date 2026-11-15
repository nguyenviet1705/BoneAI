using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using BoneAI.Application.DTOs;
using BoneAI.Application.Interfaces;
using BoneAI.Domain.Entities;
using BoneAI.Domain.Enums;
using BoneAI.Infrastructure.Configuration;
using BoneAI.Infrastructure.Persistence;
using BoneAI.Tests.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit.Abstractions;

namespace BoneAI.Tests.Performance;

public class PerformanceBenchmarkTests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;
    private readonly ITestOutputHelper _output;
    private readonly string _testStorageDir;
    private readonly Guid _benchImageId = Guid.NewGuid();
    private readonly Guid _benchAnalysisId = Guid.NewGuid();

    public PerformanceBenchmarkTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _output = output;
        _testStorageDir = Path.Combine(Path.GetTempPath(), "BoneAI_Perf_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testStorageDir);

        var dbName = "PerfBenchmarkDb_" + Guid.NewGuid().ToString("N");

        var mockAiClient = new Mock<IAIServiceClient>();
        mockAiClient.Setup(c => c.CheckHealthAsync(It.IsAny<CancellationToken>())).ReturnsAsync(true);

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
                    options.UseInMemoryDatabase(dbName);
                });

                services.Configure<ImageStorageOptions>(options =>
                {
                    options.RootPath = _testStorageDir;
                });

                var aiDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(IAIServiceClient));
                if (aiDescriptor != null)
                {
                    services.Remove(aiDescriptor);
                }
                services.AddSingleton(mockAiClient.Object);
            });
        });

        _client = _factory.CreateClient();

        // Seed 50 historical analyses for realistic pagination benchmark
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BoneAIDbContext>();

        var imagePath = Path.Combine(_testStorageDir, "bench_wrist.png");
        File.WriteAllBytes(imagePath, TestImageGenerator.CreateValidPng(200, 200));

        db.XRayImages.Add(new XRayImage
        {
            Id = _benchImageId,
            FileName = "bench_wrist.png",
            FilePath = "bench_wrist.png",
            ContentType = "image/png",
            FileSize = 1024,
            CreatedAt = DateTime.UtcNow
        });

        var model = new AIModel { Id = Guid.NewGuid(), Name = "Benchmark Model", ModelKey = "bench-model", Version = "1.0", IsActive = true };
        db.AIModels.Add(model);

        for (int i = 0; i < 50; i++)
        {
            var analysisId = (i == 0) ? _benchAnalysisId : Guid.NewGuid();
            var analysis = new Analysis
            {
                Id = analysisId,
                XRayImageId = _benchImageId,
                Mode = AnalysisMode.SingleModel,
                Status = AnalysisStatus.Completed,
                CreatedAt = DateTime.UtcNow.AddMinutes(-i),
                AnalysisRuns = new List<AnalysisRun>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        AIModelId = model.Id,
                        Status = AnalysisRunStatus.Completed,
                        ProcessingTimeMs = 45 + (i % 20),
                        ResultImagePath = "bench_wrist.png",
                        Detections = new List<Detection>
                        {
                            new() { Id = Guid.NewGuid(), ClassName = "fracture", Confidence = 0.92m, X = 10, Y = 10, Width = 30, Height = 30 }
                        }
                    }
                }
            };
            db.Analyses.Add(analysis);
        }

        db.SaveChanges();
    }

    [Fact]
    public async Task Benchmark_HistoryPaginationQuery_MeasuresResponseLatency()
    {
        // Warmup
        await _client.GetAsync("/api/history?page=1&pageSize=10");

        var sw = Stopwatch.StartNew();
        const int iterations = 10;
        for (int i = 0; i < iterations; i++)
        {
            var res = await _client.GetAsync($"/api/history?page={(i % 5) + 1}&pageSize=10");
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        }
        sw.Stop();

        var avgMs = (double)sw.ElapsedMilliseconds / iterations;
        _output.WriteLine($"[PERF] History List & Pagination avg response time: {avgMs:F2} ms over {iterations} iterations");
        Assert.True(avgMs < 100, $"History pagination query avg time {avgMs}ms exceeded 100ms threshold.");
    }

    [Fact]
    public async Task Benchmark_AnalysisDetailAndComparisonQuery_MeasuresResponseLatency()
    {
        // Warmup
        await _client.GetAsync($"/api/results/{_benchAnalysisId}/comparison");

        var sw = Stopwatch.StartNew();
        const int iterations = 10;
        for (int i = 0; i < iterations; i++)
        {
            var res = await _client.GetAsync($"/api/results/{_benchAnalysisId}/comparison");
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        }
        sw.Stop();

        var avgMs = (double)sw.ElapsedMilliseconds / iterations;
        _output.WriteLine($"[PERF] Comparison API avg response time: {avgMs:F2} ms over {iterations} iterations");
        Assert.True(avgMs < 50, $"Comparison query avg time {avgMs}ms exceeded 50ms threshold.");
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
