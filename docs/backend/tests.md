# Backend Test Coverage Documentation

**Last Updated**: 2026-03-16
**Framework**: xUnit v3 + Testcontainers.MsSql
**Test Project**: `backend/Tests/Backend.Tests.csproj`

## Overview

The Lexiq backend has **12 test classes** covering authentication, authorization, exercise submission, leaderboard, streaks, levels, and JWT generation. Tests use **Testcontainers** to spin up real SQL Server 2022 containers for integration tests, ensuring production-like behavior.

---

## Test Inventory

### 1. **AuthControllerTests.cs** (6 tests) — HTTP-level cookie behavior

**Type**: Integration (WebApplicationFactory + Testcontainers)
**Coverage**: POST /api/auth/google-login and POST /api/auth/logout cookie mechanics

| Test | Purpose |
|------|---------|
| `GoogleLogin_SetsAuthTokenCookie` | Verifies `Set-Cookie: AuthToken=...` header exists |
| `GoogleLogin_CookieIsHttpOnly` | Verifies `httponly` flag is present |
| `GoogleLogin_CookieExpiresInConfiguredHours` | Verifies cookie expires in 24h (JWT_EXPIRATION_HOURS) |
| `GoogleLogin_CookieValueIsValidJwt` | Verifies JWT structure (3 base64url segments) |
| `Logout_SetsExpiredAuthTokenCookie` | Verifies logout sets past expiry date |

**Mocking**: `IGoogleAuthService` mocked with Moq (avoids real Google API calls)

---

### 2. **AuthorizationTests.cs** (92 tests across 8 categories) — Role-based access control

**Type**: Integration (WebApplicationFactory + Testcontainers)
**Coverage**: Every protected endpoint in the system

#### Categories

1. **Unauthenticated → 401** (20 tests)
   - Verifies all `[Authorize]`-guarded endpoints reject requests with no JWT
   - Includes special case for `PUT /api/user/avatar` with multipart form data
   - Tests: GET/POST/PUT/DELETE on /api/course, /api/lesson, /api/exercise
   - Additional: GET /api/user/xp, /api/auth/is-admin, POST /api/lesson/{id}/complete, POST /api/lesson/{id}/unlock

2. **Student → 403 on role-restricted** (13 tests)
   - Students cannot create/update/delete content
   - Tests: POST/PUT/DELETE on /api/language, /api/course, /api/lesson, /api/exercise
   - Unlock endpoint: POST /api/lesson/{id}/unlock

3. **ContentCreator → 403 on Admin-only** (5 tests)
   - ContentCreators can create content but cannot manage languages or manually unlock
   - Tests: POST/PUT/DELETE /api/language, DELETE /api/course, POST /api/lesson/{id}/unlock

4. **ContentCreator → not rejected on Admin/ContentCreator endpoints** (8 tests)
   - ContentCreators can access `[Authorize(Roles = "Admin,ContentCreator")]` endpoints
   - Tests: POST/PUT /api/course, POST/PUT/DELETE /api/lesson, POST/PUT/DELETE /api/exercise
   - Verifies ContentCreator role satisfies dual-role authorization

