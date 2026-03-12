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
│   ├── DatabaseFixture.cs     ← Testcontainers container, migrations, shared content hierarchy
│   └── ControllerTestBase.cs ← Base class for E2E tests with auth helpers
├── Builders/
│   └── UserBuilder.cs         ← Fluent builder for Identity-compliant User rows
├── Helpers/
│   └── DbSeeder.cs            ← Insert helpers and ClearLeaderboardDataAsync
├── End-to-End/                ← Full user journey HTTP tests
│   ├── StudentExerciseProgressJourneyTests.cs     ← Exercise completion, XP, unlocking (7 tests)
│   ├── StudentSessionPersistenceTests.cs          ← Session persistence, progress restoration (6 tests)
│   ├── ExerciseSubmissionSecurityTests.cs         ← Security, edge cases, MultipleChoice (9 tests)
│   ├── LeaderboardAndStreaksTests.cs              ← Leaderboard rankings, streak tracking (5 tests)
│   └── AdminContentManagementJourneyTests.cs      ← Admin/creator CRUD workflows (6 tests)
├── Controllers/
│   ├── AuthControllerTests.cs        ← WebApplicationFactory HTTP-level tests (6 tests)
│   └── AuthorizationTests.cs         ← Complete authorization matrix (92 tests across 7 categories)
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
                 └─ 40 × Exercise (4 types)  (ExerciseIds[0..39])
                      ├─ [0-9]:   FillInBlank (10 exercises)
                      ├─ [10-19]: MultipleChoice (10 exercises, 3 options each)
                      ├─ [20-29]: Listening (10 exercises)
                      └─ [30-39]: Translation (10 exercises)
```

**Why 40 exercises across 4 types?**
- `UserExerciseProgress` PK is `(UserId, ExerciseId)` — streak tests need one distinct `ExerciseId` per calendar day
- 10 iterations per type enables comprehensive testing of type-specific validation logic
- MultipleChoice tests require option IDs (GUIDs) instead of text answers
- Listening/Translation tests verify fuzzy matching and accepted answers

**Why a system user?**
`Course.CreatedById` is a FK to `Users`. The system user is created first and excluded from `ClearLeaderboardDataAsync` so the content hierarchy survives between tests.

### Creating a DbContext in Tests

```csharp
var ctx = _fixture.CreateDbContext();
```

`PendingModelChangesWarning` is suppressed via `ConfigureWarnings(w => w.Ignore(...))` — this warning is benign in tests where the model and DB are migrated in sync.

## Test Naming Convention

All test methods follow the pattern: `MethodName_StateUnderTest_ExpectedBehavior`

Examples:
- `Student_CompletesFirstExercise_UnlocksNextExercise`
- `Admin_DeletesLesson_StudentCannotAccess`
- `Student_SubmitsWrongAnswer_CanRetryInfinitely`
- `TwoStudents_CompeteForRank_OrderedByXp`

**Not**:
- ~~`ShouldUnlockNextExerciseWhenCompleted`~~ (avoid "should" in test names)
- ~~`TestExerciseCompletion`~~ (too vague)
- ~~`CompletesFirstExercise`~~ (missing context about who/what)

## AAA Pattern (Arrange/Act/Assert)

All tests follow **strict AAA structure** as defined in `.claude/agents/test-generator.md`:

### Rules

1. **Arrange**: Setup only — no `.Should()` assertions
   - Use guard clauses (`if (x == null) throw new InvalidOperationException(...)`) for fixture validation
   - Distinguish fixture problems from test failures with descriptive error messages

2. **Act**: Execute operations only — no verification
   - Call methods under test
   - Fetch result data
   - No `.Should()` calls

3. **Assert**: All verification with FluentAssertions
   - Every `.Should()` assertion belongs here
   - Verify outcomes, state changes, response codes

### Example

```csharp
[Fact]
public async Task Student_CompletesExercise_UnlocksNextExercise()
{
    // Arrange
    var exercises = await GetExercisesForLessonAsync(Fixture.ExerciseIds[0]);
    if (exercises == null || exercises.Count < 2)
        throw new InvalidOperationException("Fixture should seed at least 2 exercises");

    var firstEx = exercises.First(e => e.OrderIndex == 0);
    var secondEx = exercises.First(e => e.OrderIndex == 1);

    // Act
    var submitResult = await SubmitAnswerAsync(firstEx.Id, "answer");
    var exercisesAfter = await GetExercisesForLessonAsync(Fixture.ExerciseIds[0]);

    // Assert
    firstEx.IsLocked.Should().BeFalse("first exercise should be unlocked");
    secondEx.IsLocked.Should().BeTrue("second exercise should be locked initially");
    submitResult.Should().NotBeNull();
    submitResult!.IsCorrect.Should().BeTrue();
    submitResult.PointsEarned.Should().Be(10);
    exercisesAfter!.First(e => e.OrderIndex == 1).IsLocked.Should().BeFalse();
}
```

### FluentAssertions "because" Clauses

Use `because` clauses to **explain business rules and edge cases** — not to repeat what the assertion already says. The `because` message should add context that isn't obvious from the test name or assertion itself.

#### When "because" Adds Value

✅ **Explains WHY a rule exists** (business context):
```csharp
// BAD — just restates the assertion
response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
    because: "response should be forbidden");

