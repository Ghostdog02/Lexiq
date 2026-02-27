# Backend CLAUDE.md

ASP.NET Core 10.0 Web API with Entity Framework Core and SQL Server 2022.

## Development Commands

```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build Backend.sln

# Run the development server (listens on port 8080)
dotnet run

# Run with watch mode (auto-reload on changes)
dotnet watch run

# Create a new EF Core migration
dotnet ef migrations add <MigrationName> --project Database/Backend.Database.csproj

# Apply migrations to database
dotnet ef database update --project Database/Backend.Database.csproj

# Remove last migration
dotnet ef migrations remove --project Database/Backend.Database.csproj
```

## Project Structure

```
backend/
├── Controllers/          # API endpoints
│   ├── AuthController.cs              # Google OAuth login, logout, auth-status
│   ├── LeaderboardController.cs       # Leaderboard rankings (GET /api/leaderboard)
│   ├── CourseController.cs            # Course CRUD
│   ├── LessonController.cs           # Lesson CRUD
│   ├── ExerciseController.cs         # Exercise CRUD (polymorphic types)
│   ├── LanguageController.cs         # Language management
│   ├── UserLanguageController.cs     # User ↔ Language enrollment
│   ├── UserManagementController.cs   # User CRUD (admin)
│   ├── RoleManagementController.cs   # Role management (admin)
│   ├── UploadsController.cs          # File/image uploads
│   └── UserController.cs            # User profile operations (avatar upload)
├── Database/
│   ├── BackendDbContext.cs        # EF Core DbContext
│   ├── Entities/                  # Database models (Users/, Exercises/ subdirs)
│   ├── Migrations/                # EF Core migrations
│   └── Extensions/                # Seeders & migration helpers
├── Services/
│   ├── GoogleAuthService.cs       # Google token validation & user creation
│   ├── JwtService.cs              # JWT generation (HS256, cookie-set by AuthController)
│   ├── CourseService.cs           # Course business logic
│   ├── LessonService.cs          # Lesson business logic
│   ├── ExerciseService.cs        # Exercise business logic
│   ├── ExerciseProgressService.cs # Answer validation, progress tracking, sequential unlocking, unified progress queries
│   ├── LeaderboardService.cs    # Leaderboard queries, streak/level calculation
│   ├── AvatarService.cs        # Avatar download (Google), upsert, retrieval, validation
│   ├── UserService.cs           # Avatar upload orchestration
│   ├── LanguageService.cs        # Language business logic
│   ├── UserLanguageService.cs    # Enrollment logic
│   ├── FileUploadsService.cs     # File upload handling
│   └── UserExtensions.cs         # User utility methods
├── Models/              # Request/response models (EditorJSModel, FileModel)
├── Dtos/                # Data Transfer Objects
├── Mapping/             # DTO ↔ Entity mappings
├── Middleware/
│   └── UserContextMiddleware.cs  # Loads User entity from JWT claims
├── Extensions/          # Service collection & app builder extensions
└── Program.cs          # Application entry point
```

## Key Patterns

### JSON Polymorphism for Exercise DTOs
- Type discriminator MUST be first property in JSON: `{ "type": "MultipleChoice", ...rest }`
- Frontend mapping: `return { type: ExerciseType.X, ...base }` NOT `{ ...base, type: ... }`
- System.Text.Json fails with "must specify a type discriminator" if type is not first

### Enum Serialization
- Add `[JsonConverter(typeof(JsonStringEnumConverter))]` to enums for string serialization
- Frontend sends "Beginner", backend receives DifficultyLevel.Beginner (not 0)
- Required for: DifficultyLevel, ExerciseType, LessonStatus, TimeFrame
- **Always use enums for discrete value sets** — never use raw strings for values like time frames, status codes, or categories

### IFormFile Model Binding
- `<Nullable>enable</Nullable>` + `[ApiController]` makes non-nullable `IFormFile` implicitly `[Required]`
- FormData field name MUST match the parameter name (e.g. `IFormFile file` → field `"file"`)
- Mismatch returns 400 ProblemDetails **before** controller action runs — no breakpoint will hit
- `SanitizeFilename` and other validators that throw must be inside try-catch to avoid raw 500s

