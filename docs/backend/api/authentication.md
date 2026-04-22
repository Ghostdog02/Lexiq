# Authentication & Authorization

The Lexiq API uses JWT (JSON Web Token) authentication stored in HTTP-only cookies, combined with Google OAuth for user login.

## Authentication Flow

### 1. Google OAuth Login

```
┌─────────┐          ┌──────────┐          ┌─────────┐          ┌──────────┐
│ Client  │          │ Backend  │          │ Google  │          │ Database │
└────┬────┘          └────┬─────┘          └────┬────┘          └────┬─────┘
     │                    │                     │                     │
     │ POST /api/auth/    │                     │                     │
     │ google-login       │                     │                     │
     │ {idToken: "..."}   │                     │                     │
     ├───────────────────>│                     │                     │
     │                    │ Validate token      │                     │
     │                    ├────────────────────>│                     │
     │                    │<────────────────────┤                     │
     │                    │ {sub, email, name}  │                     │
     │                    │                     │                     │
     │                    │  Find or create user│                     │
     │                    ├─────────────────────┼────────────────────>│
     │                    │<────────────────────┼─────────────────────┤
     │                    │ User entity         │                     │
     │                    │                     │                     │
     │                    │ Generate JWT        │                     │
     │                    │ (HS256, 24h exp)    │                     │
     │                    │                     │                     │
     │ Set-Cookie:        │                     │                     │
     │ AuthToken=<jwt>    │                     │                     │
     │ HttpOnly, Lax      │                     │                     │
     │<───────────────────┤                     │                     │
     │                    │                     │                     │
```

**Endpoint:** `POST /api/auth/google-login`

**Request:**
```json
{
  "idToken": "eyJhbGciOiJSUzI1NiIsImtpZCI6IjY4YTk4..."
}
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

**Response Headers:**
```
Set-Cookie: AuthToken=<jwt>; Path=/; HttpOnly; SameSite=Lax; Expires=<24h from now>
```

### 2. Authenticated Requests

All subsequent requests automatically include the `AuthToken` cookie:

```
┌─────────┐          ┌──────────┐          ┌──────────┐
│ Client  │          │ Backend  │          │ Database │
└────┬────┘          └────┬─────┘          └────┬─────┘
     │                    │                     │
     │ GET /api/lessons/  │                     │
     │ Cookie: AuthToken  │                     │
     ├───────────────────>│                     │
     │                    │                     │
     │                    │ Verify JWT          │
     │                    │ Extract user ID     │
     │                    │                     │
     │                    │ Load user entity    │
     │                    ├────────────────────>│
     │                    │<────────────────────┤
     │                    │                     │
     │                    │ Execute controller  │
     │                    │                     │
     │<───────────────────┤                     │
     │ Lesson data        │                     │
     │                    │                     │
