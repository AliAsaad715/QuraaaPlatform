# QuraaaPlatform

QuraaaPlatform is a `.NET 10` REST API for a book marketplace and library network. It supports user, administrator, and library-owner accounts; physical and digital listings; personalized discovery; carts and orders; Stripe Checkout; seller fulfillment; and paid ebook delivery.

The solution uses a Clean Architecture / vertical-slice structure with ASP.NET Core, Entity Framework Core, PostgreSQL, MediatR, FluentValidation, ASP.NET Core Identity, Firebase, Redis-compatible distributed caching, Stripe, and Google Books.

> Current state: the API is implemented and buildable, migrations and development seeders run at startup, and Swagger UI is available. There is not yet an automated test project.

## Contents

- [Features](#features)
- [Architecture](#architecture)
- [Repository layout](#repository-layout)
- [Prerequisites](#prerequisites)
- [Local setup](#local-setup)
- [Configuration](#configuration)
- [API conventions](#api-conventions)
- [Endpoint reference](#endpoint-reference)
- [Important workflows](#important-workflows)
- [Database migrations and seed data](#database-migrations-and-seed-data)
- [Build and validation](#build-and-validation)
- [Docker and deployment](#docker-and-deployment)
- [Security and production notes](#security-and-production-notes)
- [Contributing](#contributing)

## Features

### Authentication and accounts

- Phone-based user registration with OTP verification.
- User login and approved library-owner login.
- Administrator password-plus-OTP login.
- JWT access tokens and opaque refresh tokens with rotation, replay detection, family revocation, and access-token revocation.
- Refresh-token-authenticated logout that also accepts an optional bearer token.
- Authenticated password changes and unauthenticated forgot-password recovery.
- Profile, interests, and geographic-location management.

### Catalog, discovery, and libraries

- Public category, ebook, library, and library-catalog browsing.
- Most-popular books based on purchase and rating metrics, with active-listing counts in the response.
- Authenticated recommendations based on user interests and `ar` / `en` language selection.
- Favorite-book management.
- Library applications with administrator approval or rejection.
- Approved library-owner profiles and listing management.
- ISBN resolution from the local catalog first, then Google Books.
- Physical listings for users and libraries, plus PDF-based digital listings for libraries.

### Commerce and fulfillment

- One open cart per user with quantity, stock, and Stripe line-item limits.
- Order-first checkout: the order, cart lock, stock reservations, and payment attempt are persisted before Stripe is called.
- Stripe Checkout session creation, signed webhook processing, event idempotency, and expired-payment reconciliation.
- Buyer order listing, detail, shipping-location update, checkout recovery, cancellation, and archive operations.
- Paid ebook authorization and download.
- Seller queues and physical-item processing / fulfillment transitions.
- Buy and sell history.

### Platform integrations

- Firebase Cloud Messaging for push notifications.
- Firebase data messages to a separate Android SMS-gateway device for OTP delivery.
- Redis-compatible distributed OTP and access-token-revocation caching, with an in-memory development fallback.
- OpenAPI 3.0 and Swagger UI.
- Docker and `Procfile` deployment entry points.

## Architecture

The project combines Clean Architecture boundaries with CQRS-style vertical slices:

```text
Quraaa.API ───────────────┐
  │                       │
  ├──> Quraaa.Application ├──> Quraaa.Domain
  ├──> Quraaa.Persistence ┘
  └──> Quraaa.Infrastructure ──> Quraaa.Application

Quraaa.Persistence ──> Quraaa.Application + Quraaa.Domain
Quraaa.Domain ──> no project references
```

| Project                 | Responsibility                                                                                                       |
| ----------------------- | -------------------------------------------------------------------------------------------------------------------- |
| `Quraaa.Domain`         | Aggregates, entities, value objects, enums, and business invariants.                                                 |
| `Quraaa.Application`    | Commands, queries, handlers, validators, DTOs, result types, and service interfaces.                                 |
| `Quraaa.Persistence`    | EF Core context and mappings, PostgreSQL migrations, repositories, Identity persistence, and seeders.                |
| `Quraaa.Infrastructure` | Stripe, Firebase, Redis/cache, Google Books, and other external-service implementations.                             |
| `Quraaa.API`            | Controllers, HTTP contracts, authentication setup, OpenAPI, file adapters, hosted services, and application startup. |

Application operations are dispatched through MediatR. FluentValidation validators are registered automatically, and application results are mapped centrally to HTTP `200`, `400`, `401`, `403`, `404`, and `409` responses.

## Repository layout

```text
QuraaaPlatform/
├── Quraaa.API/
│   ├── Controllers/
│   ├── DesignTime/
│   ├── Extensions/
│   ├── Requests/
│   ├── Services/
│   ├── storage/books/             # Private seeded ebook files
│   └── wwwroot/                   # Public static images and current upload area
├── Quraaa.Application/
│   ├── Features/                  # CQRS feature slices
│   └── Shared/
├── Quraaa.Domain/                 # Domain model
├── Quraaa.Infrastructure/         # External providers
├── Quraaa.Persistence/
│   ├── Configurations/
│   ├── Data/
│   ├── Migrations/
│   ├── Repositories/
│   ├── Seed/
│   └── Services/
├── .github/workflows/
├── AGENTS.md                      # Detailed implementation inventory for coding agents
├── Dockerfile
├── Procfile
└── QuraaaPlatform.slnx
```

Generated `bin/` and `obj/` directories are not source and should not be edited.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- PostgreSQL
- A Firebase service-account credential
- Stripe test credentials for local development
- An FCM registration token for the Android OTP SMS gateway when testing OTP flows
- Redis for shared or production deployments; local Development can use the in-memory fallback
- Optional: the `dotnet-ef` CLI for creating or applying migrations manually
- Optional: a Google Books API key for ISBNs not already in the local catalog

## Local setup

1. Clone the repository and enter it:

   ```powershell
   git clone https://github.com/AliAsaad715/QuraaaPlatform.git
   Set-Location QuraaaPlatform
   ```

2. Create the local environment file:

   ```powershell
   Copy-Item Quraaa.API/.env.example Quraaa.API/.env
   ```

   On Bash-compatible shells, use `cp Quraaa.API/.env.example Quraaa.API/.env`.

3. Update `Quraaa.API/.env` with at least PostgreSQL, JWT, Stripe, library-dashboard, and SMTP values:

   ```dotenv
   ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=quraaa;Username=postgres;Password=change-me

   JWT_SECRET_KEY=replace-with-a-long-random-development-secret
   JWT_ISSUER=Quraaa.API
   JWT_AUDIENCE=Quraaa.Clients
   JWT_DURATION_IN_MINUTES=60

   Stripe__SecretKey=sk_test_replace_me
   Stripe__WebhookSecret=whsec_replace_me
   Stripe__Currency=usd
   Stripe__IsTestMode=true

   BaseAPIURL=http://localhost:5153
   OTP_DEVICE_TOKEN=replace-with-the-sms-gateway-fcm-token

   LIBRARY_DASHBOARD_REGISTER_URL=http://localhost:3000/libraries/register
   LIBRARY_EMAIL_OTP_PEPPER=replace-with-an-independent-random-secret-at-least-32-characters
   MAIL_MAILER=smtp
   MAIL_HOST=smtp.gmail.com
   MAIL_PORT=587
   MAIL_USERNAME=replace-with-sender@gmail.com
   MAIL_PASSWORD=replace-with-a-google-app-password
   MAIL_ENCRYPTION=tls
   MAIL_FROM_ADDRESS=replace-with-sender@gmail.com
   MAIL_FROM_NAME="Quraaa Platform"
   ```

4. Configure Firebase using one of these supported approaches:
   - Put the service-account JSON at `Quraaa.API/storage/firebase/quraa.json` (the configured local path), or
   - Set `GOOGLE_APPLICATION_CREDENTIALS` / `Firebase__CredentialsPath` to an absolute credential path, or
   - Set `FIREBASE_CREDENTIALS_JSON` to the complete JSON value in the deployment environment.

   Firebase credentials under `Quraaa.API/storage/firebase/*.json` are ignored by Git. Never commit the service-account file.

5. Restore and run the API:

   ```powershell
   dotnet restore QuraaaPlatform.slnx
   Set-Location Quraaa.API
   dotnet run
   ```

6. Open the local API documentation:
   - Swagger UI: `http://localhost:5153/docs`
   - OpenAPI JSON: `http://localhost:5153/openapi/v1.json`
   - HTTPS launch profile: `https://localhost:7260`

At startup, the API applies pending EF Core migrations and runs all configured seeders. The database account therefore needs schema-migration permissions.

## Configuration

ASP.NET Core configuration is loaded from appsettings files, `.env`, environment variables, and command-line arguments. For nested environment keys, use double underscores, for example `Stripe__SecretKey` for `Stripe:SecretKey`. Environment variables override committed appsettings values.

| Key                                                    | Required                 | Purpose / behavior                                                                                                                                       |
| ------------------------------------------------------ | ------------------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `ConnectionStrings__DefaultConnection`                 | Yes                      | Npgsql connection string for PostgreSQL.                                                                                                                 |
| `JWT_SECRET_KEY`                                       | Yes                      | Symmetric JWT signing key. Startup fails when it is missing.                                                                                             |
| `JWT_ISSUER`                                           | No                       | Enables issuer validation when set.                                                                                                                      |
| `JWT_AUDIENCE`                                         | No                       | Enables audience validation when set.                                                                                                                    |
| `JWT_DURATION_IN_MINUTES`                              | No                       | Access-token lifetime; defaults to `60`.                                                                                                                 |
| `Stripe__SecretKey`                                    | Yes                      | Must start with `sk_test_` or `sk_live_` according to `Stripe__IsTestMode`.                                                                              |
| `Stripe__WebhookSecret`                                | Yes                      | Stripe endpoint signing secret; must start with `whsec_`.                                                                                                |
| `Stripe__Currency`                                     | No                       | Must resolve to `usd`; the configured default is `usd`.                                                                                                  |
| `Stripe__IsTestMode`                                   | No                       | Selects test or live key validation; defaults to test mode.                                                                                              |
| `GOOGLE_APPLICATION_CREDENTIALS`                       | One Firebase option      | Absolute path to a Firebase service-account file.                                                                                                        |
| `Firebase__CredentialsPath`                            | One Firebase option      | Configured credential path; local appsettings uses `storage/firebase/quraa.json`.                                                                        |
| `FIREBASE_CREDENTIALS_JSON`                            | One Firebase option      | Deployment-friendly full credential JSON; startup validates and writes it to the private Firebase storage directory.                                     |
| `OTP_DEVICE_TOKEN`                                     | For OTP delivery         | Server-side FCM token for the Android SMS-gateway device. It is not accepted from OTP request bodies.                                                    |
| `ConnectionStrings__Redis` / `Redis__ConnectionString` | Production recommended   | Redis connection string for distributed OTP and revocation data.                                                                                         |
| `REDIS_URL` / `REDIS_TLS_URL`                          | Alternative Redis option | Supports Heroku-style `redis://` and `rediss://` URLs.                                                                                                   |
| `Redis__InstanceName`                                  | No                       | Cache-key prefix; defaults to `Quraaa:Otp:`.                                                                                                             |
| `Otp:AllowInMemoryCacheInProduction`                   | Set in base appsettings  | Currently defaults to `true` in `Quraaa.API/appsettings.json`; override it to `false` and configure Redis for production.                                |
| `Notifications__AllowTestEndpoint`                     | No                       | Enables the anonymous notification test endpoint outside Development. Keep `false` in production.                                                        |
| `GoogleBooks__ApiKey`                                  | No                       | API key used during external ISBN lookup.                                                                                                                |
| `GoogleBooks__BaseUrl`                                 | No                       | Defaults to `https://www.googleapis.com/`.                                                                                                               |
| `BaseAPIURL`                                           | Recommended              | Prefix used when returning locally stored library image URLs.                                                                                            |
| `LIBRARY_DASHBOARD_REGISTER_URL`                       | Yes                      | Absolute HTTPS dashboard registration page URL. Development may use HTTP only on loopback; the API appends the temporary credential in the URL fragment. |
| `LIBRARY_EMAIL_OTP_PEPPER`                             | Yes                      | Independent secret of at least 32 characters used to HMAC library email OTPs; it must differ from the JWT secret.                                        |
| `MAIL_MAILER`                                          | Yes                      | Must be `smtp`.                                                                                                                                          |
| `MAIL_HOST` / `MAIL_PORT`                              | Yes                      | SMTP server and port; Gmail STARTTLS uses `smtp.gmail.com:587`.                                                                                          |
| `MAIL_USERNAME` / `MAIL_PASSWORD`                      | Yes                      | SMTP credentials. For Gmail, use an app password rather than the account password.                                                                       |
| `MAIL_ENCRYPTION`                                      | Yes                      | `tls`/`starttls` uses STARTTLS; `ssl`/`smtps` uses implicit TLS.                                                                                         |
| `MAIL_FROM_ADDRESS` / `MAIL_FROM_NAME`                 | Yes                      | Valid sender mailbox and single-line display name.                                                                                                       |
| `Swagger__ServerUrl`                                   | No                       | Overrides the server URL advertised in OpenAPI.                                                                                                          |
| `ADMIN_PHONE_NUMBER` / `ADMIN_PASSWORD`                | No                       | Creates or synchronizes the seeded administrator when both are set.                                                                                      |

The process also respects normal ASP.NET Core settings such as `ASPNETCORE_ENVIRONMENT`, `ASPNETCORE_URLS`, and command-line `--urls`.

## API conventions

- Canonical routes are lowercase because `RouteOptions.LowercaseUrls` is enabled.
- Protected endpoints expect `Authorization: Bearer <access-token>`.
- JSON enums are serialized as names by `JsonStringEnumConverter`.
- Paged endpoints generally use `pageNumber` and `pageSize`, with feature-specific search, sort, status, or format filters.
- Validation errors return `400`; missing resources return `404`; invalid or revoked authentication returns `401`; role/ownership failures return `403`; concurrency or state conflicts return `409`.
- `GET /api/books/recommended` requires an `Accept-Language` header containing exactly `ar` or `en`.
- Dashboard library-detail submission and digital-book creation use `multipart/form-data`.
- The Stripe webhook must receive the untouched request body and its `Stripe-Signature` header.

Request and response schemas are available in Swagger UI and the generated OpenAPI document.

## Endpoint reference

The current API exposes 66 controller actions.

### Authentication and profiles

| Method   | Route                              | Access                         | Purpose                                                                     |
| -------- | ---------------------------------- | ------------------------------ | --------------------------------------------------------------------------- |
| `POST`   | `/api/auth/register`               | Public                         | Start user registration and send its OTP.                                   |
| `POST`   | `/api/auth/register/verify`        | Public                         | Verify registration OTP and complete account creation.                      |
| `POST`   | `/api/auth/login`                  | Public                         | Authenticate a user and issue an access / refresh pair.                     |
| `POST`   | `/api/auth/library/login`          | Public                         | Authenticate an approved library owner by library email and password.       |
| `POST`   | `/api/auth/admin/login`            | Public                         | Validate administrator credentials and send a login OTP.                    |
| `POST`   | `/api/auth/admin/login/verify`     | Public                         | Verify the administrator OTP and issue tokens.                              |
| `POST`   | `/api/auth/refresh`                | Refresh token                  | Rotate a valid refresh token and issue a new pair.                          |
| `POST`   | `/api/auth/logout`                 | Refresh token; bearer optional | Revoke the matching refresh-token family and optional current access token. |
| `POST`   | `/api/auth/reset-password`         | Authenticated                  | Change the current account password and revoke its sessions.                |
| `POST`   | `/api/auth/forgot-password`        | Public                         | Send a password-recovery OTP.                                               |
| `POST`   | `/api/auth/forgot-password/verify` | Public                         | Verify the recovery OTP and set a new password.                             |
| `GET`    | `/api/profile/me`                  | Authenticated                  | Get the current user profile.                                               |
| `PUT`    | `/api/profile/me`                  | Authenticated                  | Update profile fields and interests.                                        |
| `POST`   | `/api/profile/location`            | Authenticated                  | Create or replace the current profile location.                             |
| `DELETE` | `/api/profile/location`            | Authenticated                  | Delete the current profile location.                                        |

### Catalog and discovery

| Method   | Route                          | Access        | Purpose                                              |
| -------- | ------------------------------ | ------------- | ---------------------------------------------------- |
| `GET`    | `/api/categories`              | Public        | List active categories.                              |
| `GET`    | `/api/categories/{categoryId}` | Public        | Get one active category.                             |
| `POST`   | `/api/categories`              | `Admin`       | Create a category.                                   |
| `GET`    | `/api/books/most-popular`      | Public        | Browse ranked popular books.                         |
| `GET`    | `/api/books/recommended`       | Authenticated | Browse interest- and language-based recommendations. |
| `GET`    | `/api/favorite-books`          | Authenticated | List the current account's favorite books.           |
| `POST`   | `/api/favorite-books/{bookId}` | Authenticated | Add a favorite.                                      |
| `DELETE` | `/api/favorite-books/{bookId}` | Authenticated | Remove a favorite.                                   |

### Libraries and listings

| Method   | Route                                              | Access         | Purpose                                                       |
| -------- | -------------------------------------------------- | -------------- | ------------------------------------------------------------- |
| `POST`   | `/api/libraries/register`                          | `User`         | Issue/reissue a temporary dashboard registration link.        |
| `POST`   | `/api/libraries/register/context`                  | Public + token | Resolve whether the dashboard needs details or email OTP.     |
| `POST`   | `/api/libraries/register/submit`                   | Public + token | Submit details and attempt delivery of the first email OTP.   |
| `POST`   | `/api/libraries/register/email/resend`             | Public + token | Redeliver the current OTP, subject to cooldown/limits.        |
| `POST`   | `/api/libraries/register/email/verify`             | Public + token | Verify the ID-bound OTP and enter admin review.               |
| `GET`    | `/api/libraries`                                   | Public         | Search and page approved libraries.                           |
| `GET`    | `/api/libraries/{libraryId}/books`                 | Public         | Browse a library's listings with paging, search, and sorting. |
| `GET`    | `/api/libraries/my-profile`                        | `LibraryOwner` | Get the approved library owned by the caller.                 |
| `GET`    | `/api/libraries/requests`                          | `Admin`        | Page and filter library applications.                         |
| `PATCH`  | `/api/libraries/{id}/approval-status`              | `Admin`        | Approve or reject a library application.                      |
| `POST`   | `/api/library-admin/listings`                      | `LibraryOwner` | Add a physical book by ISBN.                                  |
| `POST`   | `/api/library-admin/listings/digital`              | `LibraryOwner` | Upload a PDF and add a digital book by ISBN.                  |
| `PUT`    | `/api/library-admin/listings/{listingId}`          | `LibraryOwner` | Update supplied listing fields.                               |
| `GET`    | `/api/library-admin/listings/me`                   | `LibraryOwner` | Page and filter the owner's library listings.                 |
| `GET`    | `/api/library-admin/listings/{listingId}`          | `LibraryOwner` | Get listing, book, and category details.                      |
| `DELETE` | `/api/library-admin/listings/{listingId}`          | `LibraryOwner` | Mark an owned listing as removed.                             |
| `PATCH`  | `/api/library-admin/listings/{listingId}/activate` | `LibraryOwner` | Reactivate a removed listing.                                 |
| `GET`    | `/api/listings/me`                                 | `User`         | Get physical listings created by the current user.            |
| `POST`   | `/api/listings/me/physical`                        | `User`         | Create a user-owned physical listing.                         |

The mobile app calls `POST /api/libraries/register` with its normal bearer token and receives a short-lived dashboard URL. The URL contains a 32-byte opaque credential in the fragment (`#token=...`), while PostgreSQL stores only its SHA-256 hash and the issuing login-family id. The dashboard sends that credential in request bodies, never API query strings. A details submission creates an `AwaitingEmailVerification` library and a durable, HMAC-protected six-digit email challenge. Submission and resend responses include a `verificationId` and an `emailDeliveryStatus` of `Sent`, `NotSent`, or `Unknown`; verification must submit that ID with the six-digit code. Only successful email verification changes the library to `Pending`, makes it visible in the admin request queue, and permits approval or rejection. Approval promotes both the domain profile and ASP.NET Identity role within the same database transaction.

Email OTPs are valid for 10 minutes, can be redelivered after 60 seconds, are limited to five accepted-or-ambiguous send attempts per fixed hour and five verification attempts, and cause a five-minute lockout after the fifth incorrect attempt. Redelivery keeps the same generation and derived code, so overlapping or delayed SMTP deliveries cannot make the newest email stale. A definite SMTP rejection does not consume the send quota; an ambiguous transport outcome remains usable. The submitted application stays resumable through a newly issued link, and committed image paths are not deleted.

### Cart, orders, payments, and fulfillment

| Method   | Route                                                         | Access                   | Purpose                                                                |
| -------- | ------------------------------------------------------------- | ------------------------ | ---------------------------------------------------------------------- |
| `GET`    | `/api/cart/me`                                                | `User`                   | Get the current open cart, or an empty-cart response when none exists. |
| `POST`   | `/api/cart/items`                                             | `User`                   | Add a listing to the cart.                                             |
| `PUT`    | `/api/cart/items/{listingId}`                                 | `User`                   | Change a cart item's quantity.                                         |
| `DELETE` | `/api/cart/items/{listingId}`                                 | `User`                   | Remove a cart item.                                                    |
| `DELETE` | `/api/cart/me`                                                | `User`                   | Clear the open cart.                                                   |
| `POST`   | `/api/orders`                                                 | `User`                   | Create an order from the cart and open Stripe Checkout.                |
| `GET`    | `/api/orders/me`                                              | `User`                   | Page the buyer's orders.                                               |
| `GET`    | `/api/orders/{orderId}`                                       | `User`                   | Get buyer-visible order details.                                       |
| `PUT`    | `/api/orders/{orderId}/shipping-location`                     | `User`                   | Update an eligible order's shipping coordinates.                       |
| `POST`   | `/api/orders/{orderId}/checkout-session`                      | `User`                   | Recover or create a checkout session for an eligible order.            |
| `POST`   | `/api/orders/{orderId}/cancel`                                | `User`                   | Cancel an eligible order, optionally with a reason.                    |
| `DELETE` | `/api/orders/{orderId}`                                       | `User`                   | Archive an eligible order from buyer history.                          |
| `GET`    | `/api/orders/{orderId}/items/{orderItemId}/download`          | `User`                   | Stream a purchased PDF after buyer and paid-order checks.              |
| `POST`   | `/api/payments/stripe/webhook`                                | Signed Stripe event      | Process supported Checkout Session events idempotently.                |
| `GET`    | `/api/seller/orders`                                          | `User` or `LibraryOwner` | Page paid physical order items sold by the caller.                     |
| `POST`   | `/api/seller/orders/{orderId}/items/{orderItemId}/processing` | `User` or `LibraryOwner` | Move an owned physical item to processing.                             |
| `POST`   | `/api/seller/orders/{orderId}/items/{orderItemId}/fulfilled`  | `User` or `LibraryOwner` | Mark an owned physical item fulfilled.                                 |
| `GET`    | `/api/purchases/me/buy-history`                               | `User`                   | Page the current user's purchase history.                              |
| `GET`    | `/api/purchases/me/sell-history`                              | `User`                   | Page the current user's sale history.                                  |

### OTP and notifications

| Method | Route                     | Access              | Purpose                                                        |
| ------ | ------------------------- | ------------------- | -------------------------------------------------------------- |
| `POST` | `/api/otp/send`           | Public              | Send a standalone OTP through the configured SMS gateway.      |
| `POST` | `/api/otp/verify`         | Public              | Verify a standalone OTP.                                       |
| `POST` | `/api/notifications/send` | Authenticated       | Send an FCM notification.                                      |
| `POST` | `/api/notifications/test` | Public when enabled | Send a development/test notification; otherwise returns `404`. |

## Important workflows

### Access and refresh tokens

Access tokens carry a `jti` and refresh-family `sid`. Refresh tokens are opaque random secrets; only SHA-256 hashes are persisted. A successful refresh rotates the pair and archives the consumed hash. Reuse of an unexpired consumed token revokes the active family. Logout, a newer login, password changes, and recovery flows also invalidate the affected session family.

Clients must replace both stored tokens after every successful refresh. Do not retry an already-consumed refresh token after receiving the new pair.

### Order and Stripe flow

1. The user builds an open cart.
2. `POST /api/orders` snapshots the cart into an order, locks the cart, reserves physical stock, and persists a payment attempt.
3. The API creates a Stripe Checkout Session using an idempotency key.
4. Stripe sends a signed event to `/api/payments/stripe/webhook`.
5. Successful payment finalizes the order, cart, purchases, digital entitlement, and physical reservations.
6. Confirmed failure or expiry releases reservations and reopens the cart where allowed.
7. A hosted reconciliation service checks expired pending attempts every minute, with a short webhook-delivery grace period.

Treat the verified Stripe webhook and authoritative Stripe reconciliation result as payment truth; a browser success redirect is not proof of payment.

### Ebook storage and download

The seeded ebook uses the logical path `books/book1.pdf` and the physical file `Quraaa.API/storage/books/book1.pdf`. Paid downloads verify buyer ownership, paid status, digital format, path containment, and PDF extension before streaming the file with private, no-store headers.

There is a current storage-path issue for library-uploaded ebooks: `LibraryBookStorageService` writes them under `wwwroot/storage/books`, while the paid downloader resolves files from `Quraaa.API/storage/books`. Because `UseStaticFiles` serves the web root, uploaded PDFs should be treated as **not production-ready** until uploads are moved outside `wwwroot` and both write/download paths use the same private root.

### OTP delivery

The API generates and caches OTP data, then sends an FCM data message to the configured Android gateway device. That device is responsible for sending the actual SMS. Redis should be used when more than one API instance is running; the in-memory cache is per-process and is lost on restart.

## Database migrations and seed data

Startup calls `Database.Migrate()`, then runs category, admin, user, library, ebook, and book seeders.

`AddLibraryMagicLinkEmailVerification` migrates every legacy `Pending` library to `AwaitingEmailVerification`, so it must complete email verification before entering admin review. Existing `Approved` and `Rejected` records are explicitly grandfathered by setting `EmailVerifiedAtUtc` to the migration execution timestamp. A database check constraint then rejects any `Pending`, `Approved`, or `Rejected` row without a verification timestamp, including writes from an older API instance during a rolling deployment. On downgrade, status `AwaitingEmailVerification` is mapped back to legacy `Pending` before the verification columns are removed.

Seed behavior includes:

- A deterministic development user and the `User` / `LibraryOwner` roles.
- Up to 100 library-owner identities and libraries when libraries do not already exist; three out of every four seeded libraries are approved.
- An administrator only when both `ADMIN_PHONE_NUMBER` and `ADMIN_PASSWORD` are configured.
- Initial categories.
- One digital listing backed by `Quraaa.API/storage/books/book1.pdf`.
- Sixty Arabic / English catalog books with physical listings.

Development credentials are source-controlled seed data and must never be reused in a deployed environment. See the seeders and Swagger descriptions for the current local-only values.

Run EF Core commands from the repository root:

```powershell
# Add a migration
dotnet ef migrations add MigrationName --project Quraaa.Persistence --startup-project Quraaa.API

# Apply migrations manually
dotnet ef database update --project Quraaa.Persistence --startup-project Quraaa.API

# Remove the latest unapplied migration
dotnet ef migrations remove --project Quraaa.Persistence --startup-project Quraaa.API
```

Treat migration files as generated source: create or update them through `dotnet ef` rather than editing them manually.

## Build and validation

Build the API and all referenced projects from the repository root:

```powershell
dotnet restore QuraaaPlatform.slnx
dotnet build Quraaa.API/Quraaa.API.csproj -c Release --no-restore
```

There are currently no MSTest, NUnit, or xUnit projects, so `dotnet build` is the only repository-provided automated verification. Add unit tests for domain and handler logic plus integration tests for authentication, PostgreSQL constraints, Stripe webhooks, concurrency, and private file delivery before treating the service as production-complete.

## Docker and deployment

The multi-stage Dockerfile builds with the .NET 10 SDK and runs on the .NET 10 ASP.NET runtime. The container listens on port `8080`.

```powershell
docker build -t quraaa-api .
docker run --rm -p 8080:8080 --env-file Quraaa.API/.env quraaa-api
```

The simple run command also needs Firebase credentials available inside the container. Mount the service-account file read-only and set `Firebase__CredentialsPath` to its container path, or inject `FIREBASE_CREDENTIALS_JSON` through the deployment platform. When PostgreSQL or Redis runs on the host, update their connection strings for container networking.

`Procfile` starts a pre-published API from `Quraaa.API/bin/publish` and binds to the platform-provided `$PORT`. Publish before using that entry point:

```powershell
dotnet publish Quraaa.API/Quraaa.API.csproj -c Release -o Quraaa.API/bin/publish /p:UseAppHost=false
```

## Security and production notes

Before deploying publicly:

- Store PostgreSQL, JWT, Stripe, Firebase, Redis, admin, and Google API credentials in the platform's secret store; never commit `.env` or service-account JSON files.
- Add and verify a `.dockerignore` before building with secrets in the working tree. The current repository has no `.dockerignore`, and `COPY . .` sends the full build context to Docker.
- Resolve the library-uploaded ebook storage mismatch described above; paid source PDFs must live outside the static web root.
- Configure Redis for durable, shared OTP and revocation state, and override `Otp__AllowInMemoryCacheInProduction=false`. The committed base appsettings currently set it to `true`, so production otherwise falls back to process-local memory when Redis is unavailable.
- Set `Notifications__AllowTestEndpoint=false`. The committed appsettings currently enable it.
- Decide whether Swagger UI and OpenAPI should remain public; they are currently mapped in every environment.
- Review the startup `Database.Migrate()` and seeding policy for environments where the application should not hold schema-change permissions.
- Keep `LIBRARY_DASHBOARD_REGISTER_URL` aligned with the dashboard origin allowed by the registered `library-dashboard` CORS policy; add any other browser origins explicitly rather than broadening that policy.
- Configure and monitor the Stripe webhook endpoint, and retain the exact raw body required for signature verification.
- Use HTTPS at the edge and preserve forwarded headers through only trusted proxies.
- Rotate any development or seeded credentials before deployment.
- Add automated tests, health checks, structured observability, backups, and a recovery procedure.

## Contributing

1. Create a focused branch that satisfies `.github/workflows/check-branch-name.yml` (for example `feature/123-short-description`, `fix/123-short-description`, or lowercase kebab-case).
2. Keep dependency direction and aggregate boundaries intact.
3. Add EF schema changes through `dotnet ef`.
4. Run the Release build and inspect `git diff --check` before opening a pull request.
5. Update this README and `AGENTS.md` when routes, configuration, startup behavior, or architecture change.

## License

QuraaaPlatform is licensed under the Apache License, Version 2.0. See the [LICENSE](LICENSE) file for details.
