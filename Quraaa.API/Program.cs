using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using Quraaa.API.Extensions;
using Quraaa.Persistence.Data;
using Quraaa.Infrastructure.Extensions;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

CreateFirebaseCredentialsFile(builder.Environment.ContentRootPath);

builder.Services.AddControllers();
builder.Services.AddDatabaseConfiguration(builder.Configuration);
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddSwaggerConfiguration(builder.Configuration);
builder.Services.AddInfrastructureDependencies(builder.Configuration, builder.Environment.IsDevelopment());
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders =
        ForwardedHeaders.XForwardedFor |
        ForwardedHeaders.XForwardedProto |
        ForwardedHeaders.XForwardedHost;
    options.ForwardLimit = 1;

    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseForwardedHeaders();

app.UseSwaggerDashboard();

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    db.Database.Migrate();
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
