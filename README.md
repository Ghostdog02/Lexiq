# Lexiq

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![Angular](https://img.shields.io/badge/Angular-21-DD0031)](https://angular.io/)
[![Docker](https://img.shields.io/badge/Docker-Ready-2496ED)](https://www.docker.com/)
[![License](https://img.shields.io/badge/License-Proprietary-orange)]()

A full-stack language learning platform for Bulgarian speakers studying Italian. Built as both a functional application and a technical showcase by a two-person team — a backend/DevOps specialist and a frontend developer.

**Live deployment**: [lexiqlanguage.eu](https://www.lexiqlanguage.eu) — hosted on Hetzner Cloud with Let's Encrypt TLS.

## Contributors

| Contributor | Role | GitHub |
|-------------|------|--------|
| **Ghostdog02** | Backend, DevOps & Architecture | [@Ghostdog02](https://github.com/Ghostdog02) |
| **powercell12** | Frontend & UI Design | [@powercell12](https://github.com/powercell12) |

---

## Architecture

Lexiq is a three-tier application: an Angular 21 SPA served by nginx, an ASP.NET Core 10 Web API, and a SQL Server 2022 database — all containerised via Docker Compose. In production, nginx terminates TLS for both the frontend (`lexiqlanguage.eu`) and the API subdomain (`api.lexiqlanguage.eu`), forwarding plain HTTP to the backend container on port 8080. The backend has no awareness of TLS.

### Content Model

The curriculum follows a four-level hierarchy: **Language → Course → Lesson → Exercise**.

Exercises are stored in a single table using Table-Per-Hierarchy (TPH) with a discriminator column. Four concrete types are supported: **MultipleChoice**, **FillInBlank**, **Listening**, and **Translation**. Each uses `[JsonPolymorphic]` with a `type` discriminator as the first JSON property, enabling clean round-trip serialisation without custom converters.

Lesson and exercise access is gated: exercises unlock sequentially as correct answers are submitted, and a lesson unlocks the next when 70% of its available XP has been earned.

### Lesson Editor

Lessons are authored through a dynamic, block-based content editor built on **Editor.js**, wrapped as an Angular `ControlValueAccessor` so it integrates seamlessly with Reactive Forms. Content creators can compose rich lesson material by inserting and reordering blocks of different types: text paragraphs, embedded images, uploaded documents, PDFs, and audio files. File uploads are handled by a dedicated upload service — files are stored on the server with a 1-year `max-age` cache header, and GUID-based filenames guarantee uniqueness. The editor serialises its state to structured JSON, which is persisted on the backend and re-hydrated when the lesson is opened again. A 300ms debounce on the `onChange` handler prevents redundant API calls during active editing.

### Authentication

The frontend initiates the Google OAuth flow, receives an ID token, and POSTs it to `/api/auth/google-login`. The backend validates the token server-side via `GoogleJsonWebSignature.ValidateAsync()`, creates the user record if needed, and issues a signed JWT (HS256) stored in an **HttpOnly, SameSite=Lax cookie** named `AuthToken`. The token is never exposed to JavaScript.

A custom `UserContextMiddleware` sits between authentication and authorisation in the ASP.NET Core pipeline. It extracts `ClaimTypes.NameIdentifier` from the validated JWT and pre-loads the full `User` entity into `HttpContext.Items` — so every controller has immediate access to the authenticated user without redundant DB lookups or claim parsing.

### Role-Based Access Control

The system enforces a three-tier role hierarchy — **Admin**, **ContentCreator**, and **User** — at both layers of the stack.

On the backend, roles are managed via **ASP.NET Core Identity**. Controllers declare their access requirements with `[Authorize(Roles = "Admin,ContentCreator")]` or `[AllowAnonymous]` attributes. Mutation endpoints (create, update, delete for courses, lessons, and exercises) require the Admin or ContentCreator role; read endpoints require authentication; leaderboard and avatar endpoints are public.

On the frontend, Angular route guards enforce the same boundaries without requiring a round-trip:
- `AuthGuard` — redirects unauthenticated users to the login page
- `NoAuthGuard` — redirects already-authenticated users away from the login page
- `ContentGuard` — restricts access to content management routes to users with the Admin or ContentCreator role

This dual enforcement means unauthorised users cannot reach protected UI routes, and even if they construct API requests manually, the backend will reject them independently.

### Gamification

Users earn XP for correct exercise submissions. XP is queryable both from raw progress records (used in time-windowed leaderboard queries with explicit SQL `JOIN` + `GROUP BY`) and from a materialised `TotalPointsEarned` column on the `User` entity (used for all-time ranking, avoiding per-request aggregation). Rank change is computed stateless by comparing current-period XP against the equivalent prior period — no snapshot tables are required. Levels follow the formula `floor((1 + sqrt(1 + xp/25)) / 2)`. Streaks are derived from consecutive UTC calendar days with at least one completed exercise.

Avatars are downloaded from Google on first login and stored as `varbinary(max)` in a dedicated `UserAvatars` table (1:1 with `User`). Keeping avatar bytes out of the main `User` entity ensures the context middleware does not load binary data on every request.

### Key Design Decisions

- **JWT in HttpOnly cookie** — prevents XSS access to tokens. SameSite=Lax works because nginx proxies `/api/*` same-origin from the browser's perspective, so no `SameSite=None` + HTTPS requirement in development.
- **UserContextMiddleware** — pre-loads the authenticated user once per request; controllers call `HttpContext.GetCurrentUser()` instead of re-querying.
- **TPH for exercises** — single table, EF Core handles polymorphic eager loading via cast-based `ThenInclude((e as MultipleChoiceExercise)!.Options)`.
- **Explicit JOIN before GroupBy in EF Core** — navigation property access inside a `GroupBy` key wraps rows in `TransparentIdentifier<>`, causing SQL translation failure. Leaderboard queries use explicit `.Join()` to flatten to scalar columns first.
- **nginx-only TLS with Certbot sidecar** — the backend container speaks plain HTTP on port 8080; TLS is terminated exclusively at nginx. A dedicated Certbot container runs once at stack startup to fix certificate file permissions, then exits (`restart: no`). Certificates are stored in a named Docker volume (`letsencrypt-certs`) that survives all container restarts and redeploys. Renewal runs automatically via a weekly GitHub Actions cron job that SSHs into the Hetzner server and triggers `certbot renew` inside the container. `cap_add: NET_BIND_SERVICE` is required because nginx runs as an unprivileged user and cannot otherwise bind to ports below 1024.
- **Operation result enums** — service methods that can fail for distinct reasons return a typed enum (e.g. `UnlockStatus`) rather than a bare `bool`, preserving failure context at the call site.

---

## Tech Stack

### Backend

| Component | Technology | Version |
|-----------|------------|---------|
| Framework | ASP.NET Core Web API | 10.0 |
| ORM | Entity Framework Core | 10.0 |
| Database | Microsoft SQL Server | 2022 |
| Authentication | Google OAuth 2.0 + JWT HS256 | — |
| Language | C# | 13.0 |

### Frontend

| Component | Technology | Version |
|-----------|------------|---------|
| Framework | Angular (standalone components) | 21 |
| Language | TypeScript | 5.7 |
| Reactive primitives | RxJS | 7.8 |
| Forms | Angular Reactive Forms | — |
| Rich text | Editor.js | 2.x |

### Infrastructure

| Component | Technology |
|-----------|------------|
| Containerisation | Docker Compose |
| CI/CD | GitHub Actions |
| Hosting | Hetzner Cloud |
| TLS | Let's Encrypt via Certbot |
| Reverse proxy | nginx (unprivileged) |

---

## Getting Started

### Prerequisites

- [Docker](https://docs.docker.com/get-docker/) and Docker Compose — required for all setups
- [.NET 10 SDK](https://dotnet.microsoft.com/download) — local backend development only
- [Node.js 20+](https://nodejs.org/) — local frontend development only
- A Google Cloud project with OAuth 2.0 credentials (see below)

---

### Google OAuth Setup

**1. Create a project and configure the consent screen**

Open [Google Cloud Console](https://console.cloud.google.com/) and create or select a project. Navigate to **APIs & Services → OAuth consent screen**:
- User type: **External**
- Fill in app name, support email, and developer contact email
- Add scopes: `openid`, `email`, `profile`
- Add your own Google account as a **test user** — required while the app is in testing mode

**2. Create OAuth 2.0 credentials**

Navigate to **APIs & Services → Credentials → Create Credentials → OAuth 2.0 Client ID**:
- Application type: **Web application**
- **Authorised JavaScript origins**: `http://localhost:4200` (Angular dev server). Add your production frontend domain for deployment.
- **Authorised redirect URIs**: `http://localhost:4200`. The Angular app handles the OAuth redirect; the backend only ever receives the resulting ID token via a POST request — it is never called by Google directly.

Copy the **Client ID** and **Client Secret**.

> **Note**: origin and redirect URI changes can take a few minutes to propagate before Google accepts login attempts.

---

### Environment Variables

Create `backend/.env`:

```env
DB_SERVER=db
DB_NAME=LexiqDb
DB_USER_ID=sa
DB_PASSWORD=YourStrongPassword123!

GOOGLE_CLIENT_ID=your-client-id.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=your-client-secret

JWT_SECRET=a-random-string-of-at-least-32-characters
JWT_EXPIRATION_HOURS=24
```

Create `backend/Database/password.txt` containing only the SA password (must match `DB_PASSWORD`):

```
YourStrongPassword123!
```

> Generate a secure `JWT_SECRET` with: `openssl rand -base64 32`

---

### Run with Docker

```bash
git clone https://github.com/Ghostdog02/Lexiq.git
cd Lexiq
docker compose up --build
```

| Service | URL |
|---------|-----|
| Frontend | http://localhost:4200 |
| Backend API | http://localhost:8080 |
| Swagger UI | http://localhost:8080/swagger |

The backend runs EF Core migrations automatically on startup, retrying with exponential backoff until SQL Server is ready.

---

### Local Development

**Backend** — run from `backend/`:

```bash
dotnet restore
dotnet watch run        # starts on port 8080, reloads on save
```

**Database migrations:**

```bash
dotnet ef migrations add <Name> --project Database/Backend.Database.csproj
dotnet ef database update --project Database/Backend.Database.csproj
```

**Frontend** — run from `frontend/`:

```bash
npm install
npm start              # starts on port 4200, proxies /api/* → localhost:8080
```

---

## Running Tests

Backend tests use **xUnit v3** with **Testcontainers**, spinning up a live SQL Server instance in Docker. Docker must be running before executing tests.

```bash
cd backend
dotnet test Tests/Backend.Tests.csproj
```

The test suite is organised under `backend/Tests/`:

| Directory | Contents |
|-----------|----------|
| `Services/` | Unit tests (`CalculateLevel`) and integration tests (`GetStreak`, `GetLeaderboard`) |
| `Builders/` | Fluent `UserBuilder` — constructs test users directly via `DbContext`, bypassing `UserManager` |
| `Infrastructure/` | `DatabaseFixture` — manages the Testcontainers lifecycle and per-test data seeding |
| `Helpers/` | `DbSeeder` — seeds the fixture database with the minimum schema required by each test |

---

## API Reference

### Authentication

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/api/auth/google-login` | Validate Google ID token, issue JWT cookie | No |
| POST | `/api/auth/logout` | Clear the AuthToken cookie | Yes |
| GET | `/api/auth/auth-status` | Returns whether the request is authenticated | No |
| GET | `/api/auth/is-admin` | Returns whether the user has the Admin role | Yes |

### Content

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/courses` | List all courses | Yes |
| GET | `/api/courses/{id}` | Course with lessons | Yes |
| POST | `/api/courses` | Create course | Admin / ContentCreator |
| PUT | `/api/courses/{id}` | Update course | Admin / ContentCreator |
| DELETE | `/api/courses/{id}` | Delete course | Admin / ContentCreator |
| GET | `/api/lessons/{id}` | Lesson with exercises | Yes |
| POST | `/api/lessons` | Create lesson | Admin / ContentCreator |
| PUT | `/api/lessons/{id}` | Update lesson | Admin / ContentCreator |
| DELETE | `/api/lessons/{id}` | Delete lesson | Admin / ContentCreator |
| GET | `/api/exercises/lesson/{lessonId}` | Exercises for a lesson | Yes |
| POST | `/api/exercises/{id}/submit` | Submit an answer | Yes |
| GET | `/api/exercises/lesson/{lessonId}/progress` | Progress for all exercises in lesson | Yes |

### Leaderboard & User

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/leaderboard?timeFrame=Weekly\|Monthly\|AllTime` | Ranked leaderboard with XP, level, streak, rank change | No (includes current user data if authenticated) |
| GET | `/api/user/xp` | Authenticated user's total XP | Yes |
| GET | `/api/user/{id}/xp` | Any user's total XP | No |
| GET | `/api/user/{id}/avatar` | User avatar image (24h cache) | No |
| PUT | `/api/user/avatar` | Upload a new avatar | Yes |

### Uploads

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/api/uploads/{fileType}` | Upload a file (image, audio, document) | Yes |
| GET | `/api/uploads/{fileType}/{filename}` | Retrieve an uploaded file | No |
| GET | `/api/uploads/list/{fileType}` | List uploaded files by type | Yes |

Full interactive documentation is available at `http://localhost:8080/swagger`.

---

## Project Structure

```
Lexiq/
├── backend/
│   ├── Controllers/          # HTTP layer — delegates directly to services
│   ├── Services/             # Business logic: auth, leaderboard, progress, avatars
│   ├── Database/
│   │   ├── Entities/         # EF Core models — TPH exercise hierarchy, Identity users
│   │   ├── Migrations/       # EF Core migration history
│   │   └── Extensions/       # Seed data and migration retry helpers
│   ├── Dtos/                 # Request/response contracts (record types)
│   ├── Mapping/              # Entity ↔ DTO extension methods
│   ├── Middleware/           # UserContextMiddleware: JWT → full User entity per request
│   ├── Extensions/           # Service registration and middleware pipeline setup
│   ├── Tests/                # xUnit v3 + Testcontainers integration and unit tests
│   └── Program.cs
├── frontend/
│   └── src/app/
│       ├── auth/             # AuthService, Google login component, route guards
│       ├── features/
│       │   ├── lessons/      # Course/lesson/exercise views, lesson editor, form builders
│       │   └── users/        # User profile, leaderboard
│       ├── shared/           # Editor.js ControlValueAccessor wrapper
│       └── nav-bar/
├── .github/workflows/        # CI/CD — build, push to GHCR, deploy to Hetzner
├── scripts/
│   └── deploy.sh             # Zero-downtime deployment with health-check and rollback
├── docker-compose.yml        # Local development
└── docker-compose.prod.yml   # Production: nginx TLS termination, Docker secrets
```

---

## License

Proprietary. All rights reserved by the project maintainers. For collaboration or usage enquiries, open a [GitHub Issue](https://github.com/Ghostdog02/Lexiq/issues).
