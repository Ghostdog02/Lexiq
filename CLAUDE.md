# CLAUDE.md

Top-level guidance. **Read at the start of every session.** Per-area detail lives in the linked CLAUDE.md / docs files — load them only when working in that area.

## Project

Lexiq — language learning app for Bulgarian speakers learning Italian. Also a recruitment showcase. Two-person team (Backend/DevOps + Frontend).

| Area | Stack | Detail |
|------|-------|--------|
| Backend | ASP.NET Core 10.0 + EF Core + SQL Server 2022 | [`backend/CLAUDE.md`](backend/CLAUDE.md) |
| Frontend | Angular 21 SPA | [`frontend/CLAUDE.md`](frontend/CLAUDE.md) |
| Tests | xUnit v3 + Testcontainers + Jest + Playwright | [`backend/Tests/CLAUDE.md`](backend/Tests/CLAUDE.md) |
| CI/CD | Docker Compose + GitHub Actions + Hetzner | [`.github/workflows/CLAUDE.md`](.github/workflows/CLAUDE.md) |

## Quick commands

```bash
# Backend (from backend/)
dotnet run                                              # :8080
dotnet ef migrations add <Name> --project Database/Backend.Database.csproj
dotnet ef database update     --project Database/Backend.Database.csproj

# Frontend (from frontend/)
npm start                                               # :4200

# Full stack (from root)
docker compose up --build
```

## Architecture invariants

- **Content hierarchy**: `Language → Course → Lesson → Exercise` (TPH: MultipleChoice / FillInBlank / Listening / Translation).
- **XP**: `SUM(UserExerciseProgress.PointsEarned)`. `User.TotalPointsEarned` is a materialized cache, incremented on first correct submission.
- **Levels**: `floor((1 + sqrt(1 + totalXp/25)) / 2)` — backend-computed.
- **Streaks**: distinct dates from `UserExerciseProgress.CompletedAt`, consecutive days backward from today.
- **Auth**: JWT in HttpOnly `AuthToken` cookie. HS256, 24h default. ASP.NET Core maps JWT `sub` → `ClaimTypes.NameIdentifier` — always use `NameIdentifier`. Access user via `HttpContext.GetCurrentUser()`.
- **TLS**: nginx in the frontend container is the **sole** TLS terminator for both `lexiqlanguage.eu` and `api.lexiqlanguage.eu`. Backend speaks plain HTTP on `:8080` inside the Docker network.
- **Dev cookies**: nginx proxies `/api` → `backend:8080` so requests are same-origin. CORS uses `AllowCredentials()` + specific origin. Frontend sends `withCredentials: true`. `SameSite=Lax` works through the proxy.
- **Sass**: `@use` only, never `@import`. Namespace: `@use 'path/styles.scss' as styles;`.

> **Entity reference**: do not grep entity files — read [`backend/Database/ENTITIES_DOCUMENTATION.md`](backend/Database/ENTITIES_DOCUMENTATION.md) for every property, relationship, and constraint.

## Project rules & skills

| File | Covers |
|------|--------|
| [`.claude/rules/agent-principles.md`](.claude/rules/agent-principles.md) | Plan mode, subagents, verify-before-done, autonomous bug fixing |
| [`.claude/rules/rules.md`](.claude/rules/rules.md) | Git conventions, commit format, branching, PR rules, commit grouping |
| [`.claude/rules/skills.md`](.claude/rules/skills.md) | Project & plugin skills, MCP tools, agent strategies, workflows |
| [`.claude/rules/troubleshooting.md`](.claude/rules/troubleshooting.md) | 401s, cookies, 400s, Docker, CI/CD, migrations, OpenAPI |
| [`.claude/rules/common-gotchas.md`](.claude/rules/common-gotchas.md) | EF Core, JWT, polymorphic DTOs, nginx/IPv6, Sass, Angular |
| [`.claude/skills/commit-changes/SKILL.md`](.claude/skills/commit-changes/SKILL.md) | Group changes, commit, push (with confirmation) |
| [`.claude/skills/create-pr/SKILL.md`](.claude/skills/create-pr/SKILL.md) | Open PR with strict template, no Claude attribution |
| [`.claude/agents/test-generator.md`](.claude/agents/test-generator.md) | xUnit + Testcontainers / Jest / Playwright standards |

## Commit & docs cadence

After each logical group of changes:

1. Commit code per [`.claude/rules/rules.md`](.claude/rules/rules.md). Group by concern. Imperative subject ≤72 chars + up to 4 body bullets. **No** `Co-Authored-By`, **no** Claude footer.
2. Update the relevant CLAUDE.md (whichever area changed) — only if a new pattern or rule emerged.
3. Commit docs **separately**.
4. Schema change → update [`backend/Database/ENTITIES_DOCUMENTATION.md`](backend/Database/ENTITIES_DOCUMENTATION.md) alongside the migration commit.
5. After a chain of commits, run `/claude-md-improver`.

## RTK

RTK is configured in your global `~/.claude/CLAUDE.md`. Use it for build/test/git/gh commands as documented there. **Do not duplicate the RTK reference in this repo's CLAUDE.md** — every line here is read each session.
