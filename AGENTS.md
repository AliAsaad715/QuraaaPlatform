# QuraaaPlatform Agent Context

This file is written for AI agents, coding assistants, and chatbots that need a fast, accurate working model of this repository. It describes the current codebase as it exists now, not a future intended architecture.

## Project Overview

`QuraaaPlatform` is a layered ASP.NET Core Web API solution targeting **.NET 10.0**. It is the backend for a book marketplace / library platform. The architecture follows a Clean Architecture / vertical-slice hybrid with five projects.

Current implemented business capabilities:

- User registration through `POST /api/Auth/register`.
- User login through `POST /api/Auth/login`.
- Authenticated password reset through `POST /api/Auth/reset-password`.
- Unauthenticated forgot-password OTP send through `POST /api/Auth/forgot-password`.
- Unauthenticated forgot-password OTP verification and password reset through `POST /api/Auth/forgot-password/verify`.
- Authenticated profile retrieval through `GET /api/Profile/me`.
- Authenticated profile update through `PUT /api/Profile/me`.
- Library registration through `POST /api/Library/register`.
- Public library listing through `GET /api/Library`.
- Category management through `GET /api/Categories`, `GET /api/Categories/{categoryId}`, and `POST /api/Categories` (admin-only).
- Standalone OTP send through `POST /api/Otp/send`.
- Standalone OTP verification through `POST /api/Otp/verify`.
- Authenticated push notification dispatch through `POST /api/Notifications/send`.
- Development/test push notification dispatch through `POST /api/Notifications/test`.

Domain aggregates already modeled but only partially exposed via HTTP:

- `UserAggregate` — profile/business data for authenticated users.
- `LibraryAggregate` — library profiles with pending/approved/rejected status.
- `CategoryAggregate` — book interest categories, seeded at startup.
- `BookAggregate` — catalog books.
- `ListingAggregate` — marketplace listings for physical or digital books.

Core technologies:

- ASP.NET Core Web API (.NET 10.0)
- Entity Framework Core with PostgreSQL through Npgsql
- ASP.NET Core Identity with `Guid` keys and roles
- MediatR for commands/handlers
- FluentValidation for request validation
- OneOf for application result unions
- libphonenumber-csharp for international phone validation/formatting
- Firebase Admin SDK for FCM push notifications and OTP SMS gateway data messages
- `IDistributedCache`, with Redis support for production OTP cache and in-memory cache for local/development fallback
- DotNetEnv plus environment variables for runtime secrets/configuration
- OpenAPI 3.0 / Swagger UI in development

## Repository Layout

```text
QuraaaPlatform.slnx
README.md
AGENTS.md
Dockerfile
Procfile
check-branch-name.yml
.github/workflows/check-branch-name.yml

Quraaa.API/
  Program.cs
  Quraaa.API.csproj
  Quraaa.API.http
  appsettings.json
  appsettings.Development.json
  Properties/launchSettings.json
  Controllers/
    ApiClientController.cs
    AuthController.cs
    CategoriesController.cs
    LibraryController.cs
    NotificationsController.cs
    OtpController.cs
    ProfileController.cs
  DesignTime/
    ApplicationDbContextFactory.cs
  Extensions/
    DatabaseExtensions.cs
    ServiceCollectionExtensions.cs
    SwaggerExtensions.cs
  Requests/
    Authentication/
    Files/
    Libraries/
    Notifications/
    Otp/
    Profiles/
  Services/
    LibraryImageStorageService.cs
  storage/firebase/       # Firebase service-account JSON files (ignored by git)
  wwwroot/uploads/libraries/  # Uploaded library images

Quraaa.Application/
  Quraaa.Application.csproj
  Extensions/
    ApplicationPackagesRegisterExtensions.cs
  Features/
    Authentication/
      Commands/Register/
      Commands/Login/
      Commands/ResetPassword/
      Commands/ForgotPassword/
      Commands/ResetForgotPassword/
      Common/
      Interfaces/
      Helpers/
    Categories/
      Commands/CreateCategory/
      Queries/GetAllCategories/
      Queries/GetCategoryById/
      Common/
      Interfaces/
    Libraries/
      Commands/RegisterLibrary/
      Commands/AddPhysicalBook/
      Queries/GetLibraries/
      Common/
      Interfaces/
    Notifications/
      Commands/SendNotification/
      Commands/SendTestNotification/
      Common/
      Interfaces/
    Otp/
      Commands/SendOtp/
      Commands/VerifyOtp/
      Interfaces/
    Profiles/
      Commands/UpdateProfile/
      Queries/GetMyProfile/
      Common/
  Shared/
    Exceptions/
    Files/
    Results/
    Services/

Quraaa.Domain/
  Quraaa.Domain.csproj
  Catalog/
    BookAggregate.cs
  Category/
    CategoryAggregate.cs
  Library/
    LibraryAggregate.cs
    Enums/LibraryApprovalStatus.cs
  Marketplace/
    ListingAggregate.cs
    Enums/BookCondition.cs
    Enums/ListingFormat.cs
    Enums/ListingStatus.cs
    Enums/SellerType.cs
  Shared/
    Entities/
    Errors/
    Exceptions/
  User/
    UserAggregate.cs
    Entities/Interest.cs
    Enums/Gender.cs
    Enums/Role.cs
    ValueObjects/PaymentMethodInfo.cs

Quraaa.Persistence/
  Quraaa.Persistence.csproj
  Configurations/
  Data/
    ApplicationDbContext.cs
    ApplicationUser.cs
  Extensions/
    PersistenceDependencyInjectionHandler.cs
  Migrations/
  Repositories/
  Seed/
  Services/

Quraaa.Infrastructure/
  Quraaa.Infrastructure.csproj
  Extensions/
    FirebaseExtensions.cs
    InfrastructureDependencyInjectionHandler.cs
  Models/
    GoogleBookModels.cs
  Services/
    FirebaseNotificationService.cs
    FirebaseSmsGateway.cs
    GoogleBooksService.cs
    OtpCacheService.cs
```

Ignore generated build output:

```text
**/bin/
**/obj/
```

Do not edit generated files under `bin` or `obj`. Treat EF migrations as generated source: create/update them through EF commands unless the user explicitly asks for a manual migration fix.

## Technology Stack & Key Dependencies

Consolidated NuGet packages by project:

| Package                                             | Version | Project(s)                                 |
| --------------------------------------------------- | ------- | ------------------------------------------ |
| `DotNetEnv`                                         | 3.2.0   | `Quraaa.API`                               |
| `Microsoft.AspNetCore.Authentication.JwtBearer`     | 10.0.8  | `Quraaa.API`                               |
| `Microsoft.AspNetCore.OpenApi`                      | 10.0.2  | `Quraaa.API`                               |
| `Microsoft.EntityFrameworkCore.Design`              | 10.0.8  | `Quraaa.API`                               |
| `Swashbuckle.AspNetCore`                            | 10.2.1  | `Quraaa.API`                               |
| `FluentValidation.DependencyInjectionExtensions`    | 12.1.1  | `Quraaa.Application`                       |
| `libphonenumber-csharp`                             | 9.0.32  | `Quraaa.Application`                       |
| `MediatR`                                           | 14.1.0  | `Quraaa.Application`                       |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | 10.0.8  | `Quraaa.Application`, `Quraaa.Persistence` |
| `Microsoft.Extensions.DependencyInjection`          | 10.0.8  | `Quraaa.Application`, `Quraaa.Persistence` |
| `Microsoft.Extensions.Logging`                      | 10.0.8  | `Quraaa.Application`                       |
| `OneOf`                                             | 3.0.271 | `Quraaa.Application`                       |
| `FirebaseAdmin`                                     | 3.5.0   | `Quraaa.Infrastructure`                    |
| `Microsoft.Extensions.Caching.Abstractions`         | 10.0.9  | `Quraaa.Infrastructure`                    |
| `Microsoft.Extensions.Caching.Memory`               | 10.0.8  | `Quraaa.Infrastructure`                    |
| `Microsoft.Extensions.Caching.StackExchangeRedis`   | 10.0.8  | `Quraaa.Infrastructure`                    |
| `Microsoft.EntityFrameworkCore.SqlServer`           | 10.0.8  | `Quraaa.Persistence`                       |
| `Npgsql.EntityFrameworkCore.PostgreSQL`             | 10.0.2  | `Quraaa.Persistence`                       |

All projects use `ImplicitUsings` and `Nullable` enabled.

## Architecture & Layering Rules

Project references encode this dependency direction:

```text
Quraaa.API -> Quraaa.Application
Quraaa.API -> Quraaa.Persistence
Quraaa.API -> Quraaa.Infrastructure
Quraaa.Persistence -> Quraaa.Application
Quraaa.Persistence -> Quraaa.Domain
Quraaa.Application -> Quraaa.Domain
Quraaa.Infrastructure -> Quraaa.Application
Quraaa.Domain -> no project references
```

Layer responsibilities:

- `Quraaa.Domain`: entities, aggregates, value objects, enums, business invariants. No HTTP, EF Core, PostgreSQL/Npgsql, Identity, Swagger, or external provider SDK logic belongs here.
- `Quraaa.Application`: use cases, commands/queries, handlers, validators, interfaces, DTOs, result types.
- `Quraaa.Persistence`: EF Core `DbContext`, table mapping, migrations, repositories, ASP.NET Identity implementation.
- `Quraaa.Infrastructure`: external provider implementations such as Firebase FCM, Redis caching, and future SMS/payments/files/search integrations.
- `Quraaa.API`: HTTP controllers, startup, middleware, Swagger, environment configuration.

Do not model aggregate-to-aggregate relationships in EF Core mappings. Across aggregate boundaries, keep the domain model and EF configuration to scalar identity references such as `UserId`. If referential integrity is required between aggregate tables, enforce it as a database concern through migrations or database constraints, not through `HasOne<TAggregate>()`, navigation properties, or tracked aggregate relationships.

The user-to-library ownership rule is one-to-one from user profile to library, but it still follows the aggregate boundary rule: `LibraryAggregate` stores only scalar `UserId`; `LibraryConfiguration` uses a unique index on `UserId`; the migration enforces the database unique index; application code checks for an existing library before creating another one.

## Build, Run & Test Commands

### Prerequisites

- .NET 10 SDK
- PostgreSQL server (local or remote)
- Redis (optional; in-memory cache is allowed in Development via configuration)
- Firebase service-account credentials (for FCM features; optional for basic HTTP testing)

### Restore packages

```bash
dotnet restore Quraaa.API/Quraaa.API.csproj
```

Or restore the solution:

```bash
dotnet restore QuraaaPlatform.slnx
```

### Build

```bash
dotnet build Quraaa.API/Quraaa.API.csproj
```

### Run locally

```bash
cd Quraaa.API
dotnet run
```

Development launch URLs:

```text
http://localhost:5153
https://localhost:7260
```

Swagger UI in development:

```text
/docs
/openapi/v1.json
```

### Entity Framework migrations

The design-time factory is at `Quraaa.API/DesignTime/ApplicationDbContextFactory.cs`. Run EF commands from the repository root or the `Quraaa.API` directory.

Add a migration:

```bash
dotnet ef migrations add MigrationName --project Quraaa.Persistence --startup-project Quraaa.API
```

Update the database:

```bash
dotnet ef database update --project Quraaa.Persistence --startup-project Quraaa.API
```

Remove the last migration:

```bash
dotnet ef migrations remove --project Quraaa.Persistence --startup-project Quraaa.API
```

### Docker

Build and run with Docker:

```bash
docker build -t quraaa-api .
docker run -p 8080:8080 --env-file Quraaa.API/.env quraaa-api
```

The Dockerfile uses:

- Build image: `mcr.microsoft.com/dotnet/sdk:10.0`
- Runtime image: `mcr.microsoft.com/dotnet/aspnet:10.0`
- Exposes port `8080`
- Sets `ASPNETCORE_URLS=http://+:8080`
- Publishes with `/p:UseAppHost=false`

### Testing

There are currently **no test projects** in the repository. No MSTest, NUnit, or xUnit references exist. Testing is manual via Swagger/UI or an HTTP client. Adding unit and integration tests is a high-value TODO.

## Configuration & Secrets

Configuration is layered in this order:

1. `DotNetEnv.Env.Load()` at startup loads root `.env` and `Quraaa.API/.env` if present.
2. `builder.Configuration.AddEnvironmentVariables()`
3. `appsettings.json` and `appsettings.{Environment}.json`

### Required / commonly used configuration keys

| Concern       | Keys                                                                                                                                          |
| ------------- | --------------------------------------------------------------------------------------------------------------------------------------------- |
| PostgreSQL    | `ConnectionStrings:DefaultConnection`                                                                                                         |
| JWT           | `JWT_SECRET_KEY` (required), `JWT_ISSUER`, `JWT_AUDIENCE`, `JWT_DURATION_IN_MINUTES`                                                          |
| Admin seed    | `ADMIN_PHONE_NUMBER`, `ADMIN_PASSWORD`                                                                                                        |
| Firebase      | `Firebase:CredentialsPath`, `GOOGLE_APPLICATION_CREDENTIALS`, `FIREBASE_CREDENTIALS_JSON`                                                     |
| OTP cache     | `REDIS_URL`, `REDIS_TLS_URL`, `Redis:ConnectionString`, `ConnectionStrings:Redis`, `Redis:InstanceName`, `Otp:AllowInMemoryCacheInProduction` |
| OTP gateway   | `OTP_DEVICE_TOKEN`                                                                                                                            |
| Notifications | `Notifications:AllowTestEndpoint` / `Notifications__AllowTestEndpoint`                                                                        |
| Swagger       | `Swagger:ServerUrl`                                                                                                                           |

`JWT_SECRET_KEY` is required by `IdentityService.GenerateAuthTokensAsync` and by `ServiceCollectionExtensions.AddJwtAuthentication`. If it is missing, the application throws `InvalidOperationException` at startup.