// GOOD — explains the business rule behind the status
response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
    because: "locked exercises prevent students from skipping ahead to later content");
```

✅ **Clarifies edge case behavior**:
```csharp
// BAD — obvious from the assertion
streak.Current.Should().Be(3);

// GOOD — explains the grace period edge case
streak.Current.Should().Be(3,
    because: "streak grace period extends to the entire consecutive run when last activity was yesterday");
```

✅ **Documents complex business logic**:
```csharp
// BAD — restates the comparison
completedExercises.Should().BeGreaterThanOrEqualTo(7);

// GOOD — explains the 70% threshold rule
completedExercises.Should().BeGreaterThanOrEqualTo(7,
    because: "70% completion threshold (7/10) ensures students engage with most lesson content before unlocking next lesson");
```

✅ **Provides context for "magic numbers"**:
```csharp
// BAD — doesn't explain why 10
result.PointsEarned.Should().Be(10);

// GOOD — explains where the value comes from
result.PointsEarned.Should().Be(10,
    because: "FillInBlank exercises award 10 XP per correct answer (from fixture seed data)");
```

#### When "because" is Redundant

❌ **Don't repeat the test method name**:
```csharp
// Test: Student_RoleRestrictedEndpoint_Returns403
response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
    because: "Student role is not authorized");  // ← already in test name
```

❌ **Don't restate the assertion**:
```csharp
user.Should().NotBeNull(because: "user should not be null");  // ← useless
```

❌ **Don't explain obvious SDK behavior**:
```csharp
token.Should().Contain(".",
    because: "JWT tokens contain dots");  // ← common knowledge
```

#### Pattern for Authorization Tests

For parameterized authorization tests, include the endpoint in the `because` to identify which case failed:

```csharp
// GOOD — identifies the failing endpoint in parameterized tests
response.StatusCode.Should().Be(
    HttpStatusCode.Unauthorized,
    because: $"{method} {path} requires authentication"
);

// Even better — explains WHY auth is required for this specific endpoint
response.StatusCode.Should().Be(
    HttpStatusCode.Forbidden,
    because: $"Admin-only endpoint {method} {path} prevents students from modifying course content"
);
```

#### Pattern for Security/Edge Cases

Security and edge case tests benefit most from `because` clauses:

```csharp
// Explains security rationale
submitResult.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
    because: "expired JWT must not grant access to prevent session replay attacks after timeout");

// Explains idempotency guarantee
totalXp.Should().Be(10,
    because: "resubmitting correct answer must not award XP twice — prevents XP farming");

// Explains grace period logic
streak.Current.Should().Be(1,
    because: "yesterday's activity counts toward current streak (1-day grace period for timezone flexibility)");

