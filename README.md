# Lexiq

A full-stack language learning application tailored for Bulgarian speakers and other foreign learners of Italian.

## Project Motivation

Lexiq is being developed as both a practical language learning tool and a technical showcase for professional recruitment in Bulgaria, targeting Full Stack and Backend positions. The project is a collaborative effort between a Backend/DevOps specialist and a Frontend developer with interests in Machine Learning.

## Tech Stack

| Layer | Technology |
|-------|------------|
| **Backend** | ASP.NET Core 10.0 Web API with Entity Framework Core |
| **Frontend** | Angular 21 (standalone components) |
| **Database** | Microsoft SQL Server 2022 |
| **Infrastructure** | Docker Compose |
| **CI/CD** | GitHub Actions with deployment to Hetzner |
| **Authentication** | Google OAuth with cookie-based sessions |

## Architecture

### System Overview

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              Docker Compose                                  │
│                                                                              │
│  ┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐         │
│  │    Frontend     │    │     Backend     │    │    Database     │         │
│  │   (Angular 21)  │    │  (ASP.NET Core) │    │  (SQL Server)   │         │
│  │                 │    │                 │    │                 │         │
│  │  ┌───────────┐  │    │  ┌───────────┐  │    │  ┌───────────┐  │         │
│  │  │   Nginx   │  │───▶│  │Controllers│  │───▶│  │  Tables   │  │         │
│  │  │  :4200    │  │    │  │   :8080   │  │    │  │   :1433   │  │         │
│  │  └───────────┘  │    │  └─────┬─────┘  │    │  └───────────┘  │         │
│  │                 │    │        │        │    │                 │         │
│  │  ┌───────────┐  │    │  ┌─────▼─────┐  │    │                 │         │
│  │  │  Angular  │  │    │  │ Services  │  │    │                 │         │
│  │  │   SPA     │  │    │  └─────┬─────┘  │    │                 │         │
│  │  └───────────┘  │    │        │        │    │                 │         │
│  │                 │    │  ┌─────▼─────┐  │    │                 │         │
│  │                 │    │  │ DbContext │  │    │                 │         │
│  │                 │    │  └───────────┘  │    │                 │         │
│  └─────────────────┘    └─────────────────┘    └─────────────────┘         │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Backend Architecture

The backend follows a layered architecture pattern:

```
HTTP Request
    │
    ▼
┌─────────────────────────────────────────────────────────────────┐
│  Controllers (API Layer)                                        │
│  - Handle HTTP requests/responses                               │
│  - Authorization via [Authorize] attributes                     │
│  - Route definitions via [Route("api/[controller]")]            │
└─────────────────────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────────────┐
│  Services (Business Logic Layer)                                │
│  - Scoped lifetime (per-request)                                │
│  - Async methods with EF Core queries                           │
│  - Direct DbContext access (no repository pattern)              │
└─────────────────────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────────────┐
│  BackendDbContext (Data Access Layer)                           │
│  - Entity Framework Core with SQL Server                        │
│  - ASP.NET Core Identity integration                            │
│  - Auto-migration with retry logic for Docker startup           │
└─────────────────────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────────────┐
│  SQL Server Database                                            │
│  - UUID primary keys                                            │
│  - Table-Per-Hierarchy for Exercise polymorphism                │
│  - Composite keys for many-to-many relationships                │
└─────────────────────────────────────────────────────────────────┘
```

**Key Backend Patterns:**
- **Extension Methods**: Service registration organized in `Extensions/ServiceCollectionExtensions.cs`
- **DTO Mapping**: Extension methods for entity-to-DTO conversion (`entity.ToDto()`)
- **JSON Polymorphism**: Exercise types use `[JsonPolymorphic]` for type discrimination
- **Cookie Authentication**: HttpOnly, SameSite=Lax cookies with 1-hour sliding expiration

### Frontend Architecture

Angular 21 with standalone components and RxJS-based state management:

```
┌─────────────────────────────────────────────────────────────────┐
│  App Bootstrap (main.ts → app.config.ts)                        │
│  - bootstrapApplication() with standalone components            │
│  - provideRouter(), provideHttpClient(withFetch())              │
└─────────────────────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────────────┐
│  Routing (app.routes.ts)                                        │
│  - Eager loading: home, profile, google-login                   │
│  - Lazy loading: lesson/:id, create-lesson, 404                 │
└─────────────────────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────────────┐
│  Standalone Components                                          │
│  - No NgModule required                                         │
│  - Explicit imports per component                               │
│  - inject() function for dependency injection                   │
└─────────────────────────────────────────────────────────────────┘
    │
    ▼
┌─────────────────────────────────────────────────────────────────┐
│  Services (State Management)                                    │
│  - BehaviorSubject for reactive state (AuthService)             │
│  - Subject for event broadcasting (LessonService)               │
│  - takeUntilDestroyed() for subscription cleanup                │
└─────────────────────────────────────────────────────────────────┘
```

**Key Frontend Patterns:**
- **Typed Reactive Forms**: FormGroup with TypeScript interfaces for compile-time safety
- **Form Factory Pattern**: `LessonFormService` creates typed forms for different exercise types
- **ControlValueAccessor**: Editor.js integrated as custom form control
- **Environment Variables**: `@ngx-env/builder` with `NG_` and `BACKEND_` prefixes

### Authentication Flow

```
┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐
│  User    │    │ Frontend │    │ Backend  │    │  Google  │
└────┬─────┘    └────┬─────┘    └────┬─────┘    └────┬─────┘
     │               │               │               │
     │ Click Login   │               │               │
     │──────────────▶│               │               │
     │               │  OAuth Popup  │               │
     │               │──────────────────────────────▶│
     │               │               │               │
     │               │◀──────────────────────────────│
     │               │  JWT Token    │               │
     │               │               │               │
     │               │ POST /auth/google-login       │
     │               │──────────────▶│               │
     │               │               │ Validate Token│
     │               │               │──────────────▶│
     │               │               │◀──────────────│
     │               │               │               │
     │               │  Set Cookie   │               │
     │               │◀──────────────│               │
     │               │  "AuthToken"  │               │
     │               │               │               │
     │  Redirect /   │               │               │
     │◀──────────────│               │               │
```

### Data Model

```
┌─────────────┐      ┌─────────────┐      ┌─────────────┐      ┌─────────────┐
│  Language   │      │   Course    │      │   Lesson    │      │  Exercise   │
├─────────────┤      ├─────────────┤      ├─────────────┤      ├─────────────┤
│ Id          │◀────▶│ LanguageId  │◀────▶│ CourseId    │◀────▶│ LessonId    │
│ Name        │  1:M │ Title       │  1:M │ Title       │  1:M │ Title       │
│ Code        │      │ Description │      │ Content     │      │ Type        │
│ NativeName  │      │ OrderIndex  │      │ OrderIndex  │      │ Points      │
└─────────────┘      └─────────────┘      └─────────────┘      └──────┬──────┘
                                                                      │
                     ┌────────────────────────────────────────────────┼────────┐
                     │                    │                           │        │
              ┌──────▼──────┐      ┌──────▼──────┐      ┌─────────────▼───┐    │
              │ MultiChoice │      │ FillInBlank │      │    Listening    │    │
              ├─────────────┤      ├─────────────┤      ├─────────────────┤    │
              │ Options[]   │      │CorrectAnswer│      │ AudioUrl        │    │
              │             │      │AcceptedAns[]│      │ MaxReplays      │    │
              └─────────────┘      └─────────────┘      └─────────────────┘    │
                                                                               │
                                                                        ┌──────▼──────┐
                                                                        │ Translation │
                                                                        ├─────────────┤
                                                                        │ SourceText  │
                                                                        │ TargetText  │
                                                                        └─────────────┘
```

### CI/CD Pipeline

```
┌─────────────┐    ┌─────────────────────────────────────────────────────────┐
│   GitHub    │    │                  GitHub Actions                         │
│    Push     │───▶│                                                         │
└─────────────┘    │  ┌─────────────┐    ┌─────────────┐    ┌─────────────┐  │
                   │  │   Build &   │───▶│   Deploy    │───▶│   Verify    │  │
                   │  │    Push     │    │   Script    │    │   Health    │  │
                   │  └─────────────┘    └─────────────┘    └─────────────┘  │
                   │        │                  │                             │
                   │        ▼                  ▼                             │
                   │  ┌───────────┐     ┌───────────┐                        │
                   │  │   GHCR    │     │  Hetzner  │                        │
                   │  │  Images   │     │  Server   │                        │
                   │  └───────────┘     └───────────┘                        │
                   └─────────────────────────────────────────────────────────┘
```

