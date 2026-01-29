# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

1. Project Motivation and Strategic Goals
The primary objective of this project is the development of a specialized language learning application tailored for Bulgarian speakers and other foreign learners of Italian. Beyond the educational value, this project serves as a technical showcase for professional recruitment in Bulgaria, specifically targeting Full Stack and Backend positions. The development is a collaborative effort between a Backend/DevOps specialist and a Frontend developer with interests in Machine Learning.

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
  - Each feature has its own extension method (AddCorsPolicy, AddDatabaseContext, AddApplicationServices, etc.)
  - Services are registered as Scoped for per-request lifecycle
  - No repository pattern - services directly access DbContext
- **Middleware configuration** is in `Extensions/WebApplicationExtensions.cs`
- **Authentication** uses cookie-based auth with Google OAuth support
  - Cookie name: "AuthToken" with 1-hour sliding expiration
  - HttpOnly, SameSite=Lax for CSRF protection
- **Database initialization** happens in `Program.cs` via `InitializeDatabaseAsync()`
  - Auto-migration with retry logic (10 attempts, 3-second delays) for Docker startup
  - Seed data initialization after migration

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
- Uses **standalone components** (no NgModule) - Angular 21+ approach
- **Routing** is defined in `app.routes.ts`
- **Lazy loading** for dynamic routes (lesson/:id, create-lesson, 404)
- **Environment variables** via `@ngx-env/builder` (prefix: `NG_` or `BACKEND_`)
- **State management**: Service-based with RxJS Observables (no Redux/NgRx)
  - AuthService uses BehaviorSubject for auth state
  - LessonService uses Subject for event broadcasting
- **Dependency injection** uses Angular 16+ `inject()` function
- **Subscription cleanup** via `takeUntilDestroyed(DestroyRef)` operator

### Database Schema

Built with ASP.NET Core Identity. See `backend/Database/ENTITIES_DOCUMENTATION.md` for comprehensive entity documentation.

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
- `Users` - Extended from `IdentityUser` with RegistrationDate, LastLoginDate
- `Roles` - Standard Identity roles (Admin, ContentCreator, User)
- `UserRoles` - User-role associations
- `UserLogins` - External login providers (Google)
- `UserClaims`, `RoleClaims`, `UserTokens` - Identity infrastructure

**Key Patterns:**
- **UUID Primary Keys**: All entities use `Guid.NewGuid().ToString()` for IDs
- **OrderIndex**: All content entities have OrderIndex for custom sequencing
- **Composite Keys**: UserLanguage uses (UserId, LanguageId)
- **Table-Per-Hierarchy**: Exercise uses TPH with discriminator for subtypes
- **Timestamps**: CreatedAt/UpdatedAt for audit trails

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

## Backend Patterns & Conventions

### DTO Mapping Pattern

The codebase uses extension methods for clean mapping between entities and DTOs:

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

### Polymorphic DTOs

Exercise types use .NET 8+ JSON polymorphism for type discrimination:

```csharp
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(MultipleChoiceExerciseDto), "MultipleChoice")]
[JsonDerivedType(typeof(FillInBlankExerciseDto), "FillInBlank")]
// ... other types
public abstract record ExerciseDto(...);
```

This allows sending different exercise types in a single API response with automatic serialization/deserialization.

### Service Layer Guidelines

- **Async all the way**: All service methods must be async
- **Include chains**: Use `.Include()` and `.ThenInclude()` for eager loading related entities
- **OrderBy**: Always order collections by `OrderIndex` for consistent sequencing
- **Null handling**: Use null-coalescing operators for optional relationships
- **No repository pattern**: Services directly access DbContext (simple enough for current needs)

Example from `LessonService.cs`:
```csharp
return await _context.Lessons
    .Include(l => l.Course)
        .ThenInclude(c => c.Language)
    .Include(l => l.Exercises)
    .OrderBy(l => l.OrderIndex)
    .FirstOrDefaultAsync(l => l.Id == lessonId);
```

### File Upload Handling

- **Static files** served at `/static/uploads`
- **Max file size**: 100MB (configured in ServiceCollectionExtensions)
- **CORS headers**: Enabled for cross-origin resource access
- **Cache-Control**: 1-year max-age for uploaded files
- Upload endpoints: `/api/uploads/image`, `/api/uploads/file`

### Authorization Roles

Three roles configured in the system:
- **Admin**: Full system access
- **ContentCreator**: Can create/edit courses, lessons, exercises
- **User**: Basic authenticated user

