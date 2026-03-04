# Required Backend Changes

## Overview

E2E tests require two backend modifications:
1. **Test endpoint** to fetch correct answers
2. **Mock Google OAuth** support (optional, nice-to-have)

## 1. Test Endpoint: GET /api/exercises/{id}/correct-answer

### Purpose

Allow E2E tests to fetch the correct answer for an exercise without parsing exercise details.

### Implementation

**File**: `backend/Controllers/ExerciseController.cs`

**Add new endpoint**:

```csharp
/// <summary>
/// Gets the correct answer for an exercise (test-only endpoint)
/// </summary>
[HttpGet("{id}/correct-answer")]
[Authorize]  // Requires authentication
public async Task<ActionResult<CorrectAnswerDto>> GetCorrectAnswer(string id)
{
    var exercise = await _context.Exercises
        .Include(e => (e as MultipleChoiceExercise)!.Options)
        .FirstOrDefaultAsync(e => e.Id == id);

    if (exercise == null)
    {
        return NotFound(new { message = "Exercise not found" });
    }

    var correctAnswer = GetCorrectAnswerForExercise(exercise);

    return Ok(new CorrectAnswerDto(correctAnswer));
}

/// <summary>
/// Extracts correct answer from exercise (reused from ExerciseProgressService)
/// </summary>
private static string? GetCorrectAnswerForExercise(Exercise exercise)
{
    return exercise switch
    {
        MultipleChoiceExercise mce => mce.Options.FirstOrDefault(o => o.IsCorrect)?.OptionText,
        FillInBlankExercise fib => fib.CorrectAnswer,
        TranslationExercise te => te.TargetText,
        ListeningExercise le => le.CorrectAnswer,
        _ => null,
    };
}
```

**Add DTO**:

**File**: `backend/Dtos/CorrectAnswerDto.cs` (new file)

```csharp
namespace Backend.Api.Dtos;

public record CorrectAnswerDto(string? CorrectAnswer);
```

### Security Considerations

**Q**: Should this endpoint be restricted to test environment only?

**Options**:

1. **No restriction** (Recommended) - Authenticated users can see answers
   - ✅ Simple implementation
   - ✅ Teachers/content creators benefit from this endpoint
   - ⚠️ Students can "cheat" by calling endpoint
   - **Mitigation**: Students already see answer after submitting wrong answer

2. **Conditional compilation** - Only available in Development
   ```csharp
   #if DEBUG
   [HttpGet("{id}/correct-answer")]
   public async Task<ActionResult<CorrectAnswerDto>> GetCorrectAnswer(string id)
   {
       // ... implementation
   }
   #endif
   ```
   - ✅ Not available in production builds
   - ❌ Harder to test in staging environment

3. **Role-based restriction** - Admin/ContentCreator only
   ```csharp
   [HttpGet("{id}/correct-answer")]
   [Authorize(Roles = "Admin,ContentCreator")]
   public async Task<ActionResult<CorrectAnswerDto>> GetCorrectAnswer(string id)
   ```
   - ✅ Restricts access to privileged users
   - ❌ E2E tests need admin users (more complex setup)

**Recommendation**: Start with **Option 1** (no restriction, just `[Authorize]`). Students can already see answers after wrong submissions, so this doesn't introduce new vulnerabilities.

### Testing the Endpoint

**File**: `backend/Tests/Controllers/ExerciseControllerTests.cs` (create if not exists)

