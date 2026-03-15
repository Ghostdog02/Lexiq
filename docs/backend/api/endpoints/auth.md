# Authentication Endpoints

Base path: `/api/auth`

All authentication-related operations including Google OAuth login, logout, and session status.

## Endpoints

### POST /api/auth/google-login

Authenticate user with Google OAuth and receive JWT token in HttpOnly cookie.

**Authentication:** Not required

**Request:**
```http
POST /api/auth/google-login HTTP/1.1
Host: localhost:8080
Content-Type: application/json

{
  "idToken": "eyJhbGciOiJSUzI1NiIsImtpZCI6IjY4YTk4..."
}
```

**Request Body:**
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `idToken` | string | Yes | Google OAuth ID token obtained from Google Sign-In |

**Success Response (200 OK):**
```json
{
  "message": "Login successful",
  "user": {
    "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "email": "user@example.com",
    "userName": "johndoe"
  }
}
```

**Response Headers:**
```
Set-Cookie: AuthToken=eyJhbGciOiJIUzI1NiIs...; Path=/; HttpOnly; SameSite=Lax; Expires=Sat, 16 Mar 2026 16:00:00 GMT
```

**Error Responses:**

**400 Bad Request** - Invalid Google token:
```json
{
  "message": "Invalid Google ID token",
  "statusCode": 400,
  "detail": null
}
```

**500 Internal Server Error** - Server error:
```json
{
  "message": "An unexpected error occurred. Please try again later.",
  "statusCode": 500,
  "detail": null
}
```

**Example:**

```bash
# Using cURL
curl -X POST http://localhost:8080/api/auth/google-login \
  -H "Content-Type: application/json" \
  -d '{"idToken": "eyJhbGciOiJSUzI1NiIsImtpZCI6IjY4YTk4..."}'

# Using JavaScript (Fetch)
const response = await fetch('http://localhost:8080/api/auth/google-login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ idToken: googleIdToken }),
  credentials: 'include'  // Include cookies
});

const data = await response.json();
console.log(data.user);
```

---

### POST /api/auth/logout

Logout user by clearing the authentication cookie.

**Authentication:** Not required (but cookie must exist to be cleared)

**Request:**
```http
POST /api/auth/logout HTTP/1.1
Host: localhost:8080
Cookie: AuthToken=eyJhbGciOiJIUzI1NiIs...
```

**Success Response (200 OK):**
```json
{
  "message": "Logout successful"
}
```

**Response Headers:**
```
Set-Cookie: AuthToken=; Path=/; HttpOnly; SameSite=Lax; Expires=Thu, 01 Jan 1970 00:00:00 GMT
```

**Example:**

```bash
# Using cURL
curl -X POST http://localhost:8080/api/auth/logout \
  -b "AuthToken=eyJhbGciOiJIUzI1NiIs..."

# Using JavaScript (Fetch)
await fetch('http://localhost:8080/api/auth/logout', {
  method: 'POST',
  credentials: 'include'
});
```

---

### GET /api/auth/auth-status

Get current authentication status and user information.

**Authentication:** Required

**Request:**
```http
GET /api/auth/auth-status HTTP/1.1
Host: localhost:8080
Cookie: AuthToken=eyJhbGciOiJIUzI1NiIs...
```

**Success Response (200 OK):**
```json
{
  "isAuthenticated": true,
  "user": {
    "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "email": "user@example.com",
    "userName": "johndoe",
    "totalPointsEarned": 1250,
    "registrationDate": "2026-01-15T10:30:00Z",
    "lastLoginDate": "2026-03-15T14:20:00Z"
  },
  "roles": ["Student", "ContentCreator"]
}
```

**Response Fields:**
| Field | Type | Description |
|-------|------|-------------|
| `isAuthenticated` | boolean | Always `true` if request succeeds (401 if false) |
| `user.id` | string (GUID) | Unique user identifier |
| `user.email` | string | User's email address |
| `user.userName` | string | Display name (cleaned from Google name) |
| `user.totalPointsEarned` | integer | Total XP earned across all exercises |
| `user.registrationDate` | string (ISO 8601) | When user first logged in |
| `user.lastLoginDate` | string (ISO 8601) | Most recent login timestamp |
| `roles` | string[] | User's roles (Student, ContentCreator, Admin) |

**Error Responses:**

**401 Unauthorized** - Not logged in or token expired:
```json
{
  "message": "You are not authorized to perform this action.",
  "statusCode": 401,
  "detail": null
}
```

**Example:**

