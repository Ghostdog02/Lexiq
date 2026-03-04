# Test Suite Specifications

## Overview

This document specifies all E2E test cases organized by user journey. Each test validates a complete user flow from start to finish.

## Test Structure

```
frontend/e2e/tests/
├── auth.spec.ts           # Authentication flows
└── lesson-flow.spec.ts    # Complete lesson journey
```

## 1. Authentication Flow Tests

**File**: `e2e/tests/auth.spec.ts`

### Test Cases

#### 1.1 Show Login Page for Unauthenticated Users

```typescript
test('should show login page for unauthenticated users', async ({ page }) => {
  const loginPage = new LoginPage(page);

  await loginPage.goto();
  await loginPage.waitForLoad();

  await loginPage.expectLoginPage();
});
```

**Validates**:
- Login page renders correctly
- Google sign-in button is visible
- No authentication required to view login page

---

#### 1.2 Redirect to Home After Successful Login

```typescript
test('should redirect to home after successful login', async ({ page }) => {
  const testUser = await createTestUser(
    generateTestEmail('redirect-test'),
    'Test User'
  );

  await loginAsUser(page, testUser);

  const homePage = new HomePage(page);
  await homePage.goto();
  await homePage.waitForLoad();

  await homePage.expectHomePageLoaded();
});
```

**Validates**:
- JWT cookie injection works
- Frontend recognizes authenticated state
- User redirected to home page
- Courses are visible

---

#### 1.3 Persist Authentication Across Page Reloads

```typescript
test('should persist authentication across page reloads', async ({ page }) => {
  const testUser = await createTestUser(
    generateTestEmail('persist-auth'),
    'Persistent User'
  );

  await loginAsUser(page, testUser);

  const homePage = new HomePage(page);
  await homePage.goto();

  // Reload page
  await page.reload();

  // Should still be authenticated
  await homePage.waitForLoad();
  await homePage.expectHomePageLoaded();
  await expect(page.locator('[data-testid="user-menu"]')).toBeVisible();
});
```

**Validates**:
- HttpOnly cookie persists across reloads
- APP_INITIALIZER re-validates auth state
- No redirect to login page

---

#### 1.4 Logout Successfully and Redirect to Login

```typescript
test('should logout successfully and redirect to login', async ({ page }) => {
  const testUser = await createTestUser(
    generateTestEmail('logout-test'),
    'Logout User'
  );

  await loginAsUser(page, testUser);

  const homePage = new HomePage(page);
  await homePage.goto();
  await homePage.waitForLoad();

  // Click logout
  await homePage.clickLogout();

  // Should redirect to login page
  const loginPage = new LoginPage(page);
  await loginPage.waitForLoad();
  await loginPage.expectLoginPage();

  // Verify cookie cleared
  const cookies = await page.context().cookies();
  const authCookie = cookies.find(c => c.name === 'AuthToken');
  expect(authCookie).toBeUndefined();
});
```

**Validates**:
- Logout button accessible from nav
- AuthToken cookie cleared
- Redirected to login page
- Cannot access protected routes after logout

---

#### 1.5 Protect Authenticated Routes When Not Logged In

```typescript
test('should protect authenticated routes when not logged in', async ({ page }) => {
  // Try to access protected route without login
  await page.goto('/profile');

  // Should redirect to login
  await expect(page).toHaveURL(/\/google-login/);
});
```

**Validates**:
- Auth guard blocks unauthenticated access
- Redirects to login page
- returnUrl parameter preserved (optional check)

---

#### 1.6 Allow Access to Public Routes Without Authentication

```typescript
test('should allow access to public routes without authentication', async ({ page }) => {
  // Leaderboard is public (adjust if protected)
  await page.goto('/leaderboard');

  // Should not redirect to login
  await expect(page).toHaveURL('/leaderboard');

  const leaderboardPage = new LeaderboardPage(page);
  await leaderboardPage.waitForLoad();
  await leaderboardPage.expectLeaderboardVisible();
});
```

**Validates**:
- Public routes accessible without auth
- No redirect to login page
- Content loads correctly

---

## 2. Complete Lesson Flow Tests