Apply roles via attributes:
```csharp
[Authorize(Roles = "Admin,ContentCreator")]
public async Task<IActionResult> CreateCourse(CreateCourseDto dto) { }
```

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
   - Read DTOs (e.g., `CourseDto`) for output
   - Create DTOs (e.g., `CreateCourseDto`) for input
   - Update DTOs (e.g., `UpdateCourseDto`) for partial updates
   - Use `record` types for DTOs when possible
2. Create mappings in `backend/Mapping/`
   - Extension methods: `ToDto()` for entity → DTO
   - Map methods: `MapToEntity()` for DTO → entity
3. Create service in `backend/Services/`
   - Constructor inject `BackendDbContext`
   - All methods should be async (return `Task` or `Task<T>`)
   - Register in `Extensions/ServiceCollectionExtensions.cs` as Scoped
4. Create or update controller in `backend/Controllers/`
   - Use `[ApiController]` and `[Route("api/[controller]")]`
   - Constructor inject required services
   - Apply `[Authorize(Roles = "...")]` for protected endpoints
   - Use `[AllowAnonymous]` for public endpoints

### Adding a New Angular Component/Feature

1. Create component folder with files:
   - `component-name.component.ts` (standalone: true)
   - `component-name.component.html`
   - `component-name.component.scss`
   - `component-name.service.ts` (if backend communication needed)
   - `component-name.interface.ts` (for type definitions)
2. Add route to `frontend/src/app/app.routes.ts`
   - Use lazy loading for dynamic routes: `loadComponent: () => import(...)`
3. Update navigation in `nav-bar` component if needed
4. Follow design system guidelines (see Frontend Design System section)
5. For services:
   - Use `providedIn: 'root'` for singleton services
   - Inject dependencies via `inject()` function
   - Use HttpClient with `withCredentials: true` for authenticated requests
6. For forms:
   - Use Reactive Forms with typed FormGroups
   - Create factory services for complex forms (see `lesson-form.service.ts`)
   - Implement ControlValueAccessor for custom form controls

### Testing Deployment Locally

1. Ensure secrets files exist:
   - `secrets/database/password.txt`
   - `secrets/backend/.env`
   - `secrets/frontend/.env`
2. Run: `docker compose up --build`
3. Access frontend at http://localhost:4200
4. Access backend API at http://localhost:8080
5. Access Swagger docs at http://localhost:8080/swagger

## Angular Patterns & Best Practices

### Service Communication Pattern

**Authentication Flow:**
1. User logs in via Google OAuth (frontend component)
2. AuthService receives token, calls backend `/auth/google-login`
3. Backend validates token, returns `issuedAt` timestamp
4. Frontend stores expiration in localStorage
5. AuthService emits auth status via BehaviorSubject
6. Components subscribe to `getAuthStatusListener()` for reactive updates

**HTTP Requests:**
- Always include `withCredentials: true` for cookie-based auth
- Use `firstValueFrom()` to convert Observables to Promises
- Handle errors with try-catch blocks

```typescript
async login(token: string): Promise<void> {
  const response = await firstValueFrom(
    this.httpClient.post<GoogleLoginDto>(
      `${this.apiUrl}/google-login`,
      { idToken: token },
      { withCredentials: true }
    )
  );
}
```

### Reactive Forms with Type Safety

Complex forms use typed FormGroups for compile-time safety:

```typescript
// Define form structure
export interface LessonFormControls {
  title: FormControl<string>;
  description: FormControl<string>;
  exercises: FormArray<FormGroup<ExerciseFormControls>>;
}

// Create typed FormGroup
type LessonForm = FormGroup<LessonFormControls>;
```

**Form Factory Pattern** (see `lesson-form.service.ts`):
- Factory methods create typed forms
- Separate factory methods per form type
- NonNullableFormBuilder ensures non-null values

### Subscription Management

Always clean up subscriptions using `takeUntilDestroyed()`:

```typescript
private destroyRef = inject(DestroyRef);

ngOnInit() {
  this.authService.getAuthStatusListener()
    .pipe(takeUntilDestroyed(this.destroyRef))
    .subscribe(isAuthenticated => {
      // Handle auth status
    });
}
```

### Editor.js Integration

The rich text editor implements `ControlValueAccessor` for seamless Reactive Forms integration:

```typescript
@Component({
  providers: [{
    provide: NG_VALUE_ACCESSOR,
    useExisting: forwardRef(() => EditorComponent),
    multi: true
  }]
})
export class EditorComponent implements ControlValueAccessor {
  writeValue(value: string): void { }
  registerOnChange(fn: any): void { }
  registerOnTouched(fn: any): void { }
}
```

