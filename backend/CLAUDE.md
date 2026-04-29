# Backend CLAUDE.md

ASP.NET Core 10.0 Web API + EF Core + SQL Server 2022.

> Cross-cutting bug patterns: [`.claude/rules/common-gotchas.md`](../.claude/rules/common-gotchas.md).
> Debugging playbooks: [`.claude/rules/troubleshooting.md`](../.claude/rules/troubleshooting.md).
> Entity model: [`Database/ENTITIES_DOCUMENTATION.md`](Database/ENTITIES_DOCUMENTATION.md) — read it before touching entities, migrations, or DbContext config. Don't grep entity files.

## Commands (from `backend/`)

```bash
dotnet restore
dotnet build Backend.sln
dotnet run                                              # :8080
dotnet watch run

dotnet ef migrations add <Name>     --project Database/Backend.Database.csproj
dotnet ef database update           --project Database/Backend.Database.csproj
dotnet ef migrations remove         --project Database/Backend.Database.csproj
```

## Layout

```
backend/
├── Controllers/       # API endpoints
├── Database/
│   ├── BackendDbContext.cs
│   ├── Entities/      # Users/, Exercises/ subfolders
│   ├── Migrations/
│   └── Extensions/    # seeders, migration helpers
├── Services/          # business logic — direct DbContext, no repository pattern
├── Dtos/              # records — never inline in controllers
├── Mapping/           # ToDto() extensions
├── Middleware/        # ErrorHandlingMiddleware, UserContextMiddleware
├── Extensions/        # ServiceCollectionExtensions, WebApplicationExtensions
└── Program.cs
```

API reference docs: [`/docs/backend/api/`](../docs/backend/api/) (error handling, auth, endpoint catalog).

## Conventions (must-follow)

- **Primary constructors** — every service / controller / middleware uses C# 12 primary constructor syntax. DTOs and entities are records.
- **Async all the way.** All service methods return `Task` / `Task<T>`.
- **No repository pattern** — services hold `BackendDbContext` directly.
- **Service registration** — every service goes in `AddApplicationServices` (DI graph validation crashes startup if missed).
- **Operation result enums over `bool`** when failure has multiple distinct reasons (e.g. `UnlockStatus`).
- **Never** the null-forgiving `!` operator. Throw `ArgumentNullException.ThrowIfNull` / `InvalidOperationException` / `KeyNotFoundException`.
- **DTOs** — record types in `Dtos/`. No anonymous objects in `Ok()`. Group related DTOs (`AuthDtos.cs`, `UploadDtos.cs`).
- **Mapping** — `entity.ToDto()` extension methods in `Mapping/`.
- **Auth attributes** — `[Authorize(Roles = "Admin,ContentCreator")]` on mutations. Default authenticated; opt out with `[AllowAnonymous]`.
- **User context** — always `HttpContext.GetCurrentUser()` (full entity from `UserContextMiddleware`). Never `User.FindFirstValue()`.
- **Error handling** — controllers throw, `ErrorHandlingMiddleware` maps to HTTP. No try/catch in controllers (except where you must convert to a result type before reaching the middleware).

### EF Core query rules

- `.Include()` / `.ThenInclude()` for full graphs.
- For polymorphic children: `.ThenInclude(e => (e as MultipleChoiceExercise)!.Options)`.
- Project to a `private record` when only scalar FKs are needed (cheaper than `Include`).
- `.OrderBy(e => e.OrderIndex)` for every collection.
- `GroupJoin` for left-join in a single round-trip (see `ExerciseProgressService`).
- Anonymous `new { }` for intermediate `Join`/`GroupBy`; named record only in terminal `.Select()` before `.ToListAsync()`. See [`.claude/rules/common-gotchas.md`](../.claude/rules/common-gotchas.md) for why.

### Progress & unlocking

