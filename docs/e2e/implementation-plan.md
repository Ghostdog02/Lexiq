# E2E Testing Implementation Plan

## Overview

This document provides a step-by-step guide to implement E2E testing for Lexiq. Follow phases sequentially for a smooth implementation.

---

## Phase 1: Setup & Configuration

**Estimated Time**: 30 minutes

### 1.1 Install Playwright

```bash
cd frontend/
npm install -D @playwright/test
npx playwright install chromium
```

### 1.2 Create Playwright Config

Create `frontend/playwright.config.ts`:

```typescript
import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: 'html',

  use: {
    baseURL: 'http://localhost:4200',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],

  webServer: {
    command: 'echo "Docker Compose should be running"',
    url: 'http://localhost:4200',
    reuseExistingServer: true,
    timeout: 5000,
  },
});
```

### 1.3 Create Directory Structure

```bash
mkdir -p frontend/e2e/{fixtures,helpers,pages,tests}
touch frontend/e2e/fixtures/.gitkeep
```

### 1.4 Update package.json

Add test scripts to `frontend/package.json`:

```json
{
  "scripts": {
    "test:e2e": "playwright test",
    "test:e2e:ui": "playwright test --ui",
    "test:e2e:debug": "playwright test --debug",
    "test:e2e:headed": "playwright test --headed",
    "test:e2e:report": "playwright show-report"
  }
}
```

### 1.5 Verify Setup

```bash
# Ensure Docker Compose is running
cd ..
docker compose up -d

# Verify services
curl http://localhost:4200
curl http://localhost:8080/api/courses

# Test Playwright installation
cd frontend/
npx playwright test --version
```

**✅ Phase 1 Complete**: Playwright installed and configured

---

## Phase 2: Backend Changes

**Estimated Time**: 1 hour

### 2.1 Add Test Endpoint

**File**: `backend/Controllers/ExerciseController.cs`

Add method:

```csharp
[HttpGet("{id}/correct-answer")]
[Authorize]
public async Task<ActionResult<CorrectAnswerDto>> GetCorrectAnswer(string id)
{
    var exercise = await _context.Exercises
        .Include(e => (e as MultipleChoiceExercise)!.Options)
        .FirstOrDefaultAsync(e => e.Id == id);

    if (exercise == null)
        return NotFound(new { message = "Exercise not found" });

    var correctAnswer = GetCorrectAnswerForExercise(exercise);
    return Ok(new CorrectAnswerDto(correctAnswer));
}

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

### 2.2 Add DTO

Create `backend/Dtos/CorrectAnswerDto.cs`:

```csharp
namespace Backend.Api.Dtos;

public record CorrectAnswerDto(string? CorrectAnswer);
```

### 2.3 Test Backend Endpoint

```bash
cd backend/
dotnet build
dotnet run

# In another terminal, login and test endpoint
# (See backend-changes.md for full curl commands)
```

**✅ Phase 2 Complete**: Backend test endpoint working

---

## Phase 3: Test Helpers

**Estimated Time**: 45 minutes

### 3.1 Create Auth Helper

**File**: `frontend/e2e/helpers/auth.helper.ts`

```typescript
import { Page, request } from '@playwright/test';

export interface TestUser {
  id: string;
  email: string;
  userName: string;
  token: string;
}

export async function createTestUser(
  email: string,
  userName: string
): Promise<TestUser> {
  const apiContext = await request.newContext({
    baseURL: 'http://localhost:8080'
  });

  const response = await apiContext.post('/api/auth/google-login', {
    data: {
      idToken: 'mock-google-id-token-' + Date.now(),
      email: email,
      name: userName,
      picture: 'https://example.com/avatar.jpg',
    },
  });

  if (!response.ok()) {
    throw new Error(`Failed to create test user: ${response.status()}`);
  }

  const cookies = response.headers()['set-cookie'];
  const authTokenMatch = cookies?.match(/AuthToken=([^;]+)/);

  if (!authTokenMatch) {
    throw new Error('No AuthToken cookie in response headers');
  }

  const token = authTokenMatch[1];
  const userData = await response.json();

  await apiContext.dispose();

  return {
    id: userData.id,
    email: userData.email,
    userName: userData.userName,
    token: token,
  };
}

export async function loginAsUser(page: Page, user: TestUser): Promise<void> {
  await page.goto('/');

  await page.context().addCookies([
    {
      name: 'AuthToken',
      value: user.token,
      domain: 'localhost',
      path: '/',
      httpOnly: true,
      sameSite: 'Lax',
      expires: Math.floor(Date.now() / 1000) + 24 * 60 * 60,
    },
  ]);

  await page.reload();
  await page.waitForLoadState('networkidle');
}

