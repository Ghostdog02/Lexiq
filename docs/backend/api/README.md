# Lexiq Backend API Documentation

Comprehensive API reference for the Lexiq language learning platform backend.

## Quick Links

- **[Endpoints Reference](endpoints/)** — Complete endpoint documentation by category
- **[Error Handling](error-handling.md)** — Error response format, HTTP status codes, exception mapping
- **[Authentication & Authorization](authentication.md)** — JWT authentication, Google OAuth, role-based access control

## API Overview

**Base URL:** `http://localhost:8080/api` (development) | `https://api.lexiqlanguage.eu/api` (production)

**Authentication:** JWT token in HttpOnly cookie (`AuthToken`)

**Content-Type:** `application/json`

## Getting Started

### 1. Authentication

All endpoints require authentication unless marked `[AllowAnonymous]`.

**Login:**
```bash
curl -X POST http://localhost:8080/api/auth/google-login \
  -H "Content-Type: application/json" \
  -d '{"idToken": "<google-oauth-token>"}'
```

**Response:**
```json
{
  "message": "Login successful",
  "user": {
    "id": "abc123",
    "email": "user@example.com",
    "userName": "johndoe"
  }
}
```

The JWT token is automatically stored in an HttpOnly cookie (`AuthToken`) and sent with all subsequent requests.

### 2. Making Requests

Include credentials (cookies) in all requests:

**JavaScript (Fetch API):**
```javascript
fetch('http://localhost:8080/api/lessons', {
  credentials: 'include'  // ← Required for cookie auth
})
```

**JavaScript (Axios):**
```javascript
axios.get('http://localhost:8080/api/lessons', {
  withCredentials: true  // ← Required for cookie auth
})
```

**cURL:**
```bash
curl -b "AuthToken=<token>" http://localhost:8080/api/lessons
```

### 3. Error Handling

All errors return a standardized JSON response:

```json
{
  "message": "Human-readable error message",
  "statusCode": 404,
  "detail": null
}
```

See [Error Handling](error-handling.md) for comprehensive error documentation.

## Endpoints by Category

### Authentication

| Method | Endpoint | Description | Auth | Roles |
|--------|----------|-------------|------|-------|
| POST | `/auth/google-login` | Login with Google OAuth | ❌ No | - |
| POST | `/auth/logout` | Logout (clear cookie) | ❌ No | - |
| GET | `/auth/auth-status` | Get current user info | ✅ Yes | Any |
| GET | `/auth/is-admin` | Check if user is admin | ✅ Yes | Any |

### Languages

| Method | Endpoint | Description | Auth | Roles |
|--------|----------|-------------|------|-------|
| GET | `/languages` | List all languages | ❌ No | - |
| POST | `/languages` | Create language | ✅ Yes | Admin |
| PUT | `/languages/{id}` | Update language | ✅ Yes | Admin |
| DELETE | `/languages/{id}` | Delete language | ✅ Yes | Admin |

### Courses

| Method | Endpoint | Description | Auth | Roles |
|--------|----------|-------------|------|-------|
| GET | `/courses` | List all courses | ✅ Yes | Any |
| GET | `/courses/{id}` | Get course details | ✅ Yes | Any |
| POST | `/courses` | Create course | ✅ Yes | Admin, ContentCreator |
| PUT | `/courses/{id}` | Update course | ✅ Yes | Admin, ContentCreator |
| DELETE | `/courses/{id}` | Delete course | ✅ Yes | Admin, ContentCreator |

### Lessons

| Method | Endpoint | Description | Auth | Roles |
|--------|----------|-------------|------|-------|
| GET | `/lessons/course/{courseId}` | List lessons for course | ✅ Yes | Any |
| GET | `/lessons/{lessonId}` | Get lesson details | ✅ Yes | Any |
| GET | `/lessons/{lessonId}/next` | Get next lesson | ✅ Yes | Any |
| POST | `/lessons` | Create lesson | ✅ Yes | Admin, ContentCreator |
| POST | `/lessons/{lessonId}/complete` | Complete lesson (unlocks next) | ✅ Yes | Any |
| POST | `/lessons/{lessonId}/unlock` | Manually unlock lesson | ✅ Yes | Admin |
| PUT | `/lessons/{lessonId}` | Update lesson | ✅ Yes | Admin, ContentCreator |
| DELETE | `/lessons/{lessonId}` | Delete lesson | ✅ Yes | Admin, ContentCreator |

