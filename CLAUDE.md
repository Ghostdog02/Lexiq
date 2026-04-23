# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Lexiq is a language learning application for Bulgarian speakers learning Italian. It also serves as a technical showcase for professional recruitment. Collaborative effort between a Backend/DevOps specialist and a Frontend developer.

**Stack:**
- **Backend**: ASP.NET Core 10.0 Web API with Entity Framework Core → see [`backend/CLAUDE.md`](backend/CLAUDE.md)
- **Frontend**: Angular 21 single-page application → see [`frontend/CLAUDE.md`](frontend/CLAUDE.md)
- **Database**: Microsoft SQL Server 2022
- **Infrastructure**: Docker Compose with CI/CD via GitHub Actions → see [`.github/workflows/CLAUDE.md`](.github/workflows/CLAUDE.md)

## Quick Reference

### Backend Commands (run from `backend/`)

```bash
dotnet restore
dotnet build Backend.sln
dotnet run                    # port 8080
dotnet watch run              # auto-reload
dotnet ef migrations add <Name> --project Database/Backend.Database.csproj
dotnet ef database update --project Database/Backend.Database.csproj
```

### Frontend Commands (run from `frontend/`)

```bash
npm install
npm start                     # port 4200
npm run build
npm test
ng generate component <name>
```

### Docker (run from root)

```bash
docker compose up --build     # start all services
docker compose down           # stop all services
docker compose logs           # view logs
```

## Architecture Overview

### User Progress & XP

- **XP Calculation**: `SELECT SUM(PointsEarned) FROM UserExerciseProgress WHERE UserId = @id`
- **XP Caching**: `User.TotalPointsEarned` is incremented on first correct exercise submission (avoids re-aggregation for leaderboard)
- **Endpoints**: `GET /api/user/xp` (authenticated), `GET /api/user/{id}/xp` (public for leaderboard)
- **Progress Tracking**: UserExerciseProgress table (composite key: UserId + ExerciseId)

### Leaderboard & Gamification

- **Leaderboard**: `GET /api/leaderboard?timeFrame=Weekly|Monthly|AllTime` — ranks users by XP
- **Streaks**: Derived from `UserExerciseProgress.CompletedAt` (distinct dates, consecutive days backward from today)
- **Levels**: Computed on backend via formula: `level = floor((1 + sqrt(1 + totalXp/25)) / 2)`
- **Rank change**: Stateless comparison — current period vs previous equivalent period (no snapshot tables)
- **Avatars**: Stored as binary (`varbinary(max)`) in separate `UserAvatars` table (1:1 with Users). Google profile picture downloaded and stored on every login via `AvatarService`. Manual override via `PUT /api/user/avatar` (IFormFile). Served via `GET /api/user/{id}/avatar` (AllowAnonymous, 24h cache). Leaderboard queries batch-check existence without loading bytes.

### Content Hierarchy

> **⚠️ Do not search entity source files manually.**
> [`backend/Database/ENTITIES_DOCUMENTATION.md`](backend/Database/ENTITIES_DOCUMENTATION.md)
> documents every entity, property, relationship, constraint, and business rule.
> Read it first whenever you need to understand or modify the data model.

```
Language (1) → Course (M) → Lesson (M) → Exercise (M)
                                             ↓ (Abstract base, TPH)
                                             ├─ MultipleChoice
                                             ├─ FillInBlank
                                             ├─ Listening
                                             └─ Translation
```

### Authentication

- **JWT stored in an HttpOnly cookie** named `AuthToken` (not Identity cookie auth)
- JWT signed with HS256; expiry defaults to 24h (`JWT_EXPIRATION_HOURS`)
- Google OAuth via `GoogleJsonWebSignature.ValidateAsync()`
- **Critical**: ASP.NET Core maps JWT `sub` → `ClaimTypes.NameIdentifier`. Always use `ClaimTypes.NameIdentifier`, NOT `JwtRegisteredClaimNames.Sub`.
- `UserContextMiddleware` loads full User entity from JWT; access via `HttpContext.GetCurrentUser()`

### TLS Architecture (Production)

- nginx (frontend container) is the **sole TLS terminator** for both `lexiqlanguage.eu` and `api.lexiqlanguage.eu`
- certbot sidecar fixes cert permissions on startup (one-shot, `restart: no`); renewal runs via weekly `infrastructure-update.yml` cron
- certs persist in `letsencrypt-certs` named volume — survives all container restarts and redeploys
- Backend speaks **plain HTTP on port 8080** inside the Docker network — no HTTPS, no LettuceEncrypt
- nginx → backend: `proxy_pass http://backend:8080`
- **`cap_add: NET_BIND_SERVICE`** required on the frontend service — `nginx-unprivileged` runs as a non-root user and cannot bind to ports < 1024 without this capability
- **IPv6 listen directives required**: all nginx server blocks must have both `listen <port>` and `listen [::]:<port>` — Alpine's BusyBox wget resolves `localhost` to `::1` (IPv6) first; without the IPv6 directive nginx is unreachable from the healthcheck despite running correctly on IPv4
- **`location ^~ /api` required**: plain `location /api` loses to regex locations (`~*`) for URLs like `/api/uploads/image/*.png` — nginx regex beats prefix in priority order; the `^~` modifier prevents regex matching for all `/api` paths