Firebase Admin credential resolution order:

1. `GOOGLE_APPLICATION_CREDENTIALS` environment variable
2. `Firebase:CredentialsPath` config value
3. Application default credentials

Additionally, `Program.cs` supports `FIREBASE_CREDENTIALS_JSON`: it validates the value as JSON, writes it to `Quraaa.API/storage/firebase/quraa.json`, and sets `GOOGLE_APPLICATION_CREDENTIALS` plus `FIREBASE_CREDENTIALS` to that generated path before Infrastructure initializes Firebase.

OTP cache configuration:

- Redis is preferred when any of `ConnectionStrings:Redis`, `Redis:ConnectionString`, `REDIS_URL`, or `REDIS_TLS_URL` is configured.
- Heroku-style `redis://...` and `rediss://...` URLs are supported.
- If Redis is missing, in-memory cache is allowed only in Development or when `Otp:AllowInMemoryCacheInProduction=true`.
- `Otp:AllowInMemoryCacheInProduction=true` is for temporary testing only; OTPs are lost on dyno/app restart and are not shared across multiple instances.

`OTP_DEVICE_TOKEN` is the FCM registration token for the secondary Android SMS gateway app that has SMS permission. It is server-side configuration and is not accepted in the `POST /api/Otp/send` request body.

Secrets handling:

- `.env` is listed in `.gitignore` and must not be committed.
- Firebase service-account JSON files under `Quraaa.API/storage/firebase/*.json` are `.gitignore`d and must not be committed.
- `appsettings.Development.json` currently contains a plaintext local PostgreSQL password. Rotate it for shared environments.

## Database & Migrations

### DbContext

`Quraaa.Persistence/Data/ApplicationDbContext.cs` inherits `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>` and configures Npgsql PostgreSQL.

DbSets:

- `UsersProfiles` (`UserAggregate`)
- `Libraries` (`LibraryAggregate`)
- `Books` (`BookAggregate`)
- `Listings` (`ListingAggregate`)
- `Categories` (`CategoryAggregate`)

It applies all `IEntityTypeConfiguration` classes from the Persistence assembly and adds a global query filter for active categories only:

```csharp
modelBuilder.Entity<CategoryAggregate>().HasQueryFilter(c => c.IsActive == true);
```

### Migrations

Located in `Quraaa.Persistence/Migrations/`:

1. `20260608185002_InitialPostgresCreate`
2. `20260608221526_AddLibraries`
3. `20260619185145_AddBooksAndListingsAndCategoriesTables`
4. `20260619195202_FixRelationBetwenInterestsAndCategroies`
5. `20260619210624_EnforceOneLibraryPerUser`
6. `20260620152744_DeleteColumnAndFixInterests`

`Program.cs` runs `db.Database.Migrate()` on startup, so the database is migrated automatically when the app starts.

### Seeders

`Program.cs` runs the following seeders after migrating:

- `CategorySeeder.SeedAsync` — seeds categories if none exist.
- `AdminSeeder.SeedAsync` — creates an admin user from `ADMIN_PHONE_NUMBER` / `ADMIN_PASSWORD`.
- `UserSeeder.SeedAsync` — seeds a default user and deterministic library-owner users.
- `LibrarySeeder.SeedAsync` — seeds libraries linked to the owner users.

## Code Style & Conventions

### Solution organization

- All projects target `net10.0` with `ImplicitUsings` and `Nullable` enabled.
- Feature folders under `Quraaa.Application/Features/{Feature}/` follow a CQRS/MediatR pattern:
  - `Commands/{CommandName}/{Command}Command.cs`
  - `Commands/{CommandName}/{Command}CommandHandler.cs`
  - `Commands/{CommandName}/{Command}CommandValidator.cs`
  - `Queries/{QueryName}/{Query}Query.cs`
  - `Queries/{QueryName}/{Query}QueryHandler.cs`
  - `Queries/{QueryName}/{Query}QueryValidator.cs`
  - `Common/` for response DTOs
  - `Interfaces/` for repository/service abstractions

### Domain conventions

- `Entity` → `AuditableEntity` → `AggregateRoot`
- Aggregates use private constructors for EF Core and public factories for application code.
- Business-rule violations throw `DomainException`.
- Value objects inherit `ValueObjectRoot`.
- Enums are stored as integers in the database and serialized as strings in JSON via `JsonStringEnumConverter`.

### Validation

- Uses FluentValidation (`AbstractValidator<T>`).
- Validators are auto-registered via `AddValidatorsFromAssembly`.
- Handlers inherit `BaseApplicationService<T>` and call `ExecuteAsync(...)` to run validation automatically.

### Result pattern

`Quraaa.Application/Shared/Results/AppResult.cs` uses OneOf:

```csharp
public class AppResult : OneOfBase<Success, ValidationFailed, NotFound, Forbidden, DomainError>
public class AppResult<TData> : OneOfBase<TData, ValidationFailed, NotFound, Forbidden, DomainError>
```

`ApiClientController.HandleResult` maps these to HTTP status codes:

- Success → `200 OK`
- Validation failure → `400 Bad Request`
- Not found → `404 Not Found`
- Forbidden → `403 Forbidden`
- `LibraryErrorCodes.DuplicateLibraryForUser` or `"DUPLICATE_APPLICATION"` → `409 Conflict`
- Other domain errors → `400 Bad Request`

### User ID extraction from JWT

Controllers repeatedly use this pattern:

```csharp
var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier)
    ?? User.FindFirstValue("nameid")
    ?? User.FindFirstValue("sub");

Guid.TryParse(claimValue, out var userId)
```

The JWT `NameClaimType` is set to `ClaimTypes.NameIdentifier` during authentication configuration.

### File upload abstraction

`IUploadedFile` in `Quraaa.Application/Shared/Files/IUploadedFile.cs` keeps ASP.NET `IFormFile` out of the Application layer. The API adapter is `FormFileUploadedFile` in `Quraaa.API/Requests/Files/FormFileUploadedFile.cs`.

## Security Considerations

- JWT authentication uses a symmetric signing key from `JWT_SECRET_KEY`. Keep this key secret and rotate it periodically.
- Passwords are hashed by ASP.NET Core Identity.
- Phone numbers are used as usernames; emails are synthesized as `{phone}@quraaa.com`.
- Phone numbers are normalized to E.164 where possible using `libphonenumber-csharp`.
- The forgot-password endpoint returns a generic success even if the phone number is not registered, to avoid leaking registration status.
- OTP send and verify endpoints implement rate limiting and failed-attempt lockouts via `IDistributedCache`.
- Firebase service-account credentials and `.env` secrets must never be committed.
- `Notifications:AllowTestEndpoint` is enabled in `appsettings.json` and `appsettings.Development.json`. Disable it in production unless you intend to allow unauthenticated test notification dispatch.
- `Otp:AllowInMemoryCacheInProduction` is set to `true` in `appsettings.json`. This is acceptable only for temporary single-instance testing; production should use Redis.
- HTTPS redirection and forwarded headers are enabled in the middleware pipeline. Configure `KnownProxies`/`KnownIPNetworks` appropriately if you deploy behind a reverse proxy.
- The `AdminSeeder` creates an admin user from environment variables on every startup if the user does not already exist. Ensure `ADMIN_PHONE_NUMBER` and `ADMIN_PASSWORD` are strong and kept secret.