```csharp
public class ExerciseControllerTests : ControllerTestBase
{
    [Fact]
    public async Task GetCorrectAnswer_ReturnsFillInBlankAnswer()
    {
        // Use first seeded exercise (FillInBlankExercise)
        var exerciseId = Fixture.ExerciseIds[0];

        // Call endpoint
        var response = await Client.GetAsync($"/api/exercises/{exerciseId}/correct-answer");

        // Verify response
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CorrectAnswerDto>();
        result.Should().NotBeNull();
        result!.CorrectAnswer.Should().Be("answer");  // Seeded value
    }

    [Fact]
    public async Task GetCorrectAnswer_ReturnsMultipleChoiceAnswer()
    {
        // Create MC exercise with known answer
        var exercise = new MultipleChoiceExercise
        {
            Id = Guid.NewGuid().ToString(),
            LessonId = Fixture.LessonId,
            Title = "Test MC",
            Points = 10,
            OrderIndex = 99,
            Options = new List<ExerciseOption>
            {
                new() { Id = Guid.NewGuid().ToString(), OptionText = "Wrong", IsCorrect = false, OrderIndex = 0 },
                new() { Id = Guid.NewGuid().ToString(), OptionText = "Correct", IsCorrect = true, OrderIndex = 1 },
            }
        };

        using var scope = Factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<BackendDbContext>();
        ctx.Exercises.Add(exercise);
        await ctx.SaveChangesAsync();

        // Call endpoint
        var response = await Client.GetAsync($"/api/exercises/{exercise.Id}/correct-answer");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CorrectAnswerDto>();
        result!.CorrectAnswer.Should().Be("Correct");
    }

    [Fact]
    public async Task GetCorrectAnswer_Returns404ForNonexistentExercise()
    {
        var response = await Client.GetAsync("/api/exercises/nonexistent-id/correct-answer");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetCorrectAnswer_RequiresAuthentication()
    {
        var unauthenticatedClient = CreateClient(authToken: null);

        var response = await unauthenticatedClient.GetAsync(
            $"/api/exercises/{Fixture.ExerciseIds[0]}/correct-answer"
        );

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
```

---

## 2. Mock Google OAuth Support (Optional)

### Current Behavior

`GoogleAuthService.ValidateGoogleTokenAsync()` calls Google's validation API for every login.

**Problem**: E2E tests should not depend on external Google API.

### Proposed Change

Skip Google validation if:
- Environment is `Development` or `Test`
- AND token starts with `"mock-"`

**File**: `backend/Services/GoogleAuthService.cs`

**Modify `ValidateGoogleTokenAsync()`**:

```csharp
public async Task<GoogleJsonWebSignature.Payload> ValidateGoogleTokenAsync(string idToken)
{
    // Skip Google validation for mock tokens in test/dev environment
    var isDevelopment = _env.IsDevelopment() || _env.IsEnvironment("Test");
    if (isDevelopment && idToken.StartsWith("mock-"))
    {
        _logger.LogWarning("⚠️ Using mock Google token (test/dev environment only)");

        // Parse mock token format: "mock-{email}|{name}"
        // Or just return a hardcoded payload
        return new GoogleJsonWebSignature.Payload
        {
            Subject = Guid.NewGuid().ToString(),  // Unique Google ID
            Email = "test@example.com",           // Override in LoginUser logic
            Name = "Test User",                   // Override in LoginUser logic
            Picture = null,
            EmailVerified = true,
        };
    }

    // Normal flow: Validate with Google
    var settings = new GoogleJsonWebSignature.ValidationSettings
    {
        Audience = new[] { _configuration["GOOGLE_CLIENT_ID"] }
    };

    return await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
}
```

**Modify `LoginUser()` to accept override values**:

```csharp
public async Task<User?> LoginUser(
    GoogleJsonWebSignature.Payload payload,
    string? emailOverride = null,  // NEW
    string? nameOverride = null    // NEW
)
{
    var email = emailOverride ?? payload.Email;
    var name = nameOverride ?? payload.Name;

    // Rest of logic uses email and name variables instead of payload.Email/Name
    // ...
}
```

**Update `AuthController.GoogleLogin()`**:

```csharp
[HttpPost("google-login")]
public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequestDto request)
{
    var payload = await _googleAuthService.ValidateGoogleTokenAsync(request.IdToken);

    // Override email/name from request if provided (for tests)
    var user = await _googleAuthService.LoginUser(
        payload,
        emailOverride: request.Email,   // NEW field in DTO
        nameOverride: request.Name      // NEW field in DTO
    );

    // Rest unchanged
    // ...
}
```

