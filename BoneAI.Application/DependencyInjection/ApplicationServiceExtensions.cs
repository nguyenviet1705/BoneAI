using BoneAI.Application.Interfaces;
using BoneAI.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BoneAI.Application.DependencyInjection;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IImageService, ImageService>();
        services.AddScoped<IAnalysisService, AnalysisService>();
        services.AddScoped<IModelManagementService, ModelManagementService>();
        return services;
    }
}