### Exercises

| Method | Endpoint | Description | Auth | Roles |
|--------|----------|-------------|------|-------|
| GET | `/exercises/lesson/{lessonId}` | List exercises for lesson | ✅ Yes | Any |
| GET | `/exercises/{exerciseId}` | Get exercise details | ✅ Yes | Any |
| POST | `/exercises` | Create exercise | ✅ Yes | Admin, ContentCreator |
| POST | `/exercises/{exerciseId}/submit` | Submit answer | ✅ Yes | Any |
| PUT | `/exercises/{exerciseId}` | Update exercise | ✅ Yes | Admin, ContentCreator |
| DELETE | `/exercises/{exerciseId}` | Delete exercise | ✅ Yes | Admin, ContentCreator |

### Progress & Leaderboard

| Method | Endpoint | Description | Auth | Roles |
|--------|----------|-------------|------|-------|
| GET | `/user/xp` | Get current user's XP | ✅ Yes | Any |
| GET | `/user/{userId}/xp` | Get user's XP (public) | ❌ No | - |
| GET | `/leaderboard?timeFrame={timeFrame}` | Get leaderboard | ❌ No | - |

**Leaderboard Time Frames:** `Weekly`, `Monthly`, `AllTime`

### User Management

| Method | Endpoint | Description | Auth | Roles |
|--------|----------|-------------|------|-------|
| GET | `/userManagement/users` | List all users | ✅ Yes | Admin |
| GET | `/userManagement/users/{id}` | Get user details | ✅ Yes | Admin |
| POST | `/userManagement/roles` | Assign role to user | ✅ Yes | Admin |
| DELETE | `/userManagement/roles` | Remove role from user | ✅ Yes | Admin |

### Uploads

| Method | Endpoint | Description | Auth | Roles |
|--------|----------|-------------|------|-------|
| POST | `/uploads/{fileType}` | Upload file | ✅ Yes | Any |
| POST | `/uploads/any` | Upload file (any type) | ✅ Yes | Any |
| GET | `/uploads/{fileType}/{filename}` | Get uploaded file | ✅ Yes | Any |
| GET | `/uploads/list/{fileType}` | List files by type | ✅ Yes | Any |

**File Types:** `image`, `audio`, `video`, `document`

**Max file size:** 100 MB

### Avatars

| Method | Endpoint | Description | Auth | Roles |
|--------|----------|-------------|------|-------|
| GET | `/user/{userId}/avatar` | Get user avatar | ❌ No | - |
| PUT | `/user/avatar` | Upload custom avatar | ✅ Yes | Any |

## Common Patterns

### Pagination

Currently not implemented. All list endpoints return full results.

**Future:** Query parameters `?page=1&pageSize=20` (planned)

### Sorting

List endpoints return results ordered by `OrderIndex` (ascending) for content entities.

Leaderboard returns results ordered by XP (descending).

### Filtering

Limited filtering available:
- `/lessons/course/{courseId}` — filter lessons by course
- `/exercises/lesson/{lessonId}` — filter exercises by lesson
- `/leaderboard?timeFrame={timeFrame}` — filter by time period

### Polymorphic Types

Exercise entities use type discrimination for subtypes:

**Response:**
```json
{
  "type": "MultipleChoice",
  "id": "abc123",
  "question": "What is 'hello' in Italian?",
  "options": [
    {"id": "opt1", "text": "Ciao", "isCorrect": true},
    {"id": "opt2", "text": "Buongiorno", "isCorrect": false}
  ]
}
```

**Types:** `MultipleChoice`, `FillInBlank`, `Listening`, `Translation`

