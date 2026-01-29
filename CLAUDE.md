# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Lexiq is a full-stack language learning application with:
- **Backend**: ASP.NET Core 10.0 Web API with Entity Framework Core
- **Frontend**: Angular 21 single-page application
- **Database**: Microsoft SQL Server 2022
- **Infrastructure**: Docker Compose with CI/CD via GitHub Actions

## Development Commands

### Backend (ASP.NET Core)

Navigate to `backend/` for all backend commands:

```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

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

### Frontend (Angular)

Navigate to `frontend/` for all frontend commands:

```bash
# Install dependencies
npm install

# Start development server (http://localhost:4200)
npm start
# or
ng serve

# Build for production
npm run build
# or
ng build

# Build with watch mode
npm run watch

# Run unit tests with Karma
npm test
# or
ng test

# Generate new component/service/etc
ng generate component <name>
ng generate service <name>
```

### Docker & Deployment

From repository root:

```bash
# Start all services locally (development)
docker compose up

# Start in detached mode
docker compose up -d

# Build images before starting
docker compose up --build

# Stop all services
docker compose down

# View container logs
docker compose logs

# View specific service logs
docker compose logs backend
docker compose logs frontend
docker compose logs db

# Build for production (uses docker-compose.prod.yml)
TAG=latest docker compose -f docker-compose.prod.yml up --build
```

## Architecture

### Backend Structure

```
backend/
├── Controllers/          # API endpoints
│   ├── AuthController.cs          # Authentication endpoints
│   ├── UserManagementController.cs # User CRUD operations
│   ├── RoleManagementController.cs # Role management
│   └── CoursesController.cs        # Course management (placeholder)
├── Database/
│   ├── BackendDbContext.cs        # EF Core DbContext
│   ├── Entities/                  # Database models
│   ├── Migrations/                # EF Core migrations
│   └── Extensions/                # Database service configuration
├── Services/
│   ├── GoogleAuthService.cs       # Google OAuth implementation
│   └── UserExtensions.cs          # User utility methods
├── Dtos/                # Data Transfer Objects
├── Mapping/             # DTO ↔ Entity mappings
├── Extensions/          # Service collection & app builder extensions
└── Program.cs          # Application entry point
```

**Key patterns:**
- **Service registration** is organized via extension methods in `Extensions/ServiceCollectionExtensions.cs`
- **Middleware configuration** is in `Extensions/WebApplicationExtensions.cs`
- **Authentication** uses cookie-based auth with Google OAuth support
- **Database initialization** happens in `Program.cs` via `InitializeDatabaseAsync()`

### Frontend Structure

```
frontend/src/app/
├── auth/
│   └── google-login/    # Google OAuth login component
├── home/                # Home page component
├── nav-bar/            # Navigation component
├── not-found/          # 404 page
├── app.routes.ts       # Route definitions
└── app.config.ts       # App configuration & providers
```

**Key patterns:**
- Uses **standalone components** (no NgModule)
- **Routing** is defined in `app.routes.ts`
- **Lazy loading** for NotFoundComponent
- **Environment variables** via `@ngx-env/builder`

### Database Schema

Built with ASP.NET Core Identity. Main tables:
- `Users` - Extended from `IdentityUser` with custom `User` entity
- `Roles` - Standard Identity roles
- `UserRoles` - User-role associations
- `UserLogins` - External login providers (Google)
- `UserClaims`, `RoleClaims`, `UserTokens` - Identity infrastructure

### CI/CD Pipeline

Three-stage pipeline in `.github/workflows/`:

1. **build-and-push-docker.yml** - Builds and pushes Docker images to GHCR
2. **development.yml** - Orchestrates the full CI/CD workflow
3. **continious-delivery.yml** - Deploys to Hetzner production server

**Deployment flow:**
- Triggered on push to `master` or `feature/ci-cd`
- Builds both frontend and backend Docker images
- Pushes to GitHub Container Registry (ghcr.io)
- SSHs into Hetzner server and runs `scripts/deploy.sh`
- deploy.sh pulls images, rebuilds containers, and verifies health

## Environment Variables

### Backend (.env or secrets/backend/.env)

```
DB_SERVER=db
DB_NAME=LexiqDb
DB_USER_ID=sa
DB_PASSWORD=<your-password>
GOOGLE_CLIENT_ID=<google-oauth-client-id>
GOOGLE_CLIENT_SECRET=<google-oauth-client-secret>
```

Backend loads secrets from `/run/secrets/backend_env` in production (Docker secrets).

### Frontend (secrets/frontend/.env)

```
NG_GOOGLE_CLIENT_ID=<google-oauth-client-id>
BACKEND_API_URL=http://backend:8080
```

Frontend build arguments are passed via Docker Compose.

## Common Development Workflows

### Adding a New Database Entity

1. Create entity class in `backend/Database/Entities/`
2. Add DbSet to `BackendDbContext.cs`
3. Configure relationships in `OnModelCreating()` if needed
4. Create migration: `dotnet ef migrations add AddEntityName --project Database/Backend.Database.csproj`
5. Apply migration: `dotnet ef database update --project Database/Backend.Database.csproj`

### Adding a New API Endpoint

1. Create DTOs in `backend/Dtos/`
2. Create mappings in `backend/Mapping/`
3. Create or update controller in `backend/Controllers/`
4. Add service in `backend/Services/` if business logic is complex
5. Register service in `Extensions/ServiceCollectionExtensions.cs`

### Adding a New Angular Route

1. Create component: `ng generate component <name>`
2. Add route to `frontend/src/app/app.routes.ts`
3. Update navigation in `nav-bar` component if needed

### Testing Deployment Locally

1. Ensure secrets files exist:
   - `secrets/database/password.txt`
   - `secrets/backend/.env`
   - `secrets/frontend/.env`
2. Run: `docker compose up --build`
3. Access frontend at http://localhost:4200
4. Access backend API at http://localhost:8080
5. Access Swagger docs at http://localhost:8080/swagger

## Frontend Design System

**CRITICAL: All new components and pages MUST follow these design guidelines for visual consistency.**

### Color Palette (CSS Custom Properties)

Use CSS variables defined in `frontend/src/styles.scss`:

```scss
// Backgrounds
--bg-dark: #0f1419;      // Darkest background
--bg: #1a2429;           // Main background
--panel: #1e2732;        // Panel/card backgrounds