### Cross-Origin Cookie Setup (Development)

- Frontend: `localhost:4200` (nginx) — Backend: `localhost:8080`
- nginx proxies `/api/*` → `backend:8080/api/*` (makes requests same-origin)
- CORS: `AllowCredentials()` + specific origin (not wildcard)
- Frontend: `withCredentials: true` in HTTP requests
- Cookie: `SameSite=Lax` works with proxy

### Sass Convention
- All component SCSS files use `@use` (not `@import`) to import `styles.scss`
- Namespace convention: `@use 'path/styles.scss' as styles;`
- Mixins accessed via namespace: `@include styles.animated-background`

## Project Guidelines

**For specific rules and workflows, always consult:**
- **[`.claude/AGENT_PRINCIPLES.md`](.claude/AGENT_PRINCIPLES.md)** → Core agent behaviour: planning, subagents, verification, elegance, autonomous bug fixing
- **[`.claude/RULES.md`](.claude/RULES.md)** → Git conventions, commit format, branching strategy, PR guidelines
- **[`.claude/SKILLS.md`](.claude/SKILLS.md)** → MCP tools usage, agent strategies, task workflows, debugging playbooks
- **[`.claude/agents/test-generator.md`](.claude/agents/test-generator.md)** → Test generation standards: xUnit + Testcontainers (.NET), Jest + TestBed (Angular), Playwright E2E, AAA pattern, mocking strategy

### Commit & Documentation Cadence

After every logical group of related changes, follow this sequence:

1. **Commit the code changes** following [`.claude/RULES.md`](.claude/RULES.md):
   - Group by concern — don't mix feature code with documentation in the same commit
   - Imperative subject line, max 72 chars, capitalized
   - Bullet-point body for non-trivial changes (what + why)
   - No `Co-Authored-By` line

2. **Update the relevant CLAUDE.md** (whichever area changed):
   - `backend/CLAUDE.md` — new patterns, service rules, API endpoints, debugging tips
   - `frontend/CLAUDE.md` — Angular patterns, component conventions, debugging
   - `.github/workflows/CLAUDE.md` — CI/CD changes, pipeline steps
   - Root `CLAUDE.md` — architecture-level changes only

3. **Commit the documentation update** as a separate commit:
   ```
   Document <what changed> in <which file>

   - <bullet explaining what was added/changed and why>
   ```

4. **For schema changes**, also update [`backend/Database/ENTITIES_DOCUMENTATION.md`](backend/Database/ENTITIES_DOCUMENTATION.md) and commit it alongside or just after the migration commit.

5. **After a chain of multiple commits**, run `/claude-md-improver` to audit all CLAUDE.md files for consistency and gaps (see [`.claude/SKILLS.md`](.claude/SKILLS.md) → Documentation Maintenance).

## Detailed Documentation

| Area | File | Covers |
|------|------|--------|
| Backend | [`backend/CLAUDE.md`](backend/CLAUDE.md) | Structure, patterns, service layer, DTOs, auth, database schema, API endpoints, debugging |
| Frontend | [`frontend/CLAUDE.md`](frontend/CLAUDE.md) | Structure, Angular patterns, forms, design system, routes, debugging |
| CI/CD | [`.github/workflows/CLAUDE.md`](.github/workflows/CLAUDE.md) | Docker, pipeline, deployment, health checks, debugging (includes `pr-validation.yml` for PR build checks) |
| Database Entities | [`backend/Database/ENTITIES_DOCUMENTATION.md`](backend/Database/ENTITIES_DOCUMENTATION.md) | Comprehensive entity documentation |
| Rules & Conventions | [`.claude/RULES.md`](.claude/RULES.md) | Git workflows, commit standards, branching |
| Skills & Workflows | [`.claude/SKILLS.md`](.claude/SKILLS.md) | Tool usage, agent strategies, debugging playbooks |
| Test Generation | [`.claude/agents/test-generator.md`](.claude/agents/test-generator.md) | xUnit, Testcontainers, Jest, Playwright E2E standards |

<!-- rtk-instructions v2 -->
# RTK (Rust Token Killer) - Token-Optimized Commands

