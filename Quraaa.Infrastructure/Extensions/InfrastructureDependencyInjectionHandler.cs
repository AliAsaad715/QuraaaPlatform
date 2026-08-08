using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Quraaa.Application.Features.AiAssistant.Interfaces;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Features.Carts.Interfaces;
using Quraaa.Application.Features.Listings.Interfaces;
using Quraaa.Application.Features.Notifications.Interfaces;
using Quraaa.Application.Features.Otp.Interfaces;
using Quraaa.Application.Features.Payments.Interfaces;
using Quraaa.Application.Shared.Files;
using Quraaa.Infrastructure.Services;
using StackExchange.Redis;
using Stripe;
using System.Net;
using System.Net.Http.Headers;

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

            services.AddOptions<StripeOptions>()
                .Bind(configuration.GetSection("Stripe"))
                .Validate(
                    options => !string.IsNullOrWhiteSpace(options.SecretKey),
                    "Stripe:SecretKey is required for order payments.")
                .Validate(
                    options =>
                    {
                        var expectedKeyPrefix =
                            options.IsTestMode ? "sk_test_" : "sk_live_";

                        return !string.IsNullOrWhiteSpace(options.SecretKey)
                            && options.SecretKey.Trim().StartsWith(
                                expectedKeyPrefix,
                                StringComparison.Ordinal);
                    },
                    "Stripe:SecretKey must match the configured Stripe payment mode.")
                .Validate(
                    options =>
                        !string.IsNullOrWhiteSpace(options.WebhookSecret)
                        && options.WebhookSecret.Trim().StartsWith(
                            "whsec_",
                            StringComparison.Ordinal),
                    "Stripe:WebhookSecret must be configured with a whsec_ signing secret.")
                .Validate(
                    options => string.Equals(
                        options.Currency?.Trim(),
                        "usd",
                        StringComparison.OrdinalIgnoreCase),
                    "Order payments currently support Stripe:Currency=usd only.")
                .ValidateOnStart();

            services.AddSingleton(serviceProvider =>
            {
                var stripeOptions = serviceProvider
                    .GetRequiredService<IOptions<StripeOptions>>()
                    .Value;

                return new StripeClient(stripeOptions.SecretKey.Trim());
            });
            services.AddScoped<StripePaymentService>();
            services.AddScoped<IPaymentGateway>(
                serviceProvider => serviceProvider.GetRequiredService<StripePaymentService>());
            services.AddScoped<IStripePaymentService>(
                serviceProvider => serviceProvider.GetRequiredService<StripePaymentService>());

            services.AddScoped<IOtpCacheService, OtpCacheService>();
            services.AddScoped<IAccessTokenRevocationService, AccessTokenRevocationService>();
            services.AddScoped<IFirebaseSmsGateway, FirebaseSmsGateway>();
            services.AddScoped<IFirebaseNotificationService, FirebaseNotificationService>();
            services.AddMemoryCache();
            services.AddHttpClient<IBookMetadataService, GoogleBooksService>(client =>
            {
                client.BaseAddress = new Uri(configuration["GoogleBooks:BaseUrl"] ?? "https://www.googleapis.com/");
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.Add("User-Agent", "QuraaaPlatformApp/1.0");
            });
            services.AddHttpClient<IOpenAiService, OpenAiService>((sp, client) =>
            {
                var config = sp.GetRequiredService<IConfiguration>();
                var apiKey = config["OpenAi:ApiKey"];

                client.BaseAddress = new Uri("https://generativelanguage.googleapis.com/v1beta/openai/");

                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                }
            })
            .AddResilienceHandler("openai-rate-limit-retry", builder =>
            {
                // Google AI Studio's free tier caps at 15 requests/minute, so a 429 is
                // expected under normal traffic, not exceptional — retry with
                // exponential backoff (2s, 4s, 8s) before giving up. Transient 5xx and
                // connection failures get the same treatment.
                builder.AddRetry(new RetryStrategyOptions<HttpResponseMessage>
                {
                    ShouldHandle = new PredicateBuilder<HttpResponseMessage>()
                        .Handle<HttpRequestException>()
                        .HandleResult(response => response.StatusCode == HttpStatusCode.TooManyRequests)
                        .HandleResult(response => (int)response.StatusCode >= 500),
                    MaxRetryAttempts = 3,
                    BackoffType = DelayBackoffType.Exponential,
                    Delay = TimeSpan.FromSeconds(2),
                    UseJitter = true,
                    // Prefer the server's own Retry-After when it sends one; returning
                    // null falls back to the exponential delay computed above.
                    DelayGenerator = args =>
                        ValueTask.FromResult(args.Outcome.Result?.Headers.RetryAfter?.Delta)
                });
            });
            services.AddSingleton<IAiUsageLimiterService, AiUsageLimiterService>();

            // Scoped, not Singleton: it depends on IFileStorageService, which is
            // itself registered Scoped (see ServiceCollectionExtensions).
            services.AddScoped<IDocumentTextExtractionService, PdfTextExtractionService>();
            services.AddScoped<IDocxTextExtractionService, DocxTextExtractionService>();
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