## Testing Strategy

There is no automated test suite in the repository. Manual testing workflow:

1. Ensure PostgreSQL is running and `ConnectionStrings:DefaultConnection` is correct.
2. Ensure `JWT_SECRET_KEY` is set.
3. Run `dotnet run --project Quraaa.API`.
4. Open `https://localhost:7260/docs` or `http://localhost:5153/docs`.
5. Use the Swagger UI or an HTTP client (Postman, curl, etc.) to exercise endpoints.
6. For OTP/forgot-password flows, configure `OTP_DEVICE_TOKEN` and Firebase credentials.

Recommended additions (TODO):

- Unit tests for validators and domain aggregates.
- Integration tests for command/query handlers using `WebApplicationFactory`.
- Repository tests against an in-memory or test-container PostgreSQL database.

## Deployment

### Docker

The multi-stage `Dockerfile` builds the API and runs it on port `8080`. Build with:

```bash
docker build -t quraaa-api .
```

Run with environment variables or an `--env-file`:

```bash
docker run -p 8080:8080 --env-file .env quraaa-api
```

### Heroku-style deployment

The `Procfile` indicates Heroku-style deployment:

```text
web: cd Quraaa.API/bin/publish/; dotnet Quraaa.API.dll --urls http://*:$PORT
```

This expects the API to be pre-published to `Quraaa.API/bin/publish/` and binds to the platform-provided `$PORT`.

### CI/CD

The only GitHub Actions workflow is `.github/workflows/check-branch-name.yml`. It validates pull-request branch names against these patterns:

- `feature/123-description`
- `fix/123-description`
- `refactor/123-description`
- `feature/branch-name`
- `<name>-patch-<number>`
- lowercase kebab-case names like `register-library`

There is currently no automated build, test, publish, or deploy workflow.

## HTTP API Surface

All controllers inherit from `ApiClientController`:

```csharp
[Route("api/[controller]")]
[ApiController]
```

Current endpoints:

```text
POST /api/Auth/register
POST /api/Auth/login
POST /api/Auth/reset-password
POST /api/Auth/forgot-password
POST /api/Auth/forgot-password/verify
GET /api/Profile/me
PUT /api/Profile/me
POST /api/Library/register
GET /api/Library
GET /api/Categories
GET /api/Categories/{categoryId}
POST /api/Categories
POST /api/Otp/send
POST /api/Otp/verify
POST /api/Notifications/send
POST /api/Notifications/test
```

### Authentication

`POST /api/Auth/register` request body:

```json
{
  "firstName": "Ali",
  "lastName": "Hassan",
  "phoneNumber": "+9647XXXXXXXXX",
  "password": "abc123",
  "gender": 1,
  "dateOfBirth": "2000-01-01",
  "interests": ["science", "history"]
}
```

`Gender` enum values:

```text
1 = Male
2 = Female
```

`interests` are category codes; each value must match an existing `CategoryAggregate.Code`.

Successful registration response is `AuthResponse`:

```json
{
  "userId": "guid",
  "accessToken": "jwt",
  "refreshToken": "secure-random-base64",
  "accessTokenExpiration": "utc-date-time"
}
```

`POST /api/Auth/login` request body:

```json
{
  "phoneNumber": "+9647XXXXXXXXX",
  "password": "abc123"
}
```

Successful login response is also `AuthResponse`.

Password reset request body maps to `ResetPasswordRequest`; the controller creates `ResetPasswordCommand` after reading `UserId` from the authenticated JWT:

```json
{
  "oldPassword": "oldPass123",
  "newPassword": "newPass123"
}
```

Forgot-password request body:

```json
{
  "phoneNumber": "+9647XXXXXXXXX"
}
```

Forgot-password verify request body:

```json
{
  "phoneNumber": "+9647XXXXXXXXX",
  "otpCode": "123456",
  "newPassword": "newPass123"
}
```

### Profile

`GET /api/Profile/me` has no request body. `ProfileController` reads the user id from JWT claims and sends `GetMyProfileQuery`.

`PUT /api/Profile/me` request body maps to `UpdateProfileRequest`:

```json
{
  "firstName": "Ali",
  "lastName": "Hassan",
  "gender": 1,
  "dateOfBirth": "2000-01-01",
  "profileImageUrl": "/uploads/profiles/user.jpg",
  "interests": ["science", "history"]
}
```

Successful profile responses use `ProfileResponse` and do not expose `PasswordHash`:

```json
{
  "userId": "guid",
  "firstName": "Ali",
  "lastName": "Hassan",
  "phoneNumber": "+9647XXXXXXXXX",
  "gender": 1,
  "role": 1,
  "dateOfBirth": "2000-01-01",
  "profileImageUrl": "/uploads/profiles/user.jpg",
  "interests": ["science", "history"],
  "lastLoginDate": null,
  "previousLoginDate": null,
  "creationTime": "utc-date-time",
  "lastModificationTime": "utc-date-time"
}
```

### Library

`POST /api/Library/register` uses `multipart/form-data`:

```text
libraryName: Central Library
location: Baghdad
libraryImage: uploaded image file
headerImage: uploaded image file
email: library@example.com
```

The request does not accept `userId`; `LibraryController` reads it from the JWT. New libraries are created with `ApprovalStatus = Pending`.

`GET /api/Library` returns a paged list of approved libraries. It accepts `[Authorize(Roles = "User")]` and query parameters for paging through `GetLibrariesQuery`.

### Categories

`GET /api/Categories` returns all active categories.

`GET /api/Categories/{categoryId}` returns a single category.

`POST /api/Categories` is admin-only (`[Authorize(Roles = "Admin")]`) and creates a new category.

### OTP

`POST /api/Otp/send` request body:

```json
{
  "phoneNumber": "+9647XXXXXXXXX"
}
```

`POST /api/Otp/verify` request body:

```json
{
  "phoneNumber": "+9647XXXXXXXXX",
  "code": "123456"
}
```

### Notifications

`POST /api/Notifications/send` request body:

```json
{
  "deviceToken": "fcm-registration-token-from-client-app",
  "title": "Welcome",
  "body": "Your notification body",
  "data": {
    "type": "general"
  }
}
```

`POST /api/Notifications/test` has the same shape (with optional fields) and is allowed anonymously in Development or when `Notifications:AllowTestEndpoint=true`.

## Feature Flows

### Registration Flow

Files:

```text
Quraaa.API/Controllers/AuthController.cs
Quraaa.Application/Features/Authentication/Commands/Register/RegisterCommand.cs
Quraaa.Application/Features/Authentication/Commands/Register/RegisterCommandValidator.cs
Quraaa.Application/Features/Authentication/Commands/Register/RegisterCommandHandler.cs
Quraaa.Persistence/Services/IdentityService.cs
Quraaa.Persistence/Repositories/UserRepository.cs
Quraaa.Domain/User/UserAggregate.cs
```

