# QuraaaPlatform Agent Context

This file is written for AI agents, coding assistants, and chatbots that need a fast, accurate working model of this repository. It describes the current codebase as it exists now, not a future intended architecture.

For a more beginner-friendly walkthrough, also read `PROJECT_GUIDE.md`.

## Project Snapshot

`QuraaaPlatform` is a layered ASP.NET Core Web API solution targeting `.NET 10.0`.

Current implemented business capability:

- User registration through `POST /api/Auth/register`.
- Library registration through `POST /api/Library/register`.
- User security identity is stored through ASP.NET Core Identity.
- User profile/business data is stored as a domain aggregate in `UsersProfiles`.
- Library data is stored as a domain aggregate in `Libraries` and linked to a user profile by `UserId`.
- Registration returns JWT access and refresh tokens.

Core technologies:

- ASP.NET Core Web API
- Entity Framework Core with SQL Server
- ASP.NET Core Identity with `Guid` keys
- MediatR for commands/handlers
- FluentValidation for request validation
- OneOf for application result unions
- libphonenumber-csharp for international phone validation/formatting
- DotNetEnv plus environment variables for runtime secrets/configuration
- Swagger/OpenAPI UI in development

## Repository Layout

```text
QuraaaPlatform.slnx
README.md
PROJECT_GUIDE.md
AGENTS.md
Dockerfile
check-branch-name.yml
.github/workflows/check-branch-name.yml

Quraaa.API/
  Program.cs
  Controllers/
  Extensions/
  Properties/launchSettings.json
  appsettings.json
  appsettings.Development.json

Quraaa.Application/
  Extensions/
  Features/Authentication/
  Shared/

Quraaa.Domain/
  Shared/
  User/

Quraaa.Persistence/
  Configurations/
  Data/
  Extensions/
  Migrations/
  Repositories/
  Services/

Quraaa.Infrastructure/
  currently minimal
```

Ignore generated build output:

```text
**/bin/
**/obj/
```

Do not edit generated files under `bin` or `obj`. Treat EF migrations as generated source: create/update them through EF commands unless the user explicitly asks for a manual migration fix.

## Layering And Dependency Rules

Project references currently encode this shape:

```text
Quraaa.API -> Quraaa.Application
Quraaa.API -> Quraaa.Persistence
Quraaa.Persistence -> Quraaa.Application
Quraaa.Persistence -> Quraaa.Domain
Quraaa.Application -> Quraaa.Domain
Quraaa.Infrastructure -> Quraaa.Application
Quraaa.Domain -> no project references
```

Keep this dependency direction:

```text
API -> Application -> Domain
API -> Persistence -> Application -> Domain
Infrastructure -> Application
```

Layer responsibilities:

- `Quraaa.Domain`: entities, aggregates, value objects, enums, business invariants.
- `Quraaa.Application`: use cases, commands/queries, handlers, validators, interfaces, DTOs, result types.
- `Quraaa.Persistence`: EF Core `DbContext`, table mapping, migrations, repositories, ASP.NET Identity implementation.
- `Quraaa.Infrastructure`: future external provider implementations such as email, SMS, payments, files, search, third-party APIs.
- `Quraaa.API`: HTTP controllers, startup, middleware, Swagger, environment configuration.

Do not put HTTP, EF Core, SQL Server, Identity, Swagger, or external provider SDK logic in `Quraaa.Domain`.

## Runtime Entry Point

`Quraaa.API/Program.cs` is the web entry point.

Startup behavior:

```text
DotNetEnv.Env.Load()
builder.Configuration.AddEnvironmentVariables()
builder.Services.AddControllers()
builder.Services.AddDatabaseConfiguration(...)
builder.Services.AddApplicationServices(...)
builder.Services.AddSwaggerConfiguration(...)
app.UseSwaggerDashboard() only in Development
app.UseHttpsRedirection()
app.UseAuthentication()
app.UseAuthorization()
app.MapControllers()
```

Development launch URLs:

```text
http://localhost:5153
https://localhost:7260
```

Swagger/OpenAPI in development:

```text
/docs
/openapi/v1.json
```

Docker runtime:

- Uses `mcr.microsoft.com/dotnet/sdk:10.0` for build/publish.
- Uses `mcr.microsoft.com/dotnet/aspnet:10.0` for final runtime.
- Exposes port `8080`.
- Sets `ASPNETCORE_URLS=http://+:8080`.

## Configuration

`Quraaa.API/appsettings.Development.json` contains the local SQL Server connection string:

```text
ConnectionStrings:DefaultConnection = Server=.;Database=QuraaaDb;Trusted_Connection=True;TrustServerCertificate=True;
```

JWT/token generation reads these configuration keys, usually from `.env` or environment variables:

```text
JWT_SECRET_KEY
JWT_ISSUER
JWT_AUDIENCE
JWT_DURATION_IN_MINUTES
```

`JWT_SECRET_KEY` is required by `IdentityService.GenerateAuthTokensAsync`. If it is missing, token generation throws `InvalidOperationException`.

## HTTP API Surface

All controllers inherit the base route from `ApiClientController`:

```csharp
[Route("api/[controller]")]
[ApiController]
```

Current endpoint:

```text
POST /api/Auth/register
POST /api/Library/register
```

Controller:

```text
Quraaa.API/Controllers/AuthController.cs
```

Request body maps directly to `RegisterCommand`:

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

Successful response is `AuthResponse`:

```json
{
  "userId": "guid",
  "accessToken": "jwt",
  "refreshToken": "secure-random-base64",
  "accessTokenExpiration": "utc-date-time"
}
```

`ApiClientController.HandleResult` maps application results to HTTP responses:

- success data -> `200 OK`
- validation failure -> `400 Bad Request`
- not found -> `404 Not Found`
- forbidden -> `403 Forbidden`
- domain/application business error -> `400 Bad Request`
- special domain message `DUPLICATE_APPLICATION` -> `409 Conflict`

There is no custom global exception middleware currently visible in the repository. Unhandled exceptions bubble to ASP.NET Core defaults.

## Library Registration Flow

Files:

```text
Quraaa.API/Controllers/LibraryController.cs
Quraaa.Application/Features/Libraries/Commands/RegisterLibrary/RegisterLibraryCommand.cs
Quraaa.Application/Features/Libraries/Commands/RegisterLibrary/RegisterLibraryCommandValidator.cs
Quraaa.Application/Features/Libraries/Commands/RegisterLibrary/RegisterLibraryCommandHandler.cs
Quraaa.Application/Features/Libraries/Common/LibraryResponse.cs
Quraaa.Application/Features/Libraries/Interfaces/ILibraryRepository.cs
Quraaa.Persistence/Repositories/LibraryRepository.cs
Quraaa.Persistence/Configurations/LibraryConfiguration.cs
Quraaa.Domain/Library/LibraryAggregate.cs
```

Route:

```text
POST /api/Library/register
```

Request body:

```text
Content-Type: multipart/form-data

libraryName: Central Library
location: Baghdad
libraryImage: uploaded image file
headerImage: uploaded image file
email: library@example.com
userId: guid
```

The image fields are uploaded files. `LibraryController` stores them under `wwwroot/uploads/libraries` with generated file names, then sends the stored paths to the application command. The database stores the path strings, for example `/uploads/libraries/<generated-name>.jpg`.

Validation rules:

- `LibraryName`: required, max 100 characters.
- `Location`: required, max 250 characters.
- `LibraryImage`: required uploaded file, JPG/PNG, max 5 MB.
- `HeaderImage`: required uploaded file, JPG/PNG, max 5 MB.
- `Email`: required, valid email format, max 256 characters.
- `UserId`: required.

Flow:

```text
HTTP POST /api/Library/register
  -> LibraryController.Register(form request)
  -> LibraryController validates uploaded images
  -> LibraryController stores images in wwwroot/uploads/libraries
  -> LibraryController creates RegisterLibraryCommand with stored image paths
  -> Mediator.Send(command)
  -> RegisterLibraryCommandHandler.Handle(...)
  -> BaseApplicationService validates RegisterLibraryCommand
  -> IUserRepository.GetUserByIdAsync(userId) verifies the user profile exists
  -> new LibraryAggregate(...)
  -> ILibraryRepository.AddLibraryAsync(library)
  -> ILibraryRepository.SaveChangesAsync()
  -> LibraryResponse
```