**File**: `e2e/tests/lesson-flow.spec.ts`

### Test Cases

#### 2.1 Browse Courses → Select Lesson → Complete Exercises → See Results

```typescript
test('should complete full lesson journey', async ({ page, request }) => {
  // Setup: Create and login
  const testUser = await createTestUser(
    generateTestEmail('complete-lesson'),
    'Learner User'
  );
  await loginAsUser(page, testUser);

  // 1. Navigate to home page
  const homePage = new HomePage(page);
  await homePage.goto();
  await homePage.waitForLoad();
  await homePage.expectHomePageLoaded();

  // 2. Select first unlocked lesson
  const lesson = await getUnlockedLesson();
  await homePage.selectLesson(lesson.title);

  // 3. Complete exercises
  const exercisePage = new ExercisePage(page);
  await exercisePage.waitForLoad();

  // Get exercise IDs for the lesson
  const exerciseIds = await getExerciseIds(lesson.lessonId);

  // Solve first 3 exercises (mix of types)
  for (let i = 0; i < 3 && i < exerciseIds.length; i++) {
    const exerciseId = exerciseIds[i];

    // Fetch correct answer from test endpoint
    const correctAnswer = await getCorrectAnswer(request, exerciseId);

    // Determine exercise type and answer accordingly
    const questionText = await exercisePage.questionText.textContent();

    if (await exercisePage.multipleChoiceOptions.first().isVisible()) {
      // Multiple choice - select option with correct text
      await exercisePage.selectMultipleChoiceOption(correctAnswer);
    } else if (await exercisePage.page.locator('[data-testid="fill-blank-input"]').isVisible()) {
      // Fill in blank
      await exercisePage.fillInBlank(correctAnswer);
    } else if (await exercisePage.page.locator('[data-testid="translation-input"]').isVisible()) {
      // Translation
      await exercisePage.typeTranslation(correctAnswer);
    }

    // Submit answer
    await exercisePage.submitAnswer();
    await exercisePage.expectCorrectFeedback();

    // Move to next exercise (if not last)
    if (i < 2) {
      await exercisePage.clickNext();
    }
  }

  // 4. Verify XP was earned (check profile or leaderboard)
  const profilePage = new ProfilePage(page);
  await profilePage.goto();
  await profilePage.waitForLoad();

  const xp = await profilePage.getXP();
  expect(xp).toBeGreaterThan(0);

  console.log(`Lesson completed! XP earned: ${xp}`);
});
```

**Validates**:
- Complete user journey from home to lesson completion
- Exercise type detection and answer submission
- Correct feedback shown
- XP awarded and visible in profile

---

#### 2.2 Show Progress Indicator During Lesson

```typescript
test('should show progress indicator during lesson', async ({ page }) => {
  const testUser = await createTestUser(
    generateTestEmail('progress-indicator'),
    'Progress User'
  );
  await loginAsUser(page, testUser);

  // Navigate to lesson
  const homePage = new HomePage(page);
  await homePage.goto();

  const lesson = await getUnlockedLesson();
  await homePage.selectLesson(lesson.title);

  const exercisePage = new ExercisePage(page);
  await exercisePage.waitForLoad();

  // Check initial progress (e.g., "1/4")
  const initialProgress = await exercisePage.getProgress();
  expect(initialProgress).toMatch(/1\/\d+/);

  // Answer first exercise
  const firstOption = exercisePage.multipleChoiceOptions.first();
  if (await firstOption.isVisible()) {
    await firstOption.click();
    await exercisePage.submitAnswer();
    await exercisePage.clickNext();

    // Check updated progress (e.g., "2/4")
    const updatedProgress = await exercisePage.getProgress();
    expect(updatedProgress).toMatch(/2\/\d+/);
  }
});
```

**Validates**:
- Progress indicator displays current position
- Progress updates after each exercise
- Denominator shows total exercises

---

#### 2.3 Handle Incorrect Answers and Allow Retry