Flow:

```text
HTTP POST /api/Auth/register
  -> AuthController.Register(command)
  -> Mediator.Send(command)
  -> RegisterCommandHandler.Handle(...)
  -> BaseApplicationService validates RegisterCommand
  -> IIdentityService.IsPhoneNumberUniqueAsync(phone)
  -> IIdentityService.CreateUserIdentityAsync(id, phone, password)
  -> IPhoneService.FormatToE164(phone)
  -> new UserAggregate(...)
  -> UserAggregate.AddInterest(...) for each interest category code
  -> IUserRepository.AddUserAsync(profile)
  -> IUserRepository.SaveChangesAsync()
  -> IIdentityService.GenerateAuthTokensAsync(id, phone)
  -> AuthResponse
```

Important registration details:

- The same generated `Guid` is used as the ASP.NET Identity user ID and the domain `UserAggregate.Id`.
- `ApplicationUser.UserName` is the submitted phone number.
- `ApplicationUser.Email` is synthesized as `{phoneNumber}@quraaa.com`.
- Email and phone are marked confirmed at registration time.
- `UserAggregate.PhoneNumber` is formatted to E.164 when possible.
- `UserAggregate.PasswordHash` stores the Identity password hash.
- New users receive `Role.User`.
- Refresh tokens are random 64-byte values encoded as Base64 and saved on the Identity user.
- Refresh token expiry is set to `DateTime.UtcNow.AddDays(30)`.

### Registration Validation Rules

`RegisterCommandValidator` enforces:

- `FirstName`: required, max 50 characters.
- `LastName`: required, max 50 characters.
- `PhoneNumber`: required, must start with `+`, must be valid according to libphonenumber.
- `Password`: required, at least 6 characters, must contain at least one digit.
- `DateOfBirth`: required, must be older than or equal to 5 years and younger than 100 years based on UTC date.
- `Gender`: must be a valid enum value.
- `Interests`: required and not empty.
- Each interest code must exist as a `CategoryAggregate.Code`.

### Login Flow

Files:

```text
Quraaa.API/Controllers/AuthController.cs
Quraaa.Application/Features/Authentication/Commands/Login/LoginCommand.cs
Quraaa.Application/Features/Authentication/Commands/Login/LoginCommandValidator.cs
Quraaa.Application/Features/Authentication/Commands/Login/LoginCommandHandler.cs
Quraaa.Persistence/Services/IdentityService.cs
Quraaa.Persistence/Repositories/UserRepository.cs
```

Flow:

```text
HTTP POST /api/Auth/login
  -> AuthController.Login(command)
  -> Mediator.Send(command)
  -> LoginCommandHandler.Handle(...)
  -> BaseApplicationService validates LoginCommand
  -> IPhoneService.FormatToE164(phone)
  -> IUserRepository.GetUserByPhoneNumberAsync(formattedPhone)
  -> IIdentityService.CheckPasswordAsync(user, password)
  -> updates last/previous login timestamps on UserAggregate
  -> IUserRepository.SaveChangesAsync()
  -> IIdentityService.GenerateAuthTokensAsync(id, phone)
  -> AuthResponse
```

### Password Reset Flow

Files:

```text
Quraaa.API/Controllers/AuthController.cs
Quraaa.API/Requests/Authentication/ResetPasswordRequest.cs
Quraaa.Application/Features/Authentication/Commands/ResetPassword/ResetPasswordCommand.cs
Quraaa.Application/Features/Authentication/Commands/ResetPassword/ResetPasswordCommandValidator.cs
Quraaa.Application/Features/Authentication/Commands/ResetPassword/ResetPasswordCommandHandler.cs
Quraaa.Application/Features/Authentication/Interfaces/IIdentityService.cs
Quraaa.Persistence/Services/IdentityService.cs
Quraaa.Persistence/Repositories/UserRepository.cs
Quraaa.Domain/User/UserAggregate.cs
```

Route:

```text
POST /api/Auth/reset-password
```

Authentication:

```text
Authorization: Bearer <access-token>
```

Validation rules:

- `UserId`: required on the command, sourced from the authenticated JWT rather than the request body.
- `OldPassword`: required string, min 8 characters, max 64 characters.
- `NewPassword`: required string, min 8 characters, max 64 characters, must be different from `OldPassword`.

Flow:

```text
HTTP POST /api/Auth/reset-password
  -> AuthController.ResetPassword(body request)
  -> [Authorize] validates JWT bearer token
  -> AuthController extracts UserId from token claims
  -> AuthController creates ResetPasswordCommand with token UserId and request passwords
  -> Mediator.Send(command)
  -> ResetPasswordCommandHandler.Handle(...)
  -> BaseApplicationService validates ResetPasswordCommand
  -> IUserRepository.GetUserByIdAsync(userId) returns the user profile or null
  -> handler throws NotFoundException if the user profile is null
  -> IIdentityService.ChangePasswordAsync(userId, oldPassword, newPassword)
  -> IdentityService uses UserManager.ChangePasswordAsync to verify the old password and update the Identity password hash
  -> handler converts Identity failures to ApplicationBusinessException
  -> handler checks the updated hash was returned
  -> UserAggregate.UpdatePasswordHash(updatedHash, userId)
  -> IUserRepository.SaveChangesAsync()
  -> AppResult success
```

### Forgot Password Flow

Files:

```text
Quraaa.API/Controllers/AuthController.cs
Quraaa.API/Requests/Authentication/ForgotPasswordRequest.cs
Quraaa.API/Requests/Authentication/ResetForgotPasswordRequest.cs
Quraaa.Application/Features/Authentication/Commands/ForgotPassword/ForgotPasswordCommand.cs
Quraaa.Application/Features/Authentication/Commands/ForgotPassword/ForgotPasswordCommandValidator.cs
Quraaa.Application/Features/Authentication/Commands/ForgotPassword/ForgotPasswordCommandHandler.cs
Quraaa.Application/Features/Authentication/Commands/ResetForgotPassword/ResetForgotPasswordCommand.cs
Quraaa.Application/Features/Authentication/Commands/ResetForgotPassword/ResetForgotPasswordCommandValidator.cs
Quraaa.Application/Features/Authentication/Commands/ResetForgotPassword/ResetForgotPasswordCommandHandler.cs
Quraaa.Application/Features/Authentication/Interfaces/IIdentityService.cs
Quraaa.Application/Features/Authentication/Interfaces/IUserRepository.cs
Quraaa.Application/Features/Otp/Interfaces/IFirebaseSmsGateway.cs
Quraaa.Application/Features/Otp/Interfaces/IOtpCacheService.cs
Quraaa.Persistence/Services/IdentityService.cs
Quraaa.Persistence/Repositories/UserRepository.cs
Quraaa.Domain/User/UserAggregate.cs
```

Routes:

```text
POST /api/Auth/forgot-password
POST /api/Auth/forgot-password/verify
```

Authentication:

```text
AllowAnonymous
```

Validation rules:

- `PhoneNumber`: required, must start with `+`, must be valid according to libphonenumber.
- `SmsGatewayDeviceToken`: required server-side configuration read from `OTP_DEVICE_TOKEN` by `AuthController`; not accepted in the request body.
- `OtpCode`: required, exactly 6 digits.
- `NewPassword`: required, min 8 characters, max 64 characters, must contain at least one digit.

Flow:

```text
HTTP POST /api/Auth/forgot-password
  -> AuthController.ForgotPassword(body request)
  -> [AllowAnonymous]
  -> AuthController reads smsGatewayDeviceToken from OTP_DEVICE_TOKEN
  -> AuthController reads clientIp from HttpContext.Connection.RemoteIpAddress
  -> ForgotPasswordCommand(phoneNumber, smsGatewayDeviceToken, clientIp)
  -> ForgotPasswordCommandHandler.Handle(...)
  -> BaseApplicationService validates ForgotPasswordCommand
  -> IPhoneService.FormatToE164(phone)
  -> IOtpCacheService checks send and verification lockouts
  -> IUserRepository.GetUserByPhoneNumberAsync(formattedPhone)
  -> if user is null, records the request lockout and returns generic success without sending an OTP to avoid leaking registration status
  -> handler generates OTP and stores it in IDistributedCache
  -> IFirebaseSmsGateway.SendSmsRequestAsync(phone, otp, smsGatewayDeviceToken)
  -> FirebaseSmsGateway sends an FCM data message to the gateway device token

HTTP POST /api/Auth/forgot-password/verify
  -> AuthController.VerifyForgotPassword(body request)
  -> [AllowAnonymous]
  -> AuthController reads clientIp from HttpContext.Connection.RemoteIpAddress
  -> ResetForgotPasswordCommand(phoneNumber, otpCode, newPassword, clientIp)
  -> ResetForgotPasswordCommandHandler.Handle(...)
  -> BaseApplicationService validates ResetForgotPasswordCommand
  -> IPhoneService.FormatToE164(phone)
  -> handler reads OTP from IDistributedCache and verifies with fixed-time comparison
  -> failed attempts are tracked; 5 failures in 5 minutes trigger a 5-minute lockout
  -> success clears OTP and verification state
  -> IUserRepository.GetUserByPhoneNumberAsync(formattedPhone)
  -> handler throws NotFoundException if the user profile is null
  -> IIdentityService.ResetPasswordAsync(user.Id, newPassword)
  -> IdentityService generates a reset token and calls UserManager.ResetPasswordAsync
  -> handler throws ApplicationBusinessException on Identity errors
  -> UserAggregate.UpdatePasswordHash(updatedHash, user.Id)
  -> IUserRepository.SaveChangesAsync()
  -> AppResult success
```

### Profile Flow

Files:

```text
Quraaa.API/Controllers/ProfileController.cs
Quraaa.API/Requests/Profiles/UpdateProfileRequest.cs
Quraaa.Application/Features/Profiles/Common/ProfileResponse.cs
Quraaa.Application/Features/Profiles/Queries/GetMyProfile/GetMyProfileQuery.cs
Quraaa.Application/Features/Profiles/Queries/GetMyProfile/GetMyProfileQueryValidator.cs
Quraaa.Application/Features/Profiles/Queries/GetMyProfile/GetMyProfileQueryHandler.cs
Quraaa.Application/Features/Profiles/Commands/UpdateProfile/UpdateProfileCommand.cs
Quraaa.Application/Features/Profiles/Commands/UpdateProfile/UpdateProfileCommandValidator.cs
Quraaa.Application/Features/Profiles/Commands/UpdateProfile/UpdateProfileCommandHandler.cs
Quraaa.Application/Features/Authentication/Interfaces/IUserRepository.cs
Quraaa.Persistence/Repositories/UserRepository.cs
Quraaa.Domain/User/UserAggregate.cs
```

Routes:

```text
GET /api/Profile/me
PUT /api/Profile/me
```

Authentication:

```text
Authorization: Bearer <access-token>
```

Validation rules:

- `UserId`: required on the command/query, sourced from the authenticated JWT rather than the request body.
- `FirstName`: required, max 50 characters.
- `LastName`: required, max 50 characters.
- `Gender`: must be a valid enum value.
- `DateOfBirth`: required, must be older than or equal to 5 years and younger than 100 years based on UTC date.
- `ProfileImageUrl`: optional, max 500 characters.
- `Interests`: required and not empty.
- Each interest code must exist as a `CategoryAggregate.Code`.

Read flow:

```text
HTTP GET /api/Profile/me
  -> ProfileController.GetMyProfile()
  -> [Authorize] validates JWT bearer token
  -> ProfileController extracts UserId from token claims
  -> ProfileController creates GetMyProfileQuery with token UserId
  -> Mediator.Send(query)
  -> GetMyProfileQueryHandler.Handle(...)
  -> BaseApplicationService validates GetMyProfileQuery
  -> IUserRepository.GetUserByIdAsync(userId) returns the user profile or null
  -> handler throws NotFoundException if the user profile is null
  -> ProfileResponse.FromUser(user)
  -> ProfileResponse
```

Update flow:

```text
HTTP PUT /api/Profile/me
  -> ProfileController.UpdateMyProfile(body request)
  -> [Authorize] validates JWT bearer token
  -> ProfileController extracts UserId from token claims
  -> ProfileController creates UpdateProfileCommand with token UserId and editable fields
  -> Mediator.Send(command)
  -> UpdateProfileCommandHandler.Handle(...)
  -> BaseApplicationService validates UpdateProfileCommand
  -> IUserRepository.GetUserByIdAsync(userId) returns the user profile or null
  -> handler throws NotFoundException if the user profile is null
  -> UserAggregate.UpdateProfile(...)
  -> IUserRepository.SaveChangesAsync()
  -> ProfileResponse
```

### Library Registration Flow

Files:

```text
Quraaa.API/Controllers/LibraryController.cs
Quraaa.API/Requests/Files/FormFileUploadedFile.cs
Quraaa.API/Services/LibraryImageStorageService.cs
Quraaa.Application/Features/Libraries/Commands/RegisterLibrary/RegisterLibraryCommand.cs
Quraaa.Application/Features/Libraries/Commands/RegisterLibrary/RegisterLibraryCommandValidator.cs
Quraaa.Application/Features/Libraries/Commands/RegisterLibrary/RegisterLibraryCommandHandler.cs
Quraaa.Application/Features/Libraries/Common/LibraryResponse.cs
Quraaa.Application/Features/Libraries/Interfaces/ILibraryImageStorageService.cs
Quraaa.Application/Features/Libraries/Interfaces/ILibraryRepository.cs
Quraaa.Persistence/Repositories/LibraryRepository.cs
Quraaa.Persistence/Configurations/LibraryConfiguration.cs
Quraaa.Domain/Library/LibraryAggregate.cs
```

Route:

```text
POST /api/Library/register
```

Authentication:

```text
Authorization: Bearer <access-token>
```