```

### 3. Logout

**Endpoint:** `POST /api/auth/logout`

**Response Headers:**
```
Set-Cookie: AuthToken=; Path=/; HttpOnly; SameSite=Lax; Expires=Thu, 01 Jan 1970 00:00:00 GMT
```

The cookie is expired (set to Unix epoch), effectively deleting it from the browser.

## JWT Token Structure

### Header
```json
{
  "alg": "HS256",
  "typ": "JWT"
}
```

### Payload (Claims)
```json
{
  "sub": "abc123",                              // User ID (maps to ClaimTypes.NameIdentifier)
  "email": "user@example.com",                  // User email
  "name": "John Doe",                           // User display name
  "iss": "lexiq-api",                           // Issuer (from JWT_ISSUER env var)
  "aud": "lexiq-frontend",                      // Audience (from JWT_AUDIENCE env var)
  "exp": 1710691200,                            // Expiration (Unix timestamp, 24h from issue)
  "iat": 1710604800,                            // Issued at (Unix timestamp)
  "role": ["Student", "ContentCreator"]         // User roles
}
```

### Signature

HMAC-SHA256 signature using the secret from `JWT_SECRET` environment variable.

## Cookie Configuration

| Property | Value | Purpose |
|----------|-------|---------|
| **Name** | `AuthToken` | Cookie identifier |
| **HttpOnly** | `true` | Prevents JavaScript access (XSS protection) |
| **SameSite** | `Lax` | CSRF protection, allows navigation |
| **Secure** | Auto (true if HTTPS) | Only sent over HTTPS in production |
| **Path** | `/` | Available to all routes |
| **Expiration** | 24 hours | Default (configurable via `JWT_EXPIRATION_HOURS`) |

### Cross-Origin Setup (Development)

**Frontend:** `localhost:4200` → **Backend:** `localhost:8080`

- Nginx proxies `/api/*` requests to backend (makes them same-origin)
- CORS enabled with `AllowCredentials()` + specific origin
- Frontend uses `withCredentials: true` in HTTP requests
- `SameSite=Lax` works with the proxy setup

## Middleware Pipeline

```csharp
app.UseErrorHandling();      // Global error handler
app.UseRouting();
app.UseCors("AllowAngular");
app.UseAuthentication();     // ← Validates JWT, populates User claims
app.UseUserContext();        // ← Loads User entity from database
app.UseAuthorization();      // ← Checks role requirements
app.MapControllers();
```

### UseAuthentication()

- Extracts JWT from `AuthToken` cookie
- Validates signature using `JWT_SECRET`
- Checks expiration
- Populates `HttpContext.User` with claims
- **Critical:** ASP.NET Core maps JWT `sub` claim → `ClaimTypes.NameIdentifier`
  - Always use `ClaimTypes.NameIdentifier`, NOT `JwtRegisteredClaimNames.Sub`

### UserContextMiddleware

- Registered after `UseAuthentication()`, before `UseAuthorization()`
- Extracts user ID from `ClaimTypes.NameIdentifier`
- Loads full `User` entity from database (with `UserLanguages` eager-loaded)
- Stores user in `HttpContext.Items["CurrentUser"]`
- Access via `HttpContext.GetCurrentUser()` extension method

**Example (with `[Authorize]` attribute):**
```csharp
[Authorize]
public async Task<IActionResult> GetProfile()
{
    // No null check needed - [Authorize] guarantees authentication
    var currentUser = HttpContext.GetCurrentUser()!;

    // currentUser is the full User entity (not just claims)
    var userId = currentUser.Id;
    var email = currentUser.Email;
    var totalXp = currentUser.TotalPointsEarned;

    return Ok(new { id = userId, email });
}
```

**Example (without `[Authorize]` - public endpoint):**
```csharp
[AllowAnonymous]
public async Task<IActionResult> GetPublicData()
{
    var currentUser = HttpContext.GetCurrentUser();
    if (currentUser == null)
        return Unauthorized(new { message = "User is not authorized." });

    // Use currentUser...
}
```

## Authorization

### Role-Based Access Control

Three roles configured:

| Role | Permissions |
|------|-------------|
| **Student** | Submit answers, view progress, enroll in languages |
| **ContentCreator** | Create/edit courses, lessons, exercises (+ Student permissions) |
| **Admin** | Full system access (+ ContentCreator permissions) |

### Controller Authorization

#### Endpoint-Level Authorization

```csharp
// Require authentication (any role)
[Authorize]
public async Task<IActionResult> GetLesson(string lessonId) { }

// Require specific role(s)
[Authorize(Roles = "Admin")]
public async Task<IActionResult> DeleteUser(string userId) { }

// Multiple roles (OR logic)
[Authorize(Roles = "Admin,ContentCreator")]
public async Task<IActionResult> CreateLesson(CreateLessonDto dto) { }

// Public endpoint (no auth required)
[AllowAnonymous]
public async Task<IActionResult> GetLanguages() { }
```

#### Controller-Level Authorization

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]  // ← All endpoints require authentication by default
public class LessonController : ControllerBase
{
    // Inherits [Authorize] - requires auth

    [AllowAnonymous]  // ← Override: public endpoint
    public async Task<IActionResult> GetPublicLessons() { }

    [Authorize(Roles = "Admin")]  // ← Override: admin-only
    public async Task<IActionResult> DeleteLesson(string id) { }
}
```

### Lock Bypass for Admins/Creators

Business logic allows Admin and ContentCreator roles to bypass exercise/lesson locks for testing purposes:

```csharp
// ExerciseController.GetExercise
[Authorize]
public async Task<ActionResult<ExerciseDto>> GetExercise(string id)
{
    var exercise = await _exerciseService.GetExerciseByIdAsync(id);
    if (exercise == null)
        return NotFound();

    var user = HttpContext.GetCurrentUser()!;
    var canBypassLocks = await user.CanBypassLocksAsync(_userManager);

    if (exercise.IsLocked && !canBypassLocks)
        return StatusCode(403, new { message = "Exercise is locked. Complete previous exercises to unlock." });

    return Ok(exercise.ToDto());
}
```

This prevents the "chicken and egg" problem where content creators can't test locked exercises without unlocking them first.

## Common Auth Scenarios

### Scenario 1: Require Authentication (Recommended Pattern)

Use `[Authorize]` attribute - authentication is guaranteed, no null check needed:

```csharp
[Authorize]
[HttpGet]
public async Task<IActionResult> GetProfile()
{
    // [Authorize] guarantees authentication - currentUser cannot be null
    var currentUser = HttpContext.GetCurrentUser()!;
    return Ok(new { id = currentUser.Id, email = currentUser.Email });
}
```

**Controller-level authorization** (all endpoints require auth by default):

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]  // ← All endpoints in this controller require authentication
public class LessonController : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> GetLesson(string id)
    {
        // No null check needed - inherited [Authorize] from class level
        var currentUser = HttpContext.GetCurrentUser()!;
        // ...
    }
}
```

### Scenario 2: Check if User Has Role

```csharp
var isAdmin = await _userManager.IsInRoleAsync(currentUser, "Admin");
if (!isAdmin)
    return Forbid();
```

### Scenario 3: Public Endpoint with Optional Auth

When using `[AllowAnonymous]`, null checks ARE required because the user may not be authenticated:

```csharp
[AllowAnonymous]
[HttpGet]
public async Task<IActionResult> GetLeaderboard()
{
    // ⚠️ Null check required - endpoint allows anonymous access
    var currentUser = HttpContext.GetCurrentUser();

    var leaderboard = await _leaderboardService.GetLeaderboardAsync();

    // Include current user's rank if authenticated
    if (currentUser != null)
    {
        leaderboard.CurrentUserRank = /* ... */;
    }

    return Ok(leaderboard);
}
```

**Rule of thumb:**
- `[Authorize]` → No null check needed (use `!` operator)
- `[AllowAnonymous]` → Null check required (user may not be authenticated)

### Understanding the `!` Operator in Auth Contexts

When you see `HttpContext.GetCurrentUser()!` in controller code, the `!` (null-forgiving operator) is a **compiler hint**, not suppressing an actual null check:

```csharp
[Authorize]
public async Task<IActionResult> SubmitAnswer(string exerciseId, SubmitAnswerRequest request)
{
    // [Authorize] guarantees this cannot be null
    var user = HttpContext.GetCurrentUser()!;

    // Use user without defensive checks
    await _progressService.SubmitAnswerAsync(user.Id, exerciseId, request.Answer);
    return Ok();
}
```

**Why this is safe:**
1. `[Authorize]` attribute runs in the authorization middleware **before** the controller
2. If authentication fails, the request is rejected with 401 — controller never executes
3. `UserContextMiddleware` loads the user entity after successful authentication
4. By the time controller code runs, the user is **guaranteed** to exist
5. The `!` tells the compiler "I know this isn't null" — it's documentation, not suppression

**When NOT to use `!`:**
- Production service/business logic code — always throw explicit exceptions
- `[AllowAnonymous]` endpoints — user genuinely can be null
- Before checking `[Authorize]` has been applied — verify the attribute exists first

## Security Considerations

### Token Storage

✅ **Do:** Store in HttpOnly cookie
- Immune to XSS attacks
- Automatically sent with requests
- Managed by browser

❌ **Don't:** Store in localStorage or sessionStorage
- Vulnerable to XSS
- Requires manual inclusion in requests

### CSRF Protection

- `SameSite=Lax` provides CSRF protection for most requests
- Top-level navigation (GET) sends cookie
- Cross-site POST/PUT/DELETE don't send cookie

### Token Expiration

- Default: 24 hours (configurable via `JWT_EXPIRATION_HOURS`)
- No automatic refresh (user must re-login)
- Backend returns 401 Unauthorized when token expires

### Debugging Auth Issues

**401 Unauthorized:**
1. Check cookie exists: DevTools → Application → Cookies → `AuthToken`
2. Verify not expired: Decode JWT at [jwt.io](https://jwt.io), check `exp` claim
3. Clear cookies and re-login if database was reset (stale user ID)

**403 Forbidden:**
1. User authenticated but lacks required role
2. Check user roles: `GET /api/auth/auth-status`
3. Ensure role was assigned during user creation

**HttpContext.GetCurrentUser() returns null:**
1. Import namespace: `using Backend.Api.Middleware;`
2. Verify `UserContextMiddleware` is registered in pipeline
3. Check that `UseUserContext()` is called after `UseAuthentication()`

## Environment Variables

```bash
# Required
JWT_SECRET=<your-secret-key-min-32-chars>