5. **Admin → not rejected** (13 tests)
   - Admins can access all role-restricted endpoints
   - Tests verify status is NOT 401 or 403 (may be 404/400 if resource doesn't exist)
   - Full coverage of language, course, lesson, exercise CRUD operations

6. **Public endpoints are not blocked** (7 tests)
   - Unauthenticated users can access public endpoints
   - Tests: GET /api/language, /api/leaderboard, /api/user/{id}/xp, /api/auth/auth-status
   - **Note**: Some endpoints marked as public in old docs are actually `[Authorize]` in code (e.g., GET /api/user/{id}/avatar)

7. **Missing auth on management controllers** (2 tests — security documentation)
   - `UserManagementController` has no `[Authorize]` — accessible without token
   - `RoleManagementController` has no `[Authorize]` — accessible without token
   - **These tests document a security gap that should be fixed before production**

8. **Expired JWT → 401** (13 tests)
   - Expired tokens (minted 1 hour ago) are rejected
   - Tests: GET/POST/PUT on /api/course, /api/lesson, /api/exercise
   - Additional: GET /api/user/xp, /api/auth/is-admin

**JWT Minting**: Tests mint their own JWTs with correct issuer/audience/signing key to test policy enforcement

---

### 3. **LoginUserTests.cs** (10 tests) — GoogleAuthService integration

**Type**: Integration (Testcontainers + real UserManager/RoleManager)
**Coverage**: `GoogleAuthService.LoginUser` — three code paths

#### Code Paths Tested

1. **New user** (4 tests)
   - Creates user in database
   - Assigns "Student" role
   - Adds Google login (UserLoginInfo)
   - Returns non-null User with correct email/username

2. **Returning Google user** (2 tests)
   - Finds existing user by Google `sub` (UserLoginInfo)
   - Does not create duplicate
   - Returns existing User entity

3. **Email-match user** (3 tests)
   - Finds existing user by email (no Google login yet)
   - Adds Google login
   - **Does not assign role** (preserves existing roles)

4. **Edge cases** (3 tests)
   - Role assignment failure throws `InvalidOperationException`
   - Missing picture URL → login succeeds (avatar download skipped)
   - Avatar download failure → login succeeds (exception caught)

**Real Dependencies**: Uses real `UserManager`, `RoleManager`, `AvatarService` backed by Testcontainers DB

---

### 4. **GetLeaderboardTests.cs** (22 tests) — Leaderboard queries

**Type**: Integration (Testcontainers)
**Coverage**: `LeaderboardService.GetLeaderboardAsync` — three time frames

#### AllTime Tests (10 tests)

- Orders by `User.TotalPointsEarned` descending (cached XP aggregate)
- Does NOT sum `UserExerciseProgress` rows (validates caching contract)
- Limits to top 50 entries
- Current user outside top 50 → fetched separately with correct rank
- Current user in top 50 → marked `IsCurrentUser: true`
- Level calculated from total XP (formula: `floor((1 + sqrt(1 + xp/25)) / 2)`)
- Username fallback chain: `UserName ?? Email ?? "Unknown"`

#### Weekly Tests (7 tests)

- Only includes progress from last 7 days (`CompletedAt >= UtcNow.AddDays(-7)`)
- Excludes users with no progress in window (even if high cached XP)
- Sums multiple progress rows in window
- Orders by window XP descending
- Current user outside top 50 → correct rank

#### Monthly Tests (2 tests)

- Only includes progress from last 30 days
- Excludes progress older than 30 days

#### Rank Change Tests (3 tests)

- New entry (not in previous period) → `Change: 0`
- User moved up (rank 3 → rank 1) → `Change: +2`
- User moved down (rank 1 → rank 3) → `Change: -2`

#### Avatar Tests (2 tests)

- User with avatar → `Avatar: "/api/user/{id}/avatar"`
- User without avatar → `Avatar: null`

#### Streak Tests (1 test)

- Leaderboard entries include `CurrentStreak` and `LongestStreak` fields

---

### 5. **GetStreakTests.cs** (9 tests) — Streak calculation

**Type**: Integration (Testcontainers)
**Coverage**: `LeaderboardService.GetStreakAsync` — consecutive day counting

#### Rules

- Counts consecutive UTC calendar days with **at least one completed exercise**
- **Grace period**: if no activity today, yesterday still counts as current streak
- Current streak resets to 0 if most recent activity is 2+ days ago

#### Test Scenarios

- No progress → `(current: 0, longest: 0)`
- Only uncompleted progress → `(0, 0)` (IsCompleted=false rows ignored)
- Single completion today → `(1, 1)`
- Single completion yesterday → `(1, 1)` (grace period)
- Single completion 2 days ago → `(0, 1)` (streak broken)
- 3 consecutive days ending today → `(3, 3)`
- 3 consecutive days ending yesterday → `(3, 3)` (grace extends to whole run)
- Gap in middle → correct current and longest (e.g. yesterday + [gap] + 5-6 days ago)

---

### 6. **CalculateLevelTests.cs** (10 tests) — Level formula

**Type**: Pure unit (no DB, no fixture)
**Coverage**: `LeaderboardService.CalculateLevel` — static formula validation

#### Formula

```
level = floor((1 + sqrt(1 + totalXp / 25)) / 2)
threshold(n) = 100 * n * (n - 1)
```

#### Thresholds

| XP | Level |
|----|-------|
| 0 | 1 |
| 200 | 2 |
| 600 | 3 |
| 1200 | 4 |
| 2000 | 5 |
| 3000 | 6 |
| 4200 | 7 |

#### Test Coverage

- Zero or negative XP → Level 1
- Boundary values (199 → 1, 200 → 2, 599 → 2, 600 → 3)
- Large XP (1,000,000) → does not overflow

---

### 7. **JwtServiceTests.cs** (13 tests) — JWT generation

**Type**: Pure unit (sets env vars, no DB)
**Coverage**: `JwtService.GenerateToken` — JWT structure and validation

#### Claims Tested

- `sub` claim contains User.Id
- `email` claim contains User.Email
- `name` claim contains User.UserName
- `jti` claim is present (unique token ID)
- `ClaimTypes.Role` claims for each role (Admin, ContentCreator, etc.)
- Empty roles → no role claims

#### Validation Tests

- Token expires in configured hours (JWT_EXPIRATION_HOURS, default 24)
- Token can be validated with correct signing key
- Token validation **fails** with wrong signing key
- Constructor throws `InvalidOperationException` if JWT_SECRET not set
- ExpirationHours defaults to 24 if env var not set

---

## Test Infrastructure

### DatabaseFixture

- Shared across test classes via `IClassFixture<DatabaseFixture>`
- Starts SQL Server 2022 container once per test run
- Runs EF Core migrations
- Seeds permanent content hierarchy:
  - System User (satisfies `Course.CreatedById` FK)
  - Language: "Italian"
  - Course (1)
  - Lesson (1)
  - **40 × Exercise** (`fixture.ExerciseIds[0..39]`) — 4 types:
    - [0-9]: FillInBlank (10 exercises)
    - [10-19]: MultipleChoice (10 exercises, 3 options each)
    - [20-29]: Listening (10 exercises)
    - [30-39]: Translation (10 exercises)
- **Why 40 exercises across 4 types?**
  - `UserExerciseProgress` PK is `(UserId, ExerciseId)` — streak tests need one distinct ExerciseId per calendar day
  - 10 iterations per type enables comprehensive testing of type-specific validation logic

### UserBuilder

- Fluent builder for creating Identity-compliant User entities
- Sets `NormalizedUserName`, `NormalizedEmail`, `SecurityStamp`, `ConcurrencyStamp`
- Methods: `WithUserName`, `WithEmail`, `WithTotalPoints`, `WithNullUserName`, `WithNullEmail`

### DbSeeder

- `AddUserAsync`, `AddUsersAsync` — insert users
- `AddProgressAsync` — insert UserExerciseProgress row
- `AddConsecutiveDaysActivityAsync` — batch insert N consecutive days of progress
- `AddAvatarAsync` — insert UserAvatar binary row
- `ClearLeaderboardDataAsync` — deletes Users, UserExerciseProgress, UserAvatars (excludes system user)

### ControllerTestBase

- Base class for `WebApplicationFactory` tests
- Provides `GoogleAuthMock` (Moq) to avoid real Google API calls
- Sets required env vars: JWT_SECRET, JWT_EXPIRATION_HOURS, DATA_PROTECTION_KEYS_PATH, GOOGLE_CLIENT_ID, GOOGLE_CLIENT_SECRET
- Clears env vars in `DisposeAsync`

---

## E2E User Journey Tests

### 8. **StudentExerciseProgressJourneyTests.cs** (7 tests) — Exercise completion workflows

**Type**: E2E (WebApplicationFactory + Testcontainers)
**Coverage**: Core student learning workflows

| Test | Purpose |
|------|---------|
| `Student_CompletesFirstExercise_UnlocksNextExercise` | Sequential unlock after correct answer |
| `Student_SubmitsWrongAnswer_CanRetryInfinitely` | Infinite retries allowed without penalty |
| `Student_ResubmitsCorrectAnswer_DoesNotDoubleXp` | XP idempotency (no double-counting) |
| `Student_CompletesLesson_UnlocksNextLesson` | 70% completion threshold triggers unlock |
| `Student_CompletesPartialLesson_ProgressRestoresCorrectly` | Progress tracking persists |
| `Student_SubmitsWrongAnswer_ProgressShowsIncorrect` | Wrong submissions create records |
| `Student_Below70Percent_LessonNotCompleted` | Below-threshold completion rejected |

---

### 9. **StudentSessionPersistenceTests.cs** (6 tests) — Progress restoration

**Type**: E2E (WebApplicationFactory + Testcontainers)
**Coverage**: Session state persistence

| Test | Purpose |
|------|---------|
| `Student_CompletesPartial_ReturnsLater_ProgressRestored` | 3/5 exercises persist across sessions |
| `Student_CompletesLesson_ReturnsLater_AllExercisesStillComplete` | Full completion persists |
| `Student_SubmitsWrongAnswer_ReturnsLater_StillShowsWrong` | Wrong answer state persists |
| `Student_PartialProgress_ThirdExerciseStillLocked` | Lock state persists |
| `MultipleSessionsConsistentState_XpDoesNotDuplicate` | XP consistent across sessions |

---

### 10. **ExerciseSubmissionSecurityTests.cs** (9 tests) — Security and edge cases

**Type**: E2E (WebApplicationFactory + Testcontainers)
**Coverage**: Lock enforcement, role-based access, MultipleChoice validation, endpoint shapes

| Test | Purpose |
|------|---------|
| `Student_SubmitsWrongAnswer_DoesNotUnlockNextExercise` | Wrong answers keep next exercise locked |
| `Student_SubmitsToLockedExercise_Returns403` | Lock enforcement for students |
| `Admin_SubmitsToLockedExercise_Success` | Admin bypass works |
| `ContentCreator_SubmitsToLockedExercise_Success` | ContentCreator bypass works |
| `Student_SubmitsToNonexistentExercise_Returns404` | Invalid exercise IDs handled |
| `Student_SubmitsToExerciseInLockedLesson_Returns403` | Lesson-level locks propagate |
| `Student_SubmitsCorrectMultipleChoiceAnswer_Success` | Option ID validation (vs text) |
| `GetLessonProgress_ReturnsCorrectStructure` | Progress endpoint shape validated |
| `GetLessonSubmissions_ReturnsAllExercisesWithSubmissionState` | Submissions endpoint validated |

---

### 11. **LeaderboardAndStreaksTests.cs** (5 tests) — Gamification features

**Type**: E2E (WebApplicationFactory + Testcontainers)
**Coverage**: Leaderboard ranking, streak tracking, avatar integration

| Test | Purpose |
|------|---------|
| `Student_EarnsXp_AppearsOnLeaderboard` | XP triggers leaderboard appearance |
| `Student_Builds3DayStreak_StreakDisplayedCorrectly` | Streak calculation works |
| `Student_Inactive2Days_StreakResets` | Streak reset after gap |
| `Student_UploadsAvatar_ShowsOnLeaderboard` | Avatar integration |
| `TwoStudents_CompeteForRank_OrderedByXp` | Multi-user ranking |

---

### 12. **AdminContentManagementJourneyTests.cs** (6 tests) — Content CRUD workflows

**Type**: E2E (WebApplicationFactory + Testcontainers)
**Coverage**: Admin/ContentCreator content management

| Test | Purpose |
|------|---------|
| `Admin_CreatesLesson_StudentCanAccessAndSolve` | Full create → unlock → solve flow |
| `Admin_UpdatesLesson_StudentSeesChanges` | Update propagation |
| `Admin_DeletesLesson_StudentCannotAccess` | Delete cascade behavior |
| `ContentCreator_CreatesLesson_LockedByDefault` | Default lock state |
| `Admin_AddsExerciseToExistingLesson_StudentSeesNewExercise` | Dynamic exercise addition |
| `Student_CannotCreateCourse_Returns403` | Role restriction enforcement |

---

## Coverage Gaps (Updated 2026-03-12)

### Still Missing

**Untested Endpoints:**
| Endpoint | Method | Coverage |
|----------|--------|----------|
| POST /api/language | POST | **Not tested** |
| PUT /api/language/{id} | PUT | **Not tested** |
| DELETE /api/language/{id} | DELETE | **Not tested** |
| PUT /api/user/avatar | PUT | **Authorization only** (file validation, upsert logic not tested) |

**Business Logic:**
- Language CRUD: DTO validation, business logic, cascade delete behavior
- Avatar upload: File size/type validation, upsert behavior, existing avatar replacement

---

## Recommendations

1. **Add Language CRUD tests** — test LanguageService create, update, delete, cascade behavior
2. **Add avatar upload validation tests** — file size/type validation, upsert behavior, binary storage
3. **Secure management controllers** — add `[Authorize(Roles = "Admin")]` to UserManagement and RoleManagement (currently open!)

---

## Running Tests

```bash
# All tests (requires Docker)
cd backend
dotnet test Tests/Backend.Tests.csproj --logger "console;verbosity=normal"

# Specific test class
dotnet test Tests/Backend.Tests.csproj --filter "FullyQualifiedName~GetLeaderboardTests"

# Unit tests only (no Docker)
dotnet test Tests/Backend.Tests.csproj --filter "FullyQualifiedName~CalculateLevelTests|JwtServiceTests"
```

---

## Test Statistics

- **Total test classes**: 12
- **Total test methods**: ~195 (exact count depends on Theory inline data rows)
- **E2E tests**: 5 classes, 33 tests (full user journeys)
- **Integration tests**: 5 classes (use Testcontainers)
- **Unit tests**: 2 classes (pure, no DB)
- **Authorization tests**: 92 tests across 8 categories (complete endpoint coverage for all roles)
- **Coverage**: Auth flow, **Complete authorization matrix**, Exercise submission (FillInBlank + MultipleChoice), Leaderboard queries, Streak calculation, Level calculation, JWT generation, Progress restoration, Admin bypass, Lock enforcement
- **Not covered**: Listening/Translation exercise types, FillInBlank edge cases, Course/Lesson CRUD business logic (DTO validation, OrderIndex auto-calc), Avatar upload validation