Content is stored as JSON in Editor.js format.

### Component Organization

```
feature/
├── feature.component.ts      # Main component (standalone)
├── feature.component.html    # Template
├── feature.component.scss    # Styles (SCSS)
├── feature.service.ts        # Backend communication
├── feature.interface.ts      # TypeScript interfaces
└── feature-form.service.ts   # Form factory (if complex forms)
```

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

### Technology Stack
- Backend uses **.NET 10.0** (latest preview as of the project)
- Frontend uses **Angular 21** with standalone components
- Database: **Microsoft SQL Server 2022**
- **Bootstrap 5** is included but should be used minimally (prefer custom design system)

### Authentication & Security
- Cookie authentication uses `AuthToken` cookie with 1-hour sliding expiration
- HttpOnly, SameSite=Lax cookies for CSRF protection
- All controllers require authentication unless explicitly marked with `[AllowAnonymous]`
- CORS configured for frontend origin (environment variable: `ANGULAR_PORT`)
- Google OAuth via `GoogleJsonWebSignature.ValidateAsync()`

### Database & Migrations
- Migrations auto-applied on startup in `Program.cs` with retry logic
- Connection string built from environment variables (DB_SERVER, DB_NAME, DB_USER_ID, DB_PASSWORD)
- Development: `TrustServerCertificate=True, Encrypt=False`
- Production: `TrustServerCertificate=True, Encrypt=True`
- Seed data applied after migration via `SeedData.InitializeAsync()`

### Deployment
- Docker health checks configured for all services (db, backend, frontend)
- Production deployment uses nginx in frontend container
- Images pushed to GitHub Container Registry (ghcr.io)
- CI/CD via GitHub Actions on push to `master` branch
- SSH deployment to Hetzner server via `scripts/deploy.sh`

### Content Storage
- Lesson content stored as JSON (Editor.js format) in database
- Media files uploaded to `/static/uploads` directory
- Images and attachments served with CORS headers and 1-year cache

### Frontend Specifics
- **Component styles**: Always use SCSS (configured in `angular.json`)
- No centralized state management (uses service-based RxJS patterns)
- Environment variables via `@ngx-env/builder` (prefix: `NG_` or `BACKEND_`)
- Lazy loading for dynamic routes (lesson detail, create lesson, 404)

### Known Limitations
- No validation layer on backend DTOs (validation done in entity layer)
- No error handling middleware (returns raw exceptions)
- Help and Leaderboard services return mock data (not yet integrated with backend)
- No logging infrastructure configured (ILogger available but not set up)

## Key Controllers & Endpoints

### Backend API Endpoints

| Controller | Base Route | Key Endpoints | Auth Required |
|-----------|-----------|---------------|---------------|
| AuthController | `/api/auth` | `POST /google-login`, `POST /logout`, `GET /auth-status` | Mixed |
| CourseController | `/api/courses` | `GET /`, `GET /{id}`, `POST /`, `PUT /{id}`, `DELETE /{id}` | Admin/Creator for mutations |
| LessonController | `/api/lessons` | `GET /`, `GET /{id}`, `POST /`, `PUT /{id}`, `DELETE /{id}` | Admin/Creator for mutations |
| ExerciseController | `/api/exercises` | `GET /lesson/{lessonId}`, `POST /`, `PUT /{id}`, `DELETE /{id}` | Admin/Creator for mutations |
| LanguageController | `/api/languages` | `GET /`, `POST /`, `PUT /{id}`, `DELETE /{id}` | Public read, Admin write |
| UserLanguageController | `/api/userLanguages` | `GET /user/{userId}`, `POST /enroll`, `DELETE /unenroll` | Yes |
| UserManagementController | `/api/userManagement` | `GET /users`, `GET /users/{id}`, `POST /roles`, etc. | Admin only |
| UploadsController | `/api/uploads` | `POST /image`, `POST /file`, `GET /files` | Yes |

### Frontend Routes

| Path | Component | Lazy Loaded | Description |
|------|-----------|-------------|-------------|
| `/` | HomeComponent | No | Main dashboard with learning path |
| `/google-login` | GoogleLoginComponent | No | OAuth login page |
| `/create-lesson` | LessonComponent | Yes | Lesson creation form with Editor.js |
| `/lesson/:id` | LessonDetailComponent | Yes | Display lesson content |
| `/profile` | ProfileComponent | No | User profile and achievements |
| `/leaderboard` | LeaderboardComponent | No | User rankings |
| `/help` | HelpComponent | No | Help and FAQ |
| `/**` | NotFoundComponent | Yes | 404 page |
