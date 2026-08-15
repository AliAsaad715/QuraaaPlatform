using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Retry;
using Quraaa.Application.Features.AiAssistant.Interfaces;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Features.Carts.Interfaces;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Features.Listings.Interfaces;
using Quraaa.Application.Features.Notifications.Interfaces;
using Quraaa.Application.Features.Otp.Interfaces;
using Quraaa.Application.Features.Payments.Interfaces;
using Quraaa.Application.Features.Payouts.Interfaces;
using Quraaa.Application.Shared.Files;
using Quraaa.Infrastructure.Services;
using StackExchange.Redis;
using Stripe;
using System.Net;
using System.Net.Http.Headers;
using System.Globalization;
using MimeKit;

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
            AddLibraryEmailServices(services, configuration);
            AddCloudinaryStorage(services, configuration);

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
            services.AddScoped<IPayoutGateway, StripePayoutGateway>();

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
            // registered as scoped by AddCloudinaryStorage below.
            services.AddScoped<IDocumentTextExtractionService, PdfTextExtractionService>();
            services.AddScoped<IDocxTextExtractionService, DocxTextExtractionService>();
            return services;
        }

        private static void AddCloudinaryStorage(
            IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddOptions<CloudinaryOptions>()
                .Configure(options =>
                {
                    options.CloudName = configuration["CLOUDINARY_CLOUD_NAME"]?.Trim()
                        ?? string.Empty;
                    options.ApiKey = configuration["CLOUDINARY_API_KEY"]?.Trim()
                        ?? string.Empty;
                    options.ApiSecret = configuration["CLOUDINARY_API_SECRET"]
                        ?? string.Empty;
                    options.PrivateDownloadUrlLifetimeSeconds = int.TryParse(
                        configuration["CLOUDINARY_PRIVATE_DOWNLOAD_TTL_SECONDS"],
                        NumberStyles.None,
                        CultureInfo.InvariantCulture,
                        out var privateDownloadTtlSeconds)
                            ? privateDownloadTtlSeconds
                            : 300;
                })
                .Validate(
                    options => !string.IsNullOrWhiteSpace(options.CloudName)
                        && !options.CloudName.Any(char.IsWhiteSpace),
                    "CLOUDINARY_CLOUD_NAME is required and must not contain whitespace.")
                .Validate(
                    options => !string.IsNullOrWhiteSpace(options.ApiKey),
                    "CLOUDINARY_API_KEY is required.")
                .Validate(
                    options => !string.IsNullOrWhiteSpace(options.ApiSecret),
                    "CLOUDINARY_API_SECRET is required.")
                .Validate(
                    options => options.PrivateDownloadUrlLifetimeSeconds is >= 60 and <= 900,
                    "CLOUDINARY_PRIVATE_DOWNLOAD_TTL_SECONDS must be between 60 and 900 seconds.")
                .ValidateOnStart();

            services.AddSingleton<IImageStorageService, CloudinaryImageStorageService>();
            services.AddHttpClient<CloudinaryFileStorageService>(client =>
            {
                client.Timeout = TimeSpan.FromMinutes(10);
            });
            // Signed private-download URLs contain a short-lived signature. Suppress
            // HttpClientFactory request-URL logs for this client; service logs omit URLs.
            services.AddLogging(logging => logging.AddFilter(
                "System.Net.Http.HttpClient.CloudinaryFileStorageService",
                LogLevel.None));
            services.AddScoped<IFileStorageService>(serviceProvider =>
                serviceProvider.GetRequiredService<CloudinaryFileStorageService>());
        }

        private static void AddLibraryEmailServices(
            IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddOptions<SmtpOptions>()
                .Configure(options =>
                {
                    options.Mailer = configuration["MAIL_MAILER"]?.Trim() ?? string.Empty;
                    options.Host = configuration["MAIL_HOST"]?.Trim() ?? string.Empty;
                    options.Port = int.TryParse(
                        configuration["MAIL_PORT"],
                        NumberStyles.None,
                        CultureInfo.InvariantCulture,
                        out var port)
                            ? port
                            : 0;
                    options.Username = configuration["MAIL_USERNAME"]?.Trim() ?? string.Empty;
                    options.Password = configuration["MAIL_PASSWORD"] ?? string.Empty;
                    options.Encryption = configuration["MAIL_ENCRYPTION"]?.Trim().ToLowerInvariant()
                        ?? string.Empty;
                    options.FromAddress = configuration["MAIL_FROM_ADDRESS"]?.Trim() ?? string.Empty;
                    options.FromName = configuration["MAIL_FROM_NAME"]?.Trim() ?? string.Empty;
                })
                .Validate(
                    options => string.Equals(
                        options.Mailer,
                        "smtp",
                        StringComparison.OrdinalIgnoreCase),
                    "MAIL_MAILER must be configured as smtp.")
                .Validate(
                    options => !string.IsNullOrWhiteSpace(options.Host),
                    "MAIL_HOST is required.")
                .Validate(
                    options => options.Port is > 0 and <= 65535,
                    "MAIL_PORT must be an integer between 1 and 65535.")
                .Validate(
                    options => !string.IsNullOrWhiteSpace(options.Username),
                    "MAIL_USERNAME is required.")
                .Validate(
                    options => !string.IsNullOrEmpty(options.Password),
                    "MAIL_PASSWORD is required.")
                .Validate(
                    options => options.Encryption is "tls" or "starttls" or "ssl" or "smtps",
                    "MAIL_ENCRYPTION must be tls, starttls, ssl, or smtps.")
                .Validate(
                    options => IsPlainMailboxAddress(options.FromAddress),
                    "MAIL_FROM_ADDRESS must be a valid email address.")
                .Validate(
                    options => !string.IsNullOrWhiteSpace(options.FromName)
                        && !options.FromName.Contains('\r')
                        && !options.FromName.Contains('\n'),
                    "MAIL_FROM_NAME is required and must be a single-line value.")
                .ValidateOnStart();

            services.AddOptions<LibraryEmailOtpOptions>()
                .Configure(options =>
                {
                    options.Pepper = configuration["LIBRARY_EMAIL_OTP_PEPPER"]
                        ?? string.Empty;
                })
                .Validate(
                    options => !string.IsNullOrWhiteSpace(options.Pepper)
                        && options.Pepper.Length >= 32,
                    "LIBRARY_EMAIL_OTP_PEPPER must contain at least 32 characters.")
                .Validate(
                    options => !string.Equals(
                        options.Pepper,
                        configuration["JWT_SECRET_KEY"],
                        StringComparison.Ordinal),
                    "LIBRARY_EMAIL_OTP_PEPPER must be independent from JWT_SECRET_KEY.")
                .ValidateOnStart();

            services.AddTransient<ILibraryEmailSender, SmtpLibraryEmailSender>();
            services.AddSingleton<ILibraryRegistrationTokenService, LibraryRegistrationTokenService>();
            services.AddSingleton<ILibraryEmailOtpProtector, LibraryEmailOtpProtector>();
        }

        private static bool IsPlainMailboxAddress(string value)
        {
            var trimmedValue = value.Trim();
            return MailboxAddress.TryParse(trimmedValue, out var mailboxAddress)
                && string.Equals(
                    mailboxAddress.Address,
                    trimmedValue,
                    StringComparison.OrdinalIgnoreCase);
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
