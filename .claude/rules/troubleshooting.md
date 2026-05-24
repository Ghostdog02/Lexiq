# Troubleshooting Playbooks

Quick reference for diagnosing common Lexiq failures. Pulled out of CLAUDE.md so per-session token cost is minimal — open this file only when actively debugging.

---

## Test cross-contamination: fixture data deleted by another test class

**Symptom**: FK constraint violation on `fixture.LessonId` / `fixture.CourseId`, or tests pass in isolation but fail in the full suite.

**Root cause**: `_ctx.Courses.FirstAsync()` or `_ctx.Languages.FirstAsync()` returns a row left by a test class that doesn't clean up courses/languages (e.g. `CourseCrudTests`, `LanguageCrudTests`). The cleanup then deletes the **fixture** course/language as a "stale" row, cascade-deleting the fixture lesson — breaking all subsequent E2E and integration tests that need it.

**Fix**: always anchor to the fixture's known ID, never to `FirstAsync()`:

```csharp
// ❌ Wrong — returns any course if another test class left rows behind
var course = await _ctx.Courses.FirstAsync(...);
_courseId = course.CourseId;

// ✅ Correct — always resolves to the fixture's course
var fixtureLesson = await _ctx
    .Lessons.Where(l => l.LessonId == fixture.LessonId)
    .Select(l => new { l.CourseId, l.Course.LanguageId })
    .FirstAsync(...);
_courseId = fixtureLesson.CourseId;
_languageId = fixtureLesson.LanguageId;
```

Apply this pattern in every test class that does its own course/lesson cleanup with `ExecuteDeleteAsync`.

---

## Test answer submission: option-ID vs text

**Symptom**: `submitResult.IsCorrect` is `false` even when submitting the "correct" text (e.g. `"answer"`).

**Root cause**: `ExerciseProgressService.ValidateAnswer` for FillInBlank / Listening / TrueFalse validates by `ExerciseOptionId` (GUID), not by `OptionText`. Submitting the text directly never matches a GUID.

**Fix — exercise DTO already loaded** (use `IsCorrect` from the API response):

```csharp
private static string GetCorrectOptionId(ExerciseDto exercise) => exercise switch
{
    FillInBlankExerciseDto fib => fib.Options.First(o => o.IsCorrect).Id,
    ListeningExerciseDto   le  => le.Options.First(o => o.IsCorrect).Id,
    TrueFalseExerciseDto   tf  => tf.Options.First(o => o.IsCorrect).Id,
    _ => throw new InvalidOperationException($"Cannot extract correct option from {exercise.GetType().Name}"),
};

// Usage
var submitResult = await SubmitAnswerAsync(ex.Id, GetCorrectOptionId(ex));
```

**Fix — only exercise ID available** (use `DbSeeder.GetCorrectOptionIdAsync`):

```csharp
// In InitializeAsync — load alongside exerciseId
_correctOptionIds.Add(await DbSeeder.GetCorrectOptionIdAsync(ctx, id));

// Or inline in a test method
using var scope = Factory.Services.CreateScope();
var ctx = scope.ServiceProvider.GetRequiredService<BackendDbContext>();
var correctOptionId = await DbSeeder.GetCorrectOptionIdAsync(ctx, exerciseId);
await SubmitAnswerAsync(client, exerciseId, correctOptionId);
```

Wrong-answer submissions are unaffected — any non-matching string correctly returns `IsCorrect = false`.

---

## 401 Unauthorized

1. Browser DevTools → Application → Cookies → confirm `AuthToken` exists.
2. Backend log should show `🔍 UserContextMiddleware: UserId from JWT = …`. If empty, the JWT is missing or malformed.
3. Confirm controllers read `ClaimTypes.NameIdentifier`, **not** `JwtRegisteredClaimNames.Sub` (ASP.NET Core remaps `sub` → `NameIdentifier`).
4. After a DB reset, the user ID inside an old JWT no longer exists — clear cookies and re-login.
5. CORS regression: ensure `AllowCredentials()` with a specific origin (not `*`) and that frontend sends `withCredentials: true`.

## Cookie not being sent (frontend → backend)

1. Frontend nginx must proxy `/api` to backend (same-origin) for `SameSite=Lax` to work.
2. CORS must allow credentials with a specific origin.
3. HTTP client call must include `withCredentials: true`.
4. Across true cross-origin calls: `SameSite=None` + `Secure=true` are required.

## 400 Bad Request with no detail

1. Check Network tab response body for the failing field.
2. Common causes:
   - Enum sent as string but backend lacks `JsonStringEnumConverter`.
   - JSON polymorphism: `type` discriminator missing or not the **first** property.
   - Required field null/empty.
3. Confirm `SuppressModelStateInvalidFilter` is **not** enabled.

## Frontend/Backend interface mismatch (data loads but UI is empty)

1. Copy full JSON from Network tab.
2. Compare names against TypeScript interfaces. Common drift:
   - Backend `type` ↔ frontend `exerciseType`.
   - Backend `text` (FillInBlank) ↔ frontend `question`.
   - Backend numeric enum ↔ frontend string enum.
3. Symptom: `@switch` never matches, template expressions silently `undefined`.

## Exercise progress not restoring

- Backend returns `SubmitAnswerResponse` for **every** exercise in the lesson, not only attempted ones.
- "Never attempted" and "attempted incorrectly" share `pointsEarned: 0, isCorrect: false`.
- Discriminator: `correctAnswer !== null` → actually attempted.
- `wasAttempted = response.isCorrect || response.correctAnswer !== null`.