### Service Registration
- Organized via extension methods in `Extensions/ServiceCollectionExtensions.cs`
- Each feature has its own extension method (AddCorsPolicy, AddDatabaseContext, AddApplicationServices, etc.)
- Services are registered as Scoped for per-request lifecycle
- No repository pattern — services directly access DbContext

### Middleware Configuration
- Configured in `Extensions/WebApplicationExtensions.cs`

### Authentication (JWT-in-Cookie)
- `JwtService` signs a JWT (HS256); default expiry **24h** (env: `JWT_EXPIRATION_HOURS`)
- `AuthController` sets it as an HttpOnly, SameSite=Lax cookie named `AuthToken`
- `AddJwtAuthentication()` extracts the token from that cookie via `OnMessageReceived`
- Google OAuth via `GoogleJsonWebSignature.ValidateAsync()`
- SameSite=Lax; `Secure` flag set automatically when the request is HTTPS
- All controllers require authentication unless explicitly marked with `[AllowAnonymous]`
- CORS configured for frontend origin (environment variable: `ANGULAR_PORT`)
- Production HTTPS terminated at nginx (certbot manages the cert); backend speaks plain HTTP internally

### UserContextMiddleware
- Registered after `UseAuthentication()` but before `UseAuthorization()` in the pipeline
- Extracts user ID from `ClaimTypes.NameIdentifier` (NOT `JwtRegisteredClaimNames.Sub` — ASP.NET Core maps `sub` → `NameIdentifier`)
- Stores user in `HttpContext.Items["CurrentUser"]` for controller access
- Access via `HttpContext.GetCurrentUser()` extension method (namespace: `Backend.Api.Middleware`)
- Eager loads `UserLanguages` and related `Language` entities

### Database Initialization
- Happens in `Program.cs` via `InitializeDatabaseAsync()`
- Auto-migration with retry logic (10 attempts, 3-second delays) for Docker startup
- Seed data initialization after migration
- Connection string built from environment variables (DB_SERVER, DB_NAME, DB_USER_ID, DB_PASSWORD)
- Development: `TrustServerCertificate=True, Encrypt=False`
- Production: `TrustServerCertificate=True, Encrypt=True`

### Data Protection Keys
- Persisted to Docker named volume (`backend-dataprotection`) via `PersistKeysToFileSystem` in `ServiceCollectionExtensions.cs`
- Path configurable via `DATA_PROTECTION_KEYS_PATH` env var (default `/app/dataprotection-keys`)
- Keys are intentionally **not encrypted at rest** — Google OAuth-only app has no password reset/antiforgery flows; accepted trade-off
- **Do NOT encrypt with the LE cert** — cert rotates every 90 days, invalidating all persisted keys and logging everyone out
- The unencrypted-at-rest warning is suppressed at `Error` level in `appsettings.json` (intentional, not a bug)

## Database Schema

Built with ASP.NET Core Identity. See `Database/ENTITIES_DOCUMENTATION.md` for comprehensive entity documentation.

**Content Hierarchy:**
```
Language (1) → Course (M) → Lesson (M) → Exercise (M)
                                             ↓ (Abstract base)
                                             ├─ MultipleChoice
                                             ├─ FillInBlank
                                             ├─ Listening
                                             └─ Translation
```

**Identity Tables:**
- `Users` — Extended from `IdentityUser` with RegistrationDate, LastLoginDate, TotalPointsEarned (materialized XP aggregate)
- `UserAvatars` — 1:1 with Users (shared PK: UserId). Stores avatar as `varbinary(max)` binary + ContentType. Separate table to avoid loading bytes on every request via UserContextMiddleware.
- `Roles` — Standard Identity roles (Admin, ContentCreator, User)
- `UserRoles`, `UserLogins`, `UserClaims`, `RoleClaims`, `UserTokens` — Identity infrastructure

**Key Patterns:**
- **UUID Primary Keys**: All entities use `Guid.NewGuid().ToString()` for IDs
- **OrderIndex**: All content entities have OrderIndex for custom sequencing
- **IsLocked flags**: Lesson and Exercise entities have IsLocked (default true) for progression control
- **Composite Keys**: UserLanguage uses (UserId, LanguageId); UserExerciseProgress uses (UserId, ExerciseId)
- **Table-Per-Hierarchy**: Exercise uses TPH with discriminator for subtypes
- **Timestamps**: CreatedAt/UpdatedAt for audit trails