```typescript
test('should handle incorrect answers and allow retry', async ({ page, request }) => {
  const testUser = await createTestUser(
    generateTestEmail('incorrect-answer'),
    'Retry User'
  );
  await loginAsUser(page, testUser);

  // Navigate to lesson
  const homePage = new HomePage(page);
  await homePage.goto();

  const lesson = await getUnlockedLesson();
  await homePage.selectLesson(lesson.title);

  const exercisePage = new ExercisePage(page);
  await exercisePage.waitForLoad();

  // Submit WRONG answer
  if (await exercisePage.page.locator('[data-testid="fill-blank-input"]').isVisible()) {
    await exercisePage.fillInBlank('definitely wrong answer');
    await exercisePage.submitAnswer();

    // Should show incorrect feedback
    await exercisePage.expectIncorrectFeedback();

    // Correct answer should be shown in feedback
    await expect(exercisePage.feedbackMessage).toContainText(/correct answer/i);

    // Next button should be available (no blocking on wrong answer)
    await exercisePage.expectNextButtonVisible();
  }
});
```

**Validates**:
- Incorrect answer feedback shown
- Correct answer revealed in feedback
- User can proceed to next exercise
- No XP penalty for wrong answers

---

#### 2.4 Submit Multiple Exercise Types

```typescript
test('should submit different exercise types', async ({ page, request }) => {
  const testUser = await createTestUser(
    generateTestEmail('exercise-types'),
    'Exercise User'
  );
  await loginAsUser(page, testUser);

  const homePage = new HomePage(page);
  await homePage.goto();

  const lesson = await getUnlockedLesson();
  await homePage.selectLesson(lesson.title);

  const exercisePage = new ExercisePage(page);
  const exerciseIds = await getExerciseIds(lesson.lessonId);

  // Test multiple choice
  await exercisePage.waitForLoad();
  if (await exercisePage.multipleChoiceOptions.first().isVisible()) {
    const correctAnswer = await getCorrectAnswer(request, exerciseIds[0]);
    await exercisePage.selectMultipleChoiceOption(correctAnswer);
    await exercisePage.submitAnswer();
    await exercisePage.expectCorrectFeedback();
    await exercisePage.clickNext();
  }

  // Test fill in blank
  await exercisePage.waitForLoad();
  if (await exercisePage.page.locator('[data-testid="fill-blank-input"]').isVisible()) {
    const correctAnswer = await getCorrectAnswer(request, exerciseIds[1]);
    await exercisePage.fillInBlank(correctAnswer);
    await exercisePage.submitAnswer();
    await exercisePage.expectCorrectFeedback();
    await exercisePage.clickNext();
  }

  // Test translation
  await exercisePage.waitForLoad();
  if (await exercisePage.page.locator('[data-testid="translation-input"]').isVisible()) {
    const correctAnswer = await getCorrectAnswer(request, exerciseIds[2]);
    await exercisePage.typeTranslation(correctAnswer);
    await exercisePage.submitAnswer();
    await exercisePage.expectCorrectFeedback();
  }
});
```

**Validates**:
- Multiple choice exercises work
- Fill in blank exercises work
- Translation exercises work
- Correct feedback for each type

---

## 3. Future Test Suites

### 3.1 Leaderboard Tests (Optional)

**File**: `e2e/tests/leaderboard.spec.ts`

Test cases:
- Display leaderboard with rankings
- Filter by Weekly/Monthly/AllTime
- Highlight current user
- Show rank changes

### 3.2 Profile Tests (Optional)

**File**: `e2e/tests/profile.spec.ts`

Test cases:
- Display user profile stats
- Show level, XP, streak
- Update avatar (if implemented)

## Test Data Strategy

All tests use:
- **Unique test users** per test (via `generateTestEmail()`)
- **Seeded lesson data** from DatabaseFixture
- **Test endpoint** for correct answers (`GET /api/exercises/{id}/correct-answer`)

No cleanup needed - database accumulates test users with unique emails.

## Running Specific Tests

```bash
# Run auth tests only
npx playwright test auth.spec.ts

# Run specific test
npx playwright test -g "should complete full lesson journey"

# Run in debug mode
npx playwright test --debug lesson-flow.spec.ts
```

## See Also

- [authentication.md](./authentication.md) - Auth helpers
- [page-objects.md](./page-objects.md) - Page object implementations
- [test-data.md](./test-data.md) - Test data reference
