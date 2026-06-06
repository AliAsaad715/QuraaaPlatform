using Microsoft.OpenApi;

namespace Quraaa.API.Extensions
{
    public static class SwaggerExtensions
    {
        internal static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services, IConfiguration config)
        {
            services.AddEndpointsApiExplorer();

            // 1. Using NSwag to generate OpenAPI documentation with custom configuration
            services.AddOpenApi("v1", options =>
            {
                options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_0;

                // Integrate JWT Bearer authentication scheme into the OpenAPI documentation
                //options.UseJwtBearerAuthentication();

                // Add custom metadata to the generated OpenAPI document
                options.AddDocumentTransformer((document, context, ct) =>
                {
                    document.Info = new OpenApiInfo
                    {
                        Title = "Quraaa API",
                        Version = "v1",
                        Description = "Swagger documentation for Quraaa API"
                    };
                    return Task.CompletedTask;
                });
            });

            return services;
        }

        // 3. MIDDLEWARE
        internal static WebApplication UseSwaggerDashboard(this WebApplication app)
        {
            // Generate the OpenAPI document at runtime and serve it at the specified endpoint
            app.MapOpenApi();

            // Serve the Swagger UI
            app.UseSwaggerUI(options =>
            {
                // Integrate the generated OpenAPI document into the Swagger UI
                options.SwaggerEndpoint("../openapi/v1.json", "Quraaa API v1");
                options.RoutePrefix = "docs"; // Access the Swagger UI at /docs
            });

            return app;
        }
    }
}
