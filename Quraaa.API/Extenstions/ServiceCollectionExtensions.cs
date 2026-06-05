using Quraaa.Application.Extensions;
using Quraaa.Persistence.Extensions;

namespace Quraaa.API.Extenstions
{
    public static class ServiceCollectionExtensions
    {
        public static void AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            PersistenceDependencyInjectionHandler.AddPersistenceDependencies(services, configuration);
        }
    }
}