## DTO Mapping Pattern

Extension methods for clean mapping between entities and DTOs:

```csharp
// In Mapping/ContentMapping.cs
public static CourseDto ToDto(this Course entity) => new(
    entity.Id,
    entity.Title,
    // ... map properties
);

// Usage in services/controllers
var courseDto = course.ToDto();
```

### DTO Conventions
- Use `record` types for all DTOs (immutable, positional parameters)
- No inline classes in controllers — always define DTOs in `Dtos/` folder
- No anonymous objects in `Ok()` responses — use typed DTOs
- Group related DTOs in one file (e.g., `AuthDtos.cs`, `UploadDtos.cs`)

## Polymorphic DTOs

Exercise types use .NET 8+ JSON polymorphism for type discrimination:

```csharp
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(MultipleChoiceExerciseDto), "MultipleChoice")]
[JsonDerivedType(typeof(FillInBlankExerciseDto), "FillInBlank")]
// ... other types
public abstract record ExerciseDto(...);
```

## Service Layer Guidelines

- **Nullable LessonId in CreateExerciseDto**: Optional when nested inside CreateLessonDto (parent assigns ID), required for standalone creation. Standalone path validates: `if (string.IsNullOrEmpty(dto.LessonId)) throw ArgumentException`
- **Async all the way**: All service methods must be async
- **Include chains**: Use `.Include()` and `.ThenInclude()` for eager loading related entities when full entity data is needed
- **Prefer LINQ projections over Include for navigation queries**: When a method only needs a few scalar FK values from related entities, project with `.Select()` into a `private record` instead of loading full entity graphs. Example: `LessonCourseContext` in `LessonService.GetNextLessonAsync` projects `CourseId`, `OrderIndex`, `LanguageId`, `CourseOrderIndex` — avoids materialising `Course` and `Language` entirely.
- **Eager load child collections for polymorphic types**: Use `.ThenInclude(e => (e as ChildType)!.ChildCollection)`
  - Example: `.Include(l => l.Exercises).ThenInclude(e => (e as MultipleChoiceExercise)!.Options)`
  - EF Core handles cast gracefully for non-matching types (TPH pattern)
- **OrderBy**: Always order collections by `OrderIndex` for consistent sequencing
- **Null handling**: Use null-coalescing operators for optional relationships
- **No repository pattern**: Services directly access DbContext
- **Upsert pattern**: `FirstOrDefaultAsync` → create if null, update if exists (see `ExerciseProgressService.SubmitAnswerAsync`)
- **User from JWT**: All controllers use `HttpContext.GetCurrentUser()` exclusively (returns full User entity, not just ID)
  - Do NOT use `User.FindFirstValue()` — UserContextMiddleware pre-loads the user entity before controllers execute
