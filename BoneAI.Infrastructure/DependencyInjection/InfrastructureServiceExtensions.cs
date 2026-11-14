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

        // Register Storage, Validation, and Persistence Services
        services.AddScoped<IXRayImageRepository, XRayImageRepository>();
        services.AddSingleton<IImageStorageService, LocalImageStorageService>();
        services.AddSingleton<IImageValidator, ImageValidator>();

        return services;
    }
}
