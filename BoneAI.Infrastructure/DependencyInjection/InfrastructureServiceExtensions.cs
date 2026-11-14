using BoneAI.Application.Interfaces;
using BoneAI.Infrastructure.Configuration;
using BoneAI.Infrastructure.Persistence;
using BoneAI.Infrastructure.Repositories;
using BoneAI.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BoneAI.Infrastructure.DependencyInjection;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<BoneAIDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.MigrationsAssembly(typeof(BoneAIDbContext).Assembly.FullName);
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null);
            }));

        // Configure Image Storage Options
        services.Configure<ImageStorageOptions>(configuration.GetSection(ImageStorageOptions.SectionName));

        // Configure AI Service Options
        services.Configure<AIServiceOptions>(configuration.GetSection(AIServiceOptions.SectionName));

        // Register Typed HttpClient for AI Service Client
        services.AddHttpClient<IAIServiceClient, AIServiceClient>();

        // Register Repositories and Services
        services.AddScoped<IXRayImageRepository, XRayImageRepository>();
        services.AddScoped<IAnalysisRepository, AnalysisRepository>();
        services.AddScoped<IAnalysisRunRepository, AnalysisRunRepository>();
        services.AddScoped<IAIModelRepository, AIModelRepository>();
        services.AddSingleton<IImageStorageService, LocalImageStorageService>();
        services.AddSingleton<IImageValidator, ImageValidator>();

        // Register Durable Background Worker
        services.AddHostedService<BoneAI.Infrastructure.BackgroundServices.AnalysisBackgroundWorker>();

        return services;
    }
}
