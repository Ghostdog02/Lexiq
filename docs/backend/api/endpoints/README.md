# API Endpoints Reference

Comprehensive documentation for all Lexiq API endpoints.

## Quick Navigation

### Core Features
- **[Authentication](auth.md)** — Google OAuth login, logout, session management
- **[Lessons](lessons.md)** — Lesson CRUD, completion, unlocking
- **[Exercises](exercises.md)** — Exercise types, answer submission, progress tracking
- **[Leaderboard & Progress](leaderboard.md)** — Rankings, XP, levels, streaks

### Content Management
- **Languages** — Language CRUD (Admin only)
- **Courses** — Course CRUD (Admin/ContentCreator)
- **User Languages** — Language enrollment

### Admin & Utilities
- **User Management** — User CRUD, role assignment (Admin only)
- **Uploads** — File upload/download (images, audio, video)
- **Avatars** — User avatar management

---

## Endpoint Overview

### Authentication (`/api/auth`)

| Method | Endpoint | Description | Auth | Roles |
|--------|----------|-------------|------|-------|
| POST | `/google-login` | Login with Google OAuth | ❌ No | - |
| POST | `/logout` | Logout and clear cookie | ❌ No | - |
| GET | `/auth-status` | Get current user info | ✅ Yes | Any |
| GET | `/is-admin` | Check if user is admin | ✅ Yes | Any |

**[Full Documentation →](auth.md)**

---

### Lessons (`/api/lessons`)

| Method | Endpoint | Description | Auth | Roles |
|--------|----------|-------------|------|-------|
| GET | `/course/{courseId}` | List lessons for course | ✅ Yes | Any |
| GET | `/{lessonId}` | Get lesson details | ✅ Yes | Any |
| GET | `/{lessonId}/next` | Get next lesson | ✅ Yes | Any |
| POST | `/{lessonId}/complete` | Complete lesson | ✅ Yes | Any |
| POST | `/` | Create lesson | ✅ Yes | Admin, ContentCreator |
| PUT | `/{lessonId}` | Update lesson | ✅ Yes | Admin, ContentCreator |
| DELETE | `/{lessonId}` | Delete lesson | ✅ Yes | Admin, ContentCreator |
| POST | `/{lessonId}/unlock` | Manually unlock lesson | ✅ Yes | Admin |

**[Full Documentation →](lessons.md)**

---

### Exercises (`/api/exercises`)

| Method | Endpoint | Description | Auth | Roles |
|--------|----------|-------------|------|-------|
| GET | `/lesson/{lessonId}` | List exercises for lesson | ✅ Yes | Any |
| GET | `/{exerciseId}` | Get exercise details | ✅ Yes | Any |
| POST | `/{exerciseId}/submit` | Submit answer | ✅ Yes | Any |
| GET | `/lesson/{lessonId}/progress` | Get progress summary | ✅ Yes | Any |
| GET | `/lesson/{lessonId}/submissions` | Get submission history | ✅ Yes | Any |
| POST | `/` | Create exercise | ✅ Yes | Admin, ContentCreator |
| PUT | `/{exerciseId}` | Update exercise | ✅ Yes | Admin, ContentCreator |
| DELETE | `/{exerciseId}` | Delete exercise | ✅ Yes | Admin, ContentCreator |

**Exercise Types:**
- `MultipleChoice` — Select from options
- `FillInBlank` — Complete missing words
- `Listening` — Transcribe audio
- `Translation` — Translate sentence

**[Full Documentation →](exercises.md)**

---

### Leaderboard & Progress

| Method | Endpoint | Description | Auth | Roles |
|--------|----------|-------------|------|-------|
| GET | `/leaderboard` | Get leaderboard rankings | ❌ No | - |
| GET | `/user/xp` | Get current user's XP | ✅ Yes | Any |
| GET | `/user/{userId}/xp` | Get any user's XP | ❌ No | - |

**Time Frames:** `Weekly`, `Monthly`, `AllTime`

**[Full Documentation →](leaderboard.md)**

---