## Golden Rule

**Always prefix commands with `rtk`**. If RTK has a dedicated filter, it uses it. If not, it passes through unchanged. This means RTK is always safe to use.

**Important**: Even in command chains with `&&`, use `rtk`:
```bash
# ❌ Wrong
git add . && git commit -m "msg" && git push

# ✅ Correct
rtk git add . && rtk git commit -m "msg" && rtk git push
```

## RTK Commands by Workflow

### Build & Compile (80-90% savings)
```bash
rtk cargo build         # Cargo build output
rtk cargo check         # Cargo check output
rtk cargo clippy        # Clippy warnings grouped by file (80%)
rtk tsc                 # TypeScript errors grouped by file/code (83%)
rtk lint                # ESLint/Biome violations grouped (84%)
rtk prettier --check    # Files needing format only (70%)
rtk next build          # Next.js build with route metrics (87%)
```

### Test (90-99% savings)
```bash
rtk cargo test          # Cargo test failures only (90%)
rtk vitest run          # Vitest failures only (99.5%)
rtk playwright test     # Playwright failures only (94%)
rtk test <cmd>          # Generic test wrapper - failures only
```

### Git (59-80% savings)
```bash
rtk git status          # Compact status
rtk git log             # Compact log (works with all git flags)
rtk git diff            # Compact diff (80%)
rtk git show            # Compact show (80%)
rtk git add             # Ultra-compact confirmations (59%)
rtk git commit          # Ultra-compact confirmations (59%)
rtk git push            # Ultra-compact confirmations
rtk git pull            # Ultra-compact confirmations
rtk git branch          # Compact branch list
rtk git fetch           # Compact fetch
rtk git stash           # Compact stash
rtk git worktree        # Compact worktree
```

Note: Git passthrough works for ALL subcommands, even those not explicitly listed.

### GitHub (26-87% savings)
```bash
rtk gh pr view <num>    # Compact PR view (87%)
rtk gh pr checks        # Compact PR checks (79%)
rtk gh run list         # Compact workflow runs (82%)
rtk gh issue list       # Compact issue list (80%)
rtk gh api              # Compact API responses (26%)
```

### JavaScript/TypeScript Tooling (70-90% savings)
```bash
rtk pnpm list           # Compact dependency tree (70%)
rtk pnpm outdated       # Compact outdated packages (80%)
rtk pnpm install        # Compact install output (90%)
rtk npm run <script>    # Compact npm script output
rtk npx <cmd>           # Compact npx command output
rtk prisma              # Prisma without ASCII art (88%)
```

### Files & Search (60-75% savings)
```bash
rtk ls <path>           # Tree format, compact (65%)
rtk read <file>         # Code reading with filtering (60%)
rtk grep <pattern>      # Search grouped by file (75%)
rtk find <pattern>      # Find grouped by directory (70%)
```

### Analysis & Debug (70-90% savings)
```bash
rtk err <cmd>           # Filter errors only from any command
rtk log <file>          # Deduplicated logs with counts
rtk json <file>         # JSON structure without values
rtk deps                # Dependency overview
rtk env                 # Environment variables compact
rtk summary <cmd>       # Smart summary of command output
rtk diff                # Ultra-compact diffs
```

### Infrastructure (85% savings)
```bash
rtk docker ps           # Compact container list
rtk docker images       # Compact image list
rtk docker logs <c>     # Deduplicated logs
rtk kubectl get         # Compact resource list
rtk kubectl logs        # Deduplicated pod logs
```

### Network (65-70% savings)
```bash
rtk curl <url>          # Compact HTTP responses (70%)
rtk wget <url>          # Compact download output (65%)
```

### Meta Commands
```bash
rtk gain                # View token savings statistics
rtk gain --history      # View command history with savings
rtk discover            # Analyze Claude Code sessions for missed RTK usage
rtk proxy <cmd>         # Run command without filtering (for debugging)
rtk init                # Add RTK instructions to CLAUDE.md
rtk init --global       # Add RTK to ~/.claude/CLAUDE.md
```

## Token Savings Overview

| Category | Commands | Typical Savings |
|----------|----------|-----------------|
| Tests | vitest, playwright, cargo test | 90-99% |
| Build | next, tsc, lint, prettier | 70-87% |
| Git | status, log, diff, add, commit | 59-80% |
| GitHub | gh pr, gh run, gh issue | 26-87% |
| Package Managers | pnpm, npm, npx | 70-90% |
| Files | ls, read, grep, find | 60-75% |
| Infrastructure | docker, kubectl | 85% |
| Network | curl, wget | 65-70% |

Overall average: **60-90% token reduction** on common development operations.
<!-- /rtk-instructions -->