// Explains bypass mechanism
response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden,
    because: "Admin bypass allows content creators to test locked exercises without unlocking");
```

#### Failure Message Impact

**Without meaningful `because`:**
```
Expected exercises.Count to be 3, but found 2.
```

**With meaningful `because`:**
```
Expected exercises.Count to be 3 because fixture seeds 3 FillInBlank exercises per lesson
(indices 0-2), but found 2.
```

The second message tells you the fixture seeding failed, not the business logic.

### Helper Method Pattern

Helper methods (private methods that fetch data or perform HTTP calls) should throw on failure, not assert:

```csharp
// CORRECT — throws descriptive exception
private async Task<LeaderboardResponse> GetLeaderboardAsync(HttpClient client, TimeFrame timeFrame)
{
    var response = await client.GetAsync($"/api/leaderboard?timeFrame={timeFrame}");

    if (response.StatusCode != HttpStatusCode.OK)
        throw new InvalidOperationException($"Failed to fetch leaderboard: {response.StatusCode}");

    var leaderboard = await response.Content.ReadFromJsonAsync<LeaderboardResponse>();

    if (leaderboard == null)
        throw new InvalidOperationException("Leaderboard response was null");

    return leaderboard;
}

// WRONG — assertions in helper
private async Task<LeaderboardResponse> GetLeaderboardAsync(HttpClient client, TimeFrame timeFrame)
{
    var response = await client.GetAsync($"/api/leaderboard?timeFrame={timeFrame}");
    response.StatusCode.Should().Be(HttpStatusCode.OK); // ← WRONG
    var leaderboard = await response.Content.ReadFromJsonAsync<LeaderboardResponse>();
    leaderboard.Should().NotBeNull(); // ← WRONG
    return leaderboard!;
}
```

**Why throw instead of assert in helpers?**
- Helper failures indicate fixture/setup problems, not test failures
- Stack traces point directly to the broken setup step
- Test failures only occur in the Assert section where actual verification happens

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

## End-to-End Tests

E2E tests verify complete user workflows via HTTP endpoints using `WebApplicationFactory` and `ControllerTestBase`.

### ControllerTestBase

Base class for E2E tests providing:
- `DatabaseFixture` integration
- `WebApplicationFactory<Program>` wired to Testcontainers DB
- `CreateAuthenticatedUserAsync(username, email, role)` — creates user + JWT token
- `CreateClient(token)` — returns HttpClient with auth cookie
- `ClearTestDataAsync()` — DB cleanup between tests

### Test Organization

| File | Verifies | Tests |
|------|----------|-------|
| `StudentExerciseProgressJourneyTests.cs` | Exercise completion, XP calculation, sequential unlocking, retry behavior | 7 |
| `StudentSessionPersistenceTests.cs` | Progress restoration across sessions, state consistency, XP idempotency | 6 |
| `ExerciseSubmissionSecurityTests.cs` | Lock enforcement, role-based bypass, MultipleChoice validation, endpoint shapes | 9 |
| `LeaderboardAndStreaksTests.cs` | Leaderboard rankings, streak tracking, avatar integration, multi-user competition | 5 |
| `AdminContentManagementJourneyTests.cs` | Admin/ContentCreator CRUD operations, role-based authorization, content lifecycle | 6 |
| **Total E2E Tests** | | **33** |

### E2E Test Pattern

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

    public override async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await base.DisposeAsync();
    }

    [Fact]
    public async Task Student_Action_ExpectedOutcome()
    {
        // Arrange
        // ... setup test data

        // Act
        // ... make HTTP calls

        // Assert
        // ... verify responses with .Should()
    }
}
```

### Key Patterns

**Multiple authenticated users**:
```csharp
var (student1Id, student1Token) = await CreateAuthenticatedUserAsync("student1", "s1@test.com", "Student");
var (adminId, adminToken) = await CreateAuthenticatedUserAsync("admin", "admin@test.com", "Admin");

var studentClient = CreateClient(student1Token);
var adminClient = CreateClient(adminToken);
```