// Accent Colors
--accent: #7c5cff;       // Primary purple accent
--accent-light: #9178ff; // Lighter purple (hover states, links)
--accent-dark: #5a3ce6;  // Darker purple (gradients, shadows)

// Text Colors
--white: #ffffff;        // Primary text
--text-secondary: #b8c4cf; // Secondary text
--muted: #8b98a5;        // Muted/tertiary text

// Glass Effects
--glass: rgba(255,255,255,0.04);   // Subtle glass overlay
--glass-hover: rgba(255,255,255,0.08); // Glass hover state
--border: rgba(255,255,255,0.08);  // Subtle borders
```

### Typography

- **Font Family**: `"Bricolage Grotesque", sans-serif` (loaded from Google Fonts)
- **Primary Text**: `var(--white)` with font weights 600-800
- **Secondary Text**: `var(--text-secondary)` with font weight 500
- **Muted Text**: `var(--muted)` for disclaimers, terms, etc.

**Heading Styles:**
```scss
.title {
  font-size: 31px;
  font-weight: 800;
  letter-spacing: -0.3px;
  // Gradient text effect
  background: linear-gradient(135deg, var(--white) 0%, rgba(255,255,255,0.9) 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
}

.subtitle {
  color: var(--text-secondary);
  font-size: 18px;
  font-weight: 500;
}
```

### Border Radius

- **Cards/Panels**: `var(--radius)` = `16px`
- **Buttons/Pills**: `var(--radius-sm)` = `100px` (fully rounded)

### Shadows

```scss
--shadow: 0 20px 60px rgba(0,0,0,0.5);  // Default shadow for cards
--shadow-hover: 0 24px 70px rgba(124, 92, 255, 0.15);  // Purple glow on hover
```

### Glass Morphism Pattern

**All cards and panels should use glassmorphic design:**

```scss
.card {
  background: linear-gradient(135deg, rgba(255,255,255,0.06) 0%, rgba(255,255,255,0.02) 100%);
  border-radius: var(--radius);
  padding: 38px 40px;
  box-shadow: var(--shadow);
  border: 1px solid var(--border);
  backdrop-filter: blur(10px);
  position: relative;

  // Inner glow border effect
  &::before {
    content: '';
    position: absolute;
    inset: 0;
    border-radius: var(--radius);
    padding: 1px;
    background: linear-gradient(135deg, rgba(124, 92, 255, 0.2), transparent 50%, rgba(145, 120, 255, 0.1));
    -webkit-mask: linear-gradient(#fff 0 0) content-box, linear-gradient(#fff 0 0);
    mask: linear-gradient(#fff 0 0) content-box, linear-gradient(#fff 0 0);
    -webkit-mask-composite: xor;
    mask-composite: exclude;
    pointer-events: none;
  }
}
```

### Button Styles

**Primary Button (Call-to-Action):**
```scss
.btn.primary {
  background: linear-gradient(135deg, var(--accent) 0%, var(--accent-dark) 100%);
  box-shadow: 0 8px 24px rgba(124, 92, 255, 0.25), 0 16px 48px rgba(90, 60, 230, 0.15);
  border-radius: var(--radius-sm);
  padding: 14px 18px;
  height: 50px;
  font-weight: 700;
  font-size: 15px;
  color: var(--white);
  border: 1px solid rgba(255,255,255,0.1);
  cursor: pointer;
  transition: all 200ms cubic-bezier(0.4, 0, 0.2, 1);

  &:hover {
    transform: translateY(-2px);
    box-shadow: 0 12px 32px rgba(124, 92, 255, 0.35), 0 20px 60px rgba(90, 60, 230, 0.25);
  }

  &:active {
    transform: translateY(0);
  }
}
```

**OAuth/Secondary Buttons:**
```scss
.btn.oauth {
  background: var(--glass);
  border: 1px solid var(--border);
  border-radius: var(--radius-sm);
  padding: 14px 18px;
  height: 50px;
  font-weight: 600;
  font-size: 15px;
  color: var(--white);
  cursor: pointer;
  transition: all 200ms cubic-bezier(0.4, 0, 0.2, 1);

  &:hover {
    background: var(--glass-hover);
    border-color: rgba(255,255,255,0.12);
    transform: translateY(-2px);
    box-shadow: var(--shadow-hover);
  }
}
```

### Hover Effects

**Standard hover interaction:**
```scss
transition: all 200ms cubic-bezier(0.4, 0, 0.2, 1);

&:hover {
  transform: translateY(-2px);  // Subtle lift
  box-shadow: var(--shadow-hover);  // Purple glow
}

&:active {
  transform: translateY(0);  // Press down effect
}
```

### Links

```scss
.link {
  color: var(--accent-light);
  text-decoration: none;
  transition: color 150ms ease;
  border-bottom: 1px solid transparent;

  &:hover {
    color: var(--accent-light);
    border-bottom-color: var(--accent-light);
  }
}
```

### Sidebar/Navigation Pattern

**Example from nav-bar component:**
```scss
#mainAside {
  border-right: 2px solid #3b4951;
  padding: 1.5em 2.2em 1em 1.3em;
  height: 100vh;
  width: 18em;

  .sideBarLinks {
    list-style: none;

    li {
      a {
        text-transform: uppercase;
        padding: .8em 1em;
        color: var(--primary-color);
        font-weight: 700;
        letter-spacing: .08px;
        text-decoration: none;
      }

      &:hover {
        background-color: #3b4951;
        border-radius: 5%;
      }
    }
  }
}
```

### Responsive Breakpoints

```scss
// Tablet
@media (max-width: 1024px) {
  // Switch to single column, adjust padding
}

// Mobile
@media (max-width: 480px) {
  // Reduce font sizes, tighter padding
}
```

### Animations

**Background pulse effect:**
```scss
&::before {
  content: '';
  position: absolute;
  background: radial-gradient(circle, rgba(124, 92, 255, 0.1) 0%, transparent 40%);
  animation: pulse 8s ease-in-out infinite;
}
```

**Floating logo effect:**
```scss
img {
  animation: float 6s ease-in-out infinite;
}
```

### Accessibility

- All interactive elements must have `aria-label` attributes
- Use semantic HTML (`<aside>`, `<nav>`, `<main>`, etc.)
- Focus states: `outline: 2px solid var(--accent); outline-offset: 3px;`
- Use `role` attributes where appropriate

### Layout Patterns

**Split-screen auth pattern (google-login component):**
```scss
.page {
  display: grid;
  grid-template-columns: 1fr 520px;  // Logo side | Form side
  min-height: 100vh;
  background: var(--bg);
}
```

### Component Styling Checklist

When creating new components, ensure:
- [ ] Uses CSS custom properties from `:root`
- [ ] Follows glassmorphism pattern for cards/panels
- [ ] Buttons use defined `.btn.primary` or `.btn.oauth` styles
- [ ] Hover effects include `transform: translateY(-2px)` and purple shadow
- [ ] Border radius uses `var(--radius)` or `var(--radius-sm)`
- [ ] Typography uses `var(--font-family)` and correct font weights
- [ ] Links use accent-light color with underline on hover
- [ ] Responsive breakpoints at 1024px and 480px
- [ ] Accessibility attributes present (aria-label, role, etc.)
- [ ] Focus states defined with purple accent outline

## Important Notes

- Backend uses **.NET 10.0** (latest preview as of the project)
- Frontend uses **Angular 21** with standalone components
- **Bootstrap 5** is included but should be used minimally (prefer custom design system)
- CORS is configured for `http://localhost:4200` and `https://localhost:8080` in development
- Cookie authentication uses `AuthToken` cookie with 1-hour sliding expiration
- All controllers require authentication unless explicitly marked with `[AllowAnonymous]`
- Database migrations are auto-applied on startup in `Program.cs`
- Docker health checks are configured for all services (db, backend, frontend)
- Production deployment uses nginx in frontend container for serving Angular app
- **Component styles**: Always use SCSS (configured in `angular.json`)
