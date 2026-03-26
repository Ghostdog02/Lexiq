# Backend Test Coverage Analysis

**Generated**: 2026-03-25
**Current Test Count**: ~332 tests across 18 test classes
**Framework**: xUnit v3 + Testcontainers.MsSql + FluentAssertions

## Executive Summary

The Lexiq backend has **strong coverage** for:
- ✅ Authentication & JWT (AuthController, GoogleAuthService, JwtService)
- ✅ Authorization matrix (92 tests across all endpoints and roles)
- ✅ **User/role management** (UserManagementController: 22 tests, RoleManagementController: 7 tests)
- ✅ **File upload security** (FileUploadsService: 57 tests - path traversal, sanitization, validation)
- ✅ Core learning workflows (exercise submission, progress, unlocking)
- ✅ Leaderboard, streaks, levels, and XP calculations
- ✅ Content CRUD (Language, Course, Lesson - both service and E2E layers)
- ✅ Exercise validation (all 4 types: FillInBlank, Translation, Listening, MultipleChoice)

**Critical gaps** identified:
- ⚠️ **Admin controllers authorization** (UserManagement, RoleManagement) - tests exist but controllers lack `[Authorize]` attributes - **security risk**
- ❌ Avatar upload validation (file size, type, upsert behavior)
- ❌ User enrollment/language management
- ❌ Achievement unlock logic
- ❌ Profile assembly service
- ❌ User XP service

---

## Coverage Gaps by Priority

### Priority 1: Security & Critical Business Logic

#### 1. Avatar Upload Validation (UserController + AvatarService)
**Status**: ⚠️ Authorization tested, but file validation and upsert logic NOT tested
**Impact**: High - user-facing feature with file upload security implications

**Missing test scenarios:**
- File size validation (max 1MB)
- File type validation (jpg, jpeg, png, gif, webp)
- Empty file rejection
- Upsert behavior (create vs. update existing avatar)
- Binary storage correctness
- Content-Type header mapping
- Download from Google (DownloadAvatarAsync)
- Batch existence checks (GetUsersWithAvatarsAsync)

**Recommended test class:**
```
Tests/Services/AvatarServiceTests.cs
Tests/Controllers/UserControllerTests.cs (E2E file upload)
```

**Critical tests to write:**
```csharp
[Fact] PutAvatar_ValidImage_CreatesNewAvatar()
[Fact] PutAvatar_ValidImage_UpdatesExistingAvatar()
[Fact] PutAvatar_ExceedsSize_Returns400()
[Fact] PutAvatar_InvalidType_Returns400()
[Fact] PutAvatar_EmptyFile_Returns400()
[Fact] GetAvatar_ExistingUser_Returns200WithImageBytes()
[Fact] GetAvatar_NoAvatar_Returns404()
[Fact] ValidateAvatarFile_ValidExtensions_ReturnsTrue()
[Fact] ValidateAvatarFile_InvalidExtension_ReturnsFalseWithError()
[Fact] DownloadAvatarAsync_GoogleUrl_ReturnsImageBytes()
[Fact] DownloadAvatarAsync_ExceedsSize_ReturnsNull()
[Fact] GetUsersWithAvatarsAsync_ReturnsOnlyUsersWithAvatars()
```

---

#### 2. File Upload Service Security (FileUploadsService + UploadsController)
**Status**: ✅ **COMPLETE** (57 tests implemented)
**Impact**: High - file upload endpoint exposed to authenticated users

