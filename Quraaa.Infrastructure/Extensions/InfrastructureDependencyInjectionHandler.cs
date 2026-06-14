using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quraaa.Application.Features.Otp.Interfaces;
using Quraaa.Infrastructure.Services;

namespace Quraaa.Infrastructure.Extensions
{
    public static class InfrastructureDependencyInjectionHandler
    {
        public static IServiceCollection AddInfrastructureDependencies(
            this IServiceCollection services,
            IConfiguration configuration,
            bool isDevelopment)
        {
            FirebaseExtensions.AddFirebaseConfiguration(services, configuration);
            AddOtpCache(services, configuration, isDevelopment);

            services.AddScoped<IOtpCacheService, OtpCacheService>();
            services.AddScoped<IFirebaseSmsGateway, FirebaseSmsGateway>();

            return services;
        }

        private static void AddOtpCache(IServiceCollection services, IConfiguration configuration, bool isDevelopment)
        {
            var allowInMemoryCacheInProduction = configuration.GetValue<bool>("Otp:AllowInMemoryCacheInProduction");

            if (!isDevelopment && !allowInMemoryCacheInProduction)
            {
                throw new InvalidOperationException(
                    "OTP in-memory cache is only enabled for development. Configure a durable distributed cache before production.");
            }

            services.AddDistributedMemoryCache();
        }
    }
}
