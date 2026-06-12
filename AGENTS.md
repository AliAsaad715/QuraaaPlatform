# QuraaaPlatform Agent Context

This file is written for AI agents, coding assistants, and chatbots that need a fast, accurate working model of this repository. It describes the current codebase as it exists now, not a future intended architecture.

For a more beginner-friendly walkthrough, also read `PROJECT_GUIDE.md`.

## Project Snapshot

`QuraaaPlatform` is a layered ASP.NET Core Web API solution targeting `.NET 10.0`.

Current implemented business capability:

- User registration through `POST /api/Auth/register`.
- Authenticated password reset through `POST /api/Auth/reset-password`.
- Library registration through `POST /api/Library/register`.
- User security identity is stored through ASP.NET Core Identity.
- User profile/business data is stored as a domain aggregate in `UsersProfiles`.
- Library data is stored as a domain aggregate in `Libraries`, linked to a user profile by `UserId`, and created with approval status `Pending`.
- Registration returns JWT access and refresh tokens.
- Password reset is JWT-protected and derives the user `UserId` from the access token.
- Library registration is JWT-protected and derives the library owner `UserId` from the access token.

Core technologies:

- ASP.NET Core Web API
- Entity Framework Core with PostgreSQL through Npgsql
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

Do not put HTTP, EF Core, PostgreSQL/Npgsql, Identity, Swagger, or external provider SDK logic in `Quraaa.Domain`.

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

`Quraaa.API/appsettings.Development.json` contains the local PostgreSQL connection string:

```text
ConnectionStrings:DefaultConnection = Host=localhost;Database=QuraaaDb;Username=postgres;Password=<local password>
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

Current endpoints:

```text
POST /api/Auth/register
POST /api/Auth/reset-password
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

Successful registration response is `AuthResponse`:

```json
{
  "userId": "guid",
  "accessToken": "jwt",
  "refreshToken": "secure-random-base64",
  "accessTokenExpiration": "utc-date-time"
}
```

Password reset request body maps to `ResetPasswordRequest`; the controller creates `ResetPasswordCommand` after reading `UserId` from the authenticated JWT:

```json
{
  "oldPassword": "oldPass123",
  "newPassword": "newPass123"
}
```

Successful password reset response comes from `HandleResult(AppResult)`:

```json
{
  "message": "Operation successful."
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
Quraaa.API/Requests/Files/FormFileUploadedFile.cs
Quraaa.API/Services/LibraryImageStorageService.cs
Quraaa.Application/Features/Libraries/Commands/RegisterLibrary/RegisterLibraryCommand.cs
Quraaa.Application/Features/Libraries/Commands/RegisterLibrary/RegisterLibraryCommandValidator.cs
Quraaa.Application/Features/Libraries/Commands/RegisterLibrary/RegisterLibraryCommandHandler.cs
Quraaa.Application/Features/Libraries/Common/LibraryResponse.cs
Quraaa.Application/Features/Libraries/Interfaces/ILibraryImageStorageService.cs
Quraaa.Application/Features/Libraries/Interfaces/ILibraryRepository.cs
Quraaa.Application/Shared/Files/IUploadedFile.cs
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

The request no longer accepts `userId`. `LibraryController` reads the user id from JWT claims (`ClaimTypes.NameIdentifier`, `nameid`, or `sub`) and sends that value to the application command.

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
  -> ILibraryImageStorageService.SaveAsync(...) stores images in wwwroot/uploads/libraries
  -> new LibraryAggregate(...)
  -> ILibraryRepository.AddLibraryAsync(library)
  -> ILibraryRepository.SaveChangesAsync()
  -> handler deletes stored image files if persistence fails
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

## Password Reset Flow

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

Request body maps to `ResetPasswordRequest`:

```json
{
  "oldPassword": "oldPass123",
  "newPassword": "newPass123"
}
```

The request does not accept `userId`. `AuthController` reads the user id from JWT claims (`ClaimTypes.NameIdentifier`, `nameid`, or `sub`) and sends that value to the application command.

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
- `UpdatePasswordHash(string passwordHash, Guid modifiedBy)` updates the profile copy of the Identity password hash and audit metadata after a successful Identity password change.
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
Id uuid not null primary key
LibraryName character varying(100) not null
Location character varying(250) not null
LibraryImage character varying(500) not null
HeaderImage character varying(500) not null
Email character varying(256) not null
UserId uuid not null FK to UsersProfiles.Id
ApprovalStatus integer not null
CreationTime timestamp with time zone not null
LastModificationTime timestamp with time zone null
LastModifiedBy uuid null
IsDeleted boolean not null
DeleationTime timestamp with time zone null
DeletedBy uuid null
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
- `Interests` is serialized to JSON text in a `character varying(500)` column.
- `PaymentMethodInfo` is owned by `UserAggregate` and stored in the same `UsersProfiles` table.

Custom profile columns from current migrations/mapping:

```text
Id uuid not null primary key / FK to AspNetUsers.Id
FirstName character varying(50) not null
LastName character varying(50) not null
PhoneNumber character varying(20) not null
PasswordHash text not null
Gender integer not null
Role integer not null
DateOfBirth date not null
ProfileImageUrl text null
LastLoginDate timestamp with time zone null
PreviousLoginDate timestamp with time zone null
Interests character varying(500) not null
PaymentCustomerId character varying(100) null
PaymentCardBrand character varying(20) null
PaymentLastFourDigits character varying(4) null
CreationTime timestamp with time zone not null
LastModificationTime timestamp with time zone null
LastModifiedBy uuid null
IsDeleted boolean not null
DeleationTime timestamp with time zone null
DeletedBy uuid null
```

Current migrations:

```text
20260608185002_InitialPostgresCreate
20260608221526_AddLibraries
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

