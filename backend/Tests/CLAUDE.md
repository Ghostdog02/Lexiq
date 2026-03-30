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
    ├── GetStreakTests.cs       ← Testcontainers integration tests
    ├── EfCoreEdgeCaseTests.cs  ← EF Core edge cases (TPH, cascade, navigation)
    └── UserXpServiceTests.cs   ← XP aggregation and activity tracking
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
3. Seeds a minimal content hierarchy once per test run

### Permanent Content Hierarchy

Seeded once, **never cleared** between tests:

```
System User (satisfies Course.CreatedById FK)
  └─ Language: "Italian"
       └─ Course
            └─ Lesson (empty — tests create their own exercises)
```

**Why a system user?**
`Course.CreatedById` is a FK to `Users`. The system user is created first and excluded from `ClearLeaderboardDataAsync` so the content hierarchy survives between tests.

**Why no pre-seeded exercises?**
Tests create their own exercises in `InitializeAsync` to avoid interdependencies. The `DatabaseFixture` exposes `CourseId` and `LessonId` so test classes can create their own courses/lessons and exclude the fixture's base ones during cleanup. Exercises are deleted in `ClearLeaderboardDataAsync` between tests to prevent accumulation. Test classes that create additional courses/lessons should clean them up in `InitializeAsync` before creating new ones.

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
    var firstExId = _exerciseIds[0];
    var secondExId = _exerciseIds[1];

    // Act
    var submitResult = await SubmitAnswerAsync(firstExId, "answer");

    var exercises = await GetExercisesForLessonAsync(Fixture.LessonId);
    var firstEx = exercises.First(e => e.Id == firstExId);
    var secondEx = exercises.First(e => e.Id == secondExId);

    // Assert
    firstEx.IsLocked.Should().BeFalse("first exercise starts unlocked");
    secondEx.IsLocked.Should().BeFalse("completing first exercise unlocks second");
    submitResult.Should().NotBeNull();
    submitResult!.IsCorrect.Should().BeTrue();
    submitResult.PointsEarned.Should().Be(10);
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

Helper methods should perform HTTP calls or data fetching without validation. Assertions belong in the test's Assert section only:

```csharp
// CORRECT — no validation in helper
private async Task<LeaderboardResponse?> GetLeaderboardAsync(HttpClient client, TimeFrame timeFrame)
{
    var response = await client.GetAsync(
        $"/api/leaderboard?timeFrame={timeFrame}",
        TestContext.Current.CancellationToken
    );

    if (response.StatusCode != HttpStatusCode.OK)
        return null;

    return await response.Content.ReadFromJsonAsync<LeaderboardResponse>(
        cancellationToken: TestContext.Current.CancellationToken
    );
}
```

**Usage in test:**
```csharp
// Act
var leaderboard = await GetLeaderboardAsync(_client, TimeFrame.AllTime);

// Assert
leaderboard.Should().NotBeNull();
leaderboard!.Entries.Should().NotBeEmpty();
```

**Why no validation in helpers?**
- Keeps Arrange section free of assertions (strict AAA pattern)
- Test failures only occur in Assert section with clear FluentAssertions messages
- Avoids ambiguity about what's being tested vs what's setup

## Test Class Pattern

Every integration test class follows this structure:

```csharp
public class MyTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private BackendDbContext _ctx = null!;
    private List<string> _exerciseIds = null!;

    public MyTests(DatabaseFixture fixture) => _fixture = fixture;

    // Runs before each test method (xUnit creates a new instance per test)
    public async ValueTask InitializeAsync()
    {
        _ctx = _fixture.CreateDbContext();
        await DbSeeder.ClearLeaderboardDataAsync(_ctx, _fixture.SystemUserId);

        // Create exercises for this test class
        _exerciseIds = new List<string>();
        for (var i = 0; i < 10; i++)
        {
            var id = await DbSeeder.CreateFillInBlankExerciseAsync(
                _ctx,
                _fixture.LessonId,
                orderIndex: i,
                isLocked: i != 0  // Only first exercise unlocked
            );
            _exerciseIds.Add(id);
        }
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

### Test Isolation for Course/Lesson Tests

Tests that create their own courses or lessons must clean them up in `InitializeAsync` to prevent state leakage when running the full test suite. `ClearLeaderboardDataAsync` only deletes exercises, users, and progress — not courses or lessons.

**Pattern for tests that create courses/lessons**:

```csharp
public async ValueTask InitializeAsync()
{
    _ctx = _fixture.CreateDbContext();

    // Clean up state from previous tests
    await DbSeeder.ClearLeaderboardDataAsync(_ctx, _fixture.SystemUserId);
    await ClearTestCoursesAndLessonsAsync();  // ← Required for test isolation

    // Now create test-specific courses/lessons
    _secondCourseId = Guid.NewGuid().ToString();
    _ctx.Courses.Add(new Course { ... });
    await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
}

