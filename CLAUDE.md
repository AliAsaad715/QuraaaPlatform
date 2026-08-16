# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

QuraaaPlatform is a .NET 10 (`net10.0`) ASP.NET Core REST API for a book marketplace and library network: phone/OTP auth, physical + digital (ebook) listings, carts, Stripe checkout, seller fulfillment, and paid ebook delivery. PostgreSQL + EF Core, MediatR, FluentValidation, ASP.NET Core Identity, Cloudinary, Firebase, Stripe, Redis.

There is **no test project** in the solution — verification is `dotnet build` plus manual exercise through Swagger (`/docs`).

## Commands

```bash
# Build (solution or API project)
dotnet build QuraaaPlatform.slnx
dotnet build Quraaa.API/Quraaa.API.csproj

# Run — serves http://localhost:5153 and https://localhost:7260, Swagger at /docs
dotnet run --project Quraaa.API

# EF Core migrations (design-time factory: Quraaa.API/DesignTime/ApplicationDbContextFactory.cs)
dotnet ef migrations add <Name> --project Quraaa.Persistence --startup-project Quraaa.API
dotnet ef database update      --project Quraaa.Persistence --startup-project Quraaa.API
dotnet ef migrations remove    --project Quraaa.Persistence --startup-project Quraaa.API

# Docker (port 8080)
docker build -t quraaa-api .
docker run -p 8080:8080 --env-file Quraaa.API/.env quraaa-api
```

Running the API requires a reachable PostgreSQL (`ConnectionStrings:DefaultConnection`) and `JWT_SECRET_KEY`; [Program.cs](Quraaa.API/Program.cs) calls `db.Database.Migrate()` and then runs every seeder on **every startup**, so the app will not boot without a working database.

For manual testing via Swagger: the user seeder creates `+963912345678` / `User@12345`, and the admin seeder creates an account from `ADMIN_PHONE_NUMBER` / `ADMIN_PASSWORD`. Registration and regular login **only accept valid Syrian (`+963`) phone numbers** (`RegisterCommandValidator` / `LoginCommandValidator`) — test accounts must use `+963` numbers.

PR branch names are validated by [.github/workflows/check-branch-name.yml](.github/workflows/check-branch-name.yml) (`feature/123-description`, `fix/...`, `refactor/...`, or lowercase kebab-case). That is the only CI — no build/test workflow exists.

## Architecture

Clean Architecture boundaries with CQRS-style vertical slices. Reference direction (do not add edges):

```
API ──> Application, Persistence, Infrastructure
Persistence ──> Application, Domain
Infrastructure ──> Application
Application ──> Domain
Domain ──> (nothing)
```

Interfaces live in the Application layer (`Features/{Feature}/Interfaces/`); implementations live in Persistence (repositories, Identity) or Infrastructure (Cloudinary, Stripe, Firebase, Redis, Google Books, OpenAI). DI wiring is in four extension files: [ServiceCollectionExtensions.cs](Quraaa.API/Extensions/ServiceCollectionExtensions.cs), [ApplicationPackagesRegisterExtensions.cs](Quraaa.Application/Extensions/ApplicationPackagesRegisterExtensions.cs), [PersistenceDependencyInjectionHandler.cs](Quraaa.Persistence/Extensions/PersistenceDependencyInjectionHandler.cs), [InfrastructureDependencyInjectionHandler.cs](Quraaa.Infrastructure/Extensions/InfrastructureDependencyInjectionHandler.cs).

### Adding a feature

Slice layout under `Quraaa.Application/Features/{Feature}/`: `Commands/{Name}/{Name}Command.cs` + `{Name}CommandHandler.cs` + `{Name}CommandValidator.cs`, `Queries/{Name}/...` in the same triple, `Common/` for response DTOs and error-code constants, `Interfaces/` for abstractions. Then add a controller action in [Quraaa.API/Controllers/](Quraaa.API/Controllers/). MediatR handlers and FluentValidation validators are auto-registered by assembly scan — no manual registration.

### The two base classes that carry the request pipeline

