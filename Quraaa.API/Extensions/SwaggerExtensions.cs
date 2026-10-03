using Microsoft.OpenApi;

namespace Quraaa.API.Extensions
{
    public static class SwaggerExtensions
    {
        internal static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services, IConfiguration config)
        {
            services.AddEndpointsApiExplorer();

            // Using NSwag to generate OpenAPI documentation with custom configuration
            services.AddOpenApi("v1", options =>
            {
                options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0;

                // Add custom metadata to the generated OpenAPI document
                options.AddDocumentTransformer((document, context, ct) =>
                {
                    var serverUrl = GetSwaggerServerUrl(config);

                    document.Info = new OpenApiInfo
                    {
                        Title = "Quraaa API",
                        Version = "v1",
                        Description = "Swagger documentation for Quraaa API"
                    };

                    document.Servers = new List<OpenApiServer>
                    {
                        new() { Url = serverUrl }
                    };

                    document.Components ??= new OpenApiComponents();

                    document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

                    var concreteScheme = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.Http,
                        Scheme = "bearer",
                        BearerFormat = "JWT",
                        Description = "JWT Authorization header using the Bearer scheme."
                    };

                    document.Components.SecuritySchemes.Add("Bearer", concreteScheme);

                    var securitySchemeReference = new OpenApiSecuritySchemeReference("Bearer", document);

                    var securityRequirement = new OpenApiSecurityRequirement
                    {
                        { securitySchemeReference, new List<string>() }
                    };

                    document.Security ??= new List<OpenApiSecurityRequirement>();
                    document.Security.Add(securityRequirement);

                    return Task.CompletedTask;
                });
            });

            return services;
        }

        private static string GetSwaggerServerUrl(IConfiguration config)
        {
            // A relative server URL keeps Swagger on the scheme and host that served
            // the UI: HTTP locally and HTTPS behind the production reverse proxy.
            // Never advertise an insecure absolute URL, because a page served over
            // HTTPS cannot call it from a browser.
            var configuredUrl = config["Swagger:ServerUrl"]?.Trim();

            if (string.IsNullOrWhiteSpace(configuredUrl))
            {
                return "/";
            }

            if (configuredUrl.StartsWith("/", StringComparison.Ordinal))
            {
                return configuredUrl == "/" ? "/" : configuredUrl.TrimEnd('/');
            }

            return Uri.TryCreate(configuredUrl, UriKind.Absolute, out var configuredUri) &&
                   string.Equals(configuredUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
                ? configuredUrl.TrimEnd('/')
                : "/";
        }

        // MIDDLEWARE
        internal static WebApplication UseSwaggerDashboard(this WebApplication app)
        {
            // Generate the OpenAPI document at runtime and serve it at the specified endpoint
            app.MapOpenApi();

            // Serve the Swagger UI
            app.UseSwaggerUI(options =>
            {
                // Integrate the generated OpenAPI document into the Swagger UI
                options.SwaggerEndpoint("/openapi/v1.json", "Quraaa API v1");
                options.RoutePrefix = "docs"; // Access the Swagger UI at /docs
            });

            return app;
        }
    }
}