# Optional (have defaults)
JWT_ISSUER=lexiq-api           # Default if not set
JWT_AUDIENCE=lexiq-frontend    # Default if not set
JWT_EXPIRATION_HOURS=24        # Default if not set

# Google OAuth (required for login)
GOOGLE_CLIENT_ID=<your-client-id>
GOOGLE_CLIENT_SECRET=<your-client-secret>
```

**Security:** `JWT_SECRET` must be:
- At least 32 characters (256 bits for HS256)
- Randomly generated (use `openssl rand -base64 32`)
- Kept secret (never commit to git)
- Same value across all backend instances (for cookie validation)

## Testing Authentication

### cURL Examples

```bash
# 1. Login with Google (get JWT cookie)
curl -i -X POST http://localhost:8080/api/auth/google-login \
  -H "Content-Type: application/json" \
  -d '{"idToken": "<google-id-token>"}'

# Extract cookie from response:
# Set-Cookie: AuthToken=eyJhbGc...

# 2. Use cookie in subsequent requests
curl -i -b "AuthToken=eyJhbGc..." \
  http://localhost:8080/api/lessons

# 3. Logout
curl -i -X POST -b "AuthToken=eyJhbGc..." \
  http://localhost:8080/api/auth/logout
```

### Postman

1. Send login request to `/api/auth/google-login`
2. Postman automatically stores cookies
3. Subsequent requests include `AuthToken` cookie automatically
4. Check cookies: Postman → Cookies → manage cookies for `localhost:8080`
