# Engineering Backlog

Open engineering tasks tracked in the repository. Task IDs use a category
prefix (`DDD`, `ARC`, `APP`, `SEC`, `PERF`, `CONC`, `CODE`, `OPS`) and a
number. Each task states the problem, the evidence, the affected files, the
proposed fix and the acceptance criteria. When a task ships, set its status to
Done and link the commit or pull request.

| ID | Task | Category | Priority | Status |
| --- | --- | --- | --- | --- |
| [CONC-01](#conc-01--make-firebase-initialization-host-scoped-and-thread-safe) | Make Firebase initialization host-scoped and thread-safe | Concurrency, Testing | Medium | Open |

---

## CONC-01 · Make Firebase initialization host-scoped and thread-safe

- **Category:** Concurrency, Testing
- **Priority:** Medium. Production risk is low today, but fix it before the
  integration tests run in CI.
- **Status:** Open
- **Found:** 2026-10-03, while verifying ARC-01
  (`refactor/arc-01-persistence-dbcontext-registration`)

### Problem

`FirebaseExtensions.AddFirebaseConfiguration` creates the process-wide default
`FirebaseApp` while services are being registered, using an unsynchronized
check-then-create:

```csharp
// Quraaa.Infrastructure/Extensions/FirebaseExtensions.cs
if (FirebaseApp.DefaultInstance is not null)          // check
{
    return services;
}

var credential = CreateCredential(configuration);     // reads and parses the key file

FirebaseApp.Create(new AppOptions                     // act
{
    Credential = credential
});
```

When two hosts start in the same process, both can pass the check before
either calls `Create`, and the second call throws. `Quraaa.API.IntegrationTests`
does exactly this: `ValidateIsbnEndpointTests` and
`RemovedOrderDownloadEndpointTests` each take an
`IClassFixture<CustomWebApplicationFactory>`, so each class boots its own host,
and xUnit runs test classes in parallel by default.

### Evidence

```text
System.ArgumentException : The default FirebaseApp already exists.
   at FirebaseAdmin.FirebaseApp.Create(AppOptions options, String name)
   at FirebaseAdmin.FirebaseApp.Create(AppOptions options)
   at Quraaa.Infrastructure.Extensions.FirebaseExtensions.AddFirebaseConfiguration(IServiceCollection services, IConfiguration configuration)
   at Quraaa.Infrastructure.Extensions.InfrastructureDependencyInjectionHandler.AddInfrastructure(IServiceCollection services, IConfiguration configuration, Boolean isDevelopment)
   at Program.<Main>$(String[] args)
```

- 8 of 8 parallel runs on 2026-10-03 failed exactly one host-booting test, on
  `main` and on the ARC-01 branch. Which test fails varies from run to run.
- Only the first test of the class that loses the race fails. Its next
  `CreateClient()` builds the host again, finds the default app already
  created, and succeeds.
- With parallelization turned off, 8 of 8 tests pass.

### Why it matters beyond flaky tests

- **No isolation between hosts.** When the default app already exists,
  registration returns early, so the second host silently reuses the first
  host's app and credentials.
- **I/O during registration.** `AddInfrastructure` reads and parses the
  service-account file before the container exists. A test cannot swap the
  Firebase services for fakes or boot without credentials.
- **Consumers bypass DI.** `FirebaseNotificationService` and
  `FirebaseSmsGateway` call `FirebaseMessaging.DefaultInstance`, so they only
  work through the global default app.
- **More global side effects at startup.** `Program.CreateFirebaseCredentialsFile`
  writes `FIREBASE_CREDENTIALS_JSON` to `storage/firebase/quraa.json` and sets
  `GOOGLE_APPLICATION_CREDENTIALS` for the whole process.
- **It grows with the test suite.** Production runs one host per process, so
  the race does not occur there today. In tests, every new class that uses
  `IClassFixture<CustomWebApplicationFactory>` adds another racer.

### Affected files

- `Quraaa.Infrastructure/Extensions/FirebaseExtensions.cs`: check-then-create
  and credential loading.
- `Quraaa.Infrastructure/Extensions/InfrastructureDependencyInjectionHandler.cs`:
  calls it at registration time.
- `Quraaa.Infrastructure/Services/FirebaseNotificationService.cs` and
  `FirebaseSmsGateway.cs`: use `FirebaseMessaging.DefaultInstance`.
- `Quraaa.API/Program.cs`: `CreateFirebaseCredentialsFile`.
- `Quraaa.API.IntegrationTests/CustomWebApplicationFactory.cs`,
  `ValidateIsbnEndpointTests.cs` and `RemovedOrderDownloadEndpointTests.cs`:
  two hosts per test run.

### Reproduce

1. Export the settings the test host needs. Dummy values work, and no test
   calls an external service:
   - `JWT_SECRET_KEY` (32+ characters) and `LIBRARY_DASHBOARD_REGISTER_URL` (HTTPS)
   - `Stripe__SecretKey` (`sk_test_…`) and `Stripe__WebhookSecret` (`whsec_…`)
   - `CLOUDINARY_CLOUD_NAME`, `CLOUDINARY_API_KEY` and `CLOUDINARY_API_SECRET`
   - `MAIL_MAILER=smtp`, plus `MAIL_HOST`, `MAIL_PORT`, `MAIL_USERNAME`,
     `MAIL_PASSWORD`, `MAIL_ENCRYPTION`, `MAIL_FROM_ADDRESS` and `MAIL_FROM_NAME`
   - `LIBRARY_EMAIL_OTP_PEPPER` (32+ characters, different from the JWT secret)
   - `GOOGLE_APPLICATION_CREDENTIALS`, pointing at a service-account JSON whose
     private key can be self-generated with `openssl genpkey -algorithm RSA`

   Run from a directory without a `.env` file, because `Program.cs` loads
   `.env` and `Quraaa.API/.env` relative to the working directory.
2. Run `dotnet test Quraaa.API.IntegrationTests/Quraaa.API.IntegrationTests.csproj`.
   One host-booting test fails with the error above.
3. Run the same command with `-- xUnit.ParallelizeTestCollections=false`.
   All 8 tests pass.

### Proposed fix

**Recommended: a lazily created, host-scoped Firebase app owned by DI.**

1. Register a named `FirebaseApp` as a singleton that a factory creates on
   first use, not during registration. A unique name per container keeps hosts
   apart, and deleting the app on disposal keeps repeated test hosts clean.
2. Register `FirebaseMessaging` from that app, and inject it into
   `FirebaseNotificationService` and `FirebaseSmsGateway` instead of calling
   `FirebaseMessaging.DefaultInstance`.
3. Resolve credentials inside the factory. Read `FIREBASE_CREDENTIALS_JSON` in
   memory with `CredentialFactory.FromJson<ServiceAccountCredential>(json)`,
   then delete `Program.CreateFirebaseCredentialsFile` and its
   environment-variable writes.
4. In `CustomWebApplicationFactory`, replace `IFirebaseNotificationService` and
   `IFirebaseSmsGateway` with fakes, so host tests need no Firebase credentials.
5. Add a regression test that builds two hosts concurrently and asserts that
   both start.

```csharp
// Quraaa.Infrastructure/Extensions/FirebaseExtensions.cs
public static IServiceCollection AddFirebase(this IServiceCollection services, IConfiguration configuration)
{
    // Created on first use and named per container: no global default app,
    // and no credential I/O while services are being registered.
    services.AddSingleton(_ => new FirebaseAppHandle(FirebaseApp.Create(
        new AppOptions { Credential = CreateCredential(configuration) },
        $"quraaa-{Guid.NewGuid():N}")));

    services.AddSingleton(provider =>
        FirebaseMessaging.GetMessaging(provider.GetRequiredService<FirebaseAppHandle>().App));

    return services;
}

// The container disposes this when the host stops.
internal sealed class FirebaseAppHandle(FirebaseApp app) : IDisposable
{
    public FirebaseApp App { get; } = app;

    public void Dispose() => App.Delete();
}
```

**Stopgap, only if the full fix must wait.** Wrap the existing
check-then-create in a lock with a second check. That removes the exception,
but hosts still share one global app and its credentials, and registration
still does I/O. Do not catch the `ArgumentException` instead: that hides the
race without removing it.

**Test-side mitigation, useful on its own.** Put every host-based test class
in one xUnit collection that shares a single `CustomWebApplicationFactory`.
One host then serves all API tests, which removes the race and speeds up the
suite:

```csharp
[CollectionDefinition(Name)]
public sealed class ApiHostCollection : ICollectionFixture<CustomWebApplicationFactory>
{
    public const string Name = "API host";
}

[Collection(ApiHostCollection.Name)]
public class ValidateIsbnEndpointTests
{
    // The constructor still receives CustomWebApplicationFactory;
    // remove IClassFixture<CustomWebApplicationFactory> from the class.
}
```

### Acceptance criteria

- [ ] `dotnet test Quraaa.API.IntegrationTests/Quraaa.API.IntegrationTests.csproj`
  passes with default parallelization in 20 consecutive runs.
- [ ] `AddInfrastructure` does no file I/O and no credential parsing. The
  Firebase app is created on first use and deleted when the host stops.
- [ ] No code references `FirebaseApp.DefaultInstance` or
  `FirebaseMessaging.DefaultInstance`.
- [ ] Host tests boot without Firebase credentials.
- [ ] Push notifications and SMS OTP still deliver in a manual check with real
  credentials.