## Registration Flow

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
  -> UserAggregate.AddInterest(...) for each interest code
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

## Registration Validation Rules

`RegisterCommandValidator` enforces:

- `FirstName`: required, max 50 characters.
- `LastName`: required, max 50 characters.
- `PhoneNumber`: required, must start with `+`, must be valid according to libphonenumber.
- `Password`: required, at least 6 characters, must contain at least one digit.
- `DateOfBirth`: required, must be older than or equal to 5 years and younger than 100 years based on UTC date.
- `Gender`: must be a valid enum value.
- `Interests`: required and not empty.
- Each interest code must exist in `Interest.FromCode`.

## Domain Model

Main aggregate:

```text
Quraaa.Domain/User/UserAggregate.cs
```

Properties:

```text
Id: Guid
FirstName: string
LastName: string
PhoneNumber: string
PasswordHash: string
Gender: Gender
Role: Role
DateOfBirth: DateOnly
ProfileImageUrl: string?
LastLoginDate: DateTime?
PreviousLoginDate: DateTime?
PaymentMethod: PaymentMethodInfo?
Interests: IReadOnlyCollection<string>
CreationTime: DateTime
LastModificationTime: DateTime?
LastModifiedBy: Guid?
IsDeleted: bool
DeleationTime: DateTime?
DeletedBy: Guid?
DomainEvents: IReadOnlyCollection<IDomainEvents>
```

Business behavior:

- `AddInterest(string interestCode)` validates against domain interest constants and deduplicates by normalized code.
- `LinkPaymentMethod(string customerId, string brand, string lastFour)` creates `PaymentMethodInfo`.
- `Delete(Guid deletedBy)` marks the aggregate as soft-deleted.
- `UpdateAudit(Guid modifiedBy)` updates last modified metadata.

Current enums:

```text
Gender.Male = 1
Gender.Female = 2
Role.User = 1
Role.Admin = 2
```

Current allowed interest codes:

```text
space_science
geography
history
encyclopedias
patrols
culture
science
novels
policy
dictionary
education
technology
art
literature
other
```

`Interest` also stores Arabic and English display names, but only interest codes are persisted on the user profile.

## Database Model

Main context:

```text
Quraaa.Persistence/Data/ApplicationDbContext.cs
```

The context inherits:

```csharp
IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
```

It includes:

```csharp
DbSet<UserAggregate> UsersProfiles
DbSet<LibraryAggregate> Libraries
```

Identity tables from ASP.NET Core Identity include:

```text
AspNetUsers
AspNetRoles
AspNetRoleClaims
AspNetUserClaims
AspNetUserLogins
AspNetUserRoles
AspNetUserTokens
```

Custom Identity user:

```text
Quraaa.Persistence/Data/ApplicationUser.cs
```

Adds:

```text
RefreshToken: string?
RefreshTokenExpiryTime: DateTime
```

Domain profile table:

```text
UsersProfiles
```

Library table:

```text
Libraries
```

Library columns:

```text
Id uniqueidentifier not null primary key
LibraryName nvarchar(100) not null
Location nvarchar(250) not null
LibraryImage nvarchar(500) not null
HeaderImage nvarchar(500) not null
Email nvarchar(256) not null
UserId uniqueidentifier not null FK to UsersProfiles.Id
CreationTime datetime2 not null
LastModificationTime datetime2 null
LastModifiedBy uniqueidentifier null
IsDeleted bit not null
DeleationTime datetime2 null
DeletedBy uniqueidentifier null
```

Important mapping:

- `UsersProfiles.Id` is the primary key.
- `UsersProfiles.Id` is also a foreign key to `AspNetUsers.Id`.
- Delete behavior is cascade from `AspNetUsers` to `UsersProfiles`.
- `Id` uses `ValueGeneratedNever`; the application generates the `Guid`.
- `FirstName` and `LastName` max length 50 and required.
- `PhoneNumber` max length 20 and required.
- `DateOfBirth` is required.
- `Gender` and `Role` are stored as integers.
- `Interests` is serialized to `nvarchar(max)` JSON.
- `PaymentMethodInfo` is owned by `UserAggregate` and stored in the same `UsersProfiles` table.

