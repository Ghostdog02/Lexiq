# Backend Tests CLAUDE.md

Integration and unit tests for the Lexiq backend using xUnit v3 and Testcontainers.

## Running Tests

```bash
# Run all tests (requires Docker)
cd backend
dotnet test Tests/Backend.Tests.csproj --logger "console;verbosity=normal"

# Run a specific class
dotnet test Tests/Backend.Tests.csproj --filter "FullyQualifiedName~GetLeaderboardTests"

# Run only unit tests (no Docker needed)
dotnet test Tests/Backend.Tests.csproj --filter "FullyQualifiedName~CalculateLevelTests"
```

> Docker must be running. Testcontainers pulls `mcr.microsoft.com/mssql/server:2022-latest`
> on first run — subsequent runs use the local image cache.

## Project Structure

```
Tests/
├── Backend.Tests.csproj
├── Infrastructure/
│   └── DatabaseFixture.cs     ← Testcontainers container, migrations, shared content hierarchy
├── Builders/
│   └── UserBuilder.cs         ← Fluent builder for Identity-compliant User rows
├── Helpers/
│   └── DbSeeder.cs            ← Insert helpers and ClearLeaderboardDataAsync
├── Controllers/
│   └── AuthControllerTests.cs ← WebApplicationFactory HTTP-level tests
└── Services/
    ├── CalculateLevelTests.cs  ← Pure unit tests (no DB)
    ├── JwtServiceTests.cs      ← Pure unit tests (no DB)
    ├── LoginUserTests.cs       ← Testcontainers integration tests
    ├── GetStreakTests.cs        ← Testcontainers integration tests
    └── GetLeaderboardTests.cs  ← Testcontainers integration tests
```

## Key Packages

| Package | Version | Purpose |
|---------|---------|---------|
| `xunit.v3` | 3.2.2 | Test framework |
| `Testcontainers.MsSql` | 4.10.0 | Real SQL Server 2022 in Docker |
| `FluentAssertions` | 8.8.0 | `.Should()` assertions |
| `Microsoft.NET.Test.Sdk` | 17.12.0 | Test host discovery |
| `Moq` | 4.20.72 | Mock external services (`IGoogleAuthService`, etc.) |
| `Microsoft.AspNetCore.Mvc.Testing` | 10.0.0 | `WebApplicationFactory` for controller/HTTP tests |

## DatabaseFixture

`DatabaseFixture` implements `IAsyncLifetime` and is shared across tests in a class via `IClassFixture<DatabaseFixture>`. It:

1. Starts a SQL Server 2022 container
2. Runs all EF Core migrations (`MigrateAsync`)
3. Seeds a permanent content hierarchy once per test run

### Permanent Content Hierarchy

Seeded once, **never cleared** between tests:

```
System User (satisfies Course.CreatedById FK)
  └─ Language: "Italian"
       └─ Course
            └─ Lesson
                 └─ 20 × FillInBlankExercise  (ExerciseIds[0..19])
```

**Why 20 exercises?**
`UserExerciseProgress` PK is `(UserId, ExerciseId)`. Streak tests seed one row per calendar day per user. If all rows share the same `ExerciseId`, the second insert for the same user fails on the composite PK. Using a different `ExerciseId` per day avoids this.

**Why a system user?**
`Course.CreatedById` is a FK to `Users`. The system user is created first and excluded from `ClearLeaderboardDataAsync` so the content hierarchy survives between tests.

### Creating a DbContext in Tests

```csharp
var ctx = _fixture.CreateDbContext();
```

`PendingModelChangesWarning` is suppressed via `ConfigureWarnings(w => w.Ignore(...))` — this warning is benign in tests where the model and DB are migrated in sync.

## Test Class Pattern

Every integration test class follows this structure:

```csharp
public class MyTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private BackendDbContext _ctx = null!;

    public MyTests(DatabaseFixture fixture) => _fixture = fixture;

    // Runs before each test method (xUnit creates a new instance per test)
    public async ValueTask InitializeAsync()
    {
        _ctx = _fixture.CreateDbContext();
        await DbSeeder.ClearLeaderboardDataAsync(_ctx, _fixture.SystemUserId);
        // ... seed test-specific data
    }

    public async ValueTask DisposeAsync()
    {
        await _ctx.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
```

**`IAsyncLifetime` uses `ValueTask`** (not `Task`) in xUnit v3 — the compiler will error if you use `Task`.

**Always call `GC.SuppressFinalize(this)` as the last statement in every `Dispose` and `DisposeAsync` method.** This prevents the GC from enqueuing the object for finalization when managed cleanup is already complete.

## UserBuilder

Direct `DbContext` inserts bypass `UserManager`. ASP.NET Core Identity enforces unique indexes on `NormalizedUserName` and `NormalizedEmail` — these must be set manually:

```csharp
var user = new UserBuilder()
    .WithUserName("alice")
    .WithEmail("alice@test.com")
    .WithTotalPoints(500)
    .Build();
```

`WithNullUserName()` and `WithNullEmail()` exist to test the leaderboard username fallback chain (`UserName ?? Email ?? "Unknown"`).

## DbSeeder

### Adding Progress

```csharp
await DbSeeder.AddProgressAsync(ctx, userId, fixture.ExerciseIds[0],
    isCompleted: true, pointsEarned: 10, completedAt: DateTime.UtcNow);

// Add N consecutive days of activity
await DbSeeder.AddConsecutiveDaysActivityAsync(ctx, userId, fixture.ExerciseIds,
    days: 5, startDaysAgo: 0);
```

### Teardown Order