/// <summary>
/// Deletes all lessons except the fixture's base lesson, and all courses except the fixture's base course.
/// Required because ClearLeaderboardDataAsync only deletes exercises, not lessons or courses.
/// </summary>
private async Task ClearTestCoursesAndLessonsAsync()
{
    await _ctx.Lessons
        .Where(l => l.Id != _fixture.LessonId)
        .ExecuteDeleteAsync(TestContext.Current.CancellationToken);

    await _ctx.Courses
        .Where(c => c.Id != _fixture.CourseId)
        .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
}
```

**Why this matters**: Tests run in unpredictable order when using `dotnet test` with the full suite. Without cleanup, courses/lessons created in one test persist and cause assertion failures in later tests (e.g., `OrderIndex` calculations, "first lesson" queries).

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

### Creating Exercises

Tests create exercises in `InitializeAsync` using helper methods. All exercises accept `"answer"` as the correct response for test simplicity:

```csharp
// In InitializeAsync
using var scope = Factory.Services.CreateScope();
var ctx = scope.ServiceProvider.GetRequiredService<BackendDbContext>();

// Create exercises for this test
_exerciseIds = new List<string>();
for (var i = 0; i < 10; i++)
{
    var id = await DbSeeder.CreateFillInBlankExerciseAsync(
        ctx,
        Fixture.LessonId,
        orderIndex: i,
        isLocked: i != 0  // Only first exercise unlocked
    );
    _exerciseIds.Add(id);
}
```

**Available exercise creation helpers:**
- `CreateFillInBlankExerciseAsync(ctx, lessonId, orderIndex, isLocked, points)`
- `CreateMultipleChoiceExerciseAsync(ctx, lessonId, orderIndex, isLocked, points)` — creates 3 options (1 correct)
- `CreateListeningExerciseAsync(ctx, lessonId, orderIndex, isLocked, points)`
- `CreateTranslationExerciseAsync(ctx, lessonId, orderIndex, isLocked, points)`

### Adding Progress

```csharp
await DbSeeder.AddProgressAsync(ctx, userId, _exerciseIds[0],
    isCompleted: true, pointsEarned: 10, completedAt: DateTime.UtcNow);

// Add N consecutive days of activity
await DbSeeder.AddConsecutiveDaysActivityAsync(ctx, userId, _exerciseIds,
    days: 5, startDaysAgo: 0);
```

### Teardown Order

`ClearLeaderboardDataAsync` deletes in FK-safe order:

1. `UserExerciseProgress` (FK → Exercises, NoAction)
2. `Exercises` (deleted after progress to prevent accumulation across tests)
3. `UserAvatars`
4. Identity junction tables: `UserClaims`, `UserLogins`, `UserRoles`, `UserTokens`
5. `Users` (excluding system user)

**Content hierarchy cleanup**:
- Language row is **never deleted**
- The fixture's base Course and Lesson (exposed via `DatabaseFixture.CourseId` and `LessonId`) are **never deleted**
- Test classes that create additional courses/lessons should clean them up in `InitializeAsync` (e.g., `LessonCrudTests` and `LessonNavigationTests` use a `ClearTestCoursesAndLessonsAsync` helper that excludes the fixture's base entities)

## End-to-End Tests

E2E tests verify complete user workflows via HTTP endpoints using `WebApplicationFactory` and `ControllerTestBase`.

### ControllerTestBase

Base class for E2E tests providing:
- `DatabaseFixture` integration
- `WebApplicationFactory<Program>` wired to Testcontainers DB
- `CreateAuthenticatedUserAsync(username, email, role)` — creates user + JWT token
- `CreateClient(token)` — returns HttpClient with auth cookie
- `JsonOptions` — application's `JsonSerializerOptions` for polymorphic deserialization
- `ClearTestDataAsync()` — DB cleanup between tests

**wwwroot/uploads directory creation:**
`ControllerTestBase.InitializeAsync()` calls `EnsureUploadDirectoriesExist()` to create the `wwwroot/uploads` directory structure before `WebApplicationFactory` starts. This is required because:
- ASP.NET Core's static file middleware expects `wwwroot` to exist
- `wwwroot/` is gitignored (uploaded files not tracked)
- Dockerfile.dev creates these at build time, but local tests run outside Docker
- Directories are created relative to backend project root (`../../..` from Tests/)

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

await DbSeeder.AddConsecutiveDaysActivityAsync(ctx, userId, _exerciseIds, days: 3);
await DbSeeder.AddAvatarAsync(ctx, userId);
```

**Content hierarchy cleanup**: Language row and the fixture's base Course/Lesson are never deleted. Exercise rows are deleted in `ClearLeaderboardDataAsync`. Test classes that create additional courses/lessons handle their own cleanup.

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

**All five are set in `ControllerTestBase.InitializeAsync` before `WebApplicationFactory` is instantiated**. They are **NOT cleared** in `DisposeAsync` — clearing them causes race conditions when xUnit runs test classes in parallel (Test A clears while Test B is still using them). The test values are harmless to leave set for the entire test process lifetime.