- **Auto-increment OrderIndex**: When `OrderIndex` is null in DTOs, calculate as `MaxAsync(e => (int?)e.OrderIndex) ?? -1 + 1` in parent entity
- **Idempotent unlocks**: All unlock methods check `IsLocked` before modifying (safe to call multiple times)
- **Operation result enums over bool**: Service methods that can fail for distinct reasons return an enum rather than `bool` — e.g. `UnlockStatus` (`Unlocked` / `AlreadyUnlocked` / `NoNextLesson`) instead of a bare `bool`. This preserves the failure reason at the call site without relying on comments.
- **Cascade unlocking**: `LessonService.UnlockNextLessonAsync()` calls `ExerciseService.UnlockFirstExerciseInLessonAsync()`
- **Service dependency chain**: ExerciseService → LessonService → ExerciseProgressService (avoid circular dependencies)
- **Admin bypass pattern**: Use `UserExtensions.CanBypassLocksAsync(user, userManager)` to check if user can bypass locks (Admin or ContentCreator role)
- **Lock validation layers**: Check locks in both service layer (ExerciseProgressService.SubmitAnswerAsync) AND controller layer (ExerciseController.GetExercise)
- **Unified progress fetching**: `GetFullLessonProgressAsync` returns both `LessonProgressSummary` and per-exercise progress dict in one query via `LessonProgressResult`. Avoid calling separate methods for summary vs. exercise-level progress.
- **Batch progress for lists**: Use `GetProgressForLessonsAsync(userId, lessonIds)` when loading progress for multiple lessons — avoids N+1 queries in controller loops
- **GroupJoin for left-join queries**: Use EF Core `GroupJoin` to left-join exercises with user progress in a single database round-trip (see `ExerciseProgressService`)
- **`GetFullLessonProgressAsync` is expensive — avoid in hot paths**: Issues a GroupJoin across all exercises in the lesson (~6 DB operations). Call it at lesson load, lesson-complete, and dedicated progress endpoints only — **never inside `SubmitAnswerAsync`**
- **DTO-per-context pattern**: Create separate DTOs when endpoints return different data shapes — e.g. `ExerciseSubmitResult` (submit endpoint — no lesson-wide aggregates) vs. `SubmitAnswerResponse` (submissions-restore endpoint — includes `LessonProgressSummary`). Keeps payloads minimal and types honest.
- **Submission endpoints return all items**: `GetLessonSubmissionsAsync` returns `SubmitAnswerResponse` for EVERY exercise in lesson, not just attempted ones. Frontend must filter appropriately.
- **Avatar binary storage**: Avatars stored as `varbinary(max)` in a separate `UserAvatars` table (not on `User` entity) to avoid loading bytes via `UserContextMiddleware` on every request. `AvatarService` handles download (Google), upsert, and retrieval. Leaderboard queries batch-check `UserAvatars` for existence and construct serving URLs (`/api/user/{id}/avatar`) — never load binary data in list queries.

Include chain example (`LessonService.GetLessonWithDetailsAsync` — full entity needed):
```csharp
return await _context.Lessons
    .Include(l => l.Course)
        .ThenInclude(c => c.Language)
    .Include(l => l.Exercises)
        .ThenInclude(e => (e as MultipleChoiceExercise)!.Options)
    .FirstOrDefaultAsync(l => l.Id == lessonId);
```

Projection record example (`LessonService.GetNextLessonAsync` — only scalar FKs needed):
```csharp
private record LessonCourseContext(string CourseId, int LessonOrderIndex, string LanguageId, int CourseOrderIndex);

var ctx = await _context.Lessons
    .Where(l => l.Id == currentLessonId)
    .Select(l => new LessonCourseContext(l.CourseId, l.OrderIndex, l.Course.LanguageId, l.Course.OrderIndex))
    .FirstOrDefaultAsync();
```

## File Upload Handling

- **Static files** served at `/static/uploads`
- **Max file size**: 100MB (configured in ServiceCollectionExtensions)
- **CORS headers**: Enabled for cross-origin resource access
- **Cache-Control**: 1-year max-age for uploaded files
- Upload endpoints: `POST /api/uploads/{fileType}`, `POST /api/uploads/any`, `GET /api/uploads/{fileType}/{filename}`, `GET /api/uploads/list/{fileType}`

## Authorization Roles

Three roles configured in the system:
- **Admin**: Full system access
- **ContentCreator**: Can create/edit courses, lessons, exercises
- **User**: Basic authenticated user

Apply roles via attributes:
```csharp
[Authorize(Roles = "Admin,ContentCreator")]
public async Task<IActionResult> CreateCourse(CreateCourseDto dto) { }
```

## Port Configuration

- Backend listens on **port 8080** (non-privileged, .NET 8+ default via `ASPNETCORE_HTTP_PORTS`)
- The `dotnet/aspnet` base image sets `ASPNETCORE_HTTP_PORTS=8080` — no code or env var needed
- **Do NOT set `ASPNETCORE_URLS`** in docker-compose or `.env` — it overrides `ASPNETCORE_HTTP_PORTS` and logs a warning: `Overriding HTTP_PORTS '8080'`
- `ConfigureHttpPort()` was removed as redundant — ASP.NET Core reads port config natively

## Data Protection Keys

