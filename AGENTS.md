# QuraaaPlatform Agent Context

This file is written for AI agents, coding assistants, and chatbots that need a fast, accurate working model of this repository. It describes the current codebase as it exists now, not a future intended architecture.

Last audited against the repository: **2026-08-15**.

## Project Overview

`QuraaaPlatform` is a layered ASP.NET Core Web API solution targeting **.NET 10.0**. It is the backend for a book marketplace / library platform. The architecture follows a Clean Architecture / vertical-slice hybrid with five projects.

Current implemented business capabilities:

- Pending user registration starts through `POST /api/auth/register`.
- Registration phone verification completes through `POST /api/auth/register/verify`.
- User login through `POST /api/auth/login`; valid credentials for an unverified pending registration resend the registration OTP and return the same pending-verification response as `POST /api/auth/register`.
- Admin login uses a password-plus-OTP flow through `POST /api/auth/admin/login` and `POST /api/auth/admin/login/verify`.
- Approved library owners can log in with their library email and Identity password through `POST /api/auth/library/login`.
- Users, admins, and library owners share refresh-token-authenticated logout through `POST /api/auth/logout`; any unexpired token from the active rotation family revokes all descendant refresh and access tokens.
- Access/refresh token pairs are rotated through `POST /api/auth/refresh`; consumed-token history detects replay and revokes the active family, while expired, revoked, or invalid tokens return `401 Unauthorized`.
- Authenticated password reset through `POST /api/auth/reset-password`; successful password changes revoke the account's refresh-token family and its access JWTs.
- Unauthenticated forgot-password OTP send through `POST /api/auth/forgot-password`.
- Unauthenticated forgot-password OTP verification and password reset through `POST /api/auth/forgot-password/verify`.
- Authenticated profile retrieval through `GET /api/profile/me`.
- Authenticated profile update through `PUT /api/profile/me`.
- Authenticated named saved-location list/create/edit/default/delete through `/api/profile/locations`.
- Authenticated mobile library-registration link issuance through `POST /api/libraries/register`.
- Token-authenticated dashboard registration context/detail submission and SMTP email-OTP verification through `/api/libraries/register/*`; only verified applications enter the admin queue.
- Admin approval atomically promotes the owner and enqueues durable delivery of an approval email plus an FCM push to the owner's registered mobile devices.
- Public, searchable library listing through `GET /api/libraries`.
- Paged/searchable/sortable active physical-book listing by library through `GET /api/libraries/{libraryId}/books`.
- Public ebook listing through `GET /api/ebooks`.
- Public most-popular book discovery through `GET /api/books/most-popular`.
- Interest/category/language-based authenticated recommendations through `GET /api/books/recommended`.
- Authenticated favorite-book list/add/remove through `/api/favorite-books`.
- User physical-book listing creation and current-user listing retrieval through `/api/listings`.
- Library inventory listing create/update/detail operations through `/api/library-admin/listings`, including digital-asset replacement with a 110 MB multipart transport allowance for the validated 100 MB PDF limit.
- User cart retrieval/mutation through `/api/cart`, with one open cart per user, Stripe-compatible line/quantity limits, and cumulative physical-stock validation.
- Authenticated checkout context through `GET /api/orders/checkout-context`; physical and mixed carts return default-first owned saved-location choices, while empty and digital-only carts require no shipping location.
- Order-driven Stripe Checkout through `POST /api/orders`: the order, cart lock, physical-stock reservations, and payment attempt are persisted before Stripe is called.
- Buyer order listing/detail, shipping update, checkout recovery, cancellation, and archive through `/api/orders`.
- Seller paid-physical-item queues and processing/fulfillment transitions through `/api/seller/orders`.
- Stripe webhook processing through `POST /api/payments/stripe/webhook`; paid events and authoritative expired-attempt reconciliation share order/cart/purchase finalization, while confirmed failure/expiry paths release reservations and reopen the cart.
- Authenticated user buy/sell history through `/api/purchases/me/buy-history` and `/api/purchases/me/sell-history`.
- Paid ebook reading through authenticated inline streaming at `GET /api/purchases/{purchaseId}/stream`; no order-item attachment-download route is exposed.
- Category management through `GET /api/categories`, `GET /api/categories/{categoryId}`, and `POST /api/categories` (admin-only).
- Admin-only author creation/detail/update plus paged moderation, activation/reactivation, and guarded single/bulk permanent deletion through `/api/admin/authors`.
- Public author profiles and paginated available works through `GET /api/authors/{authorId}` and `GET /api/authors/{authorId}/books`.
- Standalone OTP send through `POST /api/otp/send`.
- Standalone OTP verification through `POST /api/otp/verify`.
- Authenticated FCM device-token registration/removal through `PUT` and `DELETE /api/notifications/devices`; deprecated `POST /api/notifications/device-token` remains a registration alias backed by the same `PushDevices` store.
- Library listing publication and digital-asset-update pushes are committed to a durable outbox with the listing change and delivered by a retrying background worker; publications committed together for one library are coalesced into one notification.
- Authenticated push notification dispatch through `POST /api/notifications/send`.
- Development/test push notification dispatch through `POST /api/notifications/test`.

Domain aggregates already modeled but only partially exposed via HTTP:

- `UserAggregate` — profile/business data for authenticated users.
- `LibraryAggregate` — library profiles with pending/approved/rejected status.
- `CategoryAggregate` — book interest categories, seeded at startup.
- `BookAggregate` — catalog books; `AuthorId` is an optional scalar FK to `AuthorAggregate`.
- `ListingAggregate` — marketplace listings for physical or digital books.
- `FavoriteBookAggregate` — one active favorite per user/book pair.
- `BookRatingAggregate` — one 1–5 rating per user/book pair; modeled and used by popularity queries but not exposed by a rating endpoint.
- `BookPurchaseAggregate` — immutable purchase facts created for paid orders, correlated by optional `OrderId`/`OrderItemId`, and used by popularity/history queries.
- `CartAggregate` with owned `CartItem` entities — active/pending-payment/paid cart state, pending-order correlation, and Stripe identifiers.
- `OrderAggregate` with child `OrderItem` and `PaymentAttempt` entities — immutable checkout snapshots, payment lifecycle, cancellation/expiry, digital fulfillment, and physical seller fulfillment.

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
- Stripe.net for Checkout session creation and signed webhook parsing
- MailKit for SMTP/STARTTLS library email verification and approval messages
- CloudinaryDotNet for durable public images and authenticated private PDF/Word assets
- Typed `HttpClient` integration with Google Books for ISBN metadata lookup
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
    AdminAuthorsController.cs
    AdminModerationController.cs
    ApiClientController.cs
    AuthController.cs
    BooksController.cs
    CartController.cs
    CategoriesController.cs
    EbooksController.cs
    FavoriteBooksController.cs
    LibrariesController.cs
    LibraryListingsController.cs
    NotificationsController.cs
    OtpController.cs
    OrdersController.cs
    PaymentsController.cs
    ProfileController.cs
    PurchaseHistoryController.cs
    SellerOrdersController.cs
    UserListingsController.cs
  DesignTime/
    ApplicationDbContextFactory.cs
  Extensions/
    DatabaseExtensions.cs
    ServiceCollectionExtensions.cs
    SwaggerExtensions.cs
  Requests/
    Admin/
    Authentication/
    Books/
    FavoriteBooks/
    Files/
    Libraries/
    Listings/
    Notifications/
    Otp/
    Orders/
    Profiles/
    Purchases/
  Services/
    ExpiredOrderPaymentReconciliationService.cs
    LibraryImageStorageService.cs
  storage/firebase/       # Firebase service-account JSON files (ignored by git)
  storage/books/          # Legacy/seeded private PDFs copied into build/publish output
  wwwroot/                # Immutable public assets shipped with the application

Quraaa.Application/
  Quraaa.Application.csproj
  Extensions/
    ApplicationPackagesRegisterExtensions.cs
  Features/
    Admin/
      Commands/DeleteAuthors/
      Commands/SetAuthorActivation/
      Queries/GetAuthors/
      Common/
      Interfaces/
    Authentication/
      Commands/AdminLogin/
      Commands/LibraryOwnerLogin/
      Commands/Register/
      Commands/VerifyRegisterOtp/
      Commands/VerifyAdminLoginOtp/
      Commands/Login/
      Commands/ResetPassword/
      Commands/ForgotPassword/
      Commands/ResetForgotPassword/
      Common/
      Interfaces/
      Helpers/
    Authors/
      Commands/CreateAuthor/
      Commands/UpdateAuthor/
      Commands/DeleteAuthor/
      Queries/GetAuthorById/
      Queries/GetAuthorsPaginated/
      Common/
      Interfaces/
    Books/
      Queries/GetMostPopularBooks/
      Queries/GetRecommendedBooks/
      Common/
      Interfaces/
    Carts/
      Commands/AddCartItem/
      Commands/UpdateCartItemQuantity/
      Commands/RemoveCartItem/
      Commands/ClearCart/
      Commands/CreateCheckoutSession/
      Commands/ProcessStripeWebhook/
      Queries/GetMyCart/
      Common/
      Interfaces/
    Categories/
      Commands/CreateCategory/
      Queries/GetAllCategories/
      Queries/GetCategoryById/
      Common/
      Interfaces/
    Ebooks/
      Queries/GetEbooks/
      Common/
      Interfaces/
    FavoriteBooks/
      Commands/AddFavoriteBook/
      Commands/RemoveFavoriteBook/
      Queries/GetFavoriteBooks/
      Common/
      Interfaces/
    Libraries/
      Commands/RegisterLibrary/
      Queries/GetLibraries/
      Common/
      Interfaces/
    Listings/
      Commands/AddPhysicalBook/
      Commands/AddUserPhysicalBook/
      Commands/UpdateListing/
      Queries/GetLibraryBooks/
      Queries/GetListingById/
      Queries/GetMyListings/
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
    Orders/
      Commands/
      Queries/
      Common/
      Interfaces/
      Services/
    Payments/
      Commands/ProcessPaymentWebhook/
      Common/
      Exceptions/
      Interfaces/
    Profiles/
      Commands/CreateLocation/
      Commands/UpdateLocation/
      Commands/SetDefaultLocation/
      Commands/DeleteLocation/
      Commands/UpdateProfile/
      Queries/GetMyLocations/
      Queries/GetMyProfile/
      Common/
    Purchases/
      Queries/GetBuyHistory/
      Queries/GetSellHistory/
      Interfaces/
  Shared/
    Exceptions/
    Files/
    Requests/
    Results/
    Services/

Quraaa.Domain/
  Quraaa.Domain.csproj
  Author/
    AuthorAggregate.cs
  Catalog/
    BookAggregate.cs
  Cart/
    CartAggregate.cs
    Entities/CartItem.cs
    Enums/CartStatus.cs
  Category/
    CategoryAggregate.cs
  Favorites/
    FavoriteBookAggregate.cs
  Library/
    LibraryAggregate.cs
    Enums/LibraryApprovalStatus.cs
  Marketplace/
    ListingAggregate.cs
    Enums/BookCondition.cs
    Enums/ListingFormat.cs
    Enums/ListingStatus.cs
    Enums/SellerType.cs
  Orders/
    OrderAggregate.cs
    Entities/OrderItem.cs
    Entities/PaymentAttempt.cs
    Enums/
  Purchases/
    BookPurchaseAggregate.cs
  Ratings/
    BookRatingAggregate.cs
  Shared/
    Entities/
    Errors/
    Exceptions/
  User/
    UserAggregate.cs
    Entities/Interest.cs
    Entities/UserLocation.cs
    Enums/Gender.cs
    Enums/Role.cs
    ValueObjects/GeoLocation.cs
    ValueObjects/PaymentMethodInfo.cs

Quraaa.Persistence/
  Quraaa.Persistence.csproj
  Configurations/
    ApplicationUserConfiguration.cs
    AuthorConfiguration.cs
    ConsumedRefreshTokenConfiguration.cs
    OrderConfiguration.cs
    OrderItemConfiguration.cs
    OrderPaymentAttemptConfiguration.cs
    ProcessedPaymentEventConfiguration.cs
  Data/
    ApplicationDbContext.cs
    ApplicationUser.cs
    ConsumedRefreshToken.cs
    ProcessedPaymentEvent.cs
  Extensions/
    PersistenceDependencyInjectionHandler.cs
  Migrations/
  Repositories/
    AdminModerationRepository.cs
    AuthorRepository.cs
    OrderRepository.cs
    PaymentEventInboxRepository.cs
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
    AccessTokenRevocationService.cs
    StripePaymentService.cs
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
| `CloudinaryDotNet`                                  | 1.29.2  | `Quraaa.Infrastructure`                    |
| `FirebaseAdmin`                                     | 3.5.0   | `Quraaa.Infrastructure`                    |
| `Microsoft.Extensions.Caching.Abstractions`         | 10.0.9  | `Quraaa.Infrastructure`                    |
| `Microsoft.Extensions.Caching.Memory`               | 10.0.8  | `Quraaa.Infrastructure`                    |
| `Microsoft.Extensions.Caching.StackExchangeRedis`   | 10.0.8  | `Quraaa.Infrastructure`                    |
| `Microsoft.Extensions.Http`                         | 10.0.9  | `Quraaa.Infrastructure`                    |
| `Stripe.net`                                        | 52.1.0  | `Quraaa.Infrastructure`                    |
| `MailKit`                                           | 4.17.0  | `Quraaa.Infrastructure`                    |
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
- `Quraaa.Infrastructure`: external provider implementations such as Firebase FCM, Redis caching, Stripe payments, and Google Books metadata lookup.
- `Quraaa.API`: HTTP controllers, startup, middleware, Swagger, environment configuration.

Across aggregate boundaries, domain types expose scalar identity references such as `UserId`, `BookId`, `ListingId`, and `LibraryId`; do not add aggregate navigation properties or make business logic depend on tracked cross-aggregate graphs. The current Persistence mappings do use navigationless `HasOne<TAggregate>()` calls to create database foreign keys for books/listings, books/authors, libraries/users, favorites, purchases, and ratings. Treat those as database integrity mappings only.

The user-to-library ownership rule is one-to-one: `LibraryAggregate` stores only scalar `UserId`; `LibraryConfiguration` uses a navigationless one-to-one mapping plus a unique index on `UserId`; the migration enforces the unique index; and application code checks for an existing library before creating another one. Library email is also unique.

The open-cart rule is also one-to-one per user. `CartConfiguration` defines the partial unique index `IX_Carts_UserId_Open` on `UserId` for non-deleted `Active` or `PendingPayment` carts, `CartRepository` translates a concurrent unique-index violation to `409 Conflict`, and historical `Paid`/`Abandoned` carts remain unrestricted.

