# Backend Tests CLAUDE.md

xUnit v3 + Testcontainers + FluentAssertions + Moq + WebApplicationFactory.

> Test generation standards: [`/.claude/agents/test-generator.md`](../../.claude/agents/test-generator.md).
> Common bug patterns: [`.claude/rules/common-gotchas.md`](../../.claude/rules/common-gotchas.md).

## Run tests

```bash
cd backend
dotnet test Tests/Backend.Tests.csproj --logger "console;verbosity=normal"
dotnet test Tests/Backend.Tests.csproj --filter "FullyQualifiedName~GetLeaderboardTests"
dotnet test Tests/Backend.Tests.csproj --filter "FullyQualifiedName~Unit"     # no Docker needed
```

Docker must be running. Testcontainers pulls `mcr.microsoft.com/mssql/server:2022-latest` on first run.

## Layout

```
Tests/
├── Backend.Tests.csproj
├── Infrastructure/
│   ├── DatabaseFixture.cs        ← Testcontainers, migrations, content hierarchy
│   └── ControllerTestBase.cs     ← E2E base with auth helpers
├── Builders/UserBuilder.cs       ← Identity-compliant user fluent builder
├── Helpers/DbSeeder.cs           ← Insert helpers + ClearLeaderboardDataAsync
├── Unit/                         ← Pure logic tests, no DB (CalculateLevel, Jwt, FileUploads)
├── Integration/
│   ├── Services/                 ← Service tests (Leaderboard, Streak, CRUD, Avatar, Achievements, Profile)
│   ├── Controllers/              ← HTTP controller tests (Auth, Authorization)
│   └── E2E/                      ← Full user-journey tests via WebApplicationFactory
```

## Key packages

| Package | Purpose |
|---------|---------|
| `xunit.v3` 3.2.2 | Framework — `IAsyncLifetime` returns `ValueTask` |
| `Testcontainers.MsSql` 4.10.0 | Real SQL Server 2022 in Docker |
| `FluentAssertions` 8.8.0 | `.Should()` |
| `Moq` 4.20.72 | Mock external services |
| `Microsoft.AspNetCore.Mvc.Testing` 10.0.0 | `WebApplicationFactory` |

## DatabaseFixture

`IClassFixture<DatabaseFixture>` shares the container; `IAsyncLifetime` reseeds per test.

Permanent hierarchy seeded once, never cleared:

```
System User (excluded from ClearLeaderboardDataAsync — satisfies Course.CreatedById FK)
└─ Language "Italian"
   └─ Course
      └─ Lesson  ← tests create exercises in InitializeAsync via fixture.LessonId
```

`ClearLeaderboardDataAsync` deletes (FK-safe order): `UserExerciseProgress` → `Exercises` → `UserAvatars` → Identity junction tables → `Users` (excluding system user). Language / Course / Lesson are never deleted.

## Conventions

### Naming

`MethodName_StateUnderTest_ExpectedBehavior` — e.g. `Student_CompletesFirstExercise_UnlocksNextExercise`. No `Test*` / `Should*` prefixes. Always include subject + action + outcome.

### Strict AAA

```csharp
[Fact]
public async Task Student_CompletesExercise_UnlocksNextExercise()
{
    // Arrange — setup only, no .Should() assertions
    var firstExId = _exerciseIds[0];

    // Act — execute, no verification
    var submitResult = await SubmitAnswerAsync(firstExId, "answer");
    var exercises = await GetExercisesForLessonAsync(Fixture.LessonId);

    // Assert — every .Should() lives here
    submitResult!.IsCorrect.Should().BeTrue();
    exercises.First(e => e.Id == _exerciseIds[1]).IsLocked.Should().BeFalse();
}
```

Helpers do data-fetching only — no assertions. Test failures must occur in Assert with FluentAssertions messages.

### `because:` clauses

Use for **business-rule context** that isn't obvious from test name + assertion. Skip when redundant.

✅ `because: "70% completion threshold (7/10) ensures students engage with most lesson content"`
✅ `because: "resubmitting correct answer must not award XP twice — prevents XP farming"`
✅ `because: $"Admin-only endpoint {method} {path} prevents students from modifying course content"`
❌ `because: "user should not be null"` (restates assertion)
❌ `because: "Student role is not authorized"` (already in test name)

### Test class skeleton

```csharp
public class MyTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private BackendDbContext _ctx = null!;
    private List<string> _exerciseIds = null!;

    public async ValueTask InitializeAsync()
    {
        _ctx = fixture.CreateDbContext();
        await DbSeeder.ClearLeaderboardDataAsync(_ctx, fixture.SystemUserId);

        _exerciseIds = [];
        for (var i = 0; i < 10; i++)
            _exerciseIds.Add(await DbSeeder.CreateFillInBlankExerciseAsync(
                _ctx, fixture.LessonId, orderIndex: i, isLocked: i != 0));
    }

    public async ValueTask DisposeAsync()
    {
        await _ctx.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
```