**Update DTO**:

**File**: `backend/Dtos/GoogleLoginRequestDto.cs`

```csharp
public record GoogleLoginRequestDto(
    string IdToken,
    string? Email = null,   // NEW - override for tests
    string? Name = null     // NEW - override for tests
);
```

### Alternative: Separate Test Endpoint

Instead of modifying production code, add a test-only login endpoint:

**File**: `backend/Controllers/AuthController.cs`

```csharp
#if DEBUG
/// <summary>
/// Test-only login endpoint (bypasses Google OAuth)
/// </summary>
[HttpPost("test-login")]
public async Task<IActionResult> TestLogin([FromBody] TestLoginRequestDto request)
{
    // Create or get user
    var user = await _userManager.FindByEmailAsync(request.Email);

    if (user == null)
    {
        user = new User
        {
            Id = Guid.NewGuid().ToString(),
            UserName = request.UserName ?? request.Email.Split('@')[0],
            Email = request.Email,
            EmailConfirmed = true,
            RegistrationDate = DateTime.UtcNow,
            LastLoginDate = DateTime.UtcNow,
        };
        await _userManager.CreateAsync(user);
        await _userManager.AddToRoleAsync(user, "Student");
    }

    // Generate JWT
    var roles = await _userManager.GetRolesAsync(user);
    var token = _jwtService.GenerateToken(user, roles);

    // Set cookie
    SetAuthCookie(token, DateTime.UtcNow.AddHours(_jwtService.ExpirationHours));

    return Ok(new { id = user.Id, email = user.Email, userName = user.UserName });
}
#endif

public record TestLoginRequestDto(string Email, string? UserName = null);
```

**Pros**:
- ✅ No changes to production code
- ✅ Explicit test-only endpoint
- ✅ Easier to reason about

**Cons**:
- ❌ Not available in production builds (can't test in staging)

---

## Implementation Checklist

### Required (Test Endpoint)

- [ ] Add `GET /api/exercises/{id}/correct-answer` endpoint to ExerciseController
- [ ] Add `CorrectAnswerDto` record
- [ ] Add `GetCorrectAnswerForExercise()` helper method
- [ ] Write unit tests in `ExerciseControllerTests.cs`
- [ ] Test endpoint manually: `curl http://localhost:8080/api/exercises/{id}/correct-answer -H "Cookie: AuthToken=..."`

### Optional (Mock OAuth)

- [ ] Decide on approach (modify ValidateGoogleTokenAsync vs test-only endpoint)
- [ ] Implement chosen approach
- [ ] Update `GoogleLoginRequestDto` if needed
- [ ] Test with E2E tests using mock tokens
- [ ] Document mock token format in tests

---

## Testing Backend Changes

### Manual Testing

```bash
# 1. Start backend
cd backend/
dotnet run

# 2. Login to get JWT token
curl -X POST http://localhost:8080/api/auth/google-login \
  -H "Content-Type: application/json" \
  -d '{"idToken":"mock-test","email":"test@example.com","name":"Test User"}' \
  -v

# 3. Extract AuthToken from Set-Cookie header
# Example: AuthToken=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...

# 4. Call test endpoint
curl http://localhost:8080/api/exercises/{exercise-id}/correct-answer \
  -H "Cookie: AuthToken={your-token-here}"

# Expected response:
# {"correctAnswer":"answer"}
```

### Automated Testing

```bash
cd backend/Tests/
dotnet test --filter "ExerciseControllerTests"
```

---

## See Also

- [ExerciseProgressService.cs](../../../backend/Services/ExerciseProgressService.cs) - Answer validation logic
- [authentication.md](../e2e/authentication.md) - E2E auth helpers that consume this endpoint
- [test-data.md](../e2e/test-data.md) - How tests use `getCorrectAnswer()` helper
