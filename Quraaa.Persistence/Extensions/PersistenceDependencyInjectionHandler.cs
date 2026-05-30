using Microsoft.Extensions.DependencyInjection;

namespace Quraaa.Persistence.Extensions
{
    public static class PersistenceDependencyInjectionHandler
    {
        public static IServiceCollection AddPersistenceDependencies(this IServiceCollection services)
        {
            var assembly = typeof(PersistenceDependencyInjectionHandler).Assembly;


            return services;
        }
    }
}