`ValueTask` (not `Task`) on `IAsyncLifetime` — xUnit v3. Always end `Dispose*` with `GC.SuppressFinalize(this)`.

## DbSeeder helpers

| Helper | Use |
|--------|-----|
| `CreateFillInBlankExerciseAsync(ctx, lessonId, orderIndex, isLocked, points)` | Correct answer is `"answer"` |
| `CreateMultipleChoiceExerciseAsync(ctx, lessonId, orderIndex, isLocked, points)` | 3 options, 1 correct |
| `CreateListeningExerciseAsync(...)` | Same signature |
| `CreateTranslationExerciseAsync(...)` | Same signature |
| `AddProgressAsync(ctx, userId, exerciseId, isCompleted, pointsEarned, completedAt)` | Single progress row |
| `AddConsecutiveDaysActivityAsync(ctx, userId, exerciseIds, days, startDaysAgo)` | Streak setup |
| `AddAvatarAsync(ctx, userId)` | Avatar binary row |

`fixture.ExerciseIds` is FK-enforced on INSERT (`DeleteBehavior.NoAction`) — always use IDs from your test's seeded list.

## UserBuilder

Bypasses `UserManager`, so it sets the Identity-required normalized fields:

```csharp
var user = new UserBuilder()
    .WithUserName("alice")
    .WithEmail("alice@test.com")
    .WithTotalPoints(500)
    .Build();
```

`WithNullUserName()` / `WithNullEmail()` exist to test the leaderboard `UserName ?? Email ?? "Unknown"` fallback.

## E2E tests (`ControllerTestBase`)

Provides:

- DatabaseFixture wired into `WebApplicationFactory<Program>` via Testcontainers
- `CreateAuthenticatedUserAsync(username, email, role)` → `(userId, token)`
- `CreateClient(token)` → `HttpClient` with auth cookie
- `JsonOptions` — application's serializer (required for polymorphic DTOs)
- `ClearTestDataAsync()`
- `EnsureUploadDirectoriesExist()` — creates `wwwroot/uploads` (gitignored, missing locally)

Pattern:

```csharp
public class MyJourneyTests(DatabaseFixture fixture)
    : ControllerTestBase(fixture), IClassFixture<DatabaseFixture>
{
    private HttpClient _client = null!;
    private string _authToken = null!;

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        await ClearTestDataAsync();
        var (_, token) = await CreateAuthenticatedUserAsync("student", "student@test.com", "Student");
        _authToken = token;
        _client = CreateClient(_authToken);
    }
}
```

Multi-user, session-restart, and DB-seeding patterns:

```csharp
// Multiple authed users
var (sId, sToken) = await CreateAuthenticatedUserAsync("student", "s@t.com", "Student");
var (aId, aToken) = await CreateAuthenticatedUserAsync("admin",   "a@t.com", "Admin");

// Simulate session restart
_client.Dispose();
_client = CreateClient(_authToken);

// Direct DB seeding
using var scope = Factory.Services.CreateScope();
var ctx = scope.ServiceProvider.GetRequiredService<BackendDbContext>();
await DbSeeder.AddConsecutiveDaysActivityAsync(ctx, userId, _exerciseIds, days: 3);
```

## WebApplicationFactory setup

Override DbContext to Testcontainers, mock external services:

```csharp
_factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
    builder.ConfigureServices(services =>
    {
        var dbDescriptor = services.Single(d => d.ServiceType == typeof(DbContextOptions<BackendDbContext>));
        services.Remove(dbDescriptor);
        services.AddDbContext<BackendDbContext>(opts =>
            opts.UseSqlServer(_fixture.ConnectionString)
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));

        services.RemoveAll<IGoogleAuthService>();
        services.AddSingleton(googleAuthMock.Object);
    }));
_client = _factory.CreateClient(new() { AllowAutoRedirect = false });
```

### Required env vars before factory creation

Set in `ControllerTestBase.InitializeAsync` BEFORE the factory is instantiated. **Do NOT clear in DisposeAsync** — race conditions when xUnit parallelizes test classes.

```
JWT_SECRET                       # JwtService throws otherwise
JWT_EXPIRATION_HOURS=24
DATA_PROTECTION_KEYS_PATH        # = Path.GetTempPath() to avoid /app write failure
GOOGLE_CLIENT_ID                 # AddGoogleAuthentication reads at DI time
GOOGLE_CLIENT_SECRET             # ditto
```

Missing either Google var → `InvalidOperationException` collapses into an `AggregateException` DI-validation error.

## Mocking external services

### `IGoogleAuthService` (always mock — never instantiate `GoogleAuthService` in tests except `LoginUserTests`)