**Test Coverage Implemented:**
- ✅ Path traversal protection via `Path.GetFileName()` stripping
- ✅ Embedded special characters rejection (`..`, `\`)
- ✅ GUID filename generation prevents all filename-based attacks
- ✅ Size limits per category (5MB image, 10MB document, 50MB video, 10MB audio)
- ✅ Extension validation for all file types (image, document, video, audio)
- ✅ Upload by URL with error handling (empty/invalid/unreachable URLs)
- ✅ Physical path retrieval and cross-folder search
- ✅ `IsPathWithinUploadsDirectory` security checks

**Test Class:**
```
Tests/Services/FileUploadsServiceTests.cs (57 tests)
```

**Tests Organized By:**
1. **Infrastructure** (test setup, helpers, mocks)
2. **Security** (8 tests - path traversal, sanitization, GUID protection)
3. **Validation** (8 tests - null/empty, size limits, extension validation)
4. **File Type Tests** (28 Theory tests - all valid extensions per category)
5. **Upload by URL** (4 tests - empty/invalid/unreachable URL handling)
6. **Physical Path** (9 tests - retrieval, search, helper methods)

---

#### 3. User & Role Management Controllers Authorization
**Status**: ⚠️ **SECURITY GAP** - Controllers lack `[Authorize]` attributes but tests already exist
**Impact**: Critical - admin endpoints accessible without authentication

**Current state:**
- ✅ **Tests exist**: UserManagementControllerTests.cs (22 tests), RoleManagementControllerTests.cs (7 tests)
- ❌ **Controllers unprotected**: Both controllers have `[Authorize(Roles = "Admin")]` at class level but this was added recently - verify in production
- ⚠️ **Tests document expected behavior**: All tests verify 401/403 responses for unauthorized access

**Immediate action required:**
1. **Verify** `[Authorize(Roles = "Admin")]` is present on both controllers in production code
2. If missing, add the attribute to both controllers
3. Re-run the test suite to confirm authorization enforcement

**Test coverage (already implemented):**
- UserManagementController: GET all/by id/by email, POST assignRole, PUT update/updateLoginDate, DELETE user
- RoleManagementController: GET role by email
- All tests include authorization checks (Admin, Student, Unauthenticated scenarios)
- All mutations restricted to Admin role
- Edge cases: nonexistent users, duplicate role assignments, users without roles

---

### Priority 2: Business Logic Coverage

#### 4. User Language Enrollment (UserLanguageService + UserLanguageController)
**Status**: ❌ No tests exist
**Impact**: Medium - user enrollment feature with FK constraints

**Missing test scenarios:**
- Enrollment creates UserLanguage record
- Duplicate enrollment returns existing record (idempotency)
- Enrollment with invalid languageId returns null
- Unenrollment deletes record
- Unenrollment of non-existent enrollment returns false
- GetUserLanguages includes Language navigation property
- Composite key enforcement (UserId, LanguageId)

**Recommended test class:**
```
Tests/Services/UserLanguageServiceTests.cs
Tests/Controllers/UserLanguageControllerTests.cs (E2E)
```

**Critical tests to write:**
```csharp
[Fact] EnrollUserAsync_ValidLanguage_CreatesUserLanguage()
[Fact] EnrollUserAsync_AlreadyEnrolled_ReturnsExisting()
[Fact] EnrollUserAsync_InvalidLanguage_ReturnsNull()
[Fact] UnenrollUserAsync_ExistingEnrollment_ReturnsTrue()
[Fact] UnenrollUserAsync_NonExistentEnrollment_ReturnsFalse()
[Fact] GetUserLanguagesAsync_IncludesLanguageNavigation()
[Fact] EnrollUserAsync_SetsEnrolledAtTimestamp()
```

---

#### 5. Achievement Service (AchievementService)
**Status**: ❌ No tests exist
**Impact**: Medium - gamification feature with XP-based unlock logic

**Missing test scenarios:**
- CheckAndUnlockAchievementsAsync unlocks qualifying achievements
- Does not re-unlock already unlocked achievements
- Multiple achievements unlock when threshold crossed
- No unlocks when XP below threshold
- GetUserAchievementsAsync returns all achievements with unlock status
- Unlocked achievements have UnlockedAt timestamp

**Recommended test class:**
```
Tests/Services/AchievementServiceTests.cs
```

**Critical tests to write:**
```csharp
[Fact] CheckAndUnlockAchievementsAsync_QualifyingXp_UnlocksAchievement()
[Fact] CheckAndUnlockAchievementsAsync_AlreadyUnlocked_DoesNotDuplicate()
[Fact] CheckAndUnlockAchievementsAsync_MultipleThresholds_UnlocksAll()
[Fact] CheckAndUnlockAchievementsAsync_BelowThreshold_NoUnlock()
[Fact] GetUserAchievementsAsync_ReturnsAllWithStatus()
[Fact] CheckAndUnlockAchievementsAsync_SetsUnlockedAtTimestamp()
```

---

#### 6. Profile Service (ProfileService)
**Status**: ❌ No tests exist
**Impact**: Medium - aggregates data from multiple services

**Missing test scenarios:**
- GetUserProfileAsync assembles profile from multiple sources
- Returns null for non-existent user
- Includes streak data from StreakService
- Includes level calculation from LeaderboardService
- Includes avatar URL if avatar exists
- Includes achievements from AchievementService
- Username fallback chain handled correctly

**Recommended test class:**
```
Tests/Services/ProfileServiceTests.cs
```

**Critical tests to write:**
```csharp
[Fact] GetUserProfileAsync_ExistingUser_ReturnsCompleteProfile()
[Fact] GetUserProfileAsync_NonExistentUser_ReturnsNull()
[Fact] GetUserProfileAsync_WithAvatar_IncludesAvatarUrl()
[Fact] GetUserProfileAsync_NoAvatar_AvatarUrlIsNull()
[Fact] GetUserProfileAsync_IncludesStreakFromStreakService()
[Fact] GetUserProfileAsync_IncludesLevelFromLeaderboardService()
[Fact] GetUserProfileAsync_IncludesAchievements()
```

---

#### 7. User XP Service (UserXpService)
**Status**: ❌ No tests exist
**Impact**: Low - simple aggregation, covered indirectly by E2E tests

**Missing test scenarios:**
- GetUserXpAsync sums PointsEarned correctly
- Returns null for non-existent user
- Counts completed exercises
- Returns last activity timestamp
- Handles user with no progress

**Recommended test class:**
```
Tests/Services/UserXpServiceTests.cs
```

**Critical tests to write:**
```csharp
[Fact] GetUserXpAsync_ExistingUser_SumsXpCorrectly()
[Fact] GetUserXpAsync_NonExistentUser_ReturnsNull()
[Fact] GetUserXpAsync_CountsCompletedExercises()
[Fact] GetUserXpAsync_ReturnsLastActivityTimestamp()
[Fact] GetUserXpAsync_NoProgress_ReturnsZero()
```

---

### Priority 3: Test Endpoint (E2E Support)

#### 8. Exercise Correct Answer Endpoint
**Status**: ❌ Documented in `e2e-backend-changes.md` but NOT implemented
**Impact**: Low - nice-to-have for E2E tests

**Recommendation**: Implement `GET /api/exercises/{id}/correct-answer` per the spec in `docs/backend/e2e-backend-changes.md`

**Implementation checklist:**
- [ ] Add endpoint to ExerciseController
- [ ] Add CorrectAnswerDto
- [ ] Add GetCorrectAnswerForExercise helper
- [ ] Write controller tests (4 tests as specified in docs)

---

## Test Scenarios by Domain

### EF Core Edge Cases (Partially Covered)

**Covered:**
✅ Multiple cascade paths (UserExerciseProgress.ExerciseId uses NoAction)
✅ TPH polymorphic queries (MultipleChoiceExercise with Options)

**Missing:**
❌ Shadow FK verification - test that `ExerciseId1` shadow column doesn't appear
❌ Composite key enforcement - UserLanguage (UserId, LanguageId)
❌ Cascade delete behavior - Language → Course → Lesson → Exercise chain

**Recommended additions:**
```csharp
[Fact] DeleteLanguage_WithFullHierarchy_CascadesCorrectly()
[Fact] UserExerciseProgress_NoShadowFk_ExerciseIdColumnExists()
[Fact] UserLanguage_CompositeKey_EnforcesUniqueness()
```

---

### Exercise Flow (Well Covered)

**Covered:**
✅ Submit correct answer → unlock next exercise
✅ Submit wrong answer → infinite retry
✅ Idempotent correct submission (no double XP)
✅ Lesson completion → unlock next lesson
✅ Admin bypass for locked exercises

**No gaps identified** - this is the most thoroughly tested domain.

---

### Avatar Integration (Partial Coverage)

**Covered:**
✅ Leaderboard includes avatar URLs
✅ GET /api/user/{id}/avatar authorization

**Missing:**
❌ PUT /api/user/avatar file validation
❌ AvatarService.UpsertAvatarAsync behavior
❌ AvatarService.DownloadAvatarAsync from Google
❌ Size/type validation logic

See **Priority 1, Item 1** above for detailed avatar test plan.

---

## Recommendations

### Immediate Actions (This Sprint)

1. ✅ **User/Role management tests complete**: UserManagementControllerTests (22 tests), RoleManagementControllerTests (7 tests)
2. ✅ **File upload security tests complete**: FileUploadsServiceTests (57 tests)
3. ⚠️ **Verify controller authorization**: Confirm `[Authorize(Roles = "Admin")]` is present on UserManagement and RoleManagement controllers
4. **Add avatar upload tests**: UserController + AvatarService (10-12 tests)

### Short Term (Next Sprint)

5. **Add user enrollment tests**: UserLanguageService (6-8 tests)
6. **Add achievement tests**: AchievementService (5-6 tests)
7. **Add profile service tests**: ProfileService (6-7 tests)

### Nice to Have

8. **Add UserXpService tests**: Simple aggregation logic (5 tests)
9. **Add EF Core edge case tests**: Shadow FK, composite keys, cascade deletes (3 tests)

---

## Test Organization Strategy

Follow existing patterns from `backend/Tests/CLAUDE.md`:

### Service Layer Tests
- **File**: `Tests/Services/{ServiceName}Tests.cs`
- **Base class**: `IClassFixture<DatabaseFixture>, IAsyncLifetime`
- **Pattern**: Use real DbContext, no mocking (integration style)
- **Example**: `AvatarServiceTests.cs`, `UserLanguageServiceTests.cs`

### Controller Tests (E2E)
- **File**: `Tests/End-to-End/{Feature}Tests.cs` OR `Tests/Controllers/{Controller}Tests.cs`
- **Base class**: `ControllerTestBase(DatabaseFixture)`
- **Pattern**: WebApplicationFactory + HTTP calls
- **Example**: `UserAvatarUploadTests.cs`, `FileUploadSecurityTests.cs`

### Pure Unit Tests
- **File**: `Tests/Services/{ServiceName}Tests.cs`
- **Pattern**: No database, static methods only
- **Example**: `AvatarService.ValidateAvatarFile` (static validation)

---

## Estimated Test Count by Area

| Area | Current | Recommended | Total After |
|------|---------|-------------|-------------|
| Avatar upload | 0 | 12 | 12 |
| File uploads | 57 ✅ | 0 | 57 |
| User/Role management | 29 ✅ | 0 | 29 |
| User enrollment | 0 | 8 | 8 |
| Achievements | 0 | 6 | 6 |
| Profile service | 0 | 7 | 7 |
| UserXp service | 0 | 5 | 5 |
| EF Core edge cases | 0 | 3 | 3 |
| **Total** | **~332** | **+41** | **~373** |

---

## Coverage Metrics After Implementation

### Service Coverage
- **Current**: 11/17 services tested (65%) — added FileUploadsService
- **After remaining work**: 17/17 services tested (100%)

### Controller Coverage
- **Current**: 7/11 controllers tested (64%) — added UserManagement, RoleManagement
- **After remaining work**: 11/11 controllers tested (100%)

### Business Logic Coverage
- **Before**: Strong (auth, content, progress, leaderboard)
- **After**: Comprehensive (adds file uploads, avatars, enrollment, achievements, profile)

---

## Next Steps

1. **Review this analysis** with the team
2. **Prioritize** security gaps (file uploads, user management authorization)
3. **Assign** test implementation to sprint backlog
4. **Document** new test patterns in `backend/Tests/CLAUDE.md` as they're added
5. **Update** `docs/backend/tests.md` after each test class is completed

---

**Document maintained by**: Claude Code
**Last updated**: 2026-03-24