**Simulating session restart** (for progress restoration tests):
```csharp
// Complete exercises
await SubmitAnswerAsync(exercises[0].Id, "answer");

// "Leave" and return
_client.Dispose();
_client = CreateClient(_authToken);

// Verify progress restored
var progress = await GetLessonProgressAsync(lessonId);
```

**Direct DB seeding** (for streaks, avatars):
```csharp
using var scope = Factory.Services.CreateScope();
var ctx = scope.ServiceProvider.GetRequiredService<BackendDbContext>();

await DbSeeder.AddConsecutiveDaysActivityAsync(ctx, userId, Fixture.ExerciseIds, days: 3);
await DbSeeder.AddAvatarAsync(ctx, userId);
```

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
- `JWT_EXPIRATION_HOURS` — set to `"24"` (defaults in production but not in test env)
- `DATA_PROTECTION_KEYS_PATH` — set to `Path.GetTempPath()` to avoid `/app/dataprotection-keys` write failure
- `GOOGLE_CLIENT_ID` — required by `AddGoogleAuthentication()` at DI registration time
- `GOOGLE_CLIENT_SECRET` — required by `AddGoogleAuthentication()` at DI registration time

**All five must be set in `InitializeAsync` before `WebApplicationFactory` is instantiated**, and cleared to `null` in `DisposeAsync`. `AddGoogleAuthentication` reads both Google vars during `ConfigureServices` — not at request time — so missing either one throws `InvalidOperationException: GOOGLE_CLIENT_SECRET not found in environment variables` and collapses into an `AggregateException` wrapping a DI validation error.

## Assert Helper Pattern

Use a private `Assert*` method when a test class needs to verify the same set of properties across multiple tests. The method is `void`, uses FluentAssertions internally, and the test fails with a readable message at the assertion line if any property is wrong.

```csharp
private static void AssertValidUser(User? user, GoogleJsonWebSignature.Payload payload)
{
    user.Should().NotBeNull();
    user!.Email.Should().Be(payload.Email);
    user.UserName.Should().Be(CleanUsername(payload.Name));
    user.EmailConfirmed.Should().BeTrue();
}
```

Call site:

```csharp
var resultUser = await _sut.LoginUser(payload);
AssertValidUser(resultUser, payload);           // fails here with clear message if invalid

var roles = await _userManager.GetRolesAsync(resultUser!);  // ! is safe post-assertion
```

### Duplicating `private static` helpers from production code

If a production mapping/utility method is `private static`, it cannot be accessed from the test project. Duplicate it locally and add a comment explaining the source so it stays in sync:

```csharp
/// <summary>
/// Mirrors UserMapping.CleanUsername (private static there, so duplicated here).
/// </summary>
private static string CleanUsername(string name)
{
    char[] charsToRemove = ['-', ' ', '_', '*', '&'];
    return new string(name.Where(c => !charsToRemove.Contains(c)).ToArray());
}
```

If the production implementation changes, the duplicate will cause test failures — this acts as a useful tripwire.

## Gotchas

### ExerciseId FK on INSERT
`UserExerciseProgress.ExerciseId` uses `DeleteBehavior.NoAction` — SQL Server enforces this FK on INSERT, not just DELETE. Any `AddProgressAsync` call with an unknown `ExerciseId` will throw. Always use IDs from `fixture.ExerciseIds`.

### Flatpak IDE and NuGet
If running VS Code / VS Codium inside a Flatpak container, the IDE cannot see NuGet packages installed outside the Flatpak sandbox. This causes red squiggles for `Testcontainers.MsSql` and other packages. The `dotnet` CLI is unaffected — build and test from the terminal.

### xUnit v3 IAsyncLifetime
Return type is `ValueTask`, not `Task`. Using `Task` compiles but fails at runtime or produces a linter error about interface mismatch.

### xUnit1051 — CancellationToken on async calls
xUnit v3 analyzer raises xUnit1051 warnings when async methods that accept `CancellationToken` are called without it. Always pass `TestContext.Current.CancellationToken` for responsive test cancellation.

