using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Features.Notifications.Interfaces;
using Quraaa.Application.Features.Otp.Interfaces;
using Quraaa.Infrastructure.Services;
using StackExchange.Redis;

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
            services.AddScoped<IFirebaseNotificationService, FirebaseNotificationService>();
            //services.AddHttpClient<IBookMetadataService, GoogleBooksService>(client =>
            //{
            //    client.BaseAddress = new Uri(configuration["GoogleBooks__BaseUrl"] ?? "https://www.googleapis.com/");
            //    client.Timeout = TimeSpan.FromSeconds(10);
            //});

            return services;
        }

        private static void AddOtpCache(IServiceCollection services, IConfiguration configuration, bool isDevelopment)
        {
            var redisConnection = GetRedisConnection(configuration);

            if (!string.IsNullOrWhiteSpace(redisConnection))
            {
                services.AddSingleton<IConnectionMultiplexer>(_ =>
                    ConnectionMultiplexer.Connect(CreateRedisConfiguration(redisConnection)));

                services.AddStackExchangeRedisCache(options =>
                {
                    options.ConfigurationOptions = CreateRedisConfiguration(redisConnection);
                    options.InstanceName = configuration["Redis:InstanceName"] ?? "Quraaa:Otp:";
                });

                return;
            }

            var allowInMemoryCacheInProduction = configuration.GetValue<bool>("Otp:AllowInMemoryCacheInProduction");

            if (!isDevelopment && !allowInMemoryCacheInProduction)
            {
                throw new InvalidOperationException(
                    "OTP cache requires Redis in production. Configure REDIS_URL, REDIS_TLS_URL, Redis:ConnectionString, or ConnectionStrings:Redis.");
            }

            services.AddDistributedMemoryCache();
        }

        private static string? GetRedisConnection(IConfiguration configuration)
        {
            return configuration.GetConnectionString("Redis")
                ?? configuration["Redis:ConnectionString"]
                ?? configuration["REDIS_URL"]
                ?? configuration["REDIS_TLS_URL"]
                ?? Environment.GetEnvironmentVariable("REDIS_URL")
                ?? Environment.GetEnvironmentVariable("REDIS_TLS_URL");
        }

        private static ConfigurationOptions CreateRedisConfiguration(string redisConnection)
        {
            if (!Uri.TryCreate(redisConnection, UriKind.Absolute, out var uri)
                || (uri.Scheme != "redis" && uri.Scheme != "rediss"))
            {
                var parsedOptions = ConfigurationOptions.Parse(redisConnection);
                parsedOptions.AbortOnConnectFail = false;
                return parsedOptions;
            }

            var options = new ConfigurationOptions
            {
                AbortOnConnectFail = false,
                Ssl = uri.Scheme == "rediss"
            };

            options.EndPoints.Add(uri.Host, uri.Port > 0 ? uri.Port : options.Ssl ? 6380 : 6379);

            if (!string.IsNullOrWhiteSpace(uri.UserInfo))
            {
                var userInfo = uri.UserInfo.Split(':', 2);
                options.User = userInfo.Length == 2 ? Uri.UnescapeDataString(userInfo[0]) : null;
                options.Password = Uri.UnescapeDataString(userInfo.Length == 2 ? userInfo[1] : userInfo[0]);
            }

            return options;
        }
    }
}
