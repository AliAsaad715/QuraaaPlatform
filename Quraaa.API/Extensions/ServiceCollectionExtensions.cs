using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Quraaa.API.Services;
using Quraaa.Application.Features.Authentication.Common;
using Quraaa.Application.Features.Authentication.Interfaces;
using Quraaa.Application.Features.Books.Interfaces;
using Quraaa.Application.Features.Libraries.Interfaces;
using Quraaa.Application.Features.Libraries.Common;
using Quraaa.Application.Features.Listings.Interfaces;
using Quraaa.Application.Features.Orders.Common;
using Quraaa.Application.Features.Payouts.Common;
using Quraaa.Application.Shared.Files;
using System.IdentityModel.Tokens.Jwt;
using System.Globalization;
using System.Net;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

namespace Quraaa.API.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public const string DefaultCorsPolicy = "Default";
        public const string LibraryDashboardCorsPolicy = "library-dashboard";
        public const string LibraryRegistrationLinkRateLimitPolicy = "library-registration-link";
        public const string LibraryRegistrationPublicRateLimitPolicy = "library-registration-public";
        public const string LibraryWalletRateLimitPolicy = "library-wallet";
        public const string LibraryPasswordResetRateLimitPolicy = "library-password-reset";
        public const string LibraryWalletSyncRateLimitPolicy = "library-wallet-sync";

        /// <summary>
        /// Registers what the HTTP host itself owns: MVC, authentication, rate
        /// limiting, CORS, forwarded headers, host-bound options, file delivery,
        /// background workers and the OpenAPI document. Application, Persistence
        /// and Infrastructure register themselves (see Program.cs).
        /// </summary>
        public static IServiceCollection AddApi(
            this IServiceCollection services,
            IConfiguration configuration,
            IHostEnvironment environment)
        {
            var libraryRegistrationOptions = CreateLibraryRegistrationOptions(
                configuration,
                environment.IsDevelopment());

            services.AddApiControllers();
            services.AddApiAuthentication(configuration);
            services.AddApiRateLimiting();
            services.AddApiCors(configuration, libraryRegistrationOptions);
            services.AddApiForwardedHeaders(configuration);
            services.AddApiOptions(configuration, libraryRegistrationOptions);
            services.AddApiFileServices();
            services.AddWorkers();
            services.AddSwaggerConfiguration(configuration);

            return services;
        }

        private static void AddApiControllers(this IServiceCollection services)
        {
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                });

            services.Configure<RouteOptions>(options =>
            {
                options.LowercaseUrls = true;
            });
        }

        private static void AddApiCors(
            this IServiceCollection services,
            IConfiguration configuration,
            LibraryRegistrationOptions libraryRegistrationOptions)
        {
            // Origins are configurable via Cors:AllowedOrigins (see appsettings/.env)
            // so production can be locked down to real frontend origins once they're known.
            // With no allow-list configured, fall back to allowing any origin — Bearer-token
            // auth travels in the Authorization header, not cookies, so AllowAnyOrigin() here
            // never needs (and must never be combined with) AllowCredentials().
            // ReadAllowedOrigins also feeds the provider redirect allow-list, so the
            // same setting cannot mean two different things.
            var allowedOrigins = ReadAllowedOrigins(configuration);

            services.AddCors(options =>
            {
                options.AddPolicy(DefaultCorsPolicy, policy =>
                {
                    if (allowedOrigins.Length > 0)
                    {
                        policy.WithOrigins(allowedOrigins)
                              .AllowAnyMethod()
                              .AllowAnyHeader();
                    }
                    else
                    {
                        policy.AllowAnyOrigin()
                              .AllowAnyMethod()
                              .AllowAnyHeader();
                    }
                });

                options.AddPolicy(LibraryDashboardCorsPolicy, policy =>
                {
                    policy
                        .WithOrigins(libraryRegistrationOptions.DashboardRegisterUrl.GetLeftPart(UriPartial.Authority))
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                });
            });
        }

        private static void AddApiForwardedHeaders(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor |
                    ForwardedHeaders.XForwardedProto |
                    ForwardedHeaders.XForwardedHost;
                options.ForwardLimit = 1;

                var configuredProxies = configuration
                    .GetSection("ForwardedHeaders:KnownProxies")
                    .Get<string[]>() ?? Array.Empty<string>();
                var configuredNetworks = configuration
                    .GetSection("ForwardedHeaders:KnownNetworks")
                    .Get<string[]>() ?? Array.Empty<string>();

                if (configuredProxies.Length > 0 || configuredNetworks.Length > 0)
                {
                    options.KnownIPNetworks.Clear();
                    options.KnownProxies.Clear();
                }

                foreach (var configuredProxy in configuredProxies)
                {
                    if (!IPAddress.TryParse(configuredProxy, out var proxyAddress))
                    {
                        throw new InvalidOperationException(
                            $"Invalid forwarded-header proxy address: '{configuredProxy}'.");
                    }

                    options.KnownProxies.Add(proxyAddress);
                }

                foreach (var configuredNetwork in configuredNetworks)
                {
                    if (!System.Net.IPNetwork.TryParse(configuredNetwork, out var network))
                    {
                        throw new InvalidOperationException(
                            $"Invalid forwarded-header proxy network: '{configuredNetwork}'.");
                    }

                    options.KnownIPNetworks.Add(network);
                }
            });
        }

        private static void AddApiOptions(
            this IServiceCollection services,
            IConfiguration configuration,
            LibraryRegistrationOptions libraryRegistrationOptions)
        {
            services.Configure<FileStorageOptions>(configuration.GetSection("Storage"));
            services.Configure<FileRetentionOptions>(configuration.GetSection("Storage:FileRetention"));
            services.AddSingleton(libraryRegistrationOptions);
            services.AddSingleton(CreateCheckoutRedirectOptions(configuration));
            services.AddOptions<PayoutOptions>()
                .Bind(configuration.GetSection("Payouts"))
                .Validate(
                    options => options.MaxTransferAttempts is >= 1 and <= 100,
                    "Payouts:MaxTransferAttempts must be between 1 and 100.")
                .ValidateOnStart();
        }

        private static void AddApiFileServices(this IServiceCollection services)
        {
            services.AddScoped<IFileAccessService, FileAccessService>();
            services.AddHttpClient("PrivateAssetDelivery", client =>
            {
                // Large ebooks are streamed until the caller disconnects. RequestAborted
                // remains the timeout/cancellation authority for this proxy client.
                client.Timeout = Timeout.InfiniteTimeSpan;
            });
            services.AddLogging(logging => logging.AddFilter(
                "System.Net.Http.HttpClient.PrivateAssetDelivery",
                LogLevel.None));
            services.AddScoped<ILibraryImageStorageService, LibraryImageStorageService>();
            services.AddScoped<ILibraryBookStorageService, LibraryBookStorageService>();
            services.AddScoped<IBulkBookStorageService, BulkBookStorageService>();
            services.AddScoped<IListingImageStorageService, ListingImageStorageService>();
        }

        private static void AddWorkers(this IServiceCollection services)
        {
            services.AddHostedService<ExpiredOrderPaymentReconciliationService>();
            services.AddHostedService<FileRetentionCleanupService>();
            services.AddHostedService<SellerPayoutProcessingService>();
            services.AddHostedService<BookModerationNotificationDeliveryService>();
            services.AddHostedService<LibraryApprovalNotificationDeliveryService>();
            services.AddHostedService<ListingPushNotificationDeliveryService>();
        }

        private static void AddApiRateLimiting(this IServiceCollection services)
        {
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
                options.AddPolicy("regular-login", httpContext =>
                {
                    var clientAddress = httpContext.Connection.RemoteIpAddress?.ToString()
                        ?? "unknown-client";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: clientAddress,
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 30,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                            AutoReplenishment = true
                        });
                });

                options.AddPolicy(
                    LibraryRegistrationLinkRateLimitPolicy,
                    PerUserOrClientFixedWindow(permitLimit: 5, TimeSpan.FromMinutes(10)));

                // Every wallet PUT performs a live Stripe Account retrieve, so
                // keep it from becoming an account-probing / quota-burning
                // oracle.
                options.AddPolicy(
                    LibraryWalletRateLimitPolicy,
                    PerUserOrClientFixedWindow(permitLimit: 5, TimeSpan.FromMinutes(10)));

                // Anonymous and email-triggering: throttle per client so it
                // cannot be used to spray reset mail or probe addresses.
                options.AddPolicy(
                    LibraryPasswordResetRateLimitPolicy,
                    PerUserOrClientFixedWindow(permitLimit: 10, TimeSpan.FromMinutes(10)));

                // Wallet status sync also reads the account at Stripe, but a
                // normal start -> return -> refresh cycle legitimately calls it
                // several times, so it gets its own, roomier bucket.
                options.AddPolicy(
                    LibraryWalletSyncRateLimitPolicy,
                    PerUserOrClientFixedWindow(permitLimit: 20, TimeSpan.FromMinutes(10)));

                options.AddPolicy(LibraryRegistrationPublicRateLimitPolicy, httpContext =>
                {
                    var clientAddress = httpContext.Connection.RemoteIpAddress?.ToString()
                        ?? "unknown-client";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: clientAddress,
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 30,
                            Window = TimeSpan.FromMinutes(1),
                            QueueLimit = 0,
                            AutoReplenishment = true
                        });
                });

            });
        }

        /// <summary>
        /// A fixed window partitioned by the authenticated user, falling back to
        /// the client address for anonymous callers.
        /// </summary>
        private static Func<HttpContext, RateLimitPartition<string>> PerUserOrClientFixedWindow(
            int permitLimit,
            TimeSpan window)
        {
            return httpContext =>
            {
                var userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var clientAddress = httpContext.Connection.RemoteIpAddress?.ToString()
                    ?? "unknown-client";

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: userId ?? clientAddress,
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = permitLimit,
                        Window = window,
                        QueueLimit = 0,
                        AutoReplenishment = true
                    });
            };
        }

        /// <summary>
        /// The configured frontend origins, normalised to scheme://host[:port].
        /// Shared by the CORS policy and the provider redirect allow-list so the
        /// same setting cannot mean two different things.
        /// </summary>
        private static string[] ReadAllowedOrigins(IConfiguration configuration)
        {
            return (configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [])
                .Select(origin => origin?.Trim())
                .Where(origin => !string.IsNullOrWhiteSpace(origin))
                .Select(origin => Uri.TryCreate(origin, UriKind.Absolute, out var originUri)
                    ? originUri.GetLeftPart(UriPartial.Authority)
                    : null)
                .OfType<string>()
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        /// <summary>
        /// Bound from the "Checkout" section. An unset app scheme simply
        /// disables the mobile hand-off, leaving a plain confirmation page.
        /// </summary>
        private static CheckoutRedirectOptions CreateCheckoutRedirectOptions(
            IConfiguration configuration)
        {
            var options = new CheckoutRedirectOptions();
            configuration.GetSection("Checkout").Bind(options);

            var publicBaseUrl = options.PublicBaseUrl?.Trim() ?? string.Empty;

            if (!string.IsNullOrEmpty(publicBaseUrl)
                && !Uri.TryCreate(publicBaseUrl, UriKind.Absolute, out _))
            {
                throw new InvalidOperationException(
                    "Checkout:PublicBaseUrl must be an absolute URL when configured.");
            }

            options.PublicBaseUrl = publicBaseUrl;

            // A scheme with "://" or a slash in it would produce a broken deep
            // link that fails silently on the device.
            var scheme = options.MobileAppScheme?.Trim() ?? string.Empty;

            if (scheme.Contains("://", StringComparison.Ordinal) || scheme.Contains('/'))
            {
                throw new InvalidOperationException(
                    "Checkout:MobileAppScheme must be the bare scheme, for example \"quraaa\".");
            }

            options.MobileAppScheme = scheme;

            return options;
        }

        private static LibraryRegistrationOptions CreateLibraryRegistrationOptions(
            IConfiguration configuration,
            bool isDevelopment)
        {
            var configuredUrl = configuration["LIBRARY_DASHBOARD_REGISTER_URL"]?.Trim();
            if (!Uri.TryCreate(configuredUrl, UriKind.Absolute, out var dashboardRegisterUrl)
                || (dashboardRegisterUrl.Scheme != Uri.UriSchemeHttps
                    && !(isDevelopment
                        && dashboardRegisterUrl.Scheme == Uri.UriSchemeHttp
                        && dashboardRegisterUrl.IsLoopback))
                || !string.IsNullOrEmpty(dashboardRegisterUrl.Query)
                || !string.IsNullOrEmpty(dashboardRegisterUrl.Fragment)
                || !string.IsNullOrEmpty(dashboardRegisterUrl.UserInfo))
            {
                throw new InvalidOperationException(
                    "LIBRARY_DASHBOARD_REGISTER_URL must be an absolute HTTPS URL without embedded credentials, a query string, or a fragment. Development may use HTTP only for a loopback URL.");
            }

            // Stripe-hosted onboarding may redirect owners back to the
            // dashboard origin or to any explicitly configured frontend origin.
            var allowedReturnOrigins = ReadAllowedOrigins(configuration)
                .Append(dashboardRegisterUrl.GetLeftPart(UriPartial.Authority))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return new LibraryRegistrationOptions
            {
                DashboardRegisterUrl = dashboardRegisterUrl,
                AllowedReturnOrigins = allowedReturnOrigins
            };
        }

        private static void AddApiAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            var secretKey = configuration["JWT_SECRET_KEY"];
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                throw new InvalidOperationException("JWT Secret Key is missing.");
            }

            var issuer = configuration["JWT_ISSUER"];
            var audience = configuration["JWT_AUDIENCE"];
            var durationValue = configuration["JWT_DURATION_IN_MINUTES"] ?? "60";
            if (!double.TryParse(
                    durationValue,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var durationInMinutes)
                || !double.IsFinite(durationInMinutes)
                || durationInMinutes <= 0
                || durationInMinutes > TimeSpan.FromDays(7).TotalMinutes)
            {
                throw new InvalidOperationException(
                    "JWT_DURATION_IN_MINUTES must be an invariant positive number no greater than 10080.");
            }

            services.AddSingleton(new AuthenticationTokenOptions
            {
                AccessTokenDurationInMinutes = durationInMinutes
            });

            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                        ValidateIssuer = !string.IsNullOrWhiteSpace(issuer),
                        ValidIssuer = issuer,
                        ValidateAudience = !string.IsNullOrWhiteSpace(audience),
                        ValidAudience = audience,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero,
                        NameClaimType = ClaimTypes.NameIdentifier
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnTokenValidated = async context =>
                        {
                            var tokenId = context.Principal?
                                .FindFirstValue(JwtRegisteredClaimNames.Jti);

                            if (string.IsNullOrWhiteSpace(tokenId))
                            {
                                context.Fail("Access token does not contain a token identifier.");
                                return;
                            }

                            var revocationService = context.HttpContext.RequestServices
                                .GetRequiredService<IAccessTokenRevocationService>();

                            if (await revocationService.IsRevokedAsync(
                                    tokenId,
                                    context.HttpContext.RequestAborted))
                            {
                                context.Fail("Access token has been revoked.");
                                return;
                            }

                            var userIdValue = context.Principal?
                                .FindFirstValue(ClaimTypes.NameIdentifier);
                            var familyIdValue = context.Principal?
                                .FindFirstValue(AuthenticationClaimNames.SessionId)
                                ?? context.Principal?.FindFirstValue(ClaimTypes.Sid);

                            if (!Guid.TryParse(userIdValue, out var userId)
                                || !Guid.TryParse(familyIdValue, out var familyId))
                            {
                                context.Fail("Access token does not contain a valid session identifier.");
                                return;
                            }

                            var identityService = context.HttpContext.RequestServices
                                .GetRequiredService<IIdentityService>();

                            if (!await identityService.IsRefreshTokenFamilyActiveAsync(
                                    userId,
                                    familyId,
                                    context.HttpContext.RequestAborted))
                            {
                                context.Fail("Access-token session has been revoked or replaced.");
                            }
                        }
                    };
                });

            services.AddAuthorization();
        }
    }
}
