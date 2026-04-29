# Common Gotchas

Bugs and surprises that have bitten this codebase. Read before touching the listed area.

---

## EF Core

### Shadow FKs from `.WithMany()`

`.WithMany()` without a navigation reference creates a duplicate shadow FK (e.g. `ExerciseId1`). Always pass the inverse nav: `.WithMany(e => e.ExerciseProgress)`. See `BackendDbContext.cs` — `UserExerciseProgress` configuration.

### `GroupBy` on navigation properties fails to translate

Never group on `p.User.UserName`. EF Core wraps rows in `TransparentIdentifier<TOuter, TInner>` and the subsequent `Sum` cannot translate to SQL.

**Fix**: explicit `.Join()` → flatten to anonymous type → `GroupBy` over scalar columns. See `LeaderboardService.GetTimeFilteredLeaderboardAsync`.

### `OrderBy` after `GroupBy.Select()` into a named record fails

EF Core treats named `record`/`class` projections as terminal — it loses the SQL mapping for properties like `TotalXp = SUM(...)`. Symptom: `InvalidOperationException: The LINQ expression could not be translated`.

**Fix**: project to anonymous type → `OrderBy` → `Take` → terminal `Select` into the named record. See `LeaderboardService`.

### Anonymous types vs named records — terminal projection rule

Use anonymous `new { }` for intermediate `Join`/`GroupBy`/`OrderBy` steps. Use named `record` only for the final `.Select()` before `.ToListAsync()`.

### Navigation properties don't auto-populate on `Add`

`_context.Exercises.Add(exercise)` tracks the entity but does **not** populate `lesson.Exercises`. If the controller then calls `.ToDto()`, the response ignores the new exercise.

**Fix**: add to the parent collection — `lesson.Exercises.Add(exercise)`.

### EF Core 10 `PendingModelChangesWarning`

Thrown as **error** by default. Suppressed via `ConfigureWarnings(w => w.Log(RelationalEventId.PendingModelChangesWarning))` in `AddDatabaseContext()`. Migration class names MUST match the file name AND the `[Migration]` attribute.

### Eager-loading polymorphic child collections

```csharp
.Include(l => l.Exercises)
.ThenInclude(e => (e as MultipleChoiceExercise)!.Options)
```

EF Core handles non-matching TPH subtypes gracefully.

### Prefer projections over `Include` for scalar-only lookups

If you only need a few FK columns from related entities, project into a `private record` rather than materialising the full entity graph. Example: `LessonCourseContext` in `LessonService.GetNextLessonAsync`.

---

## Authentication & JWT

- ASP.NET Core JWT middleware maps `sub` → `ClaimTypes.NameIdentifier`. Always use `ClaimTypes.NameIdentifier`, never `JwtRegisteredClaimNames.Sub`.
- All controllers use `HttpContext.GetCurrentUser()` — do **not** use `User.FindFirstValue()`. `UserContextMiddleware` pre-loads the User entity.
- `HttpContext.GetCurrentUser()` lives in namespace `Backend.Api.Middleware`. Import is mandatory.
- Data-protection keys are intentionally **not encrypted at rest**. Do NOT encrypt with the LE cert (90-day rotation invalidates all keys).

---

## Service & DTO conventions

- Services must be registered in `AddApplicationServices` — DI graph validation (`ValidateOnBuild`) crashes startup at runtime, not compile time.
- All services / controllers / middleware use **C# 12 primary constructors**. Records for DTOs.
- Never use the null-forgiving `!` operator. Throw the right exception (`ArgumentNullException.ThrowIfNull`, `InvalidOperationException`, `KeyNotFoundException`).
- Operation result enums over `bool` for service methods that can fail multiple ways (e.g. `UnlockStatus`).
- Never inline DTOs in controllers. Never `Ok(new { ... })` anonymous objects.

### Polymorphic DTO serialization

`Ok(dto)` passes `object` to System.Text.Json — runtime type, no discriminator. Use the `OkPolymorphic<ExerciseDto>(dto)` helper which sets `DeclaredType` on `OkObjectResult`. For `CreatedAtAction`, set `result.DeclaredType = typeof(ExerciseDto)` after creation. Discriminator MUST be the **first** JSON property.