export function generateTestEmail(testName: string): string {
  const timestamp = Date.now();
  const sanitized = testName.toLowerCase().replace(/[^a-z0-9]+/g, '-');
  return `test-${sanitized}-${timestamp}@example.com`;
}
```

### 3.2 Create Test Data Helper

**File**: `frontend/e2e/helpers/test-data.helper.ts`

```typescript
import { request, APIRequestContext } from '@playwright/test';

export interface SeedData {
  courseId: string;
  courseName: string;
  lessons: Array<{
    lessonId: string;
    title: string;
    isLocked: boolean;
  }>;
}

let cachedSeedData: SeedData | null = null;

export async function getSeedData(): Promise<SeedData> {
  if (cachedSeedData) {
    return cachedSeedData;
  }

  const apiContext = await request.newContext({
    baseURL: 'http://localhost:8080'
  });

  const coursesResponse = await apiContext.get('/api/courses');
  const courses = await coursesResponse.json();
  const course = courses[0];

  if (!course) {
    throw new Error('No courses found in seed data');
  }

  const lessonsResponse = await apiContext.get(
    `/api/lessons/course/${course.id}`
  );
  const lessons = await lessonsResponse.json();

  cachedSeedData = {
    courseId: course.id,
    courseName: course.title,
    lessons: lessons.map((l: any) => ({
      lessonId: l.lessonId,
      title: l.title,
      isLocked: l.isLocked,
    })),
  };

  await apiContext.dispose();
  return cachedSeedData;
}

export async function getUnlockedLesson(): Promise<{ lessonId: string; title: string }> {
  const seedData = await getSeedData();
  const unlocked = seedData.lessons.find(l => !l.isLocked);

  if (!unlocked) {
    throw new Error('No unlocked lessons found');
  }

  return unlocked;
}

export async function getCorrectAnswer(
  request: APIRequestContext,
  exerciseId: string
): Promise<string> {
  const response = await request.get(
    `/api/exercises/${exerciseId}/correct-answer`
  );

  if (!response.ok()) {
    throw new Error(`Failed to get correct answer: ${response.status()}`);
  }

  const body = await response.json();
  return body.correctAnswer;
}
```

**✅ Phase 3 Complete**: Test helpers implemented

---

## Phase 4: Page Objects

**Estimated Time**: 1.5 hours

Create page objects in order (each builds on previous):

### 4.1 Base Page

**File**: `frontend/e2e/pages/base.page.ts` - See [page-objects.md](./page-objects.md)

### 4.2 Login Page

**File**: `frontend/e2e/pages/login.page.ts` - See [page-objects.md](./page-objects.md)

### 4.3 Home Page

**File**: `frontend/e2e/pages/home.page.ts` - See [page-objects.md](./page-objects.md)

### 4.4 Exercise Page

**File**: `frontend/e2e/pages/exercise.page.ts` - See [page-objects.md](./page-objects.md)

### 4.5 Other Pages

- `leaderboard.page.ts`
- `profile.page.ts`

**✅ Phase 4 Complete**: Page objects created

---

## Phase 5: Add data-testid Attributes

**Estimated Time**: 1 hour

Add `data-testid` attributes to Angular components following [data-testid-guide.md](./data-testid-guide.md).

### Priority Order

1. **Exercise page** - Most complex interactions
2. **Home page** - Course/lesson selection
3. **Login page** - Authentication entry point
4. **Navigation** - Logout, user menu
5. **Leaderboard** - (optional, can test without data-testid first)
6. **Profile** - (optional)

### Verification

After adding data-testid, verify in browser:

```javascript
// Open DevTools console
document.querySelector('[data-testid="submit-btn"]')
document.querySelectorAll('[data-testid="lesson-card"]').length
```

**✅ Phase 5 Complete**: Components have test selectors

---

## Phase 6: Write Tests

**Estimated Time**: 2 hours

### 6.1 Authentication Tests

**File**: `frontend/e2e/tests/auth.spec.ts` - See [test-suites.md](./test-suites.md)

Start with basic test:

```typescript
import { test, expect } from '@playwright/test';
import { createTestUser, loginAsUser, generateTestEmail } from '../helpers/auth.helper';
import { LoginPage } from '../pages/login.page';