- **Upsert pattern**: `FirstOrDefaultAsync` → create if null else update (see `ExerciseProgressService.SubmitAnswerAsync`).
- **Auto OrderIndex**: `MaxAsync(e => (int?)e.OrderIndex) ?? -1 + 1`.
- **Idempotent unlocks** — every unlock checks `IsLocked` before mutation.
- **Cascade unlocks** — `LessonService.UnlockNextLessonAsync` calls `ExerciseService.UnlockFirstExerciseInLessonAsync`.
- **Dual lock validation** in `SubmitAnswerAsync` — check both `lesson.IsLocked` and `exercise.IsLocked`. Admin / ContentCreator bypass via `UserExtensions.CanBypassLocksAsync`.
- **Add children to parent collection**, not DbContext: `lesson.Exercises.Add(...)` — otherwise navigation properties stay empty and DTOs render incorrectly.

### Performance

- `GetFullLessonProgressAsync` is expensive (~6 DB ops). Call at lesson load, lesson-complete, and dedicated progress endpoints only — **never** inside `SubmitAnswerAsync`.
- `GetProgressForLessonsAsync(userId, lessonIds)` for list pages — avoids N+1.
- DTO-per-context: `ExerciseSubmitResult` (no aggregates) vs `SubmitAnswerResponse` (includes `LessonProgressSummary`).
- Avatars stored as `varbinary(max)` in `UserAvatars` (1:1 with Users) — never load bytes in list queries; leaderboard batch-checks existence and serves via `/api/user/{id}/avatar`.

## Polymorphic exercise DTOs

```csharp
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(MultipleChoiceExerciseDto), "MultipleChoice")]
[JsonDerivedType(typeof(FillInBlankExerciseDto), "FillInBlank")]
[JsonDerivedType(typeof(ListeningExerciseDto), "Listening")]
[JsonDerivedType(typeof(TranslationExerciseDto), "Translation")]
public abstract record ExerciseDto(...);
```

Discriminator MUST be the **first** JSON property. `Ok(dto)` loses the discriminator — use `OkPolymorphic<ExerciseDto>(dto)` (sets `DeclaredType` on `OkObjectResult`). For `CreatedAtAction`, set `result.DeclaredType = typeof(ExerciseDto)` after creation.

## Enums

`[JsonConverter(typeof(JsonStringEnumConverter))]` on every enum exposed via API: `DifficultyLevel`, `ExerciseType`, `LessonStatus`, `TimeFrame`. Always use enums for discrete value sets — never raw strings between layers.

## IFormFile binding

With `<Nullable>enable</Nullable>` + `[ApiController]`, non-nullable `IFormFile` is implicitly `[Required]`. The FormData field name MUST match the parameter name. Mismatch returns 400 ProblemDetails before the action runs (no breakpoint hits). Wrap throwing validators (e.g. `SanitizeFilename`) in try/catch.

## Error handling middleware

Registered first in the pipeline. Maps exceptions to HTTP statuses, sanitizes paths and messages, returns `{ message, statusCode, detail }`. `detail` (stack trace) only in Development. Full mapping table: [`/docs/backend/api/error-handling.md`](../docs/backend/api/error-handling.md).

## Auth pipeline

- `AddJwtAuthentication()` reads the cookie via `OnMessageReceived`.
- `UserContextMiddleware` runs **after** `UseAuthentication()`, **before** `UseAuthorization()`. Loads User + `UserLanguages` + `Language` and stashes in `HttpContext.Items["CurrentUser"]`.
- `Secure` flag on the cookie is auto-set when the request is HTTPS. nginx terminates TLS; backend is plain HTTP internally.

## Database init

`Program.cs` → `InitializeDatabaseAsync` → `MigrateDbAsync` (10 retries, exponential backoff 3s/6s/12s, fresh DbContext per attempt; fails fast on `InvalidOperationException`) → seed data.

Connection string is built from env vars (`DB_SERVER`, `DB_NAME`, `DB_USER_ID`, `DB_PASSWORD`). Dev: `TrustServerCertificate=True, Encrypt=False`. Prod: `Encrypt=True`.

## Data Protection

