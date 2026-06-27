using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Quraaa.API.Extensions;
using Quraaa.Infrastructure.Extensions;
using Quraaa.Persistence.Data;
using Quraaa.Persistence.Seed;

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

builder.Services.AddDatabaseConfiguration(builder.Configuration);
builder.Services.AddApplicationServices(builder.Configuration);
builder.Services.AddInfrastructureDependencies(builder.Configuration, builder.Environment.IsDevelopment());
builder.Services.AddSwaggerConfiguration(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseForwardedHeaders();
app.UseSwaggerDashboard();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
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