## Build, Run & Test Commands

### Prerequisites

- .NET 10 SDK
- PostgreSQL server (local or remote)
- Redis (optional; in-memory cache is allowed in Development via configuration)
- Firebase service-account credentials (for FCM features; optional for basic HTTP testing)
- Stripe secret/webhook keys for checkout and payment completion testing
- Google Books API access for ISBNs that are not already present in the local catalog

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

`Quraaa.API.IntegrationTests` is an xUnit API-host project, currently omitted
from `QuraaaPlatform.slnx`; run its `.csproj` explicitly. The `Testing`
environment skips startup migration/seeding so those host tests do not require
a real PostgreSQL instance. The current project does not cover the PostgreSQL
demo seed pipeline, payment providers, SMTP, or Firebase end to end.

## Configuration & Secrets

Startup calls `DotNetEnv.Env.Load()` before creating the builder, then also loads `Quraaa.API/.env` when that file exists. `WebApplication.CreateBuilder` loads the normal ASP.NET Core sources (`appsettings.json`, environment-specific appsettings, environment variables, and command-line arguments), and `AddEnvironmentVariables()` is called again. Environment variables therefore override appsettings values; use double underscores for nested keys in deployment environments.

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
| Cloudinary    | `CLOUDINARY_CLOUD_NAME`, `CLOUDINARY_API_KEY`, `CLOUDINARY_API_SECRET`, optional `CLOUDINARY_PRIVATE_DOWNLOAD_TTL_SECONDS`                         |
| Stripe        | `Stripe:SecretKey`, `Stripe:WebhookSecret`, `Stripe:Currency`, `Stripe:IsTestMode`                                                            |
| Google Books  | `GoogleBooks:ApiKey`, `GoogleBooks:BaseUrl` (defaults to `https://www.googleapis.com/`)                                                       |
| Swagger       | `Swagger:ServerUrl`                                                                                                                           |
| Reverse proxy | `ForwardedHeaders:KnownProxies`, `ForwardedHeaders:KnownNetworks`                                                                             |
| Library link  | `LIBRARY_DASHBOARD_REGISTER_URL`                                                                                                            |
| Email OTP     | `LIBRARY_EMAIL_OTP_PEPPER`                                                                                                                  |
| SMTP          | `MAIL_MAILER`, `MAIL_HOST`, `MAIL_PORT`, `MAIL_USERNAME`, `MAIL_PASSWORD`, `MAIL_ENCRYPTION`, `MAIL_FROM_ADDRESS`, `MAIL_FROM_NAME`          |

`JWT_SECRET_KEY` is required by `IdentityService.GenerateAuthTokensAsync` and by `ServiceCollectionExtensions.AddJwtAuthentication`. If it is missing, the application throws `InvalidOperationException` at startup.
`JWT_DURATION_IN_MINUTES` defaults to `60` and is validated at startup as an invariant finite number greater than zero and no greater than `10080` (seven days).

Firebase Admin credential resolution order:

1. `GOOGLE_APPLICATION_CREDENTIALS` environment variable
2. `Firebase:CredentialsPath` config value
3. Application default credentials

Additionally, `Program.cs` supports `FIREBASE_CREDENTIALS_JSON`: it validates the value as JSON, writes it to `Quraaa.API/storage/firebase/quraa.json`, and sets `GOOGLE_APPLICATION_CREDENTIALS` plus `FIREBASE_CREDENTIALS` to that generated path before Infrastructure initializes Firebase.

OTP cache configuration:

- Redis is preferred when any of `ConnectionStrings:Redis`, `Redis:ConnectionString`, `REDIS_URL`, or `REDIS_TLS_URL` is configured.
- Heroku-style `redis://...` and `rediss://...` URLs are supported.
- Base `Quraaa.API/appsettings.json` currently sets `Otp:AllowInMemoryCacheInProduction=true`, so a production instance without Redis falls back to process-local memory.
- Production should configure Redis and explicitly override `Otp:AllowInMemoryCacheInProduction=false`; with that setting, startup fails when Redis is missing. The in-memory fallback is suitable only for temporary testing because OTPs are lost on restart and are not shared across multiple instances.

`OTP_DEVICE_TOKEN` is the FCM registration token for the secondary Android SMS gateway app that has SMS permission. It is server-side configuration and is not accepted in the `POST /api/otp/send` request body.

Cloudinary configuration is validated at startup. Library logos, library headers, and bulk-uploaded book covers are public image assets under `quraa/libraries/logos`, `quraa/libraries/headers`, and `quraa/books/covers`; the existing database columns store their absolute HTTPS delivery URLs. Newly uploaded PDFs and Word documents are authenticated `raw` assets under `quraa/books/files/pdf` and `quraa/books/files/docs`. Document columns store opaque `cloudinary://raw/authenticated/...` references, never public/signed URLs. The authenticated purchase-stream route generates a short-lived signed URL internally and proxies the bytes inline, including range requests. `CLOUDINARY_PRIVATE_DOWNLOAD_TTL_SECONDS` defaults to `300` and must be 60–900. Existing `books/...` and legacy `uploads/books/...` records remain readable from the packaged private root, but new document writes never use the dyno filesystem.

Stripe/Google Books notes:

- Infrastructure creates an injected `StripeClient` from `Stripe:SecretKey`; it does not set Stripe's global API key. `StripePaymentService` verifies webhook signatures with `Stripe:WebhookSecret`.
- Startup requires a secret key matching `Stripe:IsTestMode` (`sk_test_` or `sk_live_`), a `whsec_` webhook secret, and `Stripe:Currency=usd`. Order payments currently support USD only.
- `.env.example` contains non-secret placeholders for the required Stripe settings.
- `GoogleBooksService` removes hyphens from ISBNs, calls `books/v1/volumes?q=isbn:{isbn}`, and returns `null` on provider/network/deserialization failures.
- Neither Stripe nor Google Books keys are present in the committed appsettings files; provide them through secret configuration.

Secrets handling:

- `.env` is listed in `.gitignore` and must not be committed.
- Firebase service-account JSON files under `Quraaa.API/storage/firebase/*.json` are `.gitignore`d and must not be committed.
- Cloudinary, Stripe, Google Books, PostgreSQL, Redis, JWT, admin, Firebase, SMTP, and library-email OTP pepper credentials must remain outside committed configuration.

## Database & Migrations

### DbContext

`Quraaa.Persistence/Data/ApplicationDbContext.cs` inherits `IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>` and configures Npgsql PostgreSQL.

DbSets:

- `Authors` (`AuthorAggregate`)
- `UsersProfiles` (`UserAggregate`)
- `UserLocations` (`UserLocation`; named profile locations with one profile-selected default)
- `PushDevices` (`PushDevice`; user-owned FCM registrations keyed by a unique SHA-256 token hash)
- `ListingPushNotifications` (`ListingPushNotification`; leased/retried durable outbox for coalesced listing publication and digital-asset-update pushes)
- `Libraries` (`LibraryAggregate`)
- `LibraryApprovalNotifications` (`LibraryApprovalNotification`; per-channel durable approval-delivery outbox)
- `Books` (`BookAggregate`)
- `Listings` (`ListingAggregate`)
- `Categories` (`CategoryAggregate`)
- `FavoriteBooks` (`FavoriteBookAggregate`)
- `BookPurchases` (`BookPurchaseAggregate`)
- `BookRatings` (`BookRatingAggregate`)
- `Carts` (`CartAggregate`; `CartItem` is mapped as a child entity)
- `Orders` (`OrderAggregate`; `OrderItem` and `PaymentAttempt` are mapped as child entities)
- `ProcessedPaymentEvents` (`ProcessedPaymentEvent`; the Stripe webhook idempotency inbox)
- `ConsumedRefreshTokens` (`ConsumedRefreshToken`; hashed, rotated-token history used for family logout and replay detection)
- `LibraryRegistrationSessions` (`LibraryRegistrationSession`; hashed temporary dashboard credentials bound to the issuing refresh family)
- `LibraryEmailVerificationChallenges` (`LibraryEmailVerificationChallenge`; durable HMAC-hashed email OTP, cooldown, attempt, and lockout state)

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
7. `20260703183738_MakeNullabeBookCatgoryId`
8. `20260705153710_AddFavoriteBooks`
9. `20260705161224_AddBookPopularityMetrics`
10. `20260706133527_FixLibraryEmailAndEngagementConstraints`
11. `20260706150706_MergeModelSnapshot`
12. `20260716101943_AddCartsAndCartItemsTables`
13. `20260724134444_AddLocationToUsers`
14. `20260726145336_AddOrdersAndPaymentTracking`
15. `20260731131240_EnforceSingleOpenCartPerUser`
16. `20260801123016_AddRefreshTokenFamiliesAndIndexes`
17. `20260806151215_AddLibraryMagicLinkEmailVerification`
18. `20260806191318_AddFileRetentionAndDigitalAssetIndexes`
19. `20260807120954_AddBookCanonicalFilesAndRenameListingDigitalAsset`
20. `20260809202025_AddMultipleUserLocations`
21. `20260810134114_PreventUserLocationOwnerReassignment`
22. `20260813192304_AddComments`
23. `20260814201322_AddUserDeviceTokens`
24. `20260815083731_AddAuthorsTable`
25. `20260815092204_RefactorBookAuthorToForeignKeyAndAddBirthDate`
22. `20260813192304_AddComments`
23. `20260814173751_AddPushDevicesAndLibraryApprovalNotifications`
24. `20260814201322_AddUserDeviceTokens`
25. `20260815102801_ConsolidateUserDeviceTokensIntoPushDevices`
26. `20260815115240_AddListingPushNotificationOutbox`

The newer migrations make `Books.CategoryId` nullable; create favorite, purchase, rating, cart, cart-item, order, order-item, payment-attempt, processed-payment-event, consumed-refresh-token, library-registration-session, library-email-challenge, orphan-file, and saved-location storage; add library/favorite uniqueness and engagement foreign keys; add `Carts.PendingOrderId`; correlate purchases to orders/items; add the partial unique `IX_Carts_UserId_Open` index; add refresh-token indexes; and add `Libraries.EmailVerifiedAtUtc` plus optimistic concurrency. `AddMultipleUserLocations` validates and moves legacy `UsersProfiles.Latitude`/`Longitude` pairs into `UserLocations`, sets `DefaultLocationId`, seeds the per-profile location concurrency stamp, installs an ownership trigger, then drops the legacy columns. `PreventUserLocationOwnerReassignment` makes each saved location's `UserId` immutable after insertion so a default location cannot be reassigned across profiles. The `AddMultipleUserLocations` downgrade retains only the default or oldest saved location. `MergeModelSnapshot` is intentionally an empty schema migration used to align the EF snapshot after branch work. `AddComments` adds book comment storage; `AddUserDeviceTokens` adds FCM device-token storage for push notifications. `AddAuthorsTable` creates the standalone `Authors` table (`AuthorAggregate`). `RefactorBookAuthorToForeignKeyAndAddBirthDate` adds `Authors.BirthDate`; adds nullable `Books.AuthorId`; backfills it by creating an `Author` row (via `gen_random_uuid()`, a PostgreSQL 13+ core builtin) for every distinct existing `Books.Author` string and matching books to it by normalized name; adds the `Books`→`Authors` foreign key and an index on `AuthorId`; and only then drops the old free-text `Books.Author` column. Its downgrade re-adds `Author` and best-effort backfills it from the linked `Authors.Name`.
The newer migrations make `Books.CategoryId` nullable; create favorite, purchase, rating, cart, cart-item, order, order-item, payment-attempt, processed-payment-event, consumed-refresh-token, library-registration-session, library-email-challenge, orphan-file, saved-location, comment, push-device, library-approval-notification, and listing-push-notification storage; add library/favorite uniqueness and engagement foreign keys; add `Carts.PendingOrderId`; correlate purchases to orders/items; add the partial unique `IX_Carts_UserId_Open` index; add refresh-token indexes; and add `Libraries.EmailVerifiedAtUtc` plus optimistic concurrency. `AddMultipleUserLocations` validates and moves legacy `UsersProfiles.Latitude`/`Longitude` pairs into `UserLocations`, sets `DefaultLocationId`, seeds the per-profile location concurrency stamp, installs an ownership trigger, then drops the legacy columns. `PreventUserLocationOwnerReassignment` makes each saved location's `UserId` immutable after insertion so a default location cannot be reassigned across profiles. `AddPushDevicesAndLibraryApprovalNotifications` stores up to the actively retained device registrations per user using a unique token hash and adds an independently retried email/push outbox created by admin approval. `AddUserDeviceTokens` is retained as applied migration history; `ConsolidateUserDeviceTokensIntoPushDevices` validates and copies its rows into `PushDevices`, retains the ten most recent devices per user, and drops the duplicate table. `AddListingPushNotificationOutbox` adds the leased/retried push outbox populated atomically from listing domain events; publication events in one save are grouped per library so bulk upload creates one push. The `AddMultipleUserLocations` downgrade retains only the default or oldest saved location. `MergeModelSnapshot` is intentionally an empty schema migration used to align the EF snapshot after branch work.

`Program.cs` runs `db.Database.MigrateAsync()` on startup, so the database is migrated automatically when the app starts.

### Seeders

Baseline startup reconciles categories and the optional configuration-driven
bootstrap administrator. The connected interview dataset is additionally run
only when the environment is `Development` and `DemoData:Enabled=true` (base
and Development appsettings both disable it by default):

- `CategorySeeder.SeedAsync` — adds each missing stable category while including inactive rows in collision checks.
- `AdminSeeder.SeedAsync` — creates or synchronizes the optional configuration-driven bootstrap administrator.
- `DemoDataSeeder.SeedAsync` — serializes replicas with a PostgreSQL advisory lock and writes the demo graph in one transaction.
- `UserSeeder.SeedAsync` / `DemoProfileSeeder.SeedAsync` — seed named buyer/seller/admin personas, 102 library-owner identities, interests, payment sample, and saved locations.
- `LibrarySeeder.SeedAsync` — reconciles 102 demo libraries with 75 approved, 25 pending, one rejected, one awaiting email verification, and wallet variants.
- `UserSeeder.EnsureApprovedLibraryOwnerRolesAsync` — promotes profiles behind approved libraries to the `LibraryOwner` domain role and ensures their Identity users retain `User` and gain `LibraryOwner`.
- `DemoCatalogSeeder`, `EbookSeeder`, and `BookSeeder` — seed curated and pagination catalog/listing data without emitting provider-ready publication notifications; the private logical ebook path is `books/book1.pdf`.
- `DemoCommerceSeeder` — seeds carts, seven order/payment/fulfillment states, order-linked purchases, and terminal payout history.
- `DemoEngagementSeeder` — seeds popularity purchases, favorites, ratings, comments, reports, and moderation states.