```csharp
var mock = new Mock<IGoogleAuthService>();
mock.Setup(s => s.ValidateGoogleTokenAsync(It.IsAny<string>())).ReturnsAsync(fakePayload);
mock.Setup(s => s.LoginUser(It.IsAny<GoogleJsonWebSignature.Payload>())).ReturnsAsync(fakeUser);

services.RemoveAll<IGoogleAuthService>();
services.AddSingleton(mock.Object);
```

### `AvatarService` stub (HTTP factory + null logger)

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

### `UserManager` / `RoleManager` for `GoogleAuthService` integration

Wire to Testcontainers DB via dedicated `ServiceCollection`. Idempotent role seeding required since `ClearLeaderboardDataAsync` doesn't delete Roles:

```csharp
if (!await roleManager.RoleExistsAsync("Student"))
    await roleManager.CreateAsync(new IdentityRole("Student"));
```

## Polymorphic DTOs in tests — JsonOptions required

`ReadFromJsonAsync<ExerciseDto>()` with default options throws "must specify a type discriminator". Always pass `JsonOptions`. For serialization, **declare as base type** so the polymorphic discriminator is included:

```csharp
// Deserialization
var dto = await response.Content.ReadFromJsonAsync<ExerciseDto>(
    JsonOptions, TestContext.Current.CancellationToken);

// Serialization — base-type declaration is critical
CreateExerciseDto addExerciseDto = new CreateFillInBlankExerciseDto(...);
await client.PostAsJsonAsync("/api/exercises", addExerciseDto, JsonOptions,
    TestContext.Current.CancellationToken);
```

When polymorphic DTOs are nested in collections (`List<CreateExerciseDto>`), the base type is implicit — just pass `JsonOptions`.

## CancellationToken (xUnit1051)

Always pass `TestContext.Current.CancellationToken` to async calls accepting one:

```csharp
await client.PostAsJsonAsync("/api/x", dto, TestContext.Current.CancellationToken);
await response.Content.ReadFromJsonAsync<Dto>(TestContext.Current.CancellationToken);
await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
await _ctx.Lessons.FindAsync(new object[] { lessonId }, TestContext.Current.CancellationToken);
await _ctx.Lessons.FirstOrDefaultAsync(l => l.Id == id, TestContext.Current.CancellationToken);
```

Service methods don't accept tokens — only setup/teardown and direct EF Core calls do.

## Null-forgiving `!`

**Forbidden** in production code. Two accepted exceptions in tests:

1. **Deferred-init fields** assigned in `InitializeAsync`:
   ```csharp
   private BackendDbContext _ctx = null!;
   ```
2. **After a `NotBeNull()` assertion** — assertion already verified non-null:
   ```csharp
   AssertValidUser(resultUser, payload);
   var roles = await _userManager.GetRolesAsync(resultUser!); // safe — assertion passed
   ```

## Assert helpers

Use a `private static void Assert*` method when multiple tests verify the same property set:

```csharp
private static void AssertValidUser(User? user, GoogleJsonWebSignature.Payload payload)
{
    user.Should().NotBeNull();
    user!.Email.Should().Be(payload.Email);
    user.UserName.Should().Be(CleanUsername(payload.Name));
    user.EmailConfirmed.Should().BeTrue();
}
```

For `private static` production helpers (e.g. `UserMapping.CleanUsername`), duplicate locally with a comment pointing at the source. Drift causes test failures — useful tripwire.

## E2E test files

| File | Verifies | Tests |
|------|----------|------:|
| `StudentExerciseProgressJourneyTests.cs` | Completion, XP, sequential unlocking, retries | 7 |
| `StudentSessionPersistenceTests.cs` | Restoration, state consistency, XP idempotency | 6 |
| `ExerciseSubmissionSecurityTests.cs` | Lock enforcement, role bypass, MultipleChoice | 9 |
| `LeaderboardAndStreaksTests.cs` | Rankings, streaks, avatar integration | 5 |
| `AdminContentManagementJourneyTests.cs` | Admin/Creator CRUD, role authz, lifecycle | 6 |

## Misc gotchas

- **Flatpak IDE + NuGet** — VS Code in a Flatpak sandbox can't see system NuGet packages → red squiggles for `Testcontainers.MsSql` etc. The `dotnet` CLI is unaffected; build/test from terminal.
- **`dotnet clean`** removes incremental build cache — periodically run `dotnet clean && dotnet build` to surface type-not-found errors hidden by incremental builds.
- **`InitializeDatabaseAsync` runs under `WebApplicationFactory`** — migrations are no-op (already applied), seed is idempotent. Safe.
- **Unregistered service** → `AggregateException` wrapping `InvalidOperationException: Unable to resolve service for type 'Foo'`. `ValidateOnBuild = true` (default in .NET 8+) catches it at startup, before the first HTTP call. Fix: register in `AddApplicationServices`.