test('should show login page', async ({ page }) => {
  const loginPage = new LoginPage(page);
  await loginPage.goto();
  await loginPage.waitForLoad();
  await loginPage.expectLoginPage();
});
```

Run test:

```bash
npx playwright test auth.spec.ts --headed
```

**Debug failures**: Use `--debug` flag to step through test.

### 6.2 Lesson Flow Tests

**File**: `frontend/e2e/tests/lesson-flow.spec.ts` - See [test-suites.md](./test-suites.md)

**✅ Phase 6 Complete**: Tests written and passing

---

## Phase 7: CI/CD Integration

**Estimated Time**: 30 minutes

### 7.1 Create GitHub Workflow

**File**: `.github/workflows/e2e-tests.yml` - See [setup.md](./setup.md)

### 7.2 Test CI Pipeline

```bash
# Push to branch and create PR
git checkout -b add-e2e-tests
git add .
git commit -m "Add E2E testing with Playwright"
git push origin add-e2e-tests

# Watch GitHub Actions run
```

**✅ Phase 7 Complete**: E2E tests run in CI

---

## Verification Checklist

After completing all phases:

- [ ] Playwright installed: `npx playwright --version`
- [ ] Backend endpoint works: `curl localhost:8080/api/exercises/{id}/correct-answer -H "Cookie: AuthToken=..."`
- [ ] Auth helper creates users: Check database for test users
- [ ] Test data helper fetches seed data: Logs show course/lesson IDs
- [ ] Page objects compile: `npx tsc --noEmit` (if using TypeScript)
- [ ] data-testid attributes present: Browser DevTools shows elements
- [ ] Auth tests pass: `npx playwright test auth.spec.ts`
- [ ] Lesson flow tests pass: `npx playwright test lesson-flow.spec.ts`
- [ ] All tests pass: `npx playwright test`
- [ ] CI pipeline runs: GitHub Actions shows green checkmark

---

## Troubleshooting Guide

### Tests Fail to Find Elements

**Problem**: `Timeout 30000ms exceeded waiting for locator`

**Solutions**:
1. Verify `data-testid` attribute exists in browser DevTools
2. Check selector syntax: `[data-testid="element-name"]`
3. Increase timeout: `test.setTimeout(60000)`
4. Run in headed mode to see what's happening: `--headed`

### Backend Endpoint Returns 401

**Problem**: `GET /api/exercises/{id}/correct-answer` returns Unauthorized

**Solutions**:
1. Verify JWT cookie is set: Check browser cookies in DevTools
2. Check `loginAsUser()` was called before test navigates
3. Verify backend accepts the JWT token (check backend logs)

### Seed Data Not Found

**Problem**: `No courses found in seed data`

**Solutions**:
1. Verify Docker Compose is running: `docker compose ps`
2. Check backend logs: `docker compose logs backend`
3. Verify migrations ran: Check for seed data creation logs
4. Restart Docker Compose: `docker compose down -v && docker compose up --build`

### Tests Pass Locally But Fail in CI

**Problem**: Tests pass on local machine, fail in GitHub Actions

**Solutions**:
1. Check Docker Compose health in CI logs
2. Verify timeout values are sufficient for CI environment
3. Add explicit waits: `await page.waitForLoadState('networkidle')`
4. Check screenshots in CI artifacts

---

## Next Steps After Implementation

1. **Expand test coverage**:
   - Add more lesson flow scenarios
   - Test error cases (network failures, invalid inputs)
   - Add leaderboard and profile tests

2. **Improve test stability**:
   - Add custom wait conditions
   - Reduce flakiness with better selectors
   - Add retry logic for network-dependent tests

3. **Performance testing**:
   - Measure page load times
   - Add performance budgets
   - Monitor test execution time

4. **Accessibility testing**:
   - Add keyboard navigation tests
   - Test screen reader compatibility
   - Verify ARIA labels

---

## Questions?

If you encounter issues not covered here:

1. Check [troubleshooting section](#troubleshooting-guide)
2. Review [Playwright documentation](https://playwright.dev)
3. Check backend logs: `docker compose logs backend`
4. Run with verbose logging: `DEBUG=pw:api npx playwright test`

## Time Estimate Summary

| Phase | Time | Cumulative |
|-------|------|------------|
| 1. Setup | 30 min | 30 min |
| 2. Backend | 1 hour | 1.5 hours |
| 3. Helpers | 45 min | 2.25 hours |
| 4. Page Objects | 1.5 hours | 3.75 hours |
| 5. data-testid | 1 hour | 4.75 hours |
| 6. Write Tests | 2 hours | 6.75 hours |
| 7. CI/CD | 30 min | 7.25 hours |

**Total Estimated Time**: ~7-8 hours for full implementation