Exact development credentials, stable showcase IDs, and the interview flow are in `docs/INTERVIEW_DEMO_DATA.md`. Never enable or copy these fixed credentials into Production. OTPs, refresh tokens, devices, active delivery outboxes, and due payouts are intentionally not seeded.

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
public class AppResult : OneOfBase<Success, ValidationFailed, NotFound, Unauthorized, Forbidden, DomainError, Conflict>
public class AppResult<TData> : OneOfBase<TData, ValidationFailed, NotFound, Unauthorized, Forbidden, DomainError, Conflict>
```

`ApiClientController.HandleResult` maps these to HTTP status codes:

- Success → `200 OK`
- Validation failure → `400 Bad Request`
- Not found → `404 Not Found`
- Unauthorized → `401 Unauthorized`
- Forbidden → `403 Forbidden`
- `ConflictException` / `Conflict` → `409 Conflict`
- `LibraryErrorCodes.DuplicateLibraryForUser`, `LibraryErrorCodes.DuplicateLibraryEmail`, or `"DUPLICATE_APPLICATION"` domain errors → `409 Conflict`
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

`IUploadedFile` in `Quraaa.Application/Shared/Files/IUploadedFile.cs` keeps ASP.NET `IFormFile` out of the Application layer. The API adapter is `FormFileUploadedFile` in `Quraaa.API/Requests/Files/FormFileUploadedFile.cs`. `IImageStorageService` is implemented by `CloudinaryImageStorageService`; it returns absolute HTTPS URLs and performs best-effort deletion only for owned `quraa/` image public IDs. `IFileStorageService` is implemented by `CloudinaryFileStorageService`; it writes authenticated raw assets, opens seekable temporary streams for PDF/DOCX extraction, generates short-lived delivery sources, enumerates/deletes owned Cloudinary assets, and reads legacy local book paths without writing new local files. Image validators enforce allow-listed extensions/content types, the 5 MB limit, and JPEG/PNG/WebP signatures. Document validators enforce PDF/Word MIME and extensions, 100 MB/50 MB limits, `%PDF-`, OLE `.doc`, or DOCX package structure before provider I/O.

## Security Considerations

- JWT authentication uses a symmetric signing key from `JWT_SECRET_KEY`. Keep this key secret and rotate it periodically.
- Logout authenticates with the refresh-token secret, so it still works after access-token expiry. The current token is resolved through the partial unique `AspNetUsers.RefreshToken` index; consumed ancestors resolve through unique hashed history. Either path clears only the currently matching family. A still-valid bearer `jti` is also cached as revoked.
- Refresh tokens are 64-byte opaque Base64 secrets stored only as SHA-256 hashes. Rotation atomically archives the presented hash, writes its replacement, and preserves a stable family id. Reuse of an unexpired consumed token revokes the active family; submitted `sha256:` database values are never accepted as raw credentials.
- Access JWTs carry the family id in a `sid` claim. JWT validation checks that `sid` against the current Identity row, so logout, replay detection, a newer login, authenticated password change, forgot-password recovery, and configured admin password reset immediately invalidate every access token from the replaced/revoked family.
- Each account currently has one active family. A fresh login creates a new family and removes the prior consumed-token history; rotation reloads the current Identity roles and prunes expired history.
- Any code that grants a privileged Identity role must clear the refresh token and family id in the same Identity update; the current role-grant and seeding paths do this for privileged elevation.
- Passwords are hashed by ASP.NET Core Identity.
- Phone numbers are used as usernames; emails are synthesized as `{phone}@quraaa.com`.
- Phone numbers are normalized to E.164 where possible using `libphonenumber-csharp`.
- New registration and regular user login are limited to valid Syrian (`+963`) phone numbers; forgot-password/admin/standalone OTP validators currently accept any valid international number. Login passwords must contain 6 through 256 characters before credential verification runs.
- Regular login accepts an ordinary account whose domain/Identity roles match as `User`/`{User}`, or a library-owner account whose roles match as `LibraryOwner`/`{User, LibraryOwner}`. Admin and mismatched/custom-role identities are rejected. A library owner therefore receives both roles from this route and can use role-authorized library functionality with the same token. Failed regular-login credentials are limited by account and trusted client address, and the HTTP endpoint also has a per-client fixed-window limit.
- The forgot-password endpoint returns a generic success even if the phone number is not registered, to avoid leaking registration status.
- OTP send and verify endpoints implement rate limiting and failed-attempt lockouts via `IDistributedCache`.
- Admin login requires valid admin credentials followed by a six-digit OTP. Credential attempts, OTP sends, and OTP verification are rate-limited by phone and client IP.
- Library-owner login only resolves approved libraries by normalized library email and locks credential attempts by email/client IP after repeated failures.
- The Stripe webhook is anonymous by design but authenticates the payload with the `Stripe-Signature` header and configured webhook secret. Do not expose a webhook secret or process unsigned payloads.
- Ebook storage references are omitted from public ebook responses. `/uploads/books/*.pdf` is blocked, and paid buyers receive ebook bytes only through the authenticated inline purchase-stream route, which proxies short-lived Cloudinary access or serves a validated legacy local file.
- Firebase service-account credentials and `.env` secrets must never be committed.
- Cloudinary API credentials and signed private-download URLs remain server-side. New images use `IImageStorageService`; new PDFs/Word files use `IFileStorageService`; neither may write durable uploads to `wwwroot` or the dyno filesystem.
- `Notifications:AllowTestEndpoint` is enabled in `appsettings.json` and `appsettings.Development.json`. Disable it in production unless you intend to allow unauthenticated test notification dispatch.
- `Otp:AllowInMemoryCacheInProduction` is currently `true` in the base settings. Production should configure Redis and override it to `false`, which makes startup fail when Redis is missing.
- HTTPS redirection and forwarded headers are enabled in the middleware pipeline. Forwarded headers trust loopback proxies by default; configure explicit arrays under `ForwardedHeaders:KnownProxies` and/or `ForwardedHeaders:KnownNetworks` when deploying behind another reverse proxy.
- `AdminSeeder` creates or synchronizes the configured admin Identity/profile, role, confirmed-phone state, and password on startup. Ensure `ADMIN_PHONE_NUMBER` and `ADMIN_PASSWORD` are strong and kept secret; changing the configured password resets the seeded admin password.

## Testing Strategy

Automated coverage is currently limited to the xUnit API-host project. Run it
with `dotnet test Quraaa.API.IntegrationTests/Quraaa.API.IntegrationTests.csproj`.
For database and provider flows, use this manual workflow:

1. Ensure PostgreSQL is running and `ConnectionStrings:DefaultConnection` is correct.
2. Ensure `JWT_SECRET_KEY` is set.
3. Run `dotnet run --project Quraaa.API`.
4. Open `https://localhost:7260/docs` or `http://localhost:5153/docs`.
5. Use the Swagger UI or an HTTP client (Postman, curl, etc.) to exercise endpoints.
6. For OTP/forgot-password flows, configure `OTP_DEVICE_TOKEN` and Firebase credentials.
7. For checkout, configure Stripe keys and forward signed `checkout.session.completed`, `checkout.session.async_payment_succeeded`, `checkout.session.async_payment_failed`, and `checkout.session.expired` events to `/api/payments/stripe/webhook`.
8. For an ISBN not already seeded, configure Google Books and allow outbound access.

Recommended additions (TODO):

- Unit tests for validators and domain aggregates.
- Broader integration tests for command/query handlers using `WebApplicationFactory`.
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
POST   /api/auth/register                         anonymous
POST   /api/auth/register/verify                  anonymous
POST   /api/auth/login                            anonymous by controller configuration
POST   /api/auth/library/login                    anonymous
POST   /api/auth/admin/login                      anonymous
POST   /api/auth/admin/login/verify               anonymous
POST   /api/auth/logout                           refresh token in body; valid bearer optional
POST   /api/auth/refresh                          anonymous; requires refresh token in body
POST   /api/auth/reset-password                   authenticated
POST   /api/auth/forgot-password                  anonymous
POST   /api/auth/forgot-password/verify           anonymous

GET    /api/profile/me                            authenticated
PUT    /api/profile/me                            authenticated
GET    /api/profile/locations                     authenticated
POST   /api/profile/locations                     authenticated
PUT    /api/profile/locations/{locationId}        authenticated
PUT    /api/profile/locations/{locationId}/default authenticated
DELETE /api/profile/locations/{locationId}        authenticated

POST   /api/libraries/register                    User; issues temporary dashboard URL
POST   /api/libraries/register/context            anonymous + registration token in body
POST   /api/libraries/register/submit             anonymous + token; multipart details
POST   /api/libraries/register/email/resend        anonymous + submitted registration token
POST   /api/libraries/register/email/verify        anonymous + submitted registration token + verificationId + OTP
GET    /api/libraries                             anonymous
GET    /api/libraries/{libraryId}/books           User or LibraryOwner

GET    /api/ebooks                                anonymous
GET    /api/books/most-popular                    anonymous
GET    /api/books/recommended                     authenticated

GET    /api/favorite-books                        authenticated
POST   /api/favorite-books/{bookId}               authenticated
DELETE /api/favorite-books/{bookId}               authenticated

POST   /api/library-admin/listings                LibraryAdmin
PUT    /api/library-admin/listings/{listingId}    LibraryAdmin
GET    /api/library-admin/listings/{listingId}    LibraryAdmin

GET    /api/listings/me                           User
POST   /api/listings/me/physical                  User

GET    /api/cart/me                               User
POST   /api/cart/items                            User
PUT    /api/cart/items/{listingId}                User
DELETE /api/cart/items/{listingId}                User
DELETE /api/cart/me                               User

GET    /api/orders/checkout-context               User
POST   /api/orders                                User
GET    /api/orders/me                             User
GET    /api/orders/{orderId}                      User
PUT    /api/orders/{orderId}/shipping-location    User
POST   /api/orders/{orderId}/checkout-session     User
POST   /api/orders/{orderId}/cancel               User
DELETE /api/orders/{orderId}                      User
GET    /api/seller/orders                         User or LibraryOwner
POST   /api/seller/orders/{orderId}/items/{orderItemId}/processing  User or LibraryOwner
POST   /api/seller/orders/{orderId}/items/{orderItemId}/fulfilled   User or LibraryOwner

POST   /api/payments/stripe/webhook               anonymous (Stripe signature)

GET    /api/purchases/me/buy-history              User
GET    /api/purchases/me/sell-history             User
GET    /api/purchases/{purchaseId}/stream         User; owned digital purchase only

GET    /api/categories                            anonymous by controller configuration
GET    /api/categories/{categoryId}               anonymous by controller configuration
POST   /api/categories                            Admin

POST   /api/admin/authors                         Admin
GET    /api/admin/authors                         Admin
GET    /api/admin/authors/{id}                    Admin
PUT    /api/admin/authors/{id}                    Admin
POST   /api/admin/authors/{id}/activation         Admin
POST   /api/admin/authors/activation              Admin
DELETE /api/admin/authors/{id}                    Admin
DELETE /api/admin/authors                         Admin

POST   /api/otp/send                              anonymous
POST   /api/otp/verify                            anonymous
POST   /api/notifications/send                    authenticated
POST   /api/notifications/test                    anonymous when enabled
```

`Program.cs` sets `RouteOptions.LowercaseUrls = true`; incoming ASP.NET Core routes are case-insensitive, but use the lowercase forms above in examples and generated links. `AdminAuthorsController`, `AdminModerationController`, `LibraryListingsController`, `OrdersController`, `PurchaseHistoryController`, `SellerOrdersController`, and `UserListingsController` have explicit route templates; the other controllers normally inherit `api/[controller]`.

### Authentication

`POST /api/auth/register` request body:

```json
{
  "firstName": "Ali",
  "lastName": "Hassan",
  "phoneNumber": "+9639XXXXXXXX",
  "password": "User@12345",
  "gender": 1,
  "dateOfBirth": "2000-01-01",
  "interests": ["3fa85f64-5717-4562-b3fc-2c963f66afa6"]
}
```

`Gender` enum values:

```text
1 = Male
2 = Female
```

`interests` are category IDs (`Guid`); each value must match an existing `CategoryAggregate.Id`.

Successful registration start returns a generic success message after sending an OTP. `POST /api/auth/register/verify` request body:

```json
{
  "phoneNumber": "+9639XXXXXXXX",
  "otpCode": "123456"
}
```

Successful registration verification response is `AuthResponse`:

```json
{
  "accessToken": "jwt",
  "refreshToken": "secure-random-base64",
  "accessTokenExpiration": "utc-date-time"
}
```

`POST /api/auth/login` request body:

```json
{
  "phoneNumber": "+9639XXXXXXXX",
  "password": "abc123"
}
```

The login validator requires a valid Syrian (`+963`) phone number and a password from 6 through 256 characters. A matching ordinary user receives the `User` role, while a matching library owner receives both `User` and `LibraryOwner` and can use the same token for role-authorized library functionality. Admin and mismatched/custom-role identities are rejected. Successful login response is also `AuthResponse`. If the credentials are valid but an ordinary pending registration's phone is not confirmed, login resends a `register-otp` code and returns the same `400 ValidationFailure` response used by a repeated registration attempt. The client should then continue through `POST /api/auth/register/verify`. Wrong credentials and unconfirmed library-owner identities never trigger a registration OTP.

`POST /api/auth/logout` requires the refresh token issued by any login flow:

```json
{
  "refreshToken": "secure-random-base64"
}
```

A still-valid access token is optional:

```http
Authorization: Bearer <access-token>
```

On success, the endpoint returns `200 OK` and revokes the active family resolved from either the current refresh secret or any still-unexpired consumed ancestor. This works even when an expired bearer token is sent. All family access JWTs subsequently fail `sid` validation; if a valid bearer was supplied, its `jti` is cached as revoked too. Invalid, expired, or already-revoked refresh tokens receive the same idempotent `200`; missing or oversized input returns `400`. The client must also delete both tokens from its own secure storage.

`POST /api/auth/refresh` does not require a valid access token. Send the refresh token in the request body:

```json
{
  "refreshToken": "secure-random-base64"
}
```

A successful response is a new `AuthResponse`. Refresh tokens rotate: after success, the submitted token is invalid and the client must atomically replace both locally stored tokens. Missing input returns `400`; invalid, expired, revoked, or reused tokens return the same generic `401` response.

`POST /api/auth/admin/login` validates the supplied admin phone/password and sends an OTP rather than returning tokens:

```json
{
  "phoneNumber": "+9639XXXXXXXX",
  "password": "Admin@12345"
}
```

`POST /api/auth/admin/login/verify` completes the flow:

```json
{
  "phoneNumber": "+9639XXXXXXXX",
  "otpCode": "123456"
}
```

Admin credentials are checked against both the `UserAggregate.Role == Admin` value and the Identity `Admin` role. The credential and OTP stages each lock after five failed attempts in a five-minute window; the lock lasts five minutes, and OTP sends are throttled for 60 seconds.

`POST /api/auth/library/login` logs in the owner of an approved library by normalized library email:

```json
{
  "email": "info.lib1@quraaa.com",
  "password": "User@12345"
}
```

The library must be approved, its owner profile must have `Role.LibraryOwner`, the Identity account must be confirmed with the exact role set `{User, LibraryOwner}`, and the supplied password is still the owner's Identity password. An extra `Admin` or custom role fails closed so this password-only route cannot mint privileged claims. Five failed credential attempts within five minutes trigger a five-minute lock by email and client IP.

Password reset request body maps to `ResetPasswordRequest`; the controller creates `ResetPasswordCommand` after reading `UserId` from the authenticated JWT:

```json
{
  "oldPassword": "oldPass123",
  "newPassword": "New@Pass456"
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
  "newPassword": "New@Pass456"
}
```

### Profile

`GET /api/profile/me` has no request body. `ProfileController` reads the user id from JWT claims and sends `GetMyProfileQuery`.

`PUT /api/profile/me` request body maps to `UpdateProfileRequest`:

```json
{
  "firstName": "Ali",
  "lastName": "Hassan",
  "gender": 1,
  "dateOfBirth": "2000-01-01",
  "profileImageUrl": "/uploads/profiles/user.jpg",
  "interests": ["3fa85f64-5717-4562-b3fc-2c963f66afa6"]
}
```

`interests` are category GUIDs. Successful profile responses use `ProfileResponse`, include expanded category objects and optional coordinates, and do not expose `PasswordHash`:

```json
{
  "userId": "guid",
  "firstName": "Ali",
  "lastName": "Hassan",
  "phoneNumber": "+9639XXXXXXXX",
  "gender": "Male",
  "role": "User",
  "dateOfBirth": "2000-01-01",
  "profileImageUrl": "/uploads/profiles/user.jpg",
  "interests": [
    {
      "id": "guid",
      "nameAr": "علوم",
      "nameEn": "Science"
    }
  ],
  "location": {
    "id": "location-guid",
    "name": "My University",
    "address": "Damascus - Hamak - ITEF",
    "latitude": 33.3152,
    "longitude": 44.3661,
    "isDefault": true,
    "creationTime": "utc-date-time",
    "lastModificationTime": null
  },
  "locations": [
    {
      "id": "location-guid",
      "name": "My University",
      "address": "Damascus - Hamak - ITEF",
      "latitude": 33.3152,
      "longitude": 44.3661,
      "isDefault": true,
      "creationTime": "utc-date-time",
      "lastModificationTime": null
    }
  ],
  "lastLoginDate": null,
  "previousLoginDate": null,
  "creationTime": "utc-date-time",
  "lastModificationTime": "utc-date-time"
}
```

`GET /api/profile/locations` returns the authenticated user's saved locations with the default first. `POST /api/profile/locations` creates one:

```json
{
  "name": "My University",
  "address": "Damascus - Hamak - ITEF",
  "latitude": 33.3152,
  "longitude": 44.3661,
  "isDefault": true
}
```

`PUT /api/profile/locations/{locationId}` edits the name/address/coordinates, `PUT /api/profile/locations/{locationId}/default` selects the starred checkout default, and `DELETE /api/profile/locations/{locationId}` deletes one owned row. The first location becomes default automatically; deleting it promotes the oldest remaining location. `location` remains in `ProfileResponse` as a compatibility alias for the default while `locations` is the complete collection.

### Library

`POST /api/libraries/register` requires a `User` bearer token and no request body. It returns a temporary dashboard URL whose fragment contains an opaque registration token. The raw token is never stored; PostgreSQL stores its SHA-256 hash, expiry, optimistic-concurrency stamp, and the issuing JWT refresh-family id. Reissuing replaces the prior token. An existing unverified application can be resumed with a new link.

The dashboard calls `POST /api/libraries/register/context` with `{ "token": "..." }`. It then submits `POST /api/libraries/register/submit` as `multipart/form-data`:

```text
token: opaque token copied from the dashboard URL fragment
libraryName: Central Library
location: Baghdad
libraryImage: uploaded image file
headerImage: uploaded image file
email: library@example.com
```

The dashboard request does not accept `userId`; the validated session supplies it. New libraries start as `AwaitingEmailVerification`. The API creates a durable, generation-bound HMAC-SHA256 challenge, attempts SMTP delivery, and returns `202 Accepted` with `verificationId` plus `emailDeliveryStatus` (`Sent`, `NotSent`, or `Unknown`). `POST /api/libraries/register/email/resend` redelivers the same derived code and verification id subject to a 60-second cooldown and a fixed five-send/hour window; definite non-delivery restores the quota slot. `POST /api/libraries/register/email/verify` requires that verification id plus exactly six ASCII digits, allows five failures, applies a five-minute lockout, and atomically changes the library to `Pending`. Only verified pending libraries appear in the admin queue or can be approved/rejected. Approval promotes the domain profile and Identity role and creates the durable email/push notification outbox row in the same transaction.

`GET /api/libraries` is anonymous and returns approved libraries only. It supports `pageNumber`, `pageSize`, and `searchTerm` (library name/location). `GetLibrariesQuery` inherits `PaginationRequestDTO`, which coerces non-positive page numbers to `1` and any page size outside `1..20` to `10`.

`GET /api/libraries/{libraryId}/books` requires `User` or `LibraryOwner`. It returns active listings in that library and supports:

```text
pageNumber: default 1
pageSize: default 20 in the API request, but values above 20 are coerced to 10 by PaginationRequestDTO
searchTerm: title, author, or language
sortBy: title (default), author, or quantity
sortDescending: default false
```

### Ebooks

`GET /api/ebooks` returns a paged public list of active digital listings joined with their catalog book metadata. It allows anonymous access and accepts these optional query parameters through `GetEbooksQuery`:

```text
pageNumber: int (default 1)
pageSize: int (default 20, max 100)
searchTerm: string? (filters by title or author)
```

Successful responses use `PagedResult<EbookResponse>`:

```json
{
  "items": [
    {
      "listingId": "guid",
      "bookId": "guid",
      "title": "Ebook One",
      "author": "Quraaa Seed Data",
      "description": "Seeded ebook for development and manual testing.",
      "coverImageUrl": "/uploads/books/book1-cover.jpg",
      "categoryId": "guid",
      "language": "en",
      "isbn": null,
      "price": 1.0
    }
  ],
  "pageNumber": 1,
  "pageSize": 20,
  "totalCount": 1,
  "totalPages": 1,
  "hasNextPage": false,
  "hasPreviousPage": false
}
```

### Book discovery

`GET /api/books/most-popular` is anonymous and aggregates purchase quantity, rating count/average, and active-listing count without storing denormalized counters:

```text
pageNumber: default 1
pageSize: default 20, valid 1..100
searchTerm: optional title/author search, max 100 characters
sortBy: popular (default), purchases, ratings, or averageRating
includeUnranked: default true; false excludes books with no purchases and no ratings
```

`GET /api/books/recommended` requires authentication and uses:

```text
Accept-Language header: required and must contain only the single code `ar` or `en`; regional tags, weighted lists, wildcards, and other languages are rejected
pageNumber: default 1
pageSize: default 20, valid 1..100
searchTerm: optional title/author search, max 100 characters
```

Recommendations require a book to be in one of the authenticated user's interest category IDs, have an exact case-insensitive language match, and have at least one active listing. A user with no interests receives an empty page.

Both routes return `PagedResult<PopularBookResponse>`. Each item includes `bookId`, catalog metadata, `purchaseCount`, `ratingCount`, `averageRating`, and `activeListingCount`.

### Favorite books

All favorite routes are authenticated and derive `UserId` from JWT:

```text
GET    /api/favorite-books
POST   /api/favorite-books/{bookId}
DELETE /api/favorite-books/{bookId}
```

The list accepts `pageNumber` (default `1`), `pageSize` (default `20`, max `100`), and an optional title/author `searchTerm`. Adds are idempotent at the handler level and protected by a filtered unique index on active `(UserId, BookId)` pairs. Removal soft-deletes the favorite. `FavoriteBookResponse` contains `favoriteId`, catalog metadata, and `favoritedAt`.

### Marketplace listings

Library inventory management is routed explicitly under `/api/library-admin/listings`:

```text
POST /api/library-admin/listings
{
  "price": 12.5,
  "quantity": 4,
  "condition": "Good",
  "isbn": "9783161484100"
}

PUT /api/library-admin/listings/{listingId}
{
  "price": 14.0,
  "stock": 6,
  "condition": "LikeNew"
}

GET /api/library-admin/listings/{listingId}
```

Add resolves the caller's approved library, then resolves a book by local ISBN or Google Books metadata, rejects duplicate library/book listings, and creates a physical listing in `PendingReview`. Update is partial but requires at least one of price/stock/condition and checks library ownership. For an active listing, stock changes return `409 Conflict` while an unpaid pending order reserves it; price/condition changes remain allowed, and optimistic concurrency closes the check-versus-reservation race. Detail joins listing, book, and optional category.

These controller actions currently require the literal Identity role `LibraryAdmin`, while the domain/seeder/login implementation defines and grants `LibraryOwner`. No seeded/current identity receives `LibraryAdmin`; see Known Gaps.

User listing routes require the `User` role:

```text
GET  /api/listings/me?searchTerm=&sortBy=title&sortDescending=false
POST /api/listings/me/physical
{
  "price": 9.99,
  "condition": "Good",
  "isbn": "9783161484100"
}
```

The public response intentionally omits the digital asset path. Paid ebooks are available only as an authenticated inline stream through `GET /api/purchases/{purchaseId}/stream`; there is no order-item attachment-download route.

User physical listing creation uses the same local-then-Google ISBN resolution and rejects duplicate user/book listings. It creates one-unit `PendingReview` listings. `GET /api/listings/me` returns only the caller's active user listings, can search title/author/language, and sorts by `title`, `author`, or `quantity`. The current request DTO omits page fields even though the query inherits `PaginationRequestDTO`, so this route currently always returns page `1`, size `10`.

### Cart, orders, checkout, fulfillment, and payments

`CartController` requires the `User` role and always derives `UserId` from JWT:

```text
GET    /api/cart/me
POST   /api/cart/items
PUT    /api/cart/items/{listingId}
DELETE /api/cart/items/{listingId}
DELETE /api/cart/me
```

Add/update bodies:

```json
{
  "listingId": "guid",
  "quantity": 1
}
```

```json
{
  "quantity": 2
}
```

`GET /api/cart/me` returns an empty `CartResponse` when no open cart exists. Cart responses contain `cartId`, string `status`, items with snapshotted unit prices and line totals, `totalAmount`, `itemCount`, and an optional Stripe session ID. Only active listings can be added. Add/update quantities are limited to Stripe's `1..999,999` range; adding an existing item validates the resulting cumulative quantity against available stock; and a cart can contain at most 100 distinct listings. Order creation re-checks those limits before per-listing work so legacy/invalid carts fail early. Cart mutations are rejected while the cart is locked for a pending order. A partial unique database index enforces at most one non-deleted `Active`/`PendingPayment` cart per user, including under concurrent first-add requests.

`GET /api/orders/checkout-context` is the order preflight for the authenticated user's current open cart. Its response has `requiresShippingLocation`, `selectedShippingLocationId`, and `locations`. Each checkout location contains `id`, `name`, optional `address`, `latitude`, `longitude`, and `isDefault`. For a physical-only or mixed cart, `requiresShippingLocation` is `true`, `locations` contains the user's owned saved locations ordered with the default first, and `selectedShippingLocationId` is that default ID when one exists. With no saved default the selected ID is `null`. A missing/empty cart or a digital-only cart returns `false`, `null`, and an empty collection respectively, so digital checkout does not collect or persist a shipping location. A `PendingPayment` cart returns `409 Conflict`; a cart containing a missing/inactive listing returns `404`.

`POST /api/orders` is the primary checkout entry point. New clients send the selected saved location ID:

```json
{
  "successUrl": "https://client.example/checkout/success",
  "cancelUrl": "https://client.example/checkout/cancel",
  "shippingLocationId": "saved-location-guid"
}
```

For a physical or mixed cart, an explicit ID must resolve inside the authenticated buyer's loaded locations; omission uses the buyer's current default. With neither an explicit selection nor a default, physical order creation fails validation/domain checks. Digital-only orders ignore shipping selection and store `null` shipping coordinates. The transitional request contract still accepts legacy nested `shippingLocation` coordinates, but clients must not send both forms and new clients must use `shippingLocationId`. The unpaid-order shipping-update route follows the same saved-ID contract while temporarily retaining its legacy flat `latitude`/`longitude` input.

Order creation validates the cart, seller ownership, active listings, current stock, and owned shipping selection; snapshots book, seller, price, fulfillment, digital-asset, and shipping-coordinate data into the order; creates an `OrderAggregate`; reserves physical stock by decrementing the available listing stock; locks the cart with `PendingOrderId`; and creates a payment attempt. Saved-location edits or deletion therefore do not rewrite an existing order. The order, reservation, cart lock, and stable payment inputs are saved before the external Stripe Checkout request. The response is an `OrderCheckoutResponse` containing the order, payment-attempt ID, session ID/URL, and expiry.

Buyer order routes:

```text
GET    /api/orders/checkout-context
GET    /api/orders/me
GET    /api/orders/{orderId}
PUT    /api/orders/{orderId}/shipping-location
POST   /api/orders/{orderId}/checkout-session
POST   /api/orders/{orderId}/cancel
DELETE /api/orders/{orderId}
```

Shipping can change only while the order is unpaid. A new client changes it by sending an owned `shippingLocationId`; legacy raw coordinates remain accepted only during migration. Checkout-session creation resumes or attaches the active idempotent attempt. Cancelling an unpaid pending order expires any attached Stripe session, releases its physical-stock reservations, and reopens the cart. Only terminal orders can be archived. Order responses expose neither a digital storage path nor a download-availability flag; paid ebook reading uses the separately authorized purchase stream.

`POST /api/payments/stripe/webhook` verifies `Stripe-Signature`, provider mode, order/payment-attempt correlation, session, amount, currency, and payment intent. A durable `ProcessedPaymentEvent` inbox makes provider events idempotent. Paid `checkout.session.completed` and `checkout.session.async_payment_succeeded` events use the shared order-payment finalizer to mark the order/cart paid and create order-linked `BookPurchaseAggregate` rows; physical stock is not decremented again because it was reserved before Stripe. `checkout.session.async_payment_failed` and `checkout.session.expired` release reserved stock and reopen the cart. A hosted reconciler runs every minute and, after a two-minute webhook grace period, reconciles expired pending attempts. For a local `Created` attempt, it replays the exact persisted Checkout request with `checkout:{attempt.Id:N}`, attaches any recovered Session, retrieves Stripe's authoritative state, and invokes the same paid finalizer when a paid webhook was missed. Inventory is released only after Stripe confirms an unpaid expired Session or, within a conservative 23-hour idempotency-retention window, the replay proves no Session was created; older unattached attempts require manual reconciliation. A complete-but-unpaid asynchronous payment remains pending, and the reconciler keyset-scans each fixed-cutoff candidate set so removed rows cannot shift later work and deferred attempts are retried on the next scan.

Seller fulfillment routes allow `User` or `LibraryOwner`:

```text
GET  /api/seller/orders?fulfillmentStatus=&pageNumber=1&pageSize=20
POST /api/seller/orders/{orderId}/items/{orderItemId}/processing
POST /api/seller/orders/{orderId}/items/{orderItemId}/fulfilled
```

The seller queue contains paid physical items owned directly by the current user or by their library. Only physical items transition through `Pending` → `Processing` → `Fulfilled`; an order becomes completed after every order item is fulfilled. Paid digital items are fulfilled automatically.

### Purchase history

Both routes require the `User` role and support `pageNumber`, `pageSize`, and title/author `searchTerm`. `PaginationRequestDTO` defaults to page `1`, size `10`, and coerces sizes outside `1..20` to `10`.

```text
GET /api/purchases/me/buy-history
GET /api/purchases/me/sell-history
```

Buy history returns the authenticated user's Stripe-created purchases with book/category metadata, quantity, unit price, total price, and purchase time. Sell history returns purchases whose original listing has `SellerType.User` and belongs to the authenticated seller; it additionally returns `buyerUserId` and `totalEarned`. Library sales are not included by the sell-history query.

### Categories

`GET /api/categories` returns all active categories.

`GET /api/categories/{categoryId}` returns a single category.

`POST /api/categories` is admin-only (`[Authorize(Roles = "Admin")]`) and creates a new category.

### Authors

Public, anonymous author profile routes:

```text
GET /api/authors/{authorId}
GET /api/authors/{authorId}/books
```

The profile response contains `id`, `name`, optional `bio`, optional `photoUrl`, and optional `birthDate`. The books route returns `PagedResult<HomeBookResponse>`, grouped by catalog book and limited to books that have at least one active listing with available stock. It accepts `pageNumber`, `pageSize`, optional `searchTerm`, and the same `sortBy` values as the home catalog. An existing author with no available books returns an empty `200 OK` page; an unknown author returns `404 Not Found` from either route.

Admin author management and moderation (re-audited **2026-08-16**):

All routes require the `Admin` role. Route ownership is intentionally split: `AdminAuthorsController` owns create/detail/update, while `AdminModerationController` owns the paged moderation list, activation/reactivation, and guarded permanent deletion. Do not add a parameterless `[HttpGet]` or `[HttpDelete("{id:guid}")]` back to `AdminAuthorsController`; those templates would collide with the moderation actions and cause `AmbiguousMatchException` before authorization or controller execution.

```text
POST   /api/admin/authors
GET    /api/admin/authors
GET    /api/admin/authors/{id}
PUT    /api/admin/authors/{id}
POST   /api/admin/authors/{id}/activation
POST   /api/admin/authors/activation
DELETE /api/admin/authors/{id}
DELETE /api/admin/authors
```

`POST`/`PUT` request body:

```json
{
  "name": "J.K. Rowling",
  "bio": "British author, best known for the Harry Potter series.",
  "photoUrl": "https://example.com/authors/jk-rowling.jpg",
  "birthDate": "1965-07-31"
}
```

`bio`, `photoUrl`, and `birthDate` are optional; when present, `photoUrl` must be an absolute `http`/`https` URL and `birthDate` must be in the past.

`GET /api/admin/authors` accepts `pageNumber` (default `1`, minimum `1`), `pageSize` (default `20`, range `1`–`100`), optional `searchTerm` (maximum `200` characters), and `includeDeactivated` (default `false`). `includeDeactivated=true` returns active and deactivated authors together; it is not a deactivated-only filter. The response is `PagedResult<AdminAuthorResponse>`:

```json
{
  "items": [
    {
      "authorId": "11111111-1111-1111-1111-111111111111",
      "name": "J.K. Rowling",
      "bio": "British author, best known for the Harry Potter series.",
      "photoUrl": "https://example.com/authors/jk-rowling.jpg",
      "birthDate": "1965-07-31T00:00:00",
      "bookCount": 7,
      "isDeactivated": false,
      "deactivatedAtUtc": null,
      "createdAt": "2026-08-15T09:00:00Z"
    }
  ],
  "pageNumber": 1,
  "pageSize": 20,
  "totalCount": 1,
  "totalPages": 1,
  "hasNextPage": false,
  "hasPreviousPage": false
}
```

The list DTO deliberately differs from the existing create/update/detail DTOs. List items use `authorId` and `createdAt`; `POST` and `PUT` still return `AuthorResponse` with `id` and `creationTime`, and `GET /api/admin/authors/{id}` returns `AuthorDetailsResponse` with `id`, `creationTime`, and `lastModificationTime`. Frontends must use route-specific DTOs or normalize them in an API adapter rather than globally renaming every author field.

Single activation uses `POST /api/admin/authors/{id}/activation?deactivate=true|false` and has no request body. Bulk activation uses `POST /api/admin/authors/activation` with `{ "ids": ["..."], "deactivate": true|false }`; at most `200` ids are accepted. `true` soft-deletes/deactivates and `false` restores.

Single permanent deletion uses `DELETE /api/admin/authors/{id}` with no body. Bulk permanent deletion uses `DELETE /api/admin/authors` with `{ "ids": ["..."] }`; at most `100` ids are accepted. An author must already be deactivated and must have no non-deleted books. Both single and bulk moderation calls return `BulkModerationResult`, and HTTP `200` can contain per-record skips:

```json
{
  "succeededCount": 1,
  "skippedCount": 1,
  "results": [
    {
      "id": "11111111-1111-1111-1111-111111111111",
      "succeeded": true,
      "reason": null,
      "blockers": null
    },
    {
      "id": "22222222-2222-2222-2222-222222222222",
      "succeeded": false,
      "reason": "This record cannot be deleted while other records still reference it.",
      "blockers": [
        { "reference": "Books", "count": 3 }
      ]
    }
  ]
}
```

Other per-record skip reasons are `Not found.` and `Deactivate this record before deleting it permanently.`. The frontend must inspect every `results[]` entry instead of treating HTTP `200` as proof that all requested records changed. `BookAggregate.AuthorId` remains a nullable scalar FK to `AuthorAggregate`; the moderation handler checks book blockers before physical deletion. The older `DeleteAuthorCommand` and `GetAuthorsPaginatedQuery` application features remain in source but no longer have HTTP actions under `/api/admin/authors`.

### OTP

`POST /api/otp/send` request body:

```json
{
  "phoneNumber": "+9647XXXXXXXXX"
}
```

`POST /api/otp/verify` request body:

```json
{
  "phoneNumber": "+9647XXXXXXXXX",
  "code": "123456"
}
```

### Notifications

`PUT /api/notifications/devices`, deprecated `POST /api/notifications/device-token`, and `DELETE /api/notifications/devices` request body:

```json
{
  "deviceToken": "fcm-registration-token-from-client-app"
}
```

All three routes are authenticated and bind the token to the JWT user. Both registration routes are idempotent, reassign a rotated token to the latest authenticated owner, and retain only the ten most recently registered devices for that user. `POST /api/notifications/device-token` is a compatibility alias; new clients should use `PUT`. `DELETE` removes the token only when it belongs to the caller. Clients should register after login and after every Firebase token refresh.

`POST /api/notifications/send` request body:

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

`POST /api/notifications/test` has the same shape (with optional fields) and is allowed anonymously in Development or when `Notifications:AllowTestEndpoint=true`.

## Feature Flows

### Registration Flow

Files:

```text
Quraaa.API/Controllers/AuthController.cs
Quraaa.API/Requests/Authentication/RegisterRequest.cs
Quraaa.API/Requests/Authentication/VerifyRegisterOtpRequest.cs
Quraaa.Application/Features/Authentication/Commands/Register/RegisterCommand.cs
Quraaa.Application/Features/Authentication/Commands/Register/RegisterCommandValidator.cs
Quraaa.Application/Features/Authentication/Commands/Register/RegisterCommandHandler.cs
Quraaa.Application/Features/Authentication/Commands/VerifyRegisterOtp/VerifyRegisterOtpCommand.cs
Quraaa.Application/Features/Authentication/Commands/VerifyRegisterOtp/VerifyRegisterOtpCommandValidator.cs
Quraaa.Application/Features/Authentication/Commands/VerifyRegisterOtp/VerifyRegisterOtpCommandHandler.cs
Quraaa.Application/Features/Authentication/Interfaces/IAuthenticationUnitOfWork.cs
Quraaa.Application/Features/Otp/Interfaces/IOtpCacheService.cs
Quraaa.Application/Features/Otp/Interfaces/IFirebaseSmsGateway.cs
Quraaa.Persistence/Services/IdentityService.cs
Quraaa.Persistence/Services/AuthenticationUnitOfWork.cs
Quraaa.Persistence/Repositories/UserRepository.cs
Quraaa.Infrastructure/Services/OtpCacheService.cs
Quraaa.Domain/User/UserAggregate.cs
```

Routes:

```text
POST /api/auth/register
POST /api/auth/register/verify
```

Authentication:

```text
AllowAnonymous
```

Validation rules:

- `FirstName`: required, max 50 characters.
- `LastName`: required, max 50 characters.
- `PhoneNumber`: required, must start with `+963`, and must be valid for the Syrian `SY` region according to libphonenumber.
- `Password`: required, 6 through 256 characters, with uppercase, lowercase, digit, and non-alphanumeric characters.
- `DateOfBirth`: required, must be older than or equal to 5 years and younger than 100 years based on UTC date.
- `Gender`: must be a valid enum value.
- `Interests`: required and not empty; each value must be an existing `CategoryAggregate.Id`.
- `POST /api/auth/register/verify` additionally requires `OtpCode`, exactly 6 digits.

Flow:

```text
HTTP POST /api/auth/register
  -> AuthController.Register(body request)
  -> [AllowAnonymous]
  -> AuthController reads clientIp from HttpContext.Connection.RemoteIpAddress
  -> RegisterCommand(firstName, lastName, phoneNumber, password, gender, dateOfBirth, interests, clientIp)
  -> Mediator.Send(command)
  -> RegisterCommandHandler.Handle(...)
  -> BaseApplicationService validates RegisterCommand
  -> IPhoneService.FormatToE164(phone)
  -> handler checks verification lockout and 60-second resend lockout under the "register-otp" namespace
  -> IIdentityService.GetUserIdentityByPhoneNumberAsync(formattedPhone)
  -> if the phone belongs to a confirmed account, throws ApplicationBusinessException
  -> if the phone belongs to an unconfirmed account, does not update pending password/profile data, resends an OTP, and returns a validation error telling the client to complete verification
  -> otherwise acquires one atomic owner-tagged phone/client OTP lease
  -> creates the new ASP.NET Identity user, User role membership, UserAggregate, and interests inside one database transaction
  -> commits before generating or sending an OTP; a failed persistence write leaves no partial Identity/profile account
  -> handler stores an owner-tagged OTP in IDistributedCache under "register-otp" keys
  -> IFirebaseSmsGateway.SendSmsRequestAsync(phone, otp, purpose)
  -> FirebaseSmsGateway reads OTP_DEVICE_TOKEN and sends a high-priority FCM data message with a 45-second dispatch TTL plus request/purpose/expiry metadata
  -> Success, no tokens yet

HTTP POST /api/auth/register/verify
  -> AuthController.VerifyRegisterOtp(body request)
  -> [AllowAnonymous]
  -> AuthController reads clientIp from HttpContext.Connection.RemoteIpAddress
  -> VerifyRegisterOtpCommand(phoneNumber, otpCode, clientIp)
  -> Mediator.Send(command)
  -> VerifyRegisterOtpCommandHandler.Handle(...)
  -> BaseApplicationService validates VerifyRegisterOtpCommand
  -> IPhoneService.FormatToE164(phone)
  -> handler reads OTP from IDistributedCache under the "register-otp" namespace
  -> handler compares in fixed time and atomically consumes only the inspected OTP generation
  -> failed attempts are tracked; 5 failures in 5 minutes trigger a 5-minute lockout
  -> a valid OTP can clear a legacy incomplete regular registration only when it has no profile, or has a matching User profile but no Identity role; privileged/mixed-role identities are never auto-repaired
  -> requires the pending profile domain role and exact Identity role set to both be User
  -> IIdentityService.ConfirmPhoneNumberAsync(userId) sets PhoneNumberConfirmed = true
  -> success clears verification state after the OTP has been atomically consumed under the "register-otp" namespace
  -> IIdentityService.GenerateRegularUserAuthTokensAsync(id, phone)
  -> AuthResponse
```

Important registration details:

- The same generated `Guid` is used as the ASP.NET Identity user ID and the domain `UserAggregate.Id`.
- `ApplicationUser.UserName` is the normalized E.164 phone number.
- `ApplicationUser.Email` is synthesized as `{phoneNumber}@quraaa.com`.
- Email is marked confirmed at registration start; phone is marked confirmed only after `POST /api/auth/register/verify`.
- `UserAggregate.PhoneNumber` is formatted to E.164.
- `UserAggregate.PasswordHash` stores the Identity password hash.
- New users receive `Role.User`.
- Refresh tokens are random 64-byte values encoded as Base64. Only a prefixed SHA-256 hash is saved on the Identity user; raw tokens stored by earlier builds are accepted once and migrated during rotation.
- Refresh token expiry is set to `DateTime.UtcNow.AddDays(30)`.

### Registration Validation Rules

`RegisterCommandValidator` enforces:

- `FirstName`: required, max 50 characters.
- `LastName`: required, max 50 characters.
- `PhoneNumber`: required, must start with `+963`, and must be valid for the Syrian `SY` region according to libphonenumber.
- `Password`: required, 6 through 256 characters, with uppercase, lowercase, digit, and non-alphanumeric characters.
- `DateOfBirth`: required, must be older than or equal to 5 years and younger than 100 years based on UTC date.
- `Gender`: must be a valid enum value.
- `Interests`: required and not empty.
- Each interest ID must exist as a `CategoryAggregate.Id`.
- `VerifyRegisterOtpCommandValidator` requires `OtpCode`, exactly 6 digits.

### Login Flow

Files:

```text
Quraaa.API/Controllers/AuthController.cs
Quraaa.API/Requests/Authentication/LoginRequest.cs
Quraaa.Application/Features/Authentication/Commands/Login/LoginCommand.cs
Quraaa.Application/Features/Authentication/Commands/Login/LoginCommandValidator.cs
Quraaa.Application/Features/Authentication/Commands/Login/LoginCommandHandler.cs
Quraaa.Persistence/Services/IdentityService.cs
Quraaa.Persistence/Repositories/UserRepository.cs
```

Flow:

```text
HTTP POST /api/auth/login
  -> AuthController.Login(request)
  -> LoginCommand(phone, password, server-derived client IP)
  -> Mediator.Send(command)
  -> LoginCommandHandler.Handle(...)
  -> BaseApplicationService validates LoginCommand
  -> IPhoneService.FormatToE164(phone)
  -> enforce account/client credential lockouts before password hashing
  -> load the Identity and verify the password
  -> require matching domain/Identity roles: User + {User}, or LibraryOwner + {User, LibraryOwner}
  -> if credentials are valid but PhoneNumberConfirmed is false:
       -> acquire the shared atomic registration-OTP phone/client lease
       -> cache and send an owner-tagged register-otp code
       -> return the same pending-verification validation response as repeated registration
       -> client completes POST /api/auth/register/verify
  -> issue through GenerateRegularUserAuthTokensAsync or GenerateLibraryOwnerAuthTokensAsync according to the matched profile role
  -> AuthResponse
```

### Logout Flow

Files:

```text
Quraaa.API/Controllers/AuthController.cs
Quraaa.API/Requests/Authentication/LogoutRequest.cs
Quraaa.API/Extensions/ServiceCollectionExtensions.cs
Quraaa.Application/Features/Authentication/Commands/Logout/
Quraaa.Application/Features/Authentication/Interfaces/IAccessTokenRevocationService.cs
Quraaa.Application/Features/Authentication/Interfaces/IIdentityService.cs
Quraaa.Infrastructure/Services/AccessTokenRevocationService.cs
Quraaa.Persistence/Services/IdentityService.cs
```

Flow:

```text
HTTP POST /api/auth/logout
  -> [AllowAnonymous] lets the refresh-token credential work after JWT expiry
  -> AuthController accepts the refresh token in the body and never accepts a client user id
  -> when middleware produced an authenticated principal, AuthController also extracts its jti and expiration
  -> IdentityService hashes the refresh token and first uses the indexed current-token lookup
  -> if rotation already replaced it, consumed-token history resolves the same family
  -> a conditional update clears the active token, expiry, and family id without touching a newer independent login
  -> invalid, expired, or already-revoked refresh tokens are an idempotent no-op
  -> when a valid bearer was present, AccessTokenRevocationService caches its jti until token expiry
  -> subsequent JWT validation rejects both that jti and every JWT carrying the revoked family sid
  -> AppResult success
```

The route is shared because token revocation is identical for users, admins, and library owners. The refresh-token secret identifies the account; no role or client-supplied user id is trusted. The client must delete its local access and refresh tokens after a successful response.

### Refresh Token Flow

Files:

```text
Quraaa.API/Controllers/AuthController.cs
Quraaa.API/Requests/Authentication/RefreshTokenRequest.cs
Quraaa.Application/Features/Authentication/Commands/RefreshToken/
Quraaa.Application/Features/Authentication/Interfaces/IIdentityService.cs
Quraaa.Persistence/Data/ConsumedRefreshToken.cs
Quraaa.Persistence/Services/IdentityService.cs
```

Flow:

```text
HTTP POST /api/auth/refresh
  -> [AllowAnonymous]; an access token is not required
  -> BaseApplicationService validates the refresh-token request
  -> IdentityService validates the raw Base64 credential, hashes it, and resolves the current Identity user through a partial unique index
  -> non-prefixed legacy plaintext storage is accepted for a one-time migration; a stored `sha256:` hash is never accepted as the bearer secret
  -> require a confirmed identity and an unexpired stored token
  -> preserve/create the stable family id and add it to the replacement access JWT as sid
  -> in one transaction, conditionally replace the current hash and archive the consumed hash with its expiry
  -> a consumed-token replay or concurrent rotation loser revokes the active family and returns 401
  -> JWT validation checks sid against the active Identity family, so descendant access tokens are revoked too
  -> return the new AuthResponse only when the atomic rotation commits
```

The endpoint is shared across roles. Current Identity roles are reloaded when the replacement access token is created, so role changes are reflected at refresh time.

### Admin and Library-Owner Login Flows

Files:

```text
Quraaa.API/Controllers/AuthController.cs
Quraaa.API/Requests/Authentication/AdminLoginRequest.cs
Quraaa.API/Requests/Authentication/VerifyAdminLoginOtpRequest.cs
Quraaa.API/Requests/Authentication/LibraryOwnerLoginRequest.cs
Quraaa.Application/Features/Authentication/Commands/AdminLogin/
Quraaa.Application/Features/Authentication/Commands/VerifyAdminLoginOtp/
Quraaa.Application/Features/Authentication/Commands/LibraryOwnerLogin/
Quraaa.Persistence/Services/IdentityService.cs
Quraaa.Persistence/Repositories/LibraryRepository.cs
Quraaa.Persistence/Seed/UserSeeder.cs
```

Admin flow:

```text
POST /api/auth/admin/login
  -> validate/normalize phone and password
  -> enforce credential lockout by phone and client IP
  -> require matching admin profile plus Identity Admin role
  -> generate/store ten-minute OTP in `admin-login-otp`
  -> throttle sends for 60 seconds and dispatch through FirebaseSmsGateway

POST /api/auth/admin/login/verify
  -> fixed-time OTP comparison with five-attempt/five-minute lockout
  -> re-check profile and Identity Admin roles
  -> confirm the phone if necessary
  -> clear OTP/attempt state
  -> return AuthResponse
```

Library-owner flow:

```text
POST /api/auth/library/login
  -> normalize email
  -> enforce credential lockout by email and client IP
  -> resolve an Approved library by email
  -> resolve its UserAggregate and Identity user
  -> require confirmed phone, valid password, domain LibraryOwner role, and exact Identity roles {User, LibraryOwner}
  -> clear credential state
  -> return AuthResponse
```

The library-owner endpoint does not use a separate library password: it checks the password of the Identity account referenced by `LibraryAggregate.UserId`.

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
POST /api/auth/reset-password
```

Authentication:

```text
Authorization: Bearer <access-token>
```

Validation rules:

- `UserId`: required on the command, sourced from the authenticated JWT rather than the request body.
- `OldPassword`: required string, 6 through 256 characters.
- `NewPassword`: required string, 6 through 256 characters, with uppercase, lowercase, digit, and non-alphanumeric characters; must be different from `OldPassword`.

Flow:

```text
HTTP POST /api/auth/reset-password
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
  -> IdentityService clears the refresh token and family id before UserManager.ChangePasswordAsync, so a successful Identity update atomically persists the new password/security stamp and family revocation
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
POST /api/auth/forgot-password
POST /api/auth/forgot-password/verify
```

Authentication:

```text
AllowAnonymous
```

Validation rules:

- `PhoneNumber`: required, must start with `+`, must be valid according to libphonenumber.
- `OTP_DEVICE_TOKEN`: required server-side configuration read by `FirebaseSmsGateway`; not accepted in the request body.
- `OtpCode`: required, exactly 6 digits.
- `NewPassword`: required, 6 through 256 characters, with uppercase, lowercase, digit, and non-alphanumeric characters.

Flow:

```text
HTTP POST /api/auth/forgot-password
  -> AuthController.ForgotPassword(body request)
  -> [AllowAnonymous]
  -> AuthController reads clientIp from HttpContext.Connection.RemoteIpAddress
  -> ForgotPasswordCommand(phoneNumber, clientIp)
  -> ForgotPasswordCommandHandler.Handle(...)
  -> BaseApplicationService validates ForgotPasswordCommand
  -> IPhoneService.FormatToE164(phone)
  -> IOtpCacheService checks send and verification lockouts
  -> IUserRepository.GetUserByPhoneNumberAsync(formattedPhone)
  -> if user is null, records the request lockout and returns generic success without sending an OTP to avoid leaking registration status
  -> handler generates OTP and stores it in IDistributedCache under the `forgot-password-otp` namespace
  -> IFirebaseSmsGateway.SendSmsRequestAsync(phone, otp, "forgot-password")
  -> FirebaseSmsGateway reads OTP_DEVICE_TOKEN and sends a high-priority FCM data message with a 45-second dispatch TTL plus request/purpose/expiry metadata

HTTP POST /api/auth/forgot-password/verify
  -> AuthController.VerifyForgotPassword(body request)
  -> [AllowAnonymous]
  -> AuthController reads clientIp from HttpContext.Connection.RemoteIpAddress
  -> ResetForgotPasswordCommand(phoneNumber, otpCode, newPassword, clientIp)
  -> ResetForgotPasswordCommandHandler.Handle(...)
  -> BaseApplicationService validates ResetForgotPasswordCommand
  -> IPhoneService.FormatToE164(phone)
  -> handler reads the OTP, compares in fixed time, and atomically consumes only that generation under the `forgot-password-otp` namespace
  -> failed attempts are tracked; 5 failures in 5 minutes trigger a 5-minute lockout
  -> success clears verification state after the OTP has been atomically consumed under the `forgot-password-otp` namespace
  -> IUserRepository.GetUserByPhoneNumberAsync(formattedPhone)
  -> handler throws NotFoundException if the user profile is null
  -> IIdentityService.ResetPasswordAsync(user.Id, newPassword)
  -> IdentityService generates a reset token, clears the refresh token/family id, and calls UserManager.ResetPasswordAsync so password recovery and family revocation persist in one Identity update
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
Quraaa.Application/Features/Categories/Interfaces/ICategoryRepository.cs
Quraaa.Persistence/Repositories/UserRepository.cs
Quraaa.Domain/User/UserAggregate.cs
```

Routes:

```text
GET /api/profile/me
PUT /api/profile/me
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
- Each interest GUID must exist as a `CategoryAggregate.Id`.

Read flow:

```text
HTTP GET /api/profile/me
  -> ProfileController.GetMyProfile()
  -> [Authorize] validates JWT bearer token
  -> ProfileController extracts UserId from token claims
  -> ProfileController creates GetMyProfileQuery with token UserId
  -> Mediator.Send(query)
  -> GetMyProfileQueryHandler.Handle(...)
  -> BaseApplicationService validates GetMyProfileQuery
  -> IUserRepository.GetUserByIdAsync(userId) returns the user profile or null
  -> handler throws NotFoundException if the user profile is null
  -> ICategoryRepository.GetByIdsAsync(user.InterestedCategoryIds)
  -> ProfileResponse.FromUser(user, interestCategories) expands categories and optional location
  -> ProfileResponse
```

Update flow:

```text
HTTP PUT /api/profile/me
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
  -> ICategoryRepository.GetByIdsAsync(user.InterestedCategoryIds)
  -> ProfileResponse
```

### Profile Location Flow

Files:

```text
Quraaa.API/Controllers/ProfileController.cs
Quraaa.API/Requests/Profiles/ProfileLocationRequests.cs
Quraaa.Application/Features/Profiles/Commands/CreateLocation/
Quraaa.Application/Features/Profiles/Commands/UpdateLocation/
Quraaa.Application/Features/Profiles/Commands/SetDefaultLocation/
Quraaa.Application/Features/Profiles/Commands/DeleteLocation/
Quraaa.Application/Features/Profiles/Queries/GetMyLocations/
Quraaa.Domain/User/Entities/UserLocation.cs
Quraaa.Domain/User/ValueObjects/GeoLocation.cs
Quraaa.Persistence/Configurations/UserConfiguration.cs
Quraaa.Persistence/Configurations/UserLocationConfiguration.cs
Quraaa.Persistence/Migrations/20260809202025_AddMultipleUserLocations.cs
```

Flow:

```text
POST /api/profile/locations
  -> [Authorize] and JWT UserId extraction
  -> CreateLocationCommand with token-derived UserId
  -> validate name/address and finite latitude/longitude
  -> UserAggregate.AddLocation(...)
  -> first or explicitly selected location becomes DefaultLocationId
  -> renew LocationConcurrencyStamp so overlapping writes return 409
  -> persist UserLocation child row

PUT /api/profile/locations/{locationId}
PUT /api/profile/locations/{locationId}/default
DELETE /api/profile/locations/{locationId}
  -> [Authorize] and JWT UserId extraction
  -> load only the authenticated user's aggregate and locations
  -> resolve locationId inside that collection; foreign/missing ids return 404
  -> edit, select the default, or remove one child
  -> deleting the default promotes the oldest remaining location
```

`GetMyProfile` and `UpdateProfile` return the complete `locations` collection plus nullable `location` as the default compatibility alias. Names may repeat. Address is nullable. Zero coordinates are valid, while non-finite and out-of-range values are rejected in validation/domain logic and constrained in PostgreSQL. `GetOrderCheckoutContext` exposes default-first choices only when the current cart contains a physical item. `CreateOrderCommandHandler` resolves an explicit owned saved-location ID, otherwise uses `DefaultLocation` for physical/mixed orders, and snapshots coordinates into the order; it does not attach shipping data to digital-only orders.

### Library Registration Flow

The authenticated mobile entry point is `POST /api/libraries/register`. It extracts both `UserId` and the access token's `sid`, creates a 32-byte Base64URL credential, stores only a `sha256:` hash, and returns `${LIBRARY_DASHBOARD_REGISTER_URL}#token=<credential>` with `Cache-Control: no-store`. The dashboard URL must use HTTPS outside Development; Development permits HTTP only for a loopback URL. Its origin is the sole origin allowed by the registered `library-dashboard` CORS policy. The token is valid for 15 minutes and remains bound to the issuing active refresh family.

Dashboard endpoints send the token in a body, never a query string:

```text
POST /api/libraries/register/context
POST /api/libraries/register/submit
POST /api/libraries/register/email/resend
POST /api/libraries/register/email/verify
```

Details submission requires `token`, `libraryName`, `location`, `libraryImage`, `headerImage`, and `email`. Images remain JPG/PNG with the existing 5 MB limits and matching binary-signature validation. The backend uploads them to Cloudinary before creating the `AwaitingEmailVerification` library, and the existing database fields store absolute HTTPS URLs. Submission marks the session used, extends it for 24 hours, and persists the first HMAC-hashed OTP challenge in one EF save before SMTP handoff. The committed response reports `Sent`, definite `NotSent`, or ambiguous `Unknown`; a database failure attempts best-effort Cloudinary deletion, while an SMTP failure does not delete images referenced by the committed application and can be recovered by reissuing a mobile link and redelivering the same code.

The OTP is never stored in plaintext. `LIBRARY_EMAIL_OTP_PEPPER` is an independent secret used both to derive the six-digit code and to HMAC it with separate purposes and the library id, user id, normalized email, and generation. Resends retain that generation/code and failed-attempt state; the client must echo the exposed `verificationId`, and mismatched generations fail without consuming an attempt from the current challenge. EF concurrency prevents a stale resend/verification mutation from winning. Successful verification atomically sets `EmailVerifiedAtUtc`, changes `AwaitingEmailVerification` to `Pending`, consumes the challenge, and completes the temporary session.

The admin request repository excludes `AwaitingEmailVerification` and legacy unverified `Pending` rows. `LibraryAggregate.Approve` and `Reject` independently require both `Pending` and `EmailVerifiedAtUtc`, so a direct PATCH cannot bypass email verification. Approval runs in the shared EF transaction, changes `UserAggregate.Role`, adds the Identity `LibraryOwner` role, invalidates the old refresh family immediately, and inserts one unique `LibraryApprovalNotification`; startup reconciliation remains an idempotent repair path. A hosted worker claims committed outbox rows with a PostgreSQL lease, then independently sends SMTP to the verified `LibraryAggregate.Email` and FCM to the owner's registered devices. Definite failures retry with bounded backoff, ambiguous SMTP acceptance is not automatically retried, permanently invalid FCM tokens are removed, and no provider failure reverses approval. The push payload sets `type=library_registration_approved`, `libraryId`, `approvalStatus=Approved`, and `requiresReauthentication=true` because the previous access-token family is no longer active.

### Library Listing Flow

Files:

```text
Quraaa.API/Controllers/LibrariesController.cs
Quraaa.API/Requests/Libraries/GetLibraryBooksRequest.cs
Quraaa.Application/Features/Libraries/Queries/GetLibraries/GetLibrariesQuery.cs
Quraaa.Application/Features/Libraries/Queries/GetLibraries/GetLibrariesQueryHandler.cs
Quraaa.Application/Features/Libraries/Queries/GetLibraries/PublicLibraryResponse.cs
Quraaa.Application/Features/Listings/Queries/GetLibraryBooks/
Quraaa.Application/Features/Libraries/Interfaces/ILibraryRepository.cs
Quraaa.Persistence/Repositories/LibraryRepository.cs
```

Routes:

```text
GET /api/libraries
GET /api/libraries/{libraryId}/books
```

Authentication:

```text
GET /api/libraries -> AllowAnonymous
GET /api/libraries/{libraryId}/books -> [Authorize(Roles = "User,LibraryOwner")]
```

Library discovery parameters:

```text
pageNumber: int (default 1)
pageSize: int (default 10)
searchTerm: string? (library name or location)
```

Library-book parameters:

```text
pageNumber: int (default 1)
pageSize: int (effective range 1..20)
searchTerm: string? (title, author, or language)
sortBy: title, author, or quantity
sortDescending: bool
```

Flow:

```text
HTTP GET /api/libraries
  -> LibrariesController.GetLibraries(query)
  -> Mediator.Send(query)
  -> GetLibrariesQueryHandler.Handle(...)
  -> ILibraryRepository.GetPagedAsync(pageNumber, pageSize, searchTerm)
  -> returns PagedResult<PublicLibraryResponse>

HTTP GET /api/libraries/{libraryId}/books
  -> role authorization
  -> GetLibraryBooksQuery
  -> ILibraryRepository.GetLibraryBooksAsync(...)
  -> filter Active listings and join Books plus optional Categories
  -> apply search/sort/paging
  -> returns PagedResult<ListingSummaryResponse>
```

Only libraries with `ApprovalStatus = Approved` are returned.

### Ebook Listing Flow

Files:

```text
Quraaa.API/Controllers/EbooksController.cs
Quraaa.Application/Features/Ebooks/Common/EbookResponse.cs
Quraaa.Application/Features/Ebooks/Queries/GetEbooks/GetEbooksQuery.cs
Quraaa.Application/Features/Ebooks/Queries/GetEbooks/GetEbooksQueryHandler.cs
Quraaa.Application/Features/Ebooks/Queries/GetEbooks/GetEbooksQueryValidator.cs
Quraaa.Application/Features/Ebooks/Interfaces/IEbookRepository.cs
Quraaa.Persistence/Repositories/EbookRepository.cs
Quraaa.Persistence/Seed/EbookSeeder.cs
Quraaa.Domain/Catalog/BookAggregate.cs
Quraaa.Domain/Marketplace/ListingAggregate.cs
```

Route:

```text
GET /api/ebooks
```

Authentication:

```text
AllowAnonymous
```

Query parameters:

```text
pageNumber: int (default 1)
pageSize: int (default 20, max 100)
searchTerm: string? (optional title/author filter)
```

Seeded ebook details:

- `EbookSeeder` creates a `BookAggregate` and an active digital `ListingAggregate`.
- The seeded listing uses the private logical path `DigitalAssetUrl = "books/book1.pdf"`.
- Store the PDF at `Quraaa.API/storage/books/book1.pdf`; the project copies `storage/books/**` into build and publish output.
- Public `/uploads/books/*.pdf` requests are blocked, and `EbookResponse` omits `DigitalAssetUrl`.
- A paid buyer reads the PDF through `GET /api/purchases/{purchaseId}/stream`, which verifies purchase ownership and then proxies a short-lived signed Cloudinary source inline or resolves a contained legacy file under `storage/books`. No attachment-download route is exposed.

Flow:

```text
HTTP GET /api/ebooks
  -> EbooksController.GetEbooks(query)
  -> GetEbooksQuery
  -> GetEbooksQueryHandler
  -> BaseApplicationService validates GetEbooksQuery
  -> IEbookRepository.GetPagedAsync(pageNumber, pageSize, searchTerm)
  -> EbookRepository joins Listings to Books
  -> filters ListingFormat.Digital, ListingStatus.Active, and non-null DigitalAssetUrl
  -> returns PagedResult<EbookResponse> without the private asset path
```

### Book Discovery and Favorites Flows

Files:

```text
Quraaa.API/Controllers/BooksController.cs
Quraaa.API/Controllers/FavoriteBooksController.cs
Quraaa.Application/Features/Books/
Quraaa.Application/Features/FavoriteBooks/
Quraaa.Persistence/Repositories/BookPopularityRepository.cs
Quraaa.Persistence/Repositories/FavoriteBookRepository.cs
Quraaa.Persistence/Configurations/FavoriteBookConfiguration.cs
Quraaa.Domain/Favorites/FavoriteBookAggregate.cs
Quraaa.Domain/Ratings/BookRatingAggregate.cs
Quraaa.Domain/Purchases/BookPurchaseAggregate.cs
```

Flow:

```text
GET /api/books/most-popular
  -> BookPopularityRepository left-joins Books with grouped BookPurchases, BookRatings, and Active Listings
  -> optional search/unranked filter
  -> popularity/purchase/rating/average-rating ordering
  -> PagedResult<PopularBookResponse>

GET /api/books/recommended
  -> JWT UserId -> UserAggregate.InterestedCategoryIds
  -> category + exact normalized language + active-listing filters
  -> popularity ordering
  -> PagedResult<PopularBookResponse>

GET /api/favorite-books
  -> JWT UserId -> paged Favorites/Books join

POST /api/favorite-books/{bookId}
  -> validate user/book
  -> return existing favorite or create FavoriteBookAggregate
  -> filtered unique index handles concurrent duplicate adds

DELETE /api/favorite-books/{bookId}
  -> validate user/book and soft-delete the active favorite
```

There is no HTTP endpoint yet to create/update `BookRatingAggregate`; rating rows can affect discovery when present through seed/manual/database work.

### Listing Management Flows

Files:

```text
Quraaa.API/Controllers/LibrariesController.cs
Quraaa.API/Controllers/LibraryListingsController.cs
Quraaa.API/Controllers/UserListingsController.cs
Quraaa.Application/Features/Listings/
Quraaa.Persistence/Repositories/BookRepository.cs
Quraaa.Persistence/Repositories/ListingRepository.cs
Quraaa.Persistence/Repositories/LibraryRepository.cs
Quraaa.Infrastructure/Services/GoogleBooksService.cs
Quraaa.Domain/Marketplace/ListingAggregate.cs
```

Flow:

```text
library/user physical listing create
  -> JWT UserId
  -> resolve ISBN from Books; otherwise query Google Books and create BookAggregate
  -> reject duplicate seller/book listing
  -> ListingAggregate.CreateForLibrary/CreateForUser
  -> PendingReview listing

library listing update
  -> resolve Active listing
  -> resolve caller's Approved library and verify ownership
  -> reject stock changes while an unpaid pending order reserves the listing
  -> update any supplied price/stock/condition

library listing detail
  -> listing/book/optional-category projection

current user listings
  -> filter Active + SellerType.User + token UserId
  -> search/sort and fixed current page defaults
```

`ListingAggregate.CreateForLibrary` and `CreateForUser` start in `PendingReview`; only active listings appear in library/user listing queries and cart lookups. There is currently no listing approval or removal endpoint.

### Cart, Order, Stripe, Fulfillment, and Purchase History Flows

Files:

```text
Quraaa.API/Controllers/CartController.cs
Quraaa.API/Controllers/OrdersController.cs
Quraaa.API/Controllers/PaymentsController.cs
Quraaa.API/Controllers/PurchaseHistoryController.cs
Quraaa.API/Controllers/SellerOrdersController.cs
Quraaa.API/Services/ExpiredOrderPaymentReconciliationService.cs
Quraaa.Application/Features/Carts/
Quraaa.Application/Features/Orders/
Quraaa.Application/Features/Payments/
Quraaa.Application/Features/Purchases/
Quraaa.Infrastructure/Services/StripePaymentService.cs
Quraaa.Persistence/Repositories/CartRepository.cs
Quraaa.Persistence/Repositories/BookPurchaseRepository.cs
Quraaa.Persistence/Repositories/OrderRepository.cs
Quraaa.Persistence/Repositories/PaymentEventInboxRepository.cs
Quraaa.Domain/Cart/
Quraaa.Domain/Orders/
Quraaa.Domain/Purchases/BookPurchaseAggregate.cs
```

Flow:

```text
cart read/mutation
  -> JWT UserId
  -> retrieve Active or PendingPayment cart with owned CartItems
  -> partial unique index permits one non-deleted open cart per user
  -> validate Active listing, cumulative available stock, and quantity 1..999,999
  -> allow at most 100 distinct listings
  -> keep unit-price snapshot on CartItem
  -> reject mutation while PendingPayment

order checkout
  -> require non-empty modifiable cart
  -> reject more than 100 lines or quantity above 999,999 before per-item queries
  -> re-check listing activity, seller ownership, and stock
  -> snapshot book/seller/price/asset data into OrderItems
  -> create OrderAggregate and reserve physical stock
  -> CartStatus.PendingPayment + PendingOrderId
  -> create PaymentAttempt
  -> save order + reservation + cart lock before calling Stripe
  -> create idempotent Stripe Checkout Session and attach it locally

Stripe webhook
  -> verify Stripe-Signature
  -> correlate Order + PaymentAttempt and validate mode/session/amount/currency
  -> claim provider event in ProcessedPaymentEvents
  -> paid: shared finalizer marks order/cart paid and creates order-linked purchases
  -> paid: do not decrement stock again; the reservation is the sale
  -> async failure/expiry: release reserved stock and reopen cart
  -> one DbContext SaveChanges persists the whole state transition

cancel/expiry recovery
  -> hosted reconciliation checks expired attempts every minute
  -> Created attempt: replay the exact request with checkout:{attempt.Id:N}
  -> attach a recovered Session and retrieve authoritative Stripe state
  -> complete paid order through the shared finalizer when its webhook was missed
  -> keyset-scan a fixed cutoff so removed rows cannot shift pages and deferred attempts are retried after each finite scan
  -> never auto-expire an unattached replay older than the safe idempotency window
  -> preserve complete-but-unpaid asynchronous payments for a later result
  -> otherwise expire the unpaid order and release every physical reservation
  -> reopen the source cart after confirmed failure/expiry

seller fulfillment
  -> list paid physical items owned by the user or their library
  -> Pending -> Processing -> Fulfilled
  -> complete the order when every item is fulfilled

paid ebook in-app streaming
  -> buyer/purchase authorization
  -> require an owned purchase with a private storage-reference snapshot
  -> resolve an owned authenticated Cloudinary raw asset or contained legacy storage/books file
  -> proxy an inline private PDF response with range and conditional-request support

buy/sell history
  -> JWT UserId
  -> BookPurchases joined to Books/Categories
  -> sell history additionally joins User-owned Listings
  -> PagedResult history response
```

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
POST /api/otp/send
POST /api/otp/verify
```

Authentication:

```text
AllowAnonymous
```

`POST /api/otp/send` request body:

```json
{
  "phoneNumber": "+9647XXXXXXXXX"
}
```

`FirebaseSmsGateway` reads the SMS gateway FCM token from `OTP_DEVICE_TOKEN` in configuration/environment variables.

`POST /api/otp/verify` request body:

```json
{
  "phoneNumber": "+9647XXXXXXXXX",
  "code": "123456"
}
```

OTP behavior:

- The API generates a 6-digit OTP with `RandomNumberGenerator`.
- OTPs expire after 10 minutes.
- Send requests are throttled for 60 seconds per normalized phone number and client IP.
- Definite pre-dispatch failures and permanent FCM device-token rejections clear only the owner-matched OTP and request leases; ambiguous dispatch outcomes retain them.
- Verification allows up to 5 failed attempts in a 5-minute window.
- After too many invalid attempts, the inspected OTP is consumed when it is still current, and verification is locked for 5 minutes even if a concurrent resend replaced it.
- Successful verification atomically consumes one OTP and then clears failed-attempt state.
- The standalone OTP flow stores its state under the `standalone-otp` cache namespace, so it never collides with registration (`register-otp`) or forgot-password (`forgot-password-otp`) OTP state.
- The standalone OTP feature does not mark a user or phone number as verified and is not used by login. Login only uses the registration OTP namespace when valid credentials belong to an unverified pending registration.

Flow:

```text
HTTP POST /api/otp/send
  -> OtpController.SendOtp(body request)
  -> SendOtpCommand(phoneNumber, clientIp)
  -> SendOtpCommandHandler.Handle(...)
  -> BaseApplicationService validates SendOtpCommand
  -> IPhoneService.FormatToE164(phone)
  -> IOtpCacheService checks send and verification lockouts
  -> handler generates OTP and stores it in IDistributedCache under the `standalone-otp` namespace
  -> IFirebaseSmsGateway.SendSmsRequestAsync(phone, otp, "standalone")
  -> FirebaseSmsGateway reads OTP_DEVICE_TOKEN and sends a high-priority FCM data message with a 45-second dispatch TTL plus request/purpose/expiry metadata

HTTP POST /api/otp/verify
  -> OtpController.VerifyOtp(body request)
  -> VerifyOtpCommand(phoneNumber, code, clientIp)
  -> VerifyOtpCommandHandler.Handle(...)
  -> BaseApplicationService validates VerifyOtpCommand
  -> handler reads OTP from IDistributedCache under the `standalone-otp` namespace
  -> handler compares in fixed time and atomically consumes only the inspected OTP generation
  -> success clears failed-attempt state; invalid attempts update failed-attempt counters
```

### Notifications Flow

Files:

```text
Quraaa.API/Controllers/NotificationsController.cs
Quraaa.API/Requests/Notifications/RegisterPushDeviceRequest.cs
Quraaa.API/Requests/Notifications/UnregisterPushDeviceRequest.cs
Quraaa.API/Requests/Notifications/SendNotificationRequest.cs
Quraaa.API/Services/LibraryApprovalNotificationDeliveryService.cs
Quraaa.Application/Features/Notifications/Commands/RegisterPushDevice/
Quraaa.Application/Features/Notifications/Commands/UnregisterPushDevice/
Quraaa.Application/Features/Notifications/Commands/SendNotification/
Quraaa.Application/Features/Notifications/Common/NotificationSendResponse.cs
Quraaa.Application/Features/Notifications/Interfaces/IFirebaseNotificationService.cs
Quraaa.Application/Features/Notifications/Interfaces/IPushDeviceRepository.cs
Quraaa.Domain/User/Entities/PushDevice.cs
Quraaa.Persistence/Repositories/PushDeviceRepository.cs
Quraaa.Infrastructure/Services/FirebaseNotificationService.cs
```

Routes:

```text
PUT /api/notifications/devices
POST /api/notifications/device-token
DELETE /api/notifications/devices
POST /api/notifications/send
POST /api/notifications/test
```

Authentication:

```text
PUT /api/notifications/devices -> Authorization: Bearer <access-token>
POST /api/notifications/device-token -> Authorization: Bearer <access-token> (deprecated alias)
DELETE /api/notifications/devices -> Authorization: Bearer <access-token>
POST /api/notifications/send -> Authorization: Bearer <access-token>
POST /api/notifications/test -> AllowAnonymous when enabled
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
HTTP PUT /api/notifications/devices
  -> NotificationsController extracts UserId from token claims
  -> RegisterPushDeviceCommand(userId, deviceToken)
  -> PushDeviceRepository hashes the token, upserts/reassigns it, and prunes old devices

HTTP POST /api/notifications/device-token (deprecated compatibility alias)
  -> follows the same RegisterPushDeviceCommand and PushDevices path as PUT /devices

HTTP DELETE /api/notifications/devices
  -> NotificationsController extracts UserId from token claims
  -> UnregisterPushDeviceCommand(userId, deviceToken)
  -> PushDeviceRepository removes only the caller-owned matching token

HTTP POST /api/notifications/send
  -> NotificationsController.Send(body request)
  -> [Authorize] validates JWT bearer token
  -> NotificationsController extracts UserId from token claims
  -> SendNotificationCommand(userId, deviceToken, title, body, data)
  -> SendNotificationCommandHandler.Handle(...)
  -> BaseApplicationService validates SendNotificationCommand
  -> IUserRepository.GetUserByIdAsync(userId) confirms the authenticated profile exists
  -> IFirebaseNotificationService.SendToDeviceAsync(...)
  -> FirebaseNotificationService sends an FCM notification message to the requested device token

HTTP POST /api/notifications/test
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
GET /api/categories
GET /api/categories/{categoryId}
POST /api/categories
```

Authentication:

```text
GET /api/categories -> anonymous by controller configuration
GET /api/categories/{categoryId} -> anonymous by controller configuration
POST /api/categories -> Authorization: Bearer <admin-access-token>
```

`POST /api/categories` requires the `Admin` role.

The `CategoryAggregate` model includes:

- `Code` — unique stable category code used by seed/configuration logic; registration/profile interest payloads use `CategoryAggregate.Id` GUIDs.
- `NameAr` — Arabic name.
- `NameEn` — English name.
- `ParentCategoryId` — optional parent category.
- `IsActive` — soft-active flag; inactive categories are filtered from `GET` queries globally.

Flow:

```text
HTTP GET /api/categories
  -> CategoriesController.GetAllCategories()
  -> GetAllCategoriesQuery
  -> GetAllCategoriesQueryHandler
  -> ICategoryRepository.GetAllAsync()
  -> List<CategoryResponse>

HTTP GET /api/categories/{categoryId}
  -> CategoriesController.GetCategoryById(categoryId)
  -> GetCategoryByIdQuery(categoryId)
  -> GetCategoryByIdQueryHandler
  -> ICategoryRepository.GetByIdAsync(categoryId)
  -> CategoryResponse or NotFound

HTTP POST /api/categories
  -> CategoriesController.CreateCategory(request)
  -> [Authorize(Roles = "Admin")]
  -> CreateCategoryCommand(...)
  -> CreateCategoryCommandHandler
  -> ICategoryRepository.AddAsync(category)
  -> CategoryResponse
```

### Authors Flow

Files:

```text
Quraaa.API/Controllers/AdminAuthorsController.cs
Quraaa.API/Controllers/AdminModerationController.cs
Quraaa.API/Controllers/AuthorsController.cs
Quraaa.API/Requests/Admin/BulkIdsRequest.cs
Quraaa.API/Requests/Admin/GetAdminUsersRequest.cs
Quraaa.Application/Features/Admin/Commands/SetAuthorActivation/
Quraaa.Application/Features/Admin/Commands/DeleteAuthors/
Quraaa.Application/Features/Admin/Queries/GetAuthors/
Quraaa.Application/Features/Admin/Common/AdminAuthorResponse.cs
Quraaa.Application/Features/Admin/Common/BulkModerationResult.cs
Quraaa.Application/Features/Admin/Interfaces/IAdminModerationRepository.cs
Quraaa.Application/Features/Authors/Commands/CreateAuthor/
Quraaa.Application/Features/Authors/Commands/UpdateAuthor/
Quraaa.Application/Features/Authors/Commands/DeleteAuthor/
Quraaa.Application/Features/Authors/Queries/GetAuthorById/
Quraaa.Application/Features/Authors/Queries/GetAuthorsPaginated/
Quraaa.Application/Features/Authors/Queries/GetPublicAuthorDetails/
Quraaa.Application/Features/Authors/Queries/GetAuthorBooks/
Quraaa.Application/Features/Authors/Common/AuthorResponse.cs
Quraaa.Application/Features/Authors/Interfaces/IAuthorRepository.cs
Quraaa.Persistence/Repositories/AdminModerationRepository.cs
Quraaa.Persistence/Repositories/AuthorRepository.cs
Quraaa.Persistence/Configurations/AuthorConfiguration.cs
Quraaa.Domain/Author/AuthorAggregate.cs
```

Routes:

```text
GET    /api/authors/{authorId}
GET    /api/authors/{authorId}/books
POST   /api/admin/authors
GET    /api/admin/authors
GET    /api/admin/authors/{id}
PUT    /api/admin/authors/{id}
POST   /api/admin/authors/{id}/activation
POST   /api/admin/authors/activation
DELETE /api/admin/authors/{id}
DELETE /api/admin/authors
```

Authentication:

```text
GET /api/authors/* -> anonymous
All /api/admin/authors routes -> Authorization: Bearer <admin-access-token> ([Authorize(Roles = "Admin")] at the controller level)
```

The `AuthorAggregate` model includes:

- `Name` — required, max 150 characters.
- `Bio` — optional, max 2000 characters.
- `PhotoUrl` — optional, max 500 characters; must be an absolute `http`/`https` URL when present.
- `BirthDate` — optional; must be in the past when present.

`AuthorAggregate` inherits the shared `AggregateRoot` soft-delete state. Moderation deactivation calls `Delete(adminId)` and restoration calls `Restore(adminId)`. Permanent deletion is a separate operation: `DeleteAuthorsCommandHandler` only removes an already-deactivated author after `AdminModerationRepository` confirms that no non-deleted books reference it. The older direct `DeleteAuthorCommandHandler` remains compiled but is no longer exposed by an HTTP action.

Flow:

```text
HTTP POST /api/admin/authors
  -> AdminAuthorsController.CreateAuthor(command)
  -> [Authorize(Roles = "Admin")]
  -> CreateAuthorCommand(...)
  -> CreateAuthorCommandHandler
  -> IAuthorRepository.AddAsync(author)
  -> AuthorResponse

HTTP GET /api/admin/authors
  -> AdminModerationController.GetAuthors(request)
  -> GetAuthorsQuery(pageNumber, pageSize, searchTerm, includeDeactivated)
  -> GetAuthorsQueryHandler
  -> IAdminModerationRepository.GetAuthorsAsync(...)
  -> PagedResult<AdminAuthorResponse>

HTTP GET /api/admin/authors/{id}
  -> AdminAuthorsController.GetAuthorById(id)
  -> GetAuthorByIdQuery(id)
  -> GetAuthorByIdQueryHandler
  -> IAuthorRepository.GetByIdAsync(id)
  -> AuthorDetailsResponse or NotFound

HTTP GET /api/authors/{authorId}
  -> AuthorsController.GetAuthorDetails(authorId)
  -> GetPublicAuthorDetailsQuery
  -> IAuthorRepository.GetByIdAsync(authorId)
  -> PublicAuthorDetailsResponse or NotFound

HTTP GET /api/authors/{authorId}/books
  -> AuthorsController.GetAuthorBooks(authorId, request)
  -> GetAuthorBooksQuery
  -> IAuthorRepository.ExistsAsync(authorId)
  -> IHomeCatalogRepository.GetByAuthorAsync(...)
  -> PagedResult<HomeBookResponse> or NotFound

HTTP PUT /api/admin/authors/{id}
  -> AdminAuthorsController.UpdateAuthor(id, command)
  -> UpdateAuthorCommand(...) with { Id = id, ModifiedBy = adminId }
  -> UpdateAuthorCommandHandler
  -> IAuthorRepository.GetByIdAsync(id) -> AuthorAggregate.UpdateDetails(...) -> IAuthorRepository.SaveChangesAsync()
  -> AuthorResponse or NotFound

HTTP POST /api/admin/authors/{id}/activation?deactivate=true|false
  -> AdminModerationController.SetAuthorActivation(id, deactivate)
  -> SetAuthorActivationCommand { AdminId, Ids = [id], Deactivate }
  -> AuthorAggregate.Delete(adminId) or Restore(adminId)
  -> BulkModerationResult with one per-record outcome

HTTP POST /api/admin/authors/activation
  -> AdminModerationController.SetAuthorsActivation({ ids, deactivate })
  -> SetAuthorActivationCommand (maximum 200 ids)
  -> partial per-record processing
  -> BulkModerationResult

HTTP DELETE /api/admin/authors/{id}
  -> AdminModerationController.DeleteAuthor(id)
  -> DeleteAuthorsCommand { AdminId, Ids = [id] }
  -> require IsDeleted and no active Books blocker
  -> BulkModerationResult with one per-record outcome

HTTP DELETE /api/admin/authors
  -> AdminModerationController.DeleteAuthors({ ids })
  -> DeleteAuthorsCommand (maximum 100 ids)
  -> partial per-record processing; remove only eligible authors
  -> BulkModerationResult
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
- **Listing review lifecycle**: all non-seeded library/user physical listings start in `PendingReview`, but there is no approval/rejection/removal endpoint. Active-only list/cart queries therefore do not expose newly created listings.
- **Library inventory authorization mismatch**: `LibraryListingsController` requires `LibraryAdmin`, while `Role`, seeders, and library login use `LibraryOwner`. No current code grants `LibraryAdmin`, so those three routes are not reachable with the roles produced by this repository.
- **Library add/update edge cases**: `AddPhysicalBookCommand.Isbn` is declared nullable and has no required validator but the handler dereferences it; omission can produce a `500`. `UpdateListingCommandHandler` retrieves only `Active` listings, so a new `PendingReview` listing cannot be updated and an `OutOfStock` listing cannot be restocked through that route.
- **Single-session authentication model**: each Identity user has one active refresh-token family. A fresh login invalidates the prior family's refresh and access tokens. Multiple per-device concurrent sessions, device-scoped logout, and independent session management are not modeled yet.
- **OTP coverage**: registration, forgot-password, admin login, and standalone OTP use OTP state. Regular user login resends the registration OTP only for valid credentials on an unverified pending registration; library-owner login does not use OTP, and standalone OTP still does not mark a phone/user verified.
- **Rating API**: `BookRatingAggregate`, its table, and popularity aggregation exist, but there are no rating create/update/read endpoints.
- **Pagination inconsistency**: `PaginationRequestDTO` silently coerces invalid values and caps at 20, while other feature validators allow 100. `GET /api/listings/me` cannot bind page fields and is fixed to page 1/size 10.
- **Category projection order**: listing/library-book/purchase repository projections pass `NameEn` and `NameAr` in the opposite order expected by `CategoryResponse`; profile/category handlers use the correct order.
- **Aggregate mapping consistency**: domain aggregates retain scalar IDs, but several current EF configurations express cross-aggregate foreign keys with navigationless `HasOne<TAggregate>()`. Keep navigation properties out of the domain and decide on one persistence convention before adding more mappings.
- **Tests**: no unit, integration, or end-to-end tests exist.
- **CI/CD**: only branch-name validation is automated; add build, test, and publish workflows.
- **Production readiness**: `Notifications:AllowTestEndpoint=true` and `Stripe:IsTestMode=true` are committed defaults. Disable the test-only settings as appropriate, configure Redis plus Stripe/Firebase secrets, rotate secrets, and add payment/OTP/checkout observability before production deployment.
