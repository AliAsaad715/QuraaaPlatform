using Microsoft.AspNetCore.HttpOverrides;
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
                    var serverUrl = config["Swagger:ServerUrl"];

                    document.Info = new OpenApiInfo
                    {
                        Title = "Quraaa API",
                        Version = "v1",
                        Description = "Swagger documentation for Quraaa API"
                    };

                    if (!string.IsNullOrWhiteSpace(serverUrl))
                    {
                        document.Servers = new List<OpenApiServer>
                        {
                            new() { Url = serverUrl.TrimEnd('/') }
                        };
                    }

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

            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor |
                    ForwardedHeaders.XForwardedProto |
                    ForwardedHeaders.XForwardedHost;
                options.ForwardLimit = 1;

                options.KnownIPNetworks.Clear();
                options.KnownProxies.Clear();
            });

            return services;
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