```bash
# Using cURL
curl http://localhost:8080/api/auth/auth-status \
  -b "AuthToken=eyJhbGciOiJIUzI1NiIs..."

# Using JavaScript (Fetch)
const response = await fetch('http://localhost:8080/api/auth/auth-status', {
  credentials: 'include'
});

if (response.ok) {
  const data = await response.json();
  console.log(`Welcome, ${data.user.userName}!`);
  console.log(`You have ${data.user.totalPointsEarned} XP`);
}
```

---

### GET /api/auth/is-admin

Check if current user has Admin role.

**Authentication:** Required

**Request:**
```http
GET /api/auth/is-admin HTTP/1.1
Host: localhost:8080
Cookie: AuthToken=eyJhbGciOiJIUzI1NiIs...
```

**Success Response (200 OK):**
```json
{
  "isAdmin": true
}
```

or

```json
{
  "isAdmin": false
}
```

**Error Responses:**

**401 Unauthorized** - Not logged in:
```json
{
  "message": "You are not authorized to perform this action.",
  "statusCode": 401,
  "detail": null
}
```

**Example:**

```bash
# Using cURL
curl http://localhost:8080/api/auth/is-admin \
  -b "AuthToken=eyJhbGciOiJIUzI1NiIs..."

# Using JavaScript (Fetch)
const response = await fetch('http://localhost:8080/api/auth/is-admin', {
  credentials: 'include'
});

const { isAdmin } = await response.json();

if (isAdmin) {
  console.log('User has admin privileges');
}
```

---

## Authentication Flow

### First-Time User

```
1. User clicks "Sign in with Google" on frontend
   ↓
2. Google OAuth popup opens → user signs in → returns idToken
   ↓
3. Frontend: POST /api/auth/google-login { idToken }
   ↓
4. Backend validates token with Google
   ↓
5. Backend creates User record (if first login)
   ↓
6. Backend assigns "Student" role (default)
   ↓
7. Backend generates JWT, sets AuthToken cookie
   ↓
8. Frontend receives user data + cookie
   ↓
9. All subsequent requests include cookie automatically
```

### Returning User

```
1. User clicks "Sign in with Google" on frontend
   ↓
2. Google OAuth popup → returns idToken
   ↓
3. Frontend: POST /api/auth/google-login { idToken }
   ↓
4. Backend validates token with Google
   ↓
5. Backend finds existing User record
   ↓
6. Backend updates LastLoginDate
   ↓
7. Backend generates JWT, sets AuthToken cookie
   ↓
8. Frontend receives user data + cookie
```

### Session Check on Page Load

```
1. User visits app (page load/refresh)
   ↓
2. Frontend: GET /api/auth/auth-status
   ↓
3. If 200 OK → user is logged in, show app
   ↓
4. If 401 Unauthorized → show login button
```

## Security Notes

### Token Storage

- JWT stored in **HttpOnly cookie** (not localStorage/sessionStorage)
- Prevents XSS attacks (JavaScript cannot access token)
- Cookie sent automatically with requests (`credentials: 'include'`)

### Token Expiration

- Default: 24 hours (configurable via `JWT_EXPIRATION_HOURS`)
- No automatic refresh - user must re-login when expired
- Backend returns 401 when token expires

### CORS & Cookies

**Development:**
- Frontend: `http://localhost:4200`
- Backend: `http://localhost:8080`
- CORS configured with `AllowCredentials()`
- Requests must use `credentials: 'include'` or `withCredentials: true`

**Production:**
- Frontend: `https://lexiqlanguage.eu`
- Backend: `https://api.lexiqlanguage.eu`
- SameSite=Lax prevents CSRF
- Secure flag enabled (HTTPS only)

### Role Assignment

Default role for new users: **Student**

To assign additional roles:
1. Admin calls `POST /api/userManagement/roles`
2. Roles: `Student`, `ContentCreator`, `Admin`
3. Roles are hierarchical (Admin has all permissions)

## Common Issues

### "401 Unauthorized" after database reset

**Problem:** JWT contains old user ID from before DB reset

**Solution:**
1. Clear browser cookies (DevTools → Application → Cookies)
2. Re-login via Google OAuth

### Cookie not being sent with requests

**Problem:** Missing `credentials: 'include'` in fetch requests

**Solution:**
```javascript
// ✅ CORRECT
fetch('http://localhost:8080/api/lessons', {
  credentials: 'include'
});

// ❌ WRONG
fetch('http://localhost:8080/api/lessons');
```

### CORS error in development

**Problem:** Backend not configured for frontend origin

**Solution:**
- Ensure `ANGULAR_PORT` env var set to `http://localhost:4200`
- Backend CORS policy uses this value
- Restart backend after changing env vars

### Token expired

**Problem:** JWT older than 24 hours

**Solution:**
- No automatic refresh implemented
- User must re-login via Google OAuth
- Consider session persistence or refresh tokens in future