`ClearLeaderboardDataAsync` deletes in FK-safe order:

1. `UserExerciseProgress` (FK → Exercises, NoAction)
2. `UserAvatars`
3. Identity junction tables: `UserClaims`, `UserLogins`, `UserRoles`, `UserTokens`
4. `Users` (excluding system user)

Language / Course / Lesson / Exercise rows are **never deleted**.

## AvatarService in Tests

`AvatarService` requires `IHttpClientFactory` and `ILogger`, but these are only used in `DownloadAvatarAsync` — never called by `LeaderboardService`. Stub with:

```csharp
private static AvatarService CreateAvatarService(BackendDbContext ctx)
{
    var factory = new ServiceCollection()
        .AddHttpClient()
        .BuildServiceProvider()
        .GetRequiredService<IHttpClientFactory>();

    return new AvatarService(ctx, factory, NullLogger<AvatarService>.Instance);
}
```

## IGoogleAuthService in Tests

`GoogleAuthService` makes real HTTP calls to Google's token validation endpoint — **never instantiate it directly in tests that don't test it**. Always mock `IGoogleAuthService` with Moq when the SUT depends on it:

```csharp
var googleAuthMock = new Mock<IGoogleAuthService>();
googleAuthMock
    .Setup(s => s.ValidateGoogleTokenAsync(It.IsAny<string>()))
    .ReturnsAsync(fakePayload);
googleAuthMock
    .Setup(s => s.LoginUser(It.IsAny<GoogleJsonWebSignature.Payload>()))
    .ReturnsAsync(fakeUser);

// In WebApplicationFactory:
services.RemoveAll<IGoogleAuthService>();
services.AddSingleton(googleAuthMock.Object);
```

`LoginUserTests.cs` is the exception — it tests `GoogleAuthService` itself, so it constructs a real instance backed by Testcontainers.

## UserManager and RoleManager in Tests

`GoogleAuthService` requires a real `UserManager<User>`. Wire it to the Testcontainers DB via a
dedicated `ServiceCollection` — do NOT use the fixture's `_ctx` directly:

```csharp
private static (UserManager<User>, RoleManager<IdentityRole>) BuildManagers(BackendDbContext ctx)
{
    var services = new ServiceCollection();
    services.AddLogging();
    services.AddSingleton(ctx); // test-scoped DbContext as singleton
    services.AddIdentityCore<User>()
        .AddRoles<IdentityRole>()
        .AddEntityFrameworkStores<BackendDbContext>();
    var sp = services.BuildServiceProvider();
    return (sp.GetRequiredService<UserManager<User>>(), sp.GetRequiredService<RoleManager<IdentityRole>>());
}
```

**Role seeding**: `ClearLeaderboardDataAsync` does not delete Roles. Seed idempotently:
```csharp
if (!await roleManager.RoleExistsAsync("Student"))
    await roleManager.CreateAsync(new IdentityRole("Student"));
```

## WebApplicationFactory Pattern

Use for HTTP-level controller tests (cookie headers, status codes, response shape). Overrides run
after main `ConfigureServices`, so last registration wins — no need to remove before adding unless
you want to be explicit.

```csharp
_factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
    builder.ConfigureServices(services =>
    {
        // Replace DbContext with Testcontainers instance
        var dbDescriptor = services.Single(d => d.ServiceType == typeof(DbContextOptions<BackendDbContext>));
        services.Remove(dbDescriptor);
        services.AddDbContext<BackendDbContext>(opts =>
            opts.UseSqlServer(_fixture.ConnectionString)
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

        // Replace service with Moq
        services.RemoveAll<IGoogleAuthService>();
        services.AddSingleton(googleAuthMock.Object);
    })
);
_client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
```

**Required env vars before factory creation** (read at service construction time, not request time):
- `JWT_SECRET` — `JwtService` constructor throws if missing
- `DATA_PROTECTION_KEYS_PATH` — set to `Path.GetTempPath()` to avoid `/app/dataprotection-keys` write failure

## Gotchas

### ExerciseId FK on INSERT
`UserExerciseProgress.ExerciseId` uses `DeleteBehavior.NoAction` — SQL Server enforces this FK on INSERT, not just DELETE. Any `AddProgressAsync` call with an unknown `ExerciseId` will throw. Always use IDs from `fixture.ExerciseIds`.

### Flatpak IDE and NuGet
If running VS Code / VS Codium inside a Flatpak container, the IDE cannot see NuGet packages installed outside the Flatpak sandbox. This causes red squiggles for `Testcontainers.MsSql` and other packages. The `dotnet` CLI is unaffected — build and test from the terminal.

### xUnit v3 IAsyncLifetime
Return type is `ValueTask`, not `Task`. Using `Task` compiles but fails at runtime or produces a linter error about interface mismatch.

### xUnit1051 — CancellationToken on HTTP client calls
`PostAsJsonAsync` and `PostAsync` accept a `CancellationToken`. Pass
`TestContext.Current.CancellationToken` or the xUnit analyzer raises xUnit1051 warnings.

### dotnet clean reveals incremental build gaps
`dotnet clean` removes cached assemblies and forces a full rebuild, which can expose
pre-existing type-not-found errors hidden by incremental builds (e.g. a missing entity
referenced in a nav property). Run `dotnet clean && dotnet build` periodically to catch these.

### WebApplicationFactory — `InitializeDatabaseAsync` is called
`Program.Main`'s `InitializeDatabaseAsync` (migrations + seed) **does** run when
`WebApplicationFactory` starts. With the DbContext overridden to the Testcontainers DB,
migrations are a no-op (already applied) and seeding is idempotent — safe.