Request body:

```text
Content-Type: multipart/form-data

libraryName: Central Library
location: Baghdad
libraryImage: uploaded image file
headerImage: uploaded image file
email: library@example.com
```

The request does not accept `userId`. `LibraryController` reads the user id from JWT claims and sends that value to the application command.

The successful `LibraryResponse` does not expose `UserId`; ownership stays internal and token-derived.

Library ownership is one-to-one: one authenticated user profile can register at most one library. `RegisterLibraryCommandHandler` checks `ILibraryRepository.ExistsByUserIdAsync(userId)` before storing uploaded images. If a library already exists for that user, the handler returns an application business error.

The image fields are uploaded files. `LibraryController` wraps ASP.NET `IFormFile` values in the application-level `IUploadedFile` abstraction. `RegisterLibraryCommandValidator` validates the uploaded files before storage. After validation succeeds, `RegisterLibraryCommandHandler` stores them through `ILibraryImageStorageService`; the API implementation writes files under `wwwroot/uploads/libraries` with generated file names. The database stores the path strings, for example `/uploads/libraries/<generated-name>.jpg`.

The request does not accept approval status. New libraries are always created as `Pending`; future admin logic should transition them to `Approved` or `Rejected`.

Validation rules:

- `LibraryName`: required, max 100 characters.
- `Location`: required, max 250 characters.
- `LibraryImage`: required uploaded file, JPG/PNG, max 5 MB.
- `HeaderImage`: required uploaded file, JPG/PNG, max 5 MB.
- `Email`: required, valid email format, max 256 characters.
- `UserId`: required on the command, sourced from the authenticated JWT rather than the form body.

Flow:

```text
HTTP POST /api/Library/register
  -> LibraryController.Register(form request)
  -> [Authorize] validates JWT bearer token
  -> LibraryController extracts UserId from token claims
  -> LibraryController wraps form files as IUploadedFile
  -> LibraryController creates RegisterLibraryCommand with uploaded files and token UserId
  -> Mediator.Send(command)
  -> RegisterLibraryCommandHandler.Handle(...)
  -> BaseApplicationService validates RegisterLibraryCommand
  -> RegisterLibraryCommandValidator validates image presence, size, extension, and content type
  -> IUserRepository.GetUserByIdAsync(userId) returns the user profile or null
  -> handler throws NotFoundException if the user profile is null
  -> ILibraryRepository.ExistsByUserIdAsync(userId) confirms the user does not already own a library
  -> handler throws ApplicationBusinessException if a library already exists for the user
  -> ILibraryImageStorageService.SaveAsync(...) stores images in wwwroot/uploads/libraries
  -> new LibraryAggregate(...)
  -> ILibraryRepository.AddLibraryAsync(library)
  -> ILibraryRepository.SaveChangesAsync()
  -> handler deletes stored image files if persistence fails
  -> LibraryResponse
```

### Library Listing Flow

Files:

```text
Quraaa.API/Controllers/LibraryController.cs
Quraaa.Application/Features/Libraries/Queries/GetLibraries/GetLibrariesQuery.cs
Quraaa.Application/Features/Libraries/Queries/GetLibraries/GetLibrariesQueryHandler.cs
Quraaa.Application/Features/Libraries/Queries/GetLibraries/GetLibrariesQueryValidator.cs
Quraaa.Application/Features/Libraries/Queries/GetLibraries/PublicLibraryResponse.cs
Quraaa.Application/Features/Libraries/Interfaces/ILibraryRepository.cs
Quraaa.Persistence/Repositories/LibraryRepository.cs
```

Route:

```text
GET /api/Library
```

Authentication:

```text
Authorization: Bearer <access-token>
[Authorize(Roles = "User")]
```

Query parameters (paging):

```text
pageNumber: int (default 1)
pageSize: int (default 10)
```

Flow:

```text
HTTP GET /api/Library
  -> LibraryController.GetLibraries(query)
  -> [Authorize(Roles = "User")] validates JWT bearer token and role
  -> Mediator.Send(query)
  -> GetLibrariesQueryHandler.Handle(...)
  -> BaseApplicationService validates GetLibrariesQuery
  -> ILibraryRepository.GetApprovedLibrariesAsync(pageNumber, pageSize)
  -> returns PagedResult<PublicLibraryResponse>
```

Only libraries with `ApprovalStatus = Approved` are returned.

### OTP Flow

Files:

```text
Quraaa.API/Controllers/OtpController.cs
Quraaa.API/Requests/Otp/SendOtpRequest.cs
Quraaa.API/Requests/Otp/VerifyOtpRequest.cs
Quraaa.Application/Features/Otp/Commands/SendOtp/
Quraaa.Application/Features/Otp/Commands/VerifyOtp/
Quraaa.Application/Features/Otp/Interfaces/IFirebaseSmsGateway.cs
Quraaa.Application/Features/Otp/Interfaces/IOtpCacheService.cs
Quraaa.Infrastructure/Services/FirebaseSmsGateway.cs
Quraaa.Infrastructure/Services/OtpCacheService.cs
Quraaa.Infrastructure/Extensions/FirebaseExtensions.cs
Quraaa.Infrastructure/Extensions/InfrastructureDependencyInjectionHandler.cs
```

Routes:

```text
POST /api/Otp/send
POST /api/Otp/verify
```

Authentication:

```text
AllowAnonymous
```

`POST /api/Otp/send` request body:

```json
{
  "phoneNumber": "+9647XXXXXXXXX"
}
```

`OtpController` reads the SMS gateway FCM token from `OTP_DEVICE_TOKEN` in configuration/environment variables.

`POST /api/Otp/verify` request body:

```json
{
  "phoneNumber": "+9647XXXXXXXXX",
  "code": "123456"
}
```

OTP behavior:

- The API generates a 6-digit OTP with `RandomNumberGenerator`.
- OTPs expire after 5 minutes.
- Send requests are throttled for 60 seconds per normalized phone number and client IP.
- Verification allows up to 5 failed attempts in a 5-minute window.
- After too many invalid attempts, the OTP is cleared and verification is locked for 5 minutes.
- Successful verification clears the OTP and failed-attempt state.
- The OTP feature is standalone; it does not yet mark a user or phone number as verified and is not enforced by registration/login.

Flow:

```text
HTTP POST /api/Otp/send
  -> OtpController.SendOtp(body request)
  -> OtpController reads smsGatewayDeviceToken from OTP_DEVICE_TOKEN
  -> SendOtpCommand(phoneNumber, smsGatewayDeviceToken, clientIp)
  -> SendOtpCommandHandler.Handle(...)
  -> BaseApplicationService validates SendOtpCommand
  -> IPhoneService.FormatToE164(phone)
  -> IOtpCacheService checks send and verification lockouts
  -> handler generates OTP and stores it in IDistributedCache
  -> IFirebaseSmsGateway.SendSmsRequestAsync(phone, otp, smsGatewayDeviceToken)
  -> FirebaseSmsGateway sends an FCM data message to the requested device token

HTTP POST /api/Otp/verify
  -> OtpController.VerifyOtp(body request)
  -> VerifyOtpCommand(phoneNumber, code, clientIp)
  -> VerifyOtpCommandHandler.Handle(...)
  -> BaseApplicationService validates VerifyOtpCommand
  -> handler reads OTP from IDistributedCache
  -> handler compares code using fixed-time comparison
  -> success clears OTP state; invalid attempts update failed-attempt counters
```