`UserRepository` implements add, lookup by ID, lookup by phone number, and save changes. Lookup methods return `null` when no active user profile is found. Handlers that require a user profile should check for `null` and throw `NotFoundException` themselves so application behavior is explicit at the use-case boundary.

`LibraryRepository` implements add and save changes for `LibraryAggregate`.

`IdentityService` currently implements:

```text
IsPhoneNumberUniqueAsync(string phoneNumber)
CreateUserIdentityAsync(Guid id, string phoneNumber, string password)
ChangePasswordAsync(Guid userId, string oldPassword, string newPassword)
GenerateAuthTokensAsync(Guid userId, string phoneNumber)
```

Phone uniqueness uses `UserManager.FindByNameAsync(phoneNumber)`, so it checks the Identity username, not `UsersProfiles.PhoneNumber` directly.

Password changes use `UserManager.ChangePasswordAsync`, so the old password check and configured ASP.NET Identity password rules are enforced by Identity. After a successful change, the handler updates `UsersProfiles.PasswordHash` through `UserAggregate.UpdatePasswordHash(...)`.

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

Currently registers API-owned application integrations:

- JWT bearer authentication and authorization using `JWT_SECRET_KEY`, `JWT_ISSUER`, and `JWT_AUDIENCE`.
- `ILibraryImageStorageService -> LibraryImageStorageService`.

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

- adds `ApplicationDbContext` with PostgreSQL through `UseNpgsql`
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
- Do not introduce domain dependencies on API, EF Core, Identity, PostgreSQL/Npgsql, or provider SDKs.
- Do not add unrelated refactors while implementing a feature.
- If changing schema, update EF configuration and create/check migrations.
- If changing API behavior, update controller response metadata where appropriate.
- If adding a handler, ensure its validator and DI dependencies are registered through the existing assembly scanning or DI extension methods.
- If adding JWT-protected endpoints, use `[Authorize]` and the configured JWT bearer authentication in `Quraaa.API/Extensions/ServiceCollectionExtensions.cs`.

## Current High-Value TODOs

These are known incomplete or risky areas based on the current code:

- Add admin review endpoints to approve/reject pending libraries.
- Ensure public library listing/search endpoints only expose approved libraries.
- Add login, refresh-token, and logout/revoke-token flows.
- Add tests for registration validation and the registration handler.
- Decide whether `PasswordHash` should be duplicated in `UsersProfiles`; Identity already stores it in `AspNetUsers`.
- Align `ApplicationDbContextModelSnapshot.cs` with the current model if EF tooling reports drift.
- Normalize the `PhoneService` namespace.
- Rename `DeleationTime` only with a deliberate migration plan.
