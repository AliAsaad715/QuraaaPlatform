using Microsoft.Extensions.DependencyInjection;

namespace Quraaa.Application.Extensions
{
    public static class ApplicationPackagesRegisterExtensions
    {
        public static IServiceCollection AddApplicationDepenedncies(this IServiceCollection services)
        {
            var assembly = typeof(ApplicationPackagesRegisterExtensions).Assembly;

            return services;            
        }
    }
}