### Notifications Flow

Files:

```text
Quraaa.API/Controllers/NotificationsController.cs
Quraaa.API/Requests/Notifications/SendNotificationRequest.cs
Quraaa.Application/Features/Notifications/Commands/SendNotification/
Quraaa.Application/Features/Notifications/Common/NotificationSendResponse.cs
Quraaa.Application/Features/Notifications/Interfaces/IFirebaseNotificationService.cs
Quraaa.Infrastructure/Services/FirebaseNotificationService.cs
```

Route:

```text
POST /api/Notifications/send
POST /api/Notifications/test
```

Authentication:

```text
POST /api/Notifications/send -> Authorization: Bearer <access-token>
POST /api/Notifications/test -> AllowAnonymous when enabled
```

The test route is enabled automatically in Development. Outside Development, it is disabled unless `Notifications:AllowTestEndpoint=true` or `Notifications__AllowTestEndpoint=true` is configured.

Request body:

```json
{
  "deviceToken": "fcm-registration-token-from-client-app",
  "title": "Welcome",
  "body": "Your notification body",
  "data": {
    "type": "general"
  }
}
```

Successful response:

```json
{
  "messageId": "firebase-message-id"
}
```

Flow:

```text
HTTP POST /api/Notifications/send
  -> NotificationsController.Send(body request)
  -> [Authorize] validates JWT bearer token
  -> NotificationsController extracts UserId from token claims
  -> SendNotificationCommand(userId, deviceToken, title, body, data)
  -> SendNotificationCommandHandler.Handle(...)
  -> BaseApplicationService validates SendNotificationCommand
  -> IUserRepository.GetUserByIdAsync(userId) confirms the authenticated profile exists
  -> IFirebaseNotificationService.SendToDeviceAsync(...)
  -> FirebaseNotificationService sends an FCM notification message to the requested device token

HTTP POST /api/Notifications/test
  -> NotificationsController.SendTest(body request)
  -> route is allowed only in Development or when Notifications:AllowTestEndpoint=true
  -> SendTestNotificationCommand(deviceToken, optional title, optional body, optional data)
  -> SendTestNotificationCommandHandler.Handle(...)
  -> BaseApplicationService validates SendTestNotificationCommand
  -> IFirebaseNotificationService.SendToDeviceAsync(...)
  -> FirebaseNotificationService sends an FCM notification message to the requested device token
```

### Categories Flow

Files:

```text
Quraaa.API/Controllers/CategoriesController.cs
Quraaa.Application/Features/Categories/Commands/CreateCategory/
Quraaa.Application/Features/Categories/Queries/GetAllCategories/
Quraaa.Application/Features/Categories/Queries/GetCategoryById/
Quraaa.Application/Features/Categories/Common/CategoryResponse.cs
Quraaa.Application/Features/Categories/Interfaces/ICategoryRepository.cs
Quraaa.Persistence/Repositories/CategoryRepository.cs
Quraaa.Persistence/Configurations/CategoryConfiguration.cs
Quraaa.Persistence/Seed/CategorySeeder.cs
Quraaa.Domain/Category/CategoryAggregate.cs
```

Routes:

```text
GET /api/Categories
GET /api/Categories/{categoryId}
POST /api/Categories
```

Authentication:

```text
GET /api/Categories -> AllowAnonymous
GET /api/Categories/{categoryId} -> AllowAnonymous
POST /api/Categories -> Authorization: Bearer <admin-access-token>
```

`POST /api/Categories` requires the `Admin` role.

The `CategoryAggregate` model includes:

- `Code` — unique string code used as an interest identifier during registration/profile update.
- `NameAr` — Arabic name.
- `NameEn` — English name.
- `ParentCategoryId` — optional parent category.
- `IsActive` — soft-active flag; inactive categories are filtered from `GET` queries globally.

Flow:

```text
HTTP GET /api/Categories
  -> CategoriesController.GetAllCategories()
  -> GetAllCategoriesQuery
  -> GetAllCategoriesQueryHandler
  -> ICategoryRepository.GetAllAsync()
  -> List<CategoryResponse>

HTTP GET /api/Categories/{categoryId}
  -> CategoriesController.GetCategoryById(categoryId)
  -> GetCategoryByIdQuery(categoryId)
  -> GetCategoryByIdQueryHandler
  -> ICategoryRepository.GetByIdAsync(categoryId)
  -> CategoryResponse or NotFound

HTTP POST /api/Categories
  -> CategoriesController.CreateCategory(request)
  -> [Authorize(Roles = "Admin")]
  -> CreateCategoryCommand(...)
  -> CreateCategoryCommandHandler
  -> ICategoryRepository.AddAsync(category)
  -> CategoryResponse
```

## Development Tips & Common Patterns

- Run the API with `dotnet run --project Quraaa.API` from the repository root.
- The database is migrated and seeded automatically on startup; ensure PostgreSQL is reachable.
- If you add a new aggregate, create the domain class, an EF configuration in `Quraaa.Persistence/Configurations/`, add a `DbSet` to `ApplicationDbContext`, and create a migration.
- If you add a new feature, create the command/query/handler/validator under `Quraaa.Application/Features/{Feature}/` and a controller action in `Quraaa.API/Controllers/`.
- Keep interfaces in the Application layer and implementations in Persistence or Infrastructure.
- Use `BaseApplicationService.ExecuteAsync(...)` in handlers to get automatic validation and exception-to-result mapping.
- Use `ApplicationBusinessException` for application-level rule failures and `DomainException` for aggregate invariants.
- Avoid navigation properties between aggregates in EF Core mappings; use scalar IDs and database constraints.

## Known Gaps & High-Value TODOs

Based on the current codebase:

- **Admin library review**: `LibraryAggregate` has `Approve`/`Reject` methods, but there is no HTTP endpoint to transition a library from `Pending` to `Approved`/`Rejected`.
- **Refresh token rotation / logout**: tokens are generated and stored, but there is no logout or refresh endpoint.
- **OTP integration**: OTP send/verify is standalone; it is not yet required during registration, login, password reset, or phone verification.
- **Book/listing HTTP surface**: `BookAggregate` and `ListingAggregate` are modeled and migrated, but no controllers or handlers expose them yet. `AddPhysicalBookCommand` exists without a handler or validator, and `IBookMetadataService`/`GoogleBooksService` registration is commented out.
- **Tests**: no unit, integration, or end-to-end tests exist.
- **CI/CD**: only branch-name validation is automated; add build, test, and publish workflows.
- **Production readiness**: review `Otp:AllowInMemoryCacheInProduction=true` and `Notifications:AllowTestEndpoint=true` in `appsettings.json` before production deployment.
