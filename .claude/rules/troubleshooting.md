# Troubleshooting Playbooks

Quick reference for diagnosing common Lexiq failures. Pulled out of CLAUDE.md so per-session token cost is minimal — open this file only when actively debugging.

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