**Handlers** inherit `BaseApplicationService<THandler>` ([BaseApplicationService.cs](Quraaa.Application/Shared/Services/BaseApplicationService.cs)) and wrap their body in `ExecuteAsync(request, async () => { ... })`. That wrapper resolves `IValidator<TRequest>` from the container, runs it, and translates thrown exceptions into result cases — so handlers signal failure by **throwing**, not by returning:

| Throw | Becomes | HTTP |
| --- | --- | --- |
| `NotFoundException` | `NotFound` | 404 |
| `DomainException` (aggregate invariant) | `DomainError` | 400 |
| `ApplicationBusinessException` (app rule; carries `PropertyName`) | `ValidationFailed` | 400 |
| `ConflictException` | `Conflict` | 409 |
| `UnauthenticatedException` | `Unauthorized` | 401 |
| `UnauthorizedAccessException` | `Forbidden` | 403 |

Anything else rethrows to middleware as a 500.

**Controllers** inherit `ApiClientController` ([ApiClientController.cs](Quraaa.API/Controllers/ApiClientController.cs)) and return `HandleResult(result)`, which matches the `AppResult` / `AppResult<T>` OneOf ([AppResult.cs](Quraaa.Application/Shared/Results/AppResult.cs)) onto status codes. Use the `HandleResult(result, onSuccess)` overload for non-200 successes. Read the caller identity with `TryGetCurrentUserId(out var userId)` / `TryGetCurrentSessionId(...)` — never re-derive claims inline. Routes are lowercased globally (`RouteOptions.LowercaseUrls = true`).

Domain error *codes* that must surface as 409 (e.g. `LibraryErrorCodes.DuplicateLibraryForUser`) are special-cased by string comparison inside `HandleResult`; a new one needs a branch there.

### Domain rules

`Entity` → `AuditableEntity` → `AggregateRoot`; value objects inherit `ValueObjectRoot`. Aggregates use a private ctor for EF Core plus a public static factory; invariants throw `DomainException`.

**Aggregates reference each other by scalar id only** (`UserId`, `BookId`, `ListingId`, `LibraryId`) — no cross-aggregate navigation properties in the domain. Existing EF configurations do use navigationless `HasOne<TAggregate>()` to create database foreign keys; treat those purely as DB integrity mappings and follow the same convention rather than introducing navigations.

Enums are stored as `int` in PostgreSQL but serialized as strings in JSON (`JsonStringEnumConverter`). Roles are `User | Admin | LibraryOwner` ([Role.cs](Quraaa.Domain/User/Enums/Role.cs)) — `[Authorize(Roles = ...)]` strings must match these; there is no `LibraryAdmin` role.

### Uniqueness invariants enforced in the database

These are partial/unique indexes, and the repositories translate the resulting Npgsql violations into `409 Conflict` — check for a pre-existing row *and* handle the race:

- One library per user (unique index on `Libraries.UserId`), plus unique library email.
- One open cart per user: partial unique `IX_Carts_UserId_Open` over non-deleted `Active`/`PendingPayment` carts; historical `Paid`/`Abandoned` carts are unconstrained.
- Stripe webhook idempotency via the `ProcessedPaymentEvents` inbox.

[ApplicationDbContext](Quraaa.Persistence/Data/ApplicationDbContext.cs) also applies a **global query filter** hiding inactive categories (`c.IsActive == true`) — use `IgnoreQueryFilters()` when admin code must see them.

### Auth model

JWT access token + opaque 64-byte refresh token stored only as a SHA-256 hash. Each access token carries a refresh-family id in a session claim; `OnTokenValidated` in [ServiceCollectionExtensions.cs](Quraaa.API/Extensions/ServiceCollectionExtensions.cs) rejects the token if its `jti` is in the distributed revocation cache **or** the family is no longer the active one on the Identity row. Consequence: logout, refresh replay detection, a newer login, and password change all invalidate every outstanding access token for that user. Each user has exactly one active family — there is no per-device session model. Any code path that grants a privileged role must clear the refresh token and family id in the same Identity update.

### Files and storage

