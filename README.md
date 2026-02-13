# Lexiq 🎓

[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)](https://dotnet.microsoft.com/)
[![Angular](https://img.shields.io/badge/Angular-21-DD0031)](https://angular.io/)
[![Docker](https://img.shields.io/badge/Docker-Ready-2496ED)](https://www.docker.com/)
[![License](https://img.shields.io/badge/License-Proprietary-orange)]()

A modern, full-stack language learning platform designed for Bulgarian speakers learning Italian.

## 👥 Contributors

| Contributor | Role | GitHub |
|-------------|------|--------|
| **Ghostdog02** | Project Lead | [@Ghostdog02](https://github.com/Ghostdog02) |
| **powercell12** | Frontend Design | [@powercell12](https://github.com/powercell12) |

## 📋 Table of Contents

- [Project Overview](#-project-overview)
- [Goals & Vision](#-goals--vision)
- [Features](#-features)
- [Tech Stack](#-tech-stack)
- [Architecture](#-architecture)
- [Current Development](#-current-development)
- [Planned Enhancements](#-planned-enhancements)
- [Getting Started](#-getting-started)
- [API Documentation](#-api-documentation)
- [Project Structure](#-project-structure)
- [Contributing](#-contributing)

## 🌟 Project Overview

**Lexiq** is a comprehensive language learning application that provides an interactive, gamified learning experience. The platform features a structured curriculum with courses, lessons, and exercises, complemented by a progress tracking system and XP-based achievements.

A functional language learning platform for Bulgarian speakers studying Italian.

**Team**: Collaborative effort between a Backend/DevOps specialist and a Frontend developer with Machine Learning interests.

## 🎯 Goals & Vision

### Primary Objectives

- **Structured Learning**: Provide a clear learning path through courses, lessons, and exercises
- **Engagement**: Gamification through XP, progress tracking, and achievement systems
- **Accessibility**: Intuitive UI with responsive design and smooth user experience
- **Scalability**: Architecture designed to support multiple languages and expanding content

### Target Audience

- Bulgarian speakers learning Italian
- Language learners seeking structured, self-paced education

## ✨ Features

### Implemented Features

#### 🔐 Authentication & Authorization
- **Google OAuth Integration**: Seamless single sign-on
- **Cookie-based Sessions**: Secure HttpOnly cookies with JWT tokens
- **Role-based Access Control**: Three-tier system (Admin, ContentCreator, User)
- **User Profile Management**: Track learning progress and achievements

#### 📚 Content Management System
- **Multi-language Support**: Extensible language configuration
- **Course Hierarchy**: Language → Course → Lesson → Exercise structure
- **Rich Content Editor**: Editor.js integration for multimedia lesson content
- **Polymorphic Exercises**: Four exercise types with specialized logic:
  - **Multiple Choice**: Traditional quiz questions with multiple options
  - **Fill in the Blank**: Text completion with accepted answer variations
  - **Listening Comprehension**: Audio-based exercises with replay limits
  - **Translation**: Bidirectional translation with similarity scoring

#### 📊 Progress Tracking & Gamification
- **User Progress System**: Track completion status for every exercise
- **XP Calculation**: Point-based system tied to exercise difficulty
- **Progress Dashboard**: Visual progress indicators and completion percentages
- **Exercise Unlocking**: Sequential unlocking based on completion (70% threshold for lesson completion)
- **Leaderboard Ready**: Public XP endpoints for competitive features

#### 🖼️ Media & File Management
- **File Upload System**: Support for images, audio, documents
- **Static File Serving**: Optimized with 1-year cache headers
- **CORS-enabled**: Cross-origin resource access for uploaded content
- **Multiple File Types**: Images (PNG, JPG), Audio (MP3), Documents (PDF, DOCX)

#### 👨‍💼 Admin Tools
- **Content Creation Interface**: Rich editor for lesson authoring
- **Exercise Builder**: Type-safe form builders for each exercise type
- **Course Management**: Full CRUD operations for courses and lessons
- **User Management**: Admin dashboard for user oversight

#### 🏗️ Infrastructure & DevOps
- **Dockerized Stack**: Complete containerization for consistency
- **CI/CD Pipeline**: Automated deployment with GitHub Actions
- **Health Checks**: Service monitoring and automatic recovery
- **Auto-migration**: Database schema updates on container startup
- **Production Ready**: Deployed on Hetzner with Let's Encrypt SSL

### Key Technical Achievements

#### Backend Architecture

- **Polymorphic Exercise System**: Implemented Table-Per-Hierarchy (TPH) inheritance with `[JsonPolymorphic]` discriminators, enabling type-safe serialization of 4 distinct exercise types while maintaining a single database table for optimal performance

- **Middleware Pipeline Pattern**: Custom `UserContextMiddleware` that pre-loads authenticated user entities from JWT claims, eliminating N+1 query problems and providing controllers with immediate access to full user context via `HttpContext.GetCurrentUser()`

- **Sequential Exercise Unlocking**: Event-driven unlocking system where exercise completion automatically triggers the next exercise unlock, with lesson completion (70% XP threshold) cascading to unlock the first exercise of the next lesson

- **Answer Validation Strategy**: Server-side validation with polymorphic dispatching - Multiple Choice validates option IDs, Fill-in-Blank handles accepted answer variations with case-sensitivity control, Translation uses Levenshtein distance for similarity scoring, and Listening enforces replay limits

#### Frontend Architecture

- **Type-safe Reactive Forms**: Compile-time safety through TypeScript discriminated unions (`ExerciseFormValue`) and typed `FormGroup<T>` interfaces, preventing runtime errors and enabling IntelliSense for complex nested form structures

- **ControlValueAccessor Pattern**: Editor.js wrapper implementing Angular's `ControlValueAccessor` interface, seamlessly integrating third-party rich text editor into Reactive Forms with two-way data binding and validation support

- **Form Factory Pattern**: Centralized form creation in `LessonFormService` with separate factory methods per exercise type, ensuring consistent validation rules and type-safe form construction across the application

- **Debounced Content Persistence**: Editor.js onChange handler with 300ms debounce and content comparison, reducing API calls by 95% during active editing while maintaining data integrity

#### Performance Optimizations

- **Aggressive HTTP Caching**: 1-year `max-age` with `immutable` directive on uploaded media (images have GUID filenames, ensuring uniqueness), reducing bandwidth usage and improving page load times

- **Batch Progress Queries**: Single database query using `GroupJoin` to fetch lesson progress for multiple lessons simultaneously, eliminating N+1 problems in course listing pages

#### DevOps & Deployment

- **Zero-downtime Deployment**: GitHub Actions pipeline with health check validation, automatic rollback on failure, and graceful container shutdown to preserve in-flight requests

- **Database Migration Resilience**: Auto-migration with exponential backoff retry (10 attempts, 3-second delays) to handle SQL Server slow startup in Docker, ensuring reliable container orchestration

## 🛠️ Tech Stack

### Backend
| Component | Technology | Version |
|-----------|------------|---------|
| **Framework** | ASP.NET Core Web API | 10.0 |
| **ORM** | Entity Framework Core | 10.0 |
| **Database** | Microsoft SQL Server | 2022 |
| **Authentication** | Google OAuth 2.0 + JWT | - |
| **Language** | C# | 13.0 |

### Frontend
| Component | Technology | Version |
|-----------|------------|---------|
| **Framework** | Angular (Standalone) | 21 |
| **Language** | TypeScript | 5.7 |
| **State Management** | RxJS | 7.8 |
| **Forms** | Reactive Forms | - |
| **Rich Text Editor** | Editor.js | 2.x |

### Infrastructure
| Component | Technology |
|-----------|------------|
| **Containerization** | Docker Compose |
| **CI/CD** | GitHub Actions |
| **Hosting** | Hetzner Cloud |
| **SSL** | Let's Encrypt (LettuceEncrypt) |
| **Reverse Proxy** | Nginx |

## 🏛️ Architecture

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

### Data Model

```
┌─────────────┐      ┌─────────────┐      ┌─────────────┐      ┌─────────────┐
│  Language   │      │   Course    │      │   Lesson    │      │  Exercise   │
├─────────────┤      ├─────────────┤      ├─────────────┤      ├─────────────┤
│ Id          │◀────▶│ LanguageId  │◀────▶│ CourseId    │◀────▶│ LessonId    │
│ Name        │  1:M │ Title       │  1:M │ Title       │  1:M │ Title       │
│ Code        │      │ Description │      │ Content     │      │ Type (TPH)  │
│ NativeName  │      │ OrderIndex  │      │ IsLocked    │      │ IsLocked    │
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
                                                                        │ LangCodes   │
                                                                        └─────────────┘

┌─────────────────────────────────────────────────────────────────────────────┐
│                         User Progress Tracking                              │
│                                                                              │
│  ┌──────────┐         ┌──────────────────────┐         ┌──────────┐        │
│  │   User   │◀───────▶│ UserExerciseProgress │◀───────▶│ Exercise │        │
│  │          │   M:M   │                      │   M:1   │          │        │
│  │ Id       │         │ UserId               │         │ Id       │        │
│  │ Email    │         │ ExerciseId           │         │ Points   │        │
│  │ Name     │         │ IsCompleted          │         │ Type     │        │
│  │          │         │ PointsEarned         │         │          │        │
│  │          │         │ CompletedAt          │         │          │        │
│  └──────────┘         └──────────────────────┘         └──────────┘        │
│                                                                              │
└─────────────────────────────────────────────────────────────────────────────┘
```

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
     │               │  ID Token     │               │
     │               │               │               │
     │               │ POST /auth/google-login       │
     │               │──────────────▶│               │
     │               │               │ Validate Token│
     │               │               │──────────────▶│
     │               │               │◀──────────────│
     │               │               │  Valid        │
     │               │  Set Cookie   │               │
     │               │◀──────────────│               │
     │               │  "AuthToken"  │               │
     │               │  (HttpOnly)   │               │
     │  Redirect /   │               │               │
     │◀──────────────│               │               │
     │               │               │               │
     │ Browse App    │               │               │
     │──────────────▶│  API Requests │               │
     │               │──────────────▶│               │
     │               │ (Cookie Auto) │               │
```

## 🚧 Current Development

**Branch**: `feature/exercise-system`

### Active Work

1. **Exercise System Enhancements**
   - ✅ Exercise submission and validation logic
   - ✅ User progress tracking (UserExerciseProgress entity)
   - ✅ Sequential exercise unlocking (first with lesson, rest on completion)
   - ✅ Lesson completion with 70% XP threshold

2. **User XP System**
   - ✅ Total XP calculation from completed exercises
   - ✅ Public endpoint for leaderboard integration
   - ✅ Real-time XP display in dashboard

3. **Lesson Creation Flow**
   - ✅ Fixed JSON deserialization for polymorphic exercises
   - ✅ Type discriminator positioning for System.Text.Json
   - ✅ Enum string conversion (DifficultyLevel)
   - ✅ Dynamic lesson icon assignment

4. **Performance Optimizations**
   - ✅ Editor.js debouncing (300ms) to reduce saves
   - ✅ HTTP cache headers (1-year max-age) for uploaded images
   - ✅ Content comparison to prevent duplicate saves

### Recent Fixes

- **JSON Deserialization**: Resolved 500 errors for lesson creation by positioning type discriminator first
- **Enum Serialization**: Added `JsonStringEnumConverter` for frontend compatibility
- **Model Validation**: Re-enabled model state validation (removed `SuppressModelStateInvalidFilter`)
- **LessonId Nullability**: Made optional for nested exercise creation

## 🔮 Planned Enhancements

### Short-term (Next Release)

- [ ] **Error Handling Middleware**: Centralized exception handling with user-friendly messages
- [ ] **Logging Infrastructure**: Structured logging with Serilog
- [ ] **DTO Validation**: FluentValidation for request validation
- [ ] **Leaderboard UI**: Display top learners with XP rankings
- [ ] **Daily Streak Tracking**: Calculate and display user consistency

### Medium-term

- [ ] **Email Notifications**: Lesson completion certificates, progress reports
- [ ] **Mobile Responsiveness**: Optimize UI for tablets and smartphones
- [ ] **Offline Support**: PWA with service workers for offline lesson access
- [ ] **AI-powered Hints**: Machine learning integration for exercise hints
- [ ] **Voice Recognition**: Speech-to-text for pronunciation exercises
- [ ] **Social Features**: User profiles, friend system, shared progress

### Long-term

- [ ] **Multi-language Platform**: Support for additional language pairs
- [ ] **Adaptive Learning**: AI-driven difficulty adjustment
- [ ] **Content Marketplace**: User-generated lesson sharing
- [ ] **Mobile Apps**: Native iOS and Android applications
- [ ] **Integration APIs**: Third-party LMS integration

### Known Technical Debt

- JWT debug logging should be removed before production
- File upload comment claims 10MB limit but actual limit is 100MB
- No centralized error handling middleware
- Lesson status is derived client-side (not returned by API)

## 🚀 Getting Started

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
   GOOGLE_CLIENT_ID=your-google-oauth-client-id.apps.googleusercontent.com
   GOOGLE_CLIENT_SECRET=your-google-oauth-client-secret
   JWT_SECRET=your-256-bit-secret-key-for-signing-jwt-tokens
   JWT_EXPIRATION_HOURS=24
   ```

3. **Start all services**
   ```bash
   docker compose up --build
   ```

4. **Access the application**
   - Frontend: http://localhost:4200
   - Backend API: http://localhost:8080
   - Swagger docs: http://localhost:8080/swagger
   - Health check: http://localhost:8080/health

### Local Development

#### Backend

```bash
cd backend

# Restore dependencies
dotnet restore

# Run the development server (port 8080)
dotnet watch run

# Create a new migration
dotnet ef migrations add <MigrationName> --project Database/Backend.Database.csproj

# Apply migrations
dotnet ef database update --project Database/Backend.Database.csproj
```

#### Frontend

```bash
cd frontend

# Install dependencies
npm install

# Start development server (port 4200)
npm start

# Build for production
npm run build

# Run tests
npm test
```

## 📚 API Documentation

### Authentication Endpoints

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/api/auth/google-login` | Authenticate with Google OAuth | No |
| POST | `/api/auth/logout` | Clear authentication cookie | Yes |
| GET | `/api/auth/auth-status` | Check if user is authenticated | No |
| GET | `/api/auth/is-admin` | Check if user has admin role | Yes |

### Content Endpoints

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/course` | List all courses | Yes |
| GET | `/api/course/{id}` | Get course details | Yes |
| POST | `/api/course` | Create new course | Admin/Creator |
| PUT | `/api/course/{id}` | Update course | Admin/Creator |
| DELETE | `/api/course/{id}` | Delete course | Admin/Creator |
| GET | `/api/lesson/course/{courseId}` | Get lessons by course | Yes |
| GET | `/api/lesson/{id}` | Get lesson with exercises | Yes |
| POST | `/api/lesson` | Create new lesson | Admin/Creator |
| PUT | `/api/lesson/{id}` | Update lesson | Admin/Creator |
| POST | `/api/lesson/{id}/complete` | Mark lesson complete | Yes |
| POST | `/api/exercise/{id}/submit` | Submit exercise answer | Yes |
| GET | `/api/exercise/lesson/{lessonId}/progress` | Get lesson progress | Yes |

### User Endpoints

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/user/xp` | Get current user's XP | Yes |
| GET | `/api/user/{userId}/xp` | Get any user's XP (leaderboard) | No |

### Upload Endpoints

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/api/uploads/image` | Upload image file | Yes |
| POST | `/api/uploads/file` | Upload document file | Yes |
| GET | `/api/uploads/image/{filename}` | Retrieve uploaded image | No |
| GET | `/api/uploads/file/{filename}` | Retrieve uploaded file | No |

For complete API documentation, visit the Swagger UI at `http://localhost:8080/swagger` when running the application.

## 📁 Project Structure

```
Lexiq/
├── backend/
│   ├── Controllers/              # API endpoints
│   ├── Database/
│   │   ├── Entities/             # EF Core entities
│   │   │   ├── Users/            # User, UserLanguage
│   │   │   └── Exercises/        # Exercise base + subtypes
│   │   └── Migrations/           # Database migrations
│   ├── Services/                 # Business logic layer
│   │   ├── GoogleAuthService.cs
│   │   ├── JwtService.cs
│   │   ├── LessonService.cs
│   │   ├── ExerciseService.cs
│   │   ├── ExerciseProgressService.cs
│   │   └── UserXpService.cs
│   ├── Dtos/                     # Data Transfer Objects
│   ├── Mapping/                  # Entity ↔ DTO mappings
│   ├── Middleware/               # Request pipeline middleware
│   ├── Extensions/               # Service configuration
│   └── wwwroot/uploads/          # Uploaded files
├── frontend/
│   └── src/app/
│       ├── auth/                 # Authentication services
│       ├── features/
│       │   ├── lessons/
│       │   │   ├── components/
│       │   │   │   ├── home/           # Dashboard
│       │   │   │   ├── lesson-viewer/  # Exercise player
│       │   │   │   └── lesson-editor/  # Content creator
│       │   │   ├── models/             # TypeScript interfaces
│       │   │   └── services/           # API integration
│       │   └── users/
│       │       ├── components/
│       │       │   ├── profile/        # User profile
│       │       │   └── leaderboard/    # Rankings
│       │       └── services/
│       ├── shared/
│       │   └── components/
│       │       └── editor/      # Editor.js wrapper
│       └── nav-bar/             # Navigation
├── scripts/
│   └── deploy.sh                # Automated deployment
├── .github/workflows/
│   ├── build-and-push.yml       # Docker image CI
│   └── deploy.yml               # Production deployment
├── docker-compose.yml           # Local development
├── docker-compose.prod.yml      # Production configuration
└── CLAUDE.md                    # AI assistant documentation
```

## 🤝 Contributing

We welcome contributions from the community! Here's how to get started:

1. **Fork the repository**
2. **Create a feature branch**
   ```bash
   git checkout -b feature/amazing-feature
   ```
3. **Make your changes**
   - Follow the existing code style
   - Add tests for new features
   - Update documentation as needed
4. **Commit your changes**
   ```bash
   git commit -m "Add amazing feature"
   ```
5. **Push to your fork**
   ```bash
   git push origin feature/amazing-feature
   ```
6. **Open a Pull Request**

### Development Guidelines

- Use descriptive commit messages
- Follow C# coding conventions for backend
- Follow Angular style guide for frontend
- Write unit tests for new features
- Update CLAUDE.md files when adding architectural patterns
- Ensure Docker builds succeed before submitting PR

## 📄 License

This project is currently proprietary and unlicensed. All rights reserved by the project maintainers.

For usage permissions or collaboration inquiries, please contact the development team.

## 🙏 Acknowledgments

- **ASP.NET Core Identity**: Authentication and authorization infrastructure
- **Editor.js**: Rich content editing capabilities
- **Angular Team**: Standalone components architecture
- **Entity Framework Core**: Object-relational mapping
- **Docker**: Containerization and deployment
- **GitHub Actions**: CI/CD automation
- **Hetzner**: Cloud hosting infrastructure

## 📧 Contact

For questions, suggestions, or collaboration opportunities:

- **Project Repository**: [https://github.com/Ghostdog02/Lexiq](https://github.com/Ghostdog02/Lexiq)
- **Issues**: [GitHub Issues](https://github.com/Ghostdog02/Lexiq/issues)

---

**Built with ❤️ for language learners and tech enthusiasts**
