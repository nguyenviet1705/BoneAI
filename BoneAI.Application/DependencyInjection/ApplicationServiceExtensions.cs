using Microsoft.Extensions.DependencyInjection;

namespace BoneAI.Application.DependencyInjection;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Service registrations for Application layer
        return services;
    }
}
