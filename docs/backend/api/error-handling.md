# Error Handling

The Lexiq API uses a global error handling middleware that catches exceptions and returns standardized HTTP status codes with consistent JSON error responses.

## Error Response Format

All errors return a JSON object with the following structure:

```json
{
  "message": "Human-readable error message",
  "statusCode": 400,
  "detail": "Stack trace (only in development environment)"
}
```

### Fields

- **`message`** (string): A human-readable description of the error
- **`statusCode`** (integer): HTTP status code (matches the response status)
- **`detail`** (string, nullable): Stack trace and additional debugging information
  - Only included when `ASPNETCORE_ENVIRONMENT=Development`
  - Always `null` in production to prevent information leakage

## HTTP Status Codes

### 400 Bad Request

Invalid input data or business rule violations.

**Common causes:**
- Missing required fields
- Invalid data format (e.g., malformed GUID, invalid enum value)
- Invalid parameter values
- Business rule violations (non-locked resource errors)

**Example:**
```json
{
  "message": "Required parameter is missing: courseId",
  "statusCode": 400,
  "detail": null
}
```

### 401 Unauthorized

Authentication required but not provided or invalid.

**Common causes:**
- Missing JWT token in `AuthToken` cookie
- Expired JWT token
- Invalid JWT signature
- User not found in database (after DB reset)

**Example:**
```json
{
  "message": "You are not authorized to perform this action.",
  "statusCode": 401,
  "detail": null
}
```

**Debugging tips:**
- Clear browser cookies and re-login if you recently reset the database
- Check browser DevTools → Application → Cookies for `AuthToken`
- Verify JWT hasn't expired (default: 24 hours)

### 403 Forbidden

Authenticated but not authorized to perform the action.

**Common causes:**
- Insufficient role privileges (e.g., Student trying to access Admin endpoint)
- Attempting to submit answers for locked lesson/exercise
- Resource access restrictions

**Example:**
```json
{
  "message": "Cannot submit answers for a locked exercise",
  "statusCode": 403,
  "detail": null
}
```

### 404 Not Found

Requested resource does not exist.

**Common causes:**
- Invalid ID in URL path
- Resource deleted
- Typo in endpoint URL

**Example:**
```json
{
  "message": "Course with ID 'abc123' not found.",
  "statusCode": 404,
  "detail": null
}
```

### 409 Conflict

Resource state conflict (typically concurrent updates).

**Common causes:**
- Concurrent modification of the same resource
- Database concurrency conflicts

**Example:**
```json
{
  "message": "The resource was modified by another user. Please refresh and try again.",
  "statusCode": 409,
  "detail": null
}
```

### 500 Internal Server Error

Unexpected server error or database failure.

**Common causes:**
- Database connection failures
- Constraint violations (unique, foreign key)
- Unhandled exceptions
- Infrastructure issues

**Example:**
```json
{
  "message": "A database error occurred while processing your request.",
  "statusCode": 500,
  "detail": null
}
```

## Exception Mapping

The middleware maps .NET exceptions to HTTP status codes using the following rules:

| Exception Type | HTTP Status | Condition |
|---------------|-------------|-----------|
| `UnauthorizedAccessException` | 401 | Always |
| `ArgumentNullException` | 400 | Always |
| `ArgumentException` | 404 | Message contains "not found" |
| `ArgumentException` | 400 | Otherwise |
| `InvalidOperationException` | 403 | Message contains "locked" or "cannot" |
| `InvalidOperationException` | 400 | Otherwise |
| `KeyNotFoundException` | 404 | Always |
| `FormatException` | 400 | Always |
| `DbUpdateConcurrencyException` | 409 | Always |
| `DbUpdateException` | 500 | Always |
| `Exception` (catch-all) | 500 | Always |

### Exception Hierarchy

The middleware respects C# exception inheritance. More specific exceptions are caught before base exceptions:

1. `ArgumentNullException` (caught first)
2. `ArgumentException` (parent of ArgumentNullException)
3. ...
4. `Exception` (catch-all, caught last)

## Middleware Implementation

### Registration Order

The error handling middleware is registered **first** in the pipeline to catch exceptions from all downstream middleware and controllers:

```csharp
// Program.cs - ConfigureMiddleware
app.UseErrorHandling();      // ← FIRST - wraps everything
app.UseRouting();
app.UseCors("AllowAngular");
app.UseAuthentication();
app.UseUserContext();
app.UseAuthorization();
app.MapControllers();
```

### How It Works

```
Request → [ErrorHandlingMiddleware (try)]
            → [Authentication]
              → [Authorization]
                → [Controller throws exception] ❌
              ← exception bubbles up
            ← [ErrorHandlingMiddleware (catch)] ✅
          Response with standardized error JSON
```

The middleware wraps the entire pipeline with a try-catch block. When any downstream component throws an exception, it bubbles back up through the call stack and is caught by the error handler.

## Controller Implementation

Controllers do **not** need try-catch blocks. Just call service methods directly:

```csharp
// ✅ CORRECT - Let middleware handle exceptions
[HttpPost("{lessonId}/complete")]
public async Task<ActionResult<CompleteLessonResponse>> CompleteLesson(string lessonId)
{
    var currentUser = HttpContext.GetCurrentUser();
    if (currentUser == null)
        return Unauthorized(new { message = "User is not authorized." });

    var result = await _progressService.CompleteLessonAsync(currentUser.Id, lessonId);
    return Ok(result);
}

// ❌ WRONG - Don't add redundant try-catch
[HttpPost("{lessonId}/complete")]
public async Task<ActionResult<CompleteLessonResponse>> CompleteLesson(string lessonId)
{
    try
    {
        // ...
    }
    catch (Exception ex)
    {
        // Middleware already handles this!
    }
}
```

### Exceptions for Manual Handling

You should only add try-catch in controllers when:

1. **Multiple different responses for the same exception type**
   - Middleware can't distinguish between two `ArgumentException` cases that need different status codes

2. **Custom business logic on error**
   - Need to perform cleanup, logging, or state changes on specific errors

3. **Transform exception into a different response**
   - Need to map an exception to a specific DTO structure

For most cases, let the middleware handle it!

## Logging

All exceptions are logged with appropriate severity levels:

- **`LogInformation`**: 400-level errors (expected client errors)
  - ArgumentException, KeyNotFoundException, FormatException
- **`LogWarning`**: 401/403 errors and conflicts
  - UnauthorizedAccessException, InvalidOperationException, DbUpdateConcurrencyException
- **`LogError`**: 500-level errors (unexpected server errors)
  - DbUpdateException, unhandled Exception

Log messages include:
- Exception type and message
- Request path that triggered the error
- Contextual details (e.g., parameter names for ArgumentNullException)

## Testing Error Handling

### Using cURL

```bash
# 404 - Not Found
curl -i http://localhost:8080/api/lessons/invalid-id

# 401 - Unauthorized (no cookie)
curl -i http://localhost:8080/api/lessons/complete

# 403 - Forbidden (locked exercise)
curl -i -b "AuthToken=<token>" \
  -X POST http://localhost:8080/api/exercises/<locked-exercise-id>/submit \
  -H "Content-Type: application/json" \
  -d '{"answer": "test"}'
```

### Using Browser DevTools

1. Open Network tab
2. Trigger an error (e.g., submit to locked exercise)
3. Inspect response:
   - Status code in the response header
   - JSON error body in the Preview tab
   - Stack trace visible if `ASPNETCORE_ENVIRONMENT=Development`

## Security Considerations

### Information Disclosure Prevention

- Stack traces **never** exposed in production (`detail: null`)
- Error messages are user-friendly, not technical
- Database constraint violations don't reveal schema details

### Safe Error Messages

The middleware transforms technical database errors into safe messages:

```csharp
// Database error with UNIQUE constraint violation
var message = ex.InnerException?.Message.Contains("UNIQUE") == true
    ? "A record with this value already exists."  // ✅ Safe
    : "A database error occurred.";               // ✅ Safe

// NOT:
// "Violation of UNIQUE KEY constraint 'UQ_Users_Email'"  // ❌ Reveals schema
```

### Production vs. Development

**Development** (`ASPNETCORE_ENVIRONMENT=Development`):
- Full stack traces in `detail` field
- Detailed exception messages
- Helpful for debugging

**Production** (`ASPNETCORE_ENVIRONMENT=Production`):
- `detail` field always `null`
- Generic error messages for 500s
- Detailed logs on server, minimal info to client