## Docker container failures

```bash
docker compose logs <service>
docker compose ps
docker inspect --format='{{json .State.Health}}' <container>
```

- Health-check stdout/stderr is NOT in `docker compose logs` — use `docker inspect`.
- Secrets present? `backend/Database/password.txt`, `backend/.env`.
- Port conflicts: `sudo lsof -i :8080`, `sudo lsof -i :4200`.
- Backend retries SQL Server up to 10× with 3s backoff — wait at least 30s before declaring dead.
- Reset state: `docker compose down -v && docker compose up --build`.

## CI/CD pipeline failures

1. Inspect failed step in GitHub Actions log.
2. Reproduce locally: `docker compose -f docker-compose.prod.yml build`.
3. Common fault lines:
   - Dockerfile syntax / missing build arg.
   - GHCR auth (token scopes).
   - SSH/SCP secrets in repo settings.
   - `scripts/deploy.sh` exit codes (1=file, 3=auth/pull, 4=container start).
4. Server logs: `tail -100 /var/log/lexiq/deployment/deploy-*.log`.

## Migration errors

- Conflicting migrations → delete the offending file, recreate.
- Multiple cascade paths (SQL Server) → use `DeleteBehavior.NoAction` on the secondary FK.
- Roll back: `dotnet ef database update <PreviousMigrationName> --project Database/Backend.Database.csproj`.
- Drop bad migration: `dotnet ef migrations remove --project Database/Backend.Database.csproj`.
- EF Core 10 throws `PendingModelChangesWarning` as an error — confirm migration class names match file names AND the `[Migration]` attribute.

### Altering a column that is part of a composite primary key

`ALTER TABLE ALTER COLUMN X failed because one or more objects access this column.`

SQL Server cannot alter a column that participates in a composite PK. EF Core's auto-generated migrations **do not** insert the required PK drop/recreate around the `AlterColumn`. Fix the migration manually:

```csharp
migrationBuilder.DropPrimaryKey(name: "PK_UserLanguages", table: "UserLanguages");

migrationBuilder.AlterColumn<string>(name: "LanguageId", table: "UserLanguages", ...);

migrationBuilder.AddPrimaryKey(
    name: "PK_UserLanguages",
    table: "UserLanguages",
    columns: new[] { "UserId", "LanguageId" });
```

Affected tables in this project: `UserLanguages (UserId, LanguageId)`, `UserExerciseProgress (UserId, ExerciseId)`, `UserAchievements (UserId, AchievementId)`.

### Multiple cascade paths blocked by SQL Server

`FK_X may cause cycles or multiple cascade paths. Specify ON DELETE NO ACTION.`

Caused when two FK chains can both cascade-delete to the same table. Example: `User → Course (Cascade) → Lesson → Exercise` AND `User → Exercise via CreatedById (Cascade)`. Fix: set the secondary FK to `NoAction` in both the migration and DbContext:

```csharp
// DbContext
modelBuilder.Entity<Exercise>()
    .HasOne(e => e.CreatedBy)
    .WithMany()
    .HasForeignKey(e => e.CreatedById)
    .OnDelete(DeleteBehavior.NoAction);

// Migration
migrationBuilder.AddForeignKey(..., onDelete: ReferentialAction.NoAction);
```

### Hardcoded FK sentinel values in seeders

`INSERT conflicted with FOREIGN KEY constraint "FK_Exercises_Users_CreatedById".`

Caused by using a hardcoded string (e.g. `const string AdminUserId = "system-admin"`) as a FK value. No row with that ID exists. The real admin user ID is generated dynamically by Identity — always thread it through seeder parameters:

```csharp
// SeedData.cs
await ExerciseSeeder.SeedAsync(context, lessonIds, adminUserId); // pass it!

// ExerciseSeeder.cs
public static async Task SeedAsync(BackendDbContext ctx, List<string> lessonIds, string createdById)
```

## xUnit fixture errors

### "Class fixture type may only define a single public constructor"

Despite the message mentioning constructors, this is often caused by the fixture class being `abstract`. xUnit instantiates `IClassFixture<T>` directly — `T` must be a concrete class. Remove `abstract` from the fixture:

```csharp
// ❌ xUnit cannot instantiate this
public abstract class DatabaseFixture : IAsyncLifetime { }

// ✅ concrete — xUnit can create one instance per test class
public class DatabaseFixture : IAsyncLifetime { }
```

`ControllerTestBase` can remain `abstract` — it's a base class that tests *inherit*, not a fixture xUnit creates.

## Mixed content (HTTPS page → HTTP backend request)

- Root cause: `BACKEND_API_URL` baked into the Angular bundle as absolute `http://…`.
- Fix: set repo variable to `/api` (relative) so it inherits page scheme via nginx.

## Frontend stuck in ACME-only mode despite valid certs

- Race condition: frontend checks cert readability before certbot completes `chmod 755`.
- Already fixed via `depends_on: certbot: condition: service_completed_successfully` in `docker-compose.prod.yml`.
- Certbot uses `restart: no` (one-shot permission fix), then frontend starts.

## OpenAPI build errors

- `Tags = [...]` collection-expression error → use `new HashSet<OpenApiTag> { ... }`.
- `OpenApiSecurityRequirement` keys must be `OpenApiSecuritySchemeReference`, not `OpenApiSecurityScheme`. The `referenceId` must match a key in `document.Components.SecuritySchemes`.