### Languages (`/api/languages`)

| Method | Endpoint | Description | Auth | Roles |
|--------|----------|-------------|------|-------|
| GET | `/` | List all languages | ❌ No | - |
| POST | `/` | Create language | ✅ Yes | Admin |
| PUT | `/{id}` | Update language | ✅ Yes | Admin |
| DELETE | `/{id}` | Delete language | ✅ Yes | Admin |

**Example Response:**
```json
[
  { "id": "lang1", "name": "Italian", "code": "it" },
  { "id": "lang2", "name": "Spanish", "code": "es" }
]
```

---

### Courses (`/api/courses`)

| Method | Endpoint | Description | Auth | Roles |
|--------|----------|-------------|------|-------|
| GET | `/` | List all courses | ✅ Yes | Any |
| GET | `/{id}` | Get course details | ✅ Yes | Any |
| POST | `/` | Create course | ✅ Yes | Admin, ContentCreator |
| PUT | `/{id}` | Update course | ✅ Yes | Admin, ContentCreator |
| DELETE | `/{id}` | Delete course | ✅ Yes | Admin, ContentCreator |

**Example Response:**
```json
{
  "id": "course1",
  "languageId": "lang1",
  "title": "Italian for Beginners",
  "description": "Start your Italian journey",
  "difficulty": "Beginner",
  "orderIndex": 0
}
```

---

### User Languages (`/api/userLanguages`)

| Method | Endpoint | Description | Auth | Roles |
|--------|----------|-------------|------|-------|
| GET | `/user/{userId}` | Get user's enrolled languages | ✅ Yes | Any |
| POST | `/enroll` | Enroll in language | ✅ Yes | Any |
| DELETE | `/unenroll` | Unenroll from language | ✅ Yes | Any |

**Enroll Request:**
```json
{
  "userId": "user1",
  "languageId": "lang1"
}
```

---

### User Management (`/api/userManagement`)

| Method | Endpoint | Description | Auth | Roles |
|--------|----------|-------------|------|-------|
| GET | `/users` | List all users | ✅ Yes | Admin |
| GET | `/users/{id}` | Get user details | ✅ Yes | Admin |
| POST | `/roles` | Assign role to user | ✅ Yes | Admin |
| DELETE | `/roles` | Remove role from user | ✅ Yes | Admin |

**Assign Role Request:**
```json
{
  "userId": "user1",
  "role": "ContentCreator"
}
```

---

### Uploads (`/api/uploads`)

| Method | Endpoint | Description | Auth | Roles |
|--------|----------|-------------|------|-------|
| POST | `/{fileType}` | Upload file | ✅ Yes | Any |
| POST | `/any` | Upload file (any type) | ✅ Yes | Any |
| GET | `/{fileType}/{filename}` | Get uploaded file | ✅ Yes | Any |
| GET | `/list/{fileType}` | List files by type | ✅ Yes | Any |

**File Types:** `image`, `audio`, `video`, `document`

**Max Size:** 100 MB

**Upload Example:**
```bash
curl -X POST http://localhost:8080/api/uploads/image \
  -F "file=@photo.jpg" \
  -b "AuthToken=..."
```

**Response:**
```json
{
  "fileName": "photo_abc123.jpg",
  "url": "/static/uploads/image/photo_abc123.jpg",
  "fileType": "image"
}
```

---

### Avatars (`/api/user`)

| Method | Endpoint | Description | Auth | Roles |
|--------|----------|-------------|------|-------|
| GET | `/{userId}/avatar` | Get user avatar | ❌ No | - |
| PUT | `/avatar` | Upload custom avatar | ✅ Yes | Any |

**Notes:**
- Google profile picture auto-downloaded on first login
- Users can override with custom avatar via PUT
- Stored as varbinary(max) in `UserAvatars` table
- Served with 24h cache headers

**Upload Example:**
```bash
curl -X PUT http://localhost:8080/api/user/avatar \
  -F "file=@avatar.png" \
  -b "AuthToken=..."
```

---

## Common Patterns

### Pagination