- Keys persisted to `/app/dataprotection-keys` via `AddDataProtectionKeys()` extension
- Docker volume `backend-dataprotection` mounted at that path (survives container recreation)
- Keys are **not encrypted at rest** — acceptable because JWT auth is independent of Data Protection
- Do NOT use LE cert for key encryption: certs rotate every 90 days, making old keys unreadable
- Warning suppressed in `appsettings.json`: `XmlKeyManager` log level set to `Error`

## EF Core Shadow FK Gotcha

- `.WithMany()` without a navigation property reference creates a shadow FK (e.g. `ExerciseId1`)
- Always use `.WithMany(e => e.NavigationProperty)` in fluent configuration
- Example fix: `.WithMany()` → `.WithMany(e => e.ExerciseProgress)` in `BackendDbContext`

## EF Core 10 PendingModelChangesWarning

- Thrown as an **error** by default in EF Core 10 during `MigrateAsync()` if model hash doesn't match snapshot
- Suppressed via `ConfigureWarnings(w => w.Log(RelationalEventId.PendingModelChangesWarning))` in `AddDatabaseContext()`
- Migration class names MUST match file names and `[Migration]` attribute — mismatches break migration ID resolution

## Database Migration Retry

- `DatabaseExtensions.MigrateDbAsync()` retries with exponential backoff (3s, 6s, 12s...)
- Creates fresh DbContext scope per retry to avoid dirty state
- Fails fast on non-transient errors (`InvalidOperationException`) — no point retrying config issues

## Environment Variables

Backend env vars are in `backend/.env` (mapped as Docker secret `backend_env`):

```
DB_SERVER=db
DB_NAME=LexiqDb
DB_USER_ID=sa
DB_PASSWORD=<your-password>
GOOGLE_CLIENT_ID=<google-oauth-client-id>
GOOGLE_CLIENT_SECRET=<google-oauth-client-secret>

# JWT (JWT_SECRET is required — startup throws if missing)
JWT_SECRET=<hs256-signing-key>
JWT_ISSUER=lexiq-api            # default if unset
JWT_AUDIENCE=lexiq-frontend     # default if unset
JWT_EXPIRATION_HOURS=24         # default if unset

# Production only
ASPNETCORE_ENVIRONMENT=production
```

Backend loads secrets from `/run/secrets/backend_env` in production (Docker secrets).

## Common Workflows

### Adding a New Database Entity

1. Create entity class in `Database/Entities/`
2. Add DbSet to `BackendDbContext.cs`
3. Configure relationships in `OnModelCreating()` if needed
4. Create migration: `dotnet ef migrations add AddEntityName --project Database/Backend.Database.csproj`
5. Apply migration: `dotnet ef database update --project Database/Backend.Database.csproj`

### Adding a New API Endpoint

1. Create DTOs in `Dtos/`
   - Read DTOs (e.g., `CourseDto`) for output
   - Create DTOs (e.g., `CreateCourseDto`) for input
   - Update DTOs (e.g., `UpdateCourseDto`) for partial updates
   - Use `record` types for DTOs when possible
2. Create mappings in `Mapping/`
   - Extension methods: `ToDto()` for entity → DTO
   - Map methods: `MapToEntity()` for DTO → entity
3. Create service in `Services/`
   - Constructor inject `BackendDbContext`
   - All methods should be async (return `Task` or `Task<T>`)
   - Register in `Extensions/ServiceCollectionExtensions.cs` as Scoped
4. Create or update controller in `Controllers/`
   - Use `[ApiController]` and `[Route("api/[controller]")]`
   - Constructor inject required services
   - Apply `[Authorize(Roles = "...")]` for protected endpoints
   - Use `[AllowAnonymous]` for public endpoints

### Testing Exercise Unlocking System

1. Login via Google OAuth to create user account
2. First lesson's first exercise should be unlocked (seed data)
3. Submit correct answer: `POST /api/exercises/{id}/submit` → next exercise unlocks
4. Submit wrong answer: can retry infinitely, no unlock
5. Complete 70%+ of lesson exercises → lesson completion triggers next lesson unlock
6. Admin manual unlock: `POST /api/lessons/{id}/unlock` → unlocks lesson + first exercise