**HTTP client calls:**
```csharp
// HTTP operations
await client.PostAsJsonAsync("/api/endpoint", dto, TestContext.Current.CancellationToken);
await response.Content.ReadFromJsonAsync<Dto>(TestContext.Current.CancellationToken);
```

**EF Core operations:**
```csharp
// DbContext operations
await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

// FindAsync requires object array syntax
await _ctx.Lessons.FindAsync(
    new object[] { lessonId },
    TestContext.Current.CancellationToken
);

// LINQ query operators take token as last parameter
await _ctx.Lessons.FirstAsync(TestContext.Current.CancellationToken);
await _ctx.Lessons.FirstOrDefaultAsync(l => l.Id == id, TestContext.Current.CancellationToken);
await _ctx.Lessons.Where(l => l.CourseId == courseId).ToListAsync(TestContext.Current.CancellationToken);
```

**Service layer integration tests:**
Service methods themselves don't accept `CancellationToken` — only test setup/teardown and direct EF Core calls in Arrange/Assert sections need it.

### dotnet clean reveals incremental build gaps
`dotnet clean` removes cached assemblies and forces a full rebuild, which can expose
pre-existing type-not-found errors hidden by incremental builds (e.g. a missing entity
referenced in a nav property). Run `dotnet clean && dotnet build` periodically to catch these.

### WebApplicationFactory — `InitializeDatabaseAsync` is called
`Program.Main`'s `InitializeDatabaseAsync` (migrations + seed) **does** run when
`WebApplicationFactory` starts. With the DbContext overridden to the Testcontainers DB,
migrations are a no-op (already applied) and seeding is idempotent — safe.

### Null-Forgiving Operator (`!`) is Forbidden in Production Code
Never use `!` to suppress nullable warnings in service or controller code — always check explicitly and throw:

```csharp
// WRONG — suppresses compiler warning, crashes with NullReferenceException at runtime
var user = await _context.Users.FindAsync(id);
return user!.UserName;

// CORRECT — explicit null check with a meaningful exception
var user = await _context.Users.FindAsync(id)
    ?? throw new InvalidOperationException($"User {id} not found.");
return user.UserName;
```

Use:
- `ArgumentNullException.ThrowIfNull(arg)` for method/constructor parameters
- `?? throw new InvalidOperationException(...)` for async lookups that must return a value
- `?? throw new KeyNotFoundException(...)` for dictionary/lookup misses

**Two accepted exceptions** where `!` is a compiler hint rather than a null suppression:

1. **Deferred-init fields** assigned in `InitializeAsync` — xUnit guarantees `InitializeAsync` runs before any test method:
```csharp
private BackendDbContext _ctx = null!;

public async ValueTask InitializeAsync()
{
    _ctx = _fixture.CreateDbContext(); // guaranteed to run first
}
```

2. **After a FluentAssertions `NotBeNull()` assertion** — the assertion has already verified the value; `!` is purely a compiler hint at that point, not suppressing an unchecked null. Typically appears when passing a nullable result to a method that requires a non-nullable parameter:
```csharp
var resultUser = await _sut.LoginUser(payload);
AssertValidUser(resultUser, payload); // test fails here if null

var roles = await _userManager.GetRolesAsync(resultUser!); // ! is safe — assertion already passed
```

### WebApplicationFactory — Unregistered Service Surfaces as AggregateException
If a service class exists but was never added to `AddApplicationServices`, the DI container
validates the full graph at startup (`ValidateOnBuild = true` by default in .NET 8+) and throws:

```
AggregateException: Some services are not able to be constructed
  InvalidOperationException: Unable to resolve service for type 'Foo'
    while attempting to activate 'Bar'
```

This is caught immediately by `WebApplicationFactory` — the test never reaches its first HTTP call.
Fix: add `services.AddScoped<MissingService>()` to `AddApplicationServices` in
`ServiceCollectionExtensions.cs`.