`AddGoogleAuthentication` reads both Google vars during `ConfigureServices` — not at request time — so missing either one throws `InvalidOperationException: GOOGLE_CLIENT_SECRET not found in environment variables` and collapses into an `AggregateException` wrapping a DI validation error.

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
`UserExerciseProgress.ExerciseId` uses `DeleteBehavior.NoAction` — SQL Server enforces this FK on INSERT, not just DELETE. Any `AddProgressAsync` call with an unknown `ExerciseId` will throw. Always use IDs from exercises created in your test's `InitializeAsync` (stored in `_exerciseIds`).

### Flatpak IDE and NuGet
If running VS Code / VS Codium inside a Flatpak container, the IDE cannot see NuGet packages installed outside the Flatpak sandbox. This causes red squiggles for `Testcontainers.MsSql` and other packages. The `dotnet` CLI is unaffected — build and test from the terminal.

### xUnit v3 IAsyncLifetime
Return type is `ValueTask`, not `Task`. Using `Task` compiles but fails at runtime or produces a linter error about interface mismatch.

### xUnit1051 — CancellationToken on async calls
xUnit v3 analyzer raises xUnit1051 warnings when async methods that accept `CancellationToken` are called without it. Always pass `TestContext.Current.CancellationToken` for responsive test cancellation.

**HTTP client calls:**
```csharp
// HTTP operations (non-polymorphic DTOs)
await client.PostAsJsonAsync("/api/endpoint", dto, TestContext.Current.CancellationToken);
await response.Content.ReadFromJsonAsync<Dto>(TestContext.Current.CancellationToken);

// HTTP operations (polymorphic DTOs — requires JsonOptions)
await client.PostAsJsonAsync("/api/exercises", exerciseDto, JsonOptions, TestContext.Current.CancellationToken);
await response.Content.ReadFromJsonAsync<ExerciseDto>(JsonOptions, TestContext.Current.CancellationToken);
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

### Polymorphic DTO Serialization and Deserialization in Tests
System.Text.Json requires special handling for polymorphic types (`CreateExerciseDto`, `ExerciseDto`) in tests. Both serialization (sending) and deserialization (receiving) need `JsonOptions`.

#### Deserialization (ReadFromJsonAsync)
`ReadFromJsonAsync<ExerciseDto>()` with default `JsonSerializerOptions` fails with `NotSupportedException: must specify a type discriminator`.

**Always pass `JsonOptions`** when deserializing polymorphic types:

```csharp
// WRONG — uses default JsonSerializerOptions, fails on abstract ExerciseDto
var dto = await response.Content.ReadFromJsonAsync<ExerciseDto>(
    cancellationToken: TestContext.Current.CancellationToken
);

// CORRECT — uses application's configured options with polymorphic support
var dto = await response.Content.ReadFromJsonAsync<ExerciseDto>(
    JsonOptions,
    TestContext.Current.CancellationToken
);
```

#### Serialization (PostAsJsonAsync)
When posting polymorphic DTOs, you must:
1. Pass `JsonOptions` to include type discriminator configuration
2. Declare the variable as the **base type** to force serialization through the polymorphic serializer

**Always pass `JsonOptions` and use base type declaration:**

```csharp
// WRONG — concrete type declaration, serializer omits type discriminator
var addExerciseDto = new CreateFillInBlankExerciseDto(...);
await client.PostAsJsonAsync("/api/exercises", addExerciseDto, TestContext.Current.CancellationToken);

// CORRECT — base type declaration forces polymorphic serialization with discriminator
CreateExerciseDto addExerciseDto = new CreateFillInBlankExerciseDto(...);
await client.PostAsJsonAsync(
    "/api/exercises",
    addExerciseDto,
    JsonOptions,
    TestContext.Current.CancellationToken
);
```

**Why base type matters:** When the variable is declared as `CreateFillInBlankExerciseDto`, the serializer treats it as a concrete type and doesn't add the `"type"` discriminator. Declaring as `CreateExerciseDto` forces serialization through the `[JsonPolymorphic]` base type, which includes the discriminator.

**Nested polymorphic types:** When polymorphic DTOs are nested in collections (like `CreateLessonDto.Exercises`), the collection is already typed as `List<CreateExerciseDto>?`, so the base type is implicit. Just pass `JsonOptions`:

```csharp
var createLessonDto = new CreateLessonDto(
    ...,
    Exercises: [
        new CreateFillInBlankExerciseDto(...),  // ← implicitly treated as CreateExerciseDto
    ]
);

await client.PostAsJsonAsync("/api/lessons", createLessonDto, JsonOptions, TestContext.Current.CancellationToken);
```

This applies to all polymorphic types: `CreateExerciseDto`, `ExerciseDto`, and their derived types.

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