Custom profile columns from current migrations/mapping:

```text
Id uniqueidentifier not null primary key / FK to AspNetUsers.Id
FirstName nvarchar(50) not null
LastName nvarchar(50) not null
PhoneNumber nvarchar(20) not null
PasswordHash nvarchar(max) not null
Gender int not null
Role int not null
DateOfBirth date not null
ProfileImageUrl nvarchar(max) null
LastLoginDate datetime2 null
PreviousLoginDate datetime2 null
Interests nvarchar(max) not null
PaymentCustomerId nvarchar(100) null
PaymentCardBrand nvarchar(20) null
PaymentLastFourDigits nvarchar(4) null
CreationTime datetime2 not null
LastModificationTime datetime2 null
LastModifiedBy uniqueidentifier null
IsDeleted bit not null
DeleationTime datetime2 null
DeletedBy uniqueidentifier null
```

Current migrations:

```text
20260605074709_InitialCreate
20260606080342_AddSomeColumnsForUser
20260608003607_AddLibraries
```

Watch item: `ApplicationDbContextModelSnapshot.cs` should normally reflect the latest model. If migrations behave unexpectedly, inspect and regenerate the snapshot through normal EF tooling.

## Persistence And Identity Services

Registered in:

```text
Quraaa.Persistence/Extensions/PersistenceDependencyInjectionHandler.cs
```

Current registrations:

```text
IUserRepository -> UserRepository
ILibraryRepository -> LibraryRepository
IIdentityService -> IdentityService
```

`UserRepository` implements add, lookup by ID, lookup by phone number, and save changes. Lookup methods throw `NotFoundException` when no active user profile is found.

`LibraryRepository` implements add and save changes for `LibraryAggregate`.

`IdentityService` currently implements:

```text
IsPhoneNumberUniqueAsync(string phoneNumber)
CreateUserIdentityAsync(Guid id, string phoneNumber, string password)
GenerateAuthTokensAsync(Guid userId, string phoneNumber)
```

Phone uniqueness uses `UserManager.FindByNameAsync(phoneNumber)`, so it checks the Identity username, not `UsersProfiles.PhoneNumber` directly.

## Application Result Pattern

Result types live in:

```text
Quraaa.Application/Shared/Results/
```

`AppResult` can contain:

```text
Success
ValidationFailed
NotFound
Forbidden
DomainError
```

`AppResult<TData>` can contain:

```text
TData
ValidationFailed
NotFound
Forbidden
DomainError
```

`BaseApplicationService<TService>` wraps use case execution:

- resolves and runs `IValidator<TRequest>` when available
- logs validation failures
- converts `NotFoundException` to `Result.NotFound`
- converts domain exceptions to `Result.DomainError`
- converts `ApplicationBusinessException` to `Result.DomainError`
- converts `UnauthorizedAccessException` to `Result.Forbidden`
- rethrows unexpected exceptions

When adding new handlers, prefer returning `AppResult` or `AppResult<T>` and use the existing `ExecuteAsync` helpers unless a local pattern says otherwise.

## Dependency Injection

API-level composition:

```text
Quraaa.API/Extensions/ServiceCollectionExtensions.cs
```

Application registrations:

```text
Quraaa.Application/Extensions/ApplicationPackagesRegisterExtensions.cs
```

Currently registers:

- FluentValidation validators from the application assembly.
- MediatR handlers from the application assembly.
- `IPhoneService -> PhoneService`.

Persistence registrations:

```text
Quraaa.Persistence/Extensions/PersistenceDependencyInjectionHandler.cs
```

Database/Identity registrations:

```text
Quraaa.API/Extensions/DatabaseExtensions.cs
```

Currently:

- adds `ApplicationDbContext` with SQL Server
- adds Identity Core for `ApplicationUser`
- sets `RequireUniqueEmail = false`
- allows standard username characters
- adds EF Identity stores

## Naming And Current Quirks

Known quirks to respect or fix intentionally:

