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
- **Endpoints**: `GET /api/user/xp` (authenticated), `GET /api/user/{id}/xp` (public for leaderboard)
- **Progress Tracking**: UserExerciseProgress table (composite key: UserId + ExerciseId)

### Content Hierarchy

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
- **[`.claude/RULES.md`](.claude/RULES.md)** → Git conventions, commit format, branching strategy, PR guidelines
- **[`.claude/SKILLS.md`](.claude/SKILLS.md)** → MCP tools usage, agent strategies, task workflows, debugging playbooks

## Detailed Documentation

| Area | File | Covers |
|------|------|--------|
| Backend | [`backend/CLAUDE.md`](backend/CLAUDE.md) | Structure, patterns, service layer, DTOs, auth, database schema, API endpoints, debugging |
| Frontend | [`frontend/CLAUDE.md`](frontend/CLAUDE.md) | Structure, Angular patterns, forms, design system, routes, debugging |
| CI/CD | [`.github/workflows/CLAUDE.md`](.github/workflows/CLAUDE.md) | Docker, pipeline, deployment, health checks, debugging |
| Database Entities | [`backend/Database/ENTITIES_DOCUMENTATION.md`](backend/Database/ENTITIES_DOCUMENTATION.md) | Comprehensive entity documentation |
| Rules & Conventions | [`.claude/RULES.md`](.claude/RULES.md) | Git workflows, commit standards, branching |
| Skills & Workflows | [`.claude/SKILLS.md`](.claude/SKILLS.md) | Tool usage, agent strategies, debugging playbooks |