`IUploadedFile` ([Quraaa.Application/Shared/Files/](Quraaa.Application/Shared/Files/)) keeps `IFormFile` out of the Application layer; the API adapter is `FormFileUploadedFile`. Images go through `IImageStorageService` → `CloudinaryImageStorageService`; PDFs/Word through `IFileStorageService` → `CloudinaryFileStorageService`. **Never write durable uploads to `wwwroot` or the local filesystem** — the deploy target has an ephemeral disk. Ebook storage paths are omitted from public responses; paid buyers read through the authenticated inline purchase-stream route, and no attachment-download route is exposed.

### Seller payouts (Stripe Connect)

Library owners connect a Stripe wallet themselves via Stripe-hosted onboarding (`LibraryStripeOnboardingService`: platform creates an Express account, owner is redirected to Stripe, `SyncStatusAsync` activates it on return) — during registration (`register/stripe/onboarding|sync`, registration-token auth, stage `StripeWalletSetup`) or later from the owner dashboard (`api/library-admin/wallet/onboarding[/complete]`, `dashboard-link`, `PUT` attach-by-id, `DELETE`). Wallet state lives on `Libraries.StripeConnectAccountId` + `StripeWalletActivatedAtUtc`; only an *active* wallet receives transfers (the processor re-checks Stripe and self-activates). Admins never touch wallets — they set each library's `ProfitSharePercent` (`PUT api/libraries/{id}/profit-share`, default `LibraryAggregate.DefaultProfitSharePercent`). When an order is paid, `OrderPaymentFinalizationService.StageSellerPayoutsAsync` writes one `SellerPayout` row per library seller **in the same transaction** as the paid transition (transactional outbox; unique on `(OrderId, LibraryId)`), snapshotting the library's percentage; `SellerPayoutProcessingService` (woken via `ISellerPayoutDispatchSignal` right after commit, else every minute) creates the Stripe Transfer. Invariants that must survive edits: money never moves inside the finalization transaction; the transfer idempotency key is `seller-payout:{id}:{AttemptCount}` and **only** `RecordDefinitiveRejection` (4xx with a Stripe error body) increments `AttemptCount` — indeterminate outcomes (timeouts/5xx) must replay the same key; a processing lease is saved before Stripe is called so replicas can't double-transfer; payouts older than 23 h reconcile against Stripe (`FindTransferForPayoutAsync`) before creating.

## Configuration

[Program.cs](Quraaa.API/Program.cs) calls `DotNetEnv.Env.Load()` (repo root) and then `Quraaa.API/.env` if present, before `CreateBuilder`; `AddEnvironmentVariables()` runs again after, so environment variables win over `appsettings.json`. Use `__` for nested keys in deployment.

Keys that gate startup or whole features: `ConnectionStrings:DefaultConnection`, `JWT_SECRET_KEY` (required), `ADMIN_PHONE_NUMBER` / `ADMIN_PASSWORD` (the admin seeder resets the seeded admin password to this on every boot), `REDIS_URL` / `Otp:AllowInMemoryCacheInProduction`, `OTP_DEVICE_TOKEN` plus Firebase credentials (`FIREBASE_CREDENTIALS_JSON` is materialized to `storage/firebase/quraa.json` at startup), Stripe secret/webhook keys, Cloudinary credentials, `Cors:AllowedOrigins` (empty ⇒ `AllowAnyOrigin`).

Committed defaults that are test-only and must be overridden in production: `Notifications:AllowTestEndpoint=true`, `Otp:AllowInMemoryCacheInProduction=true`, `Stripe:IsTestMode=true`.

## Reference docs

[AGENTS.md](AGENTS.md) (~2500 lines) is the deep reference: full endpoint list, per-flow step-by-step behavior (registration, login/refresh/logout, OTP, checkout, fulfillment), migration history, and a known-gaps list. [README.md](README.md) covers setup and the endpoint table. Both **lag the code in places** — e.g. the AGENTS.md gap list still claims there are no rating endpoints and that library listing routes require `LibraryAdmin`, but `RatingsController`, `CommentsController`, and `[Authorize(Roles = "LibraryOwner")]` on `LibraryListingsController` all exist. Verify against source before relying on either.