## API Endpoints

| Controller | Base Route | Key Endpoints | Auth Required |
|-----------|-----------|---------------|---------------|
| AuthController | `/api/auth` | `POST /google-login`, `POST /logout`, `GET /auth-status`, `GET /is-admin` | Mixed |
| CourseController | `/api/courses` | `GET /`, `GET /{id}`, `POST /`, `PUT /{id}`, `DELETE /{id}` | Admin/Creator for mutations |
| LessonController | `/api/lessons` | `GET /`, `GET /{id}`, `POST /`, `PUT /{id}`, `DELETE /{id}` | Admin/Creator for mutations |
| ExerciseController | `/api/exercises` | `GET /lesson/{lessonId}`, `POST /`, `PUT /{id}`, `DELETE /{id}`, `POST /{id}/submit` | Admin/Creator for mutations; submit for any user |
| LanguageController | `/api/languages` | `GET /`, `POST /`, `PUT /{id}`, `DELETE /{id}` | Public read, Admin write |
| UserLanguageController | `/api/userLanguages` | `GET /user/{userId}`, `POST /enroll`, `DELETE /unenroll` | Yes |
| UserManagementController | `/api/userManagement` | `GET /users`, `GET /users/{id}`, `POST /roles`, etc. | Admin only |
| UploadsController | `/api/uploads` | `POST /{fileType}`, `GET /{fileType}/{filename}`, `GET /list/{fileType}` | Yes |
| LeaderboardController | `/api/leaderboard` | `GET /?timeFrame=Weekly\|Monthly\|AllTime` | No (includes current user if authenticated) |
| UserController | `/api/user` | `GET /{userId}/avatar`, `PUT /avatar` | GET public, PUT auth |

## Known Limitations

- No validation layer on backend DTOs (validation done in entity layer)
- No error handling middleware (returns raw exceptions)
- `ExerciseProgressService` validates answers server-side — frontend sends answer strings (option IDs for MC, text for others)
- Lesson completion requires 70% XP threshold (`ExerciseProgressService.DefaultCompletionThreshold`)
- `UserExerciseProgress.ExerciseId` FK uses `DeleteBehavior.NoAction` (SQL Server multiple cascade path constraint)
- `Lesson.status` is NOT returned by the API — frontend derives it from `isLocked`, `isCompleted`, `completedExercises` fields
- **Exercise unlocking**: Hybrid strategy — first exercise unlocks with lesson, rest unlock sequentially on completion (infinite retries allowed)
- **EF Core shadow FK gotcha**: `.WithMany()` without passing the navigation property creates a duplicate shadow FK (e.g. `ExerciseId1`). Always pass the inverse nav explicitly: `.WithMany(e => e.ExerciseProgress)`. See `BackendDbContext.cs` UserExerciseProgress configuration.

## Common Debugging Scenarios

### 401 Unauthorized Errors

1. **Common causes**:
   - **Stale JWT after DB reset**: Clear browser cookies and re-login
   - **Missing AuthToken cookie**: Check browser DevTools → Application → Cookies
   - **User not found in DB**: JWT has old user ID from before database reset
   - **CORS misconfiguration**: Cookie not sent with cross-origin requests

4. **JWT Claim Mapping Gotcha**:
   - ASP.NET Core JWT middleware maps `sub` → `ClaimTypes.NameIdentifier` by default
   - Always use `ClaimTypes.NameIdentifier` to extract user ID, NOT `JwtRegisteredClaimNames.Sub`

5. **HttpContext.GetCurrentUser() Not Found**:
   - The extension method is in `Backend.Api.Middleware` namespace
   - Add `using Backend.Api.Middleware;` to controllers

### Exercise Progress Not Restoring

1. Check `restorePreviousProgress()` logic in frontend exercise-viewer component
2. Backend returns `SubmitAnswerResponse` for ALL exercises (attempted or not)
3. "Never attempted" vs "attempted incorrectly" have same values: `pointsEarned: 0, isCorrect: false`
4. **Fix**: Check discriminator field — `correctAnswer !== null` indicates actual attempt
5. Pattern: `const wasAttempted = response.isCorrect || response.correctAnswer !== null`