Currently not implemented. All list endpoints return full results.

### Sorting

List endpoints return results ordered by:
- Content entities: `orderIndex` (ascending)
- Leaderboard: `totalXp` (descending)
- Timestamp fields: `createdAt` or `completedAt` (descending)

### Error Handling

All endpoints return standardized error responses:

```json
{
  "message": "Human-readable error message",
  "statusCode": 400,
  "detail": null
}
```

Stack traces (`detail`) only included in Development environment.

See **[Error Handling Documentation](../error-handling.md)** for details.

### Authentication

Most endpoints require authentication via JWT cookie:

```bash
# ✅ CORRECT - Include cookie
curl -b "AuthToken=<jwt>" http://localhost:8080/api/lessons

# ❌ WRONG - Missing cookie
curl http://localhost:8080/api/lessons  # Returns 401
```

JavaScript requests must use `credentials: 'include'` or `withCredentials: true`.

See **[Authentication Documentation](../authentication.md)** for details.

### Polymorphic Types

Exercises use type discrimination:

```json
{
  "type": "MultipleChoice",
  "id": "ex1",
  "question": "What is 'hello'?",
  "options": [...]
}
```

**Critical:** Type discriminator (`"type"`) MUST be first property in JSON.

See **[Exercises Documentation](exercises.md)** for type-specific examples.

---

## OpenAPI / Swagger

**OpenAPI JSON:** `GET /openapi/v1.json` (Development only)

Import into:
- **Postman:** File → Import → OpenAPI
- **Insomnia:** Import/Export → Import Data → OpenAPI
- **VS Code REST Client:** Use `.http` files with OpenAPI schema

**Example:**
```bash
# Download OpenAPI spec
curl http://localhost:8080/openapi/v1.json > lexiq-api.json

# Import into Postman
# File → Import → lexiq-api.json
```

---

## Testing Endpoints

### Using cURL

```bash
# Login
curl -X POST http://localhost:8080/api/auth/google-login \
  -H "Content-Type: application/json" \
  -d '{"idToken": "<google-token>"}'

# Extract cookie from response headers
# Set-Cookie: AuthToken=eyJhbGc...

# Use cookie in subsequent requests
curl -b "AuthToken=eyJhbGc..." \
  http://localhost:8080/api/lessons/course/abc123
```

### Using Postman

1. Send login request to `/api/auth/google-login`
2. Postman automatically stores `AuthToken` cookie
3. Subsequent requests include cookie automatically
4. Check cookies: Postman → Cookies → manage cookies for `localhost:8080`

### Using JavaScript (Fetch)

```javascript
// Login
const loginRes = await fetch('http://localhost:8080/api/auth/google-login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ idToken: googleIdToken }),
  credentials: 'include'  // ← Required for cookies
});

// Subsequent requests
const lessonsRes = await fetch('http://localhost:8080/api/lessons/course/abc123', {
  credentials: 'include'  // ← Required for cookies
});

const lessons = await lessonsRes.json();
```

### Using JavaScript (Axios)

```javascript
// Configure axios to always include cookies
axios.defaults.withCredentials = true;

// Login
await axios.post('http://localhost:8080/api/auth/google-login', {
  idToken: googleIdToken
});

// Subsequent requests automatically include cookie
const { data: lessons } = await axios.get(
  'http://localhost:8080/api/lessons/course/abc123'
);
```

---

## Rate Limiting

Not currently implemented.

Future considerations:
- Auth endpoints: 5 requests/minute
- Write operations: 30 requests/minute
- Read operations: 100 requests/minute

---

## Versioning

API is currently unversioned (`/api/...`).

Future: May use `/api/v1/...` or header-based versioning.

---

## Support

- **Error Handling:** [../error-handling.md](../error-handling.md)
- **Authentication:** [../authentication.md](../authentication.md)
- **Backend Patterns:** [../../../backend/CLAUDE.md](../../../backend/CLAUDE.md)
- **Tests:** [../../../backend/Tests/CLAUDE.md](../../../backend/Tests/CLAUDE.md)