- `PhoneService.cs` is under `Quraaa.Application/Features/Authentication/Helpers`, but its namespace is `IdentityServer.Helpers`. Existing code imports that namespace. Do not copy this namespace into new unrelated code.
- `AggregateRoot.DeleationTime` is misspelled. The database column also uses `DeleationTime`. Renaming it requires a coordinated migration.
- `ApplicationPackagesRegisterExtensions` creates an unused local `assembly` variable.
- `RegisterCommand.cs` has no namespace declaration while related files do. This works only because consumers reference the global type. Adding a namespace is a breaking cleanup unless all references are updated.
- `Quraaa.Infrastructure` currently has no meaningful implementation.
- `check-branch-name.yml` exists both at root and under `.github/workflows`; the GitHub workflow version is the active workflow.

## Common Change Patterns

Adding a new feature usually follows this order:

```text
1. Domain
   Add or update entities, value objects, enums, and invariant methods.

2. Application
   Add command/query, validator, handler, response DTO, and interfaces.

3. Persistence
   Implement interfaces, update EF configuration, update migrations if schema changes.

4. API
   Add controller endpoint and request/response mapping.

5. Verification
   Build, run targeted tests if available, and manually check Swagger/API behavior if needed.
```

Adding a new authentication use case:

```text
Quraaa.Application/Features/Authentication/Commands/<UseCase>/
Quraaa.Application/Features/Authentication/Common/
Quraaa.Application/Features/Authentication/Interfaces/IIdentityService.cs
Quraaa.Persistence/Services/IdentityService.cs
Quraaa.API/Controllers/AuthController.cs
```

Adding a new user profile field:

```text
Quraaa.Domain/User/UserAggregate.cs
Quraaa.Persistence/Configurations/UserConfiguration.cs
Quraaa.Persistence/Migrations/
Application command/query DTOs
API controller/request/response models if exposed
```

Adding external provider behavior:

```text
1. Define an interface in Quraaa.Application.
2. Implement the interface in Quraaa.Infrastructure.
3. Register the implementation in dependency injection.
4. Keep provider SDK objects out of Quraaa.Domain.
```

## Useful Commands

Build solution:

```bash
dotnet build QuraaaPlatform.slnx
```

Run API:

```bash
dotnet run --project Quraaa.API
```

Run HTTP launch profile:

```bash
dotnet run --project Quraaa.API --launch-profile http
```

Create EF migration:

```bash
dotnet ef migrations add MigrationName --project Quraaa.Persistence --startup-project Quraaa.API
```

Apply EF migrations:

```bash
dotnet ef database update --project Quraaa.Persistence --startup-project Quraaa.API
```

Build Docker image:

```bash
docker build -t quraaa-api .
```

Run Docker image:

```bash
docker run -p 8080:8080 quraaa-api
```

## Agent Editing Rules

When working in this repository:

- Read relevant files before changing behavior.
- Keep the existing layered architecture.
- Prefer existing patterns: MediatR handlers, FluentValidation validators, `AppResult<T>`, repository/service interfaces.
- Do not edit `bin` or `obj`.
- Do not introduce domain dependencies on API, EF Core, Identity, SQL Server, or provider SDKs.
- Do not add unrelated refactors while implementing a feature.
- If changing schema, update EF configuration and create/check migrations.
- If changing API behavior, update controller response metadata where appropriate.
- If adding a handler, ensure its validator and DI dependencies are registered through the existing assembly scanning or DI extension methods.
- If adding JWT-protected endpoints, verify authentication configuration is actually wired; the package exists, but bearer authentication options are not currently configured in visible code.

## Current High-Value TODOs

These are known incomplete or risky areas based on the current code:

- Add login, refresh-token, and logout/revoke-token flows.
- Configure JWT bearer authentication middleware options.
- Add tests for registration validation and the registration handler.
- Decide whether `PasswordHash` should be duplicated in `UsersProfiles`; Identity already stores it in `AspNetUsers`.
- Align `ApplicationDbContextModelSnapshot.cs` with the current model if EF tooling reports drift.
- Normalize the `PhoneService` namespace.
- Rename `DeleationTime` only with a deliberate migration plan.