Keys persisted to `/app/dataprotection-keys` (Docker volume `backend-dataprotection`). Override path with `DATA_PROTECTION_KEYS_PATH`. Keys are intentionally **not encrypted at rest** (Google-OAuth-only app, no antiforgery flow). **Do NOT encrypt with the LE cert** — 90-day rotation invalidates all keys.

## Authorization roles

`Admin` (full access) · `ContentCreator` (create/edit content) · `User` (default).

## Environment variables (`backend/.env`, mapped as Docker secret `backend_env`)

```
DB_SERVER=db
DB_NAME=LexiqDb
DB_USER_ID=sa
DB_PASSWORD=<...>
GOOGLE_CLIENT_ID=<...>
GOOGLE_CLIENT_SECRET=<...>

JWT_SECRET=<...>            # required — startup throws if missing
JWT_ISSUER=lexiq-api
JWT_AUDIENCE=lexiq-frontend
JWT_EXPIRATION_HOURS=24

ASPNETCORE_ENVIRONMENT=production
```

In production the backend reads from `/run/secrets/backend_env`.

## Port

Listens on `:8080`. Don't set `ASPNETCORE_URLS` — base image sets `ASPNETCORE_HTTP_PORTS=8080` natively.

## File uploads

- Static files served at `/static/uploads`. Max 100 MB. CORS enabled. 1-year cache.
- Endpoints: `POST /api/uploads/{fileType}`, `POST /api/uploads/any`, `GET /api/uploads/{fileType}/{filename}`, `GET /api/uploads/list/{fileType}`.
- All exceptions caught → `FileUploadResult.Failure()` with a generic message; full detail logged via `ILogger`. Never expose raw exception messages.

## API endpoints

| Controller | Base route | Auth |
|------------|-----------|------|
| `AuthController` | `/api/auth` | Mixed — `POST /google-login`, `POST /logout`, `GET /auth-status`, `GET /is-admin` |
| `CourseController` | `/api/courses` | Public read; Admin/Creator mutations |
| `LessonController` | `/api/lessons` | Public read; Admin/Creator mutations |
| `ExerciseController` | `/api/exercises` | Mutations Admin/Creator; `POST /{id}/submit` any user |
| `LanguageController` | `/api/languages` | Public read; Admin write |
| `UserLanguageController` | `/api/userLanguages` | Authenticated |
| `UserManagementController` | `/api/userManagement` | Admin only |
| `UploadsController` | `/api/uploads` | Authenticated |
| `LeaderboardController` | `/api/leaderboard?timeFrame=Weekly\|Monthly\|AllTime` | Public (includes self if authed) |
| `UserController` | `/api/user` | `GET /{userId}/avatar` public; `PUT /avatar` auth |

## Workflows

### Add an entity

1. Class in `Database/Entities/`.
2. `DbSet<T>` in `BackendDbContext`.
3. Fluent config in `OnModelCreating` if needed.
4. `dotnet ef migrations add <Name>` → review `Database/Migrations/` → `dotnet ef database update`.
5. Update [`Database/ENTITIES_DOCUMENTATION.md`](Database/ENTITIES_DOCUMENTATION.md). Commit migration + entity, then docs separately.

### Add an endpoint

1. DTOs in `Dtos/` (Read / Create / Update — `record` types).
2. Mapping in `Mapping/` (`ToDto()`).
3. Service method in `Services/`. Register in `ServiceCollectionExtensions.cs` as Scoped.
4. Controller action with appropriate `[Authorize]` attribute.
5. Verify via Swagger at `http://localhost:8080/swagger`.
6. Update the API endpoints table above. Commit code, then docs.

## Known limitations

- No DTO validation layer — validation happens in entities.
- `Lesson.status` is **not** returned by the API; frontend derives it from `isLocked`, `isCompleted`, `completedExercises`.
- Lesson completion threshold: 70% XP (`ExerciseProgressService.DefaultCompletionThreshold`).
- `UserExerciseProgress.ExerciseId` FK uses `DeleteBehavior.NoAction` (SQL Server multiple-cascade-path constraint).
- Exercise unlocking: hybrid — first exercise unlocks with the lesson; rest unlock sequentially on completion (infinite retries).