**Critical:** Type discriminator (`"type"`) **must** be the first property in JSON responses.

## Request/Response Examples

### Submit Exercise Answer

**Request:**
```http
POST /api/exercises/abc123/submit HTTP/1.1
Host: localhost:8080
Content-Type: application/json
Cookie: AuthToken=eyJhbGc...

{
  "answer": "Ciao"
}
```

**Response (Correct):**
```json
{
  "isCorrect": true,
  "correctAnswer": "Ciao",
  "pointsEarned": 10,
  "explanation": "Great job! 'Ciao' is an informal greeting.",
  "nextExerciseId": "def456"
}
```

**Response (Incorrect):**
```json
{
  "isCorrect": false,
  "correctAnswer": "Ciao",
  "pointsEarned": 0,
  "explanation": "'Buongiorno' means 'good morning'. Try 'Ciao' for 'hello'.",
  "nextExerciseId": null
}
```

### Complete Lesson

**Request:**
```http
POST /api/lessons/lesson123/complete HTTP/1.1
Host: localhost:8080
Cookie: AuthToken=eyJhbGc...
```

**Response (Success):**
```json
{
  "isComplete": true,
  "completedExercises": 8,
  "totalExercises": 10,
  "percentComplete": 80,
  "nextLessonId": "lesson124",
  "nextLessonUnlocked": true
}
```

**Response (Insufficient XP):**
```json
{
  "isComplete": false,
  "completedExercises": 6,
  "totalExercises": 10,
  "percentComplete": 60,
  "nextLessonId": null,
  "nextLessonUnlocked": false,
  "message": "Complete at least 70% of exercises to unlock next lesson."
}
```

### Get Leaderboard

**Request:**
```http
GET /api/leaderboard?timeFrame=Weekly HTTP/1.1
Host: localhost:8080
```

**Response:**
```json
{
  "entries": [
    {
      "rank": 1,
      "userId": "user1",
      "userName": "alice",
      "totalXp": 1250,
      "level": 5,
      "streak": 7,
      "avatarUrl": "/api/user/user1/avatar"
    },
    {
      "rank": 2,
      "userId": "user2",
      "userName": "bob",
      "totalXp": 980,
      "level": 4,
      "streak": 3,
      "avatarUrl": null
    }
  ],
  "currentUserEntry": {
    "rank": 15,
    "userId": "currentUser",
    "userName": "you",
    "totalXp": 450,
    "level": 3,
    "streak": 2,
    "avatarUrl": "/api/user/currentUser/avatar"
  }
}
```

## Rate Limiting

Not currently implemented.

**Future:** Rate limiting may be added to prevent abuse:
- Auth endpoints: 5 requests/minute
- Write operations: 30 requests/minute
- Read operations: 100 requests/minute

## Versioning

API is currently unversioned (`/api/...`).

**Future:** Versioning strategy TBD (likely `/api/v1/...` or header-based)

## CORS Configuration

**Development:**
- Frontend: `http://localhost:4200`
- Backend: `http://localhost:8080`
- CORS enabled with `AllowCredentials()` for cookie auth

**Production:**
- Frontend: `https://lexiqlanguage.eu`
- Backend: `https://api.lexiqlanguage.eu`
- nginx handles CORS headers
- `SameSite=Lax` for CSRF protection

## Health Check

**Endpoint:** `GET /health`

**Response (Healthy):**
```
HTTP/1.1 200 OK
Content-Type: text/plain

Healthy
```

Used by Docker healthchecks and monitoring tools.

## OpenAPI / Swagger

**Development only:** OpenAPI JSON available at `/openapi/v1.json`

Swagger UI not currently configured.

**Future:** May add Swagger UI at `/swagger` for interactive API documentation.

## Support

For API issues or questions:
- Backend documentation: [`/backend/CLAUDE.md`](../../../backend/CLAUDE.md)
- Issue tracker: [GitHub Issues](https://github.com/anthropics/claude-code/issues)
- Tests documentation: [`/backend/Tests/CLAUDE.md`](../../../backend/Tests/CLAUDE.md)