### Enum serialization

Add `[JsonConverter(typeof(JsonStringEnumConverter))]` to: `DifficultyLevel`, `ExerciseType`, `LessonStatus`, `TimeFrame`. Frontend sends strings; the converter parses them.

### `IFormFile` model binding

With `<Nullable>enable</Nullable>` + `[ApiController]`, non-nullable `IFormFile` is implicitly `[Required]`. The FormData field name MUST match the parameter name. Mismatch returns 400 ProblemDetails before the action runs — no breakpoint hits. Wrap `SanitizeFilename` and similar throwers in try/catch to avoid raw 500s.

---

## Infrastructure / nginx / Docker

- nginx is the **sole TLS terminator**. Backend speaks plain HTTP on port 8080 inside the Docker network.
- All nginx server blocks require **both** `listen <port>` and `listen [::]:<port>`. Alpine BusyBox wget resolves `localhost` to `::1` first; missing IPv6 directive → healthcheck failures.
- `location ^~ /api` is mandatory. Plain `location /api` loses to regex blocks like `~* \.png` because nginx regex beats prefix priority.
- `cap_add: NET_BIND_SERVICE` is required on the frontend service — `nginx-unprivileged` cannot bind ports < 1024 otherwise.
- BusyBox `wget` accepts only `-q -O -T -c -S -P -U -Y`. GNU-only flags (`--spider`, `--no-verbose`, `--tries`) silently fail with exit 1.
- Healthcheck output is NOT in `docker compose logs`. Use `docker inspect --format='{{json .State.Health}}' <container>`.
- Backend uses `dotnet/aspnet:10.0-alpine`. Required: `apk add icu-libs` + `ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false`. Without ICU, sort/compare on Bulgarian/Italian text silently falls back to invariant culture.
- Do **NOT** set `ASPNETCORE_URLS`. The base image sets `ASPNETCORE_HTTP_PORTS=8080` natively. `ASPNETCORE_URLS` overrides it and logs a warning.
- Let's Encrypt: issue **two separate** certs — one for `lexiqlanguage.eu + www`, one for `api.lexiqlanguage.eu`. Combining them creates a single SAN cert under `live/lexiqlanguage.eu/` and the api cert dir is never created.
- `letsencrypt-certs` is a Docker named volume, not a host path. Always write via `docker compose run`.

---

## Frontend / Angular

- **Standalone components only** — no NgModule.
- Always use `inject()` — no constructor injection.
- Cleanup with `takeUntilDestroyed(this.destroyRef)`.
- Functional `CanActivateFn` only — class-based guards are deprecated.
- HTTP calls require `withCredentials: true` for cookie auth.
- Never call methods in template bindings — pre-compute in `ngOnInit`.
- `transition: all` is banned — enumerate the changing properties.
- All sizes in `rem`. All colors via CSS vars (`var(--accent)`, `rgba(var(--accent-rgb), 0.4)`). No hardcoded hex/rgba.
- SCSS imports use `@use 'path/styles.scss' as styles;` — never `@import`. `@use` must precede `:root`.
- Editor.js: `uploadByFile` returns `blob:` immediately; actual upload happens later in `uploadPendingFiles`. Custom uploader's FormData field must equal backend `IFormFile` parameter name (`"file"`).
- A linter auto-formats `.scss` files on write — re-Read before any follow-up Edit, or you'll get "file modified since read".

---

## Sass

- `@use` (not `@import`).
- Namespace: `@use 'shared/styles' as styles;` then `@include styles.animated-background`.
- Never put `transition` inside an appearance mixin. Mixin says how it looks; caller says how it animates.

---

## Tests

- Never `UseInMemoryDatabase`. Always Testcontainers.
- xUnit v3: `IAsyncLifetime` returns `ValueTask`, not `Task`.
- `IClassFixture<DatabaseFixture>` shares the container; `IAsyncLifetime` on the test class reseeds per test.
- `fixture.ExerciseIds` — always use these for `UserExerciseProgress` rows; FK is enforced on INSERT.
- `UserBuilder` — always use it for test users; sets Identity's required normalized fields.
