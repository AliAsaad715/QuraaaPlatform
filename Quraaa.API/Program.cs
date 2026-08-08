using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Quraaa.API.Extensions;
using Quraaa.Application.Features.AiAssistant.Interfaces;
using Quraaa.Infrastructure.Extensions;
using Quraaa.Infrastructure.Services;
using Quraaa.Persistence.Data;
using Quraaa.Persistence.Seed;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;

DotNetEnv.Env.Load();

var apiEnvPath = Path.Combine(Directory.GetCurrentDirectory(), "Quraaa.API", ".env");
if (File.Exists(apiEnvPath))
{
    DotNetEnv.Env.Load(apiEnvPath);
}

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

CreateFirebaseCredentialsFile(builder.Environment.ContentRootPath);

// Add Controllers with JSON serialization options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.Configure<RouteOptions>(options =>
{
    options.LowercaseUrls = true;
});

builder.Services.AddDatabaseConfiguration(builder.Configuration);
builder.Services.AddApplicationServices(
    builder.Configuration,
    builder.Environment.IsDevelopment());
builder.Services.AddInfrastructureDependencies(builder.Configuration, builder.Environment.IsDevelopment());
builder.Services.AddSwaggerConfiguration(builder.Configuration);

// CORS: origins are configurable via Cors:AllowedOrigins (see appsettings/.env)
// so production can be locked down to real frontend origins once they're known.
// With no allow-list configured, fall back to allowing any origin — Bearer-token
// auth travels in the Authorization header, not cookies, so AllowAnyOrigin() here
// never needs (and must never be combined with) AllowCredentials().
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy("Default", policy =>
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
});

var app = builder.Build();

// Configure the HTTP request pipeline.
// UseForwardedHeaders must run first so Request.Scheme/Host already reflect the
// original client request (from X-Forwarded-Proto/Host) before anything below —
// HTTPS redirection, CORS origin checks, and Swagger's server URL — reads them.
app.UseForwardedHeaders();
app.UseSwaggerDashboard();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// UseCors must sit between UseRouting and UseAuthentication/UseAuthorization:
// after routing so it can see endpoint-level CORS metadata, and before auth so
// unauthenticated CORS preflight (OPTIONS) requests aren't rejected before the
// CORS headers are ever added to the response.
app.UseCors(ServiceCollectionExtensions.LibraryDashboardCorsPolicy);
app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();
app.MapControllers();

// Seed the database with categories
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
    await CategorySeeder.SeedAsync(db);
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<ApplicationUser>>();
    await AdminSeeder.SeedAsync(db, userManager, roleManager, passwordHasher, builder.Configuration);
    await UserSeeder.SeedAsync(db, userManager, roleManager, passwordHasher, builder.Configuration);
    await LibrarySeeder.SeedAsync(db);
    await UserSeeder.EnsureApprovedLibraryOwnerRolesAsync(db, userManager, roleManager, CancellationToken.None);
    await EbookSeeder.SeedAsync(db);
    await BookSeeder.SeedAsync(db);
}

app.Run();

static void CreateFirebaseCredentialsFile(string contentRootPath)
{
    var firebaseJson = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIALS_JSON");

    if (string.IsNullOrWhiteSpace(firebaseJson))
    {
        return;
    }

    try
    {
        using var jsonDoc = System.Text.Json.JsonDocument.Parse(firebaseJson);
    }
    catch (Exception ex)
    {
        throw new InvalidOperationException("Invalid FIREBASE_CREDENTIALS_JSON in environment variables.", ex);
    }

    var firebaseDir = Path.Combine(contentRootPath, "storage", "firebase");
    var firebasePath = Path.Combine(firebaseDir, "quraa.json");

    Directory.CreateDirectory(firebaseDir);
    File.WriteAllText(firebasePath, firebaseJson);

    Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", firebasePath);
    Environment.SetEnvironmentVariable("FIREBASE_CREDENTIALS", firebasePath);
}