**Pipeline Stages:**
1. **Build & Push**: Multi-stage Docker builds, push to GitHub Container Registry
2. **Deploy**: SSH to Hetzner, pull images, restart containers
3. **Verify**: Health check validation, rollback on failure

## Features Implemented

### Authentication & User Management
- Google OAuth integration for seamless login
- Cookie-based authentication with 1-hour sliding expiration
- Role-based authorization (Admin, ContentCreator, User)
- User profile management

### Content Management
- **Languages**: CRUD operations for supported languages
- **Courses**: Course creation and management
- **Lessons**: Rich content creation with Editor.js integration
- **Exercises**: Multiple exercise types with polymorphic DTOs
  - Multiple Choice
  - Fill in the Blank
  - Listening
  - Translation

### File Uploads
- Image and file upload endpoints
- Static file serving with CORS support
- 1-year cache headers for uploaded content

### Infrastructure
- Dockerized development and production environments
- Automated CI/CD pipeline with GitHub Actions
- Health checks for all services
- Database auto-migration with retry logic

## Getting Started

### Prerequisites

- [Docker](https://docs.docker.com/get-docker/) and Docker Compose
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download) (for local development)
- [Node.js 20+](https://nodejs.org/) and npm (for local development)
- A Google Cloud project with OAuth 2.0 credentials

### Quick Start with Docker

1. **Clone the repository**
   ```bash
   git clone https://github.com/Ghostdog02/Lexiq.git
   cd Lexiq
   ```

2. **Create secrets files**

   Create the following directory structure and files:
   ```
   secrets/
   ├── database/
   │   └── password.txt          # Your SQL Server SA password
   └── backend/
       └── .env                  # Backend environment variables
   ```

   **secrets/database/password.txt**
   ```
   YourStrongPassword123!
   ```

   **secrets/backend/.env**
   ```
   DB_SERVER=db
   DB_NAME=LexiqDb
   DB_USER_ID=sa
   DB_PASSWORD=YourStrongPassword123!
   GOOGLE_CLIENT_ID=your-google-client-id
   GOOGLE_CLIENT_SECRET=your-google-client-secret
   ```

3. **Start all services**
   ```bash
   docker compose up --build
   ```

4. **Access the application**
   - Frontend: http://localhost:4200
   - Backend API: http://localhost:8080
   - Swagger docs: http://localhost:8080/swagger

### Local Development

#### Backend

```bash
cd backend

# Restore dependencies
dotnet restore

# Run the development server (port 8080)
dotnet watch run
```

#### Frontend

```bash
cd frontend

# Install dependencies
npm install

# Start development server (port 4200)
npm start
```

### Database Migrations

```bash
cd backend

# Create a new migration
dotnet ef migrations add <MigrationName> --project Database/Backend.Database.csproj

# Apply migrations
dotnet ef database update --project Database/Backend.Database.csproj
```

## Project Structure

```
Lexiq/
├── backend/
│   ├── Controllers/          # API endpoints
│   ├── Database/
│   │   ├── Entities/         # Database models
│   │   └── Migrations/       # EF Core migrations
│   ├── Services/             # Business logic
│   ├── Dtos/                 # Data Transfer Objects
│   ├── Mapping/              # Entity ↔ DTO mappings
│   └── Extensions/           # Service configuration
├── frontend/
│   └── src/app/
│       ├── auth/             # Authentication components
│       ├── home/             # Dashboard
│       ├── lesson/           # Lesson creation/viewing
│       └── nav-bar/          # Navigation
├── scripts/                  # Deployment scripts
├── secrets/                  # Environment secrets (gitignored)
├── .github/workflows/        # CI/CD pipelines
└── docker-compose.yml        # Development orchestration
```

## API Endpoints

| Controller | Base Route | Description |
|-----------|-----------|-------------|
| AuthController | `/api/auth` | Authentication (Google OAuth, logout, status) |
| CourseController | `/api/courses` | Course CRUD operations |
| LessonController | `/api/lessons` | Lesson CRUD operations |
| ExerciseController | `/api/exercises` | Exercise management |
| LanguageController | `/api/languages` | Language configuration |
| UploadsController | `/api/uploads` | File and image uploads |
| UserManagementController | `/api/userManagement` | Admin user management |

## Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

This project is currently unlicensed. Please contact the maintainers for usage permissions.

## Acknowledgments

- Built with ASP.NET Core Identity for authentication infrastructure
- Editor.js for rich content editing
- Angular standalone components for modern frontend architecture
