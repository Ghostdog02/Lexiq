# Test Data Strategy

## Overview

E2E tests use **seeded data from backend DatabaseFixture** for courses, lessons, and exercises. This data is permanent and consistent across all tests.

## Seed Data Reference

### Course

- **Title**: "Italian for Beginners"
- **Description**: "A structured introduction to Italian covering greetings, essential vocabulary, present tense verbs, and everyday conversation. Designed for Bulgarian speakers learning Italian from scratch."
- **Language**: Italian (flag: https://flagcdn.com/w40/it.png)

### Lessons (8 total)

| Index | Title | Duration | IsLocked | Exercises |
|-------|-------|----------|----------|-----------|
| 0 | Greetings and Introductions | 15 min | ❌ false | 4 |
| 1 | Numbers 1 to 20 | 20 min | ✅ true | 3 |
| 2 | Colors and Descriptions | 18 min | ✅ true | 3 |
| 3 | Food and Ordering | 22 min | ✅ true | 4 |
| 4 | Travel and Directions | 20 min | ✅ true | 4 |
| 5 | Present Tense Verbs | 25 min | ✅ true | 4 |
| 6 | Days and Time | 18 min | ✅ true | 3 |
| 7 | First Conversations | 25 min | ✅ true | 4 |

**Total**: 28 exercises across 8 lessons

### Lesson 0: Greetings and Introductions (Unlocked)

| Type | Title | Points | Difficulty | Answer | Locked |
|------|-------|--------|-----------|--------|--------|
| MultipleChoice | What does 'Ciao' mean? | 10 | Beginner | Option: "Hello" | ❌ |
| FillInBlank | Complete: Mi _______ Marco | 10 | Beginner | "chiamo" | ✅ |
| Translation | Translate: Good evening | 15 | Beginner | "Buonasera" | ✅ |
| Listening | Listen and write: Buongiorno | 15 | Beginner | "Buongiorno" | ✅ |

**First exercise unlocked, rest locked until previous is completed.**

## Discovering Seed Data IDs

### Option 1: Fetch IDs at Runtime (Recommended)

**File**: `e2e/helpers/test-data.helper.ts`

```typescript
import { request } from '@playwright/test';

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

/**
 * Fetches seed data IDs from backend API
 * Caches result to avoid redundant API calls
 */
export async function getSeedData(): Promise<SeedData> {
  if (cachedSeedData) {
    return cachedSeedData;
  }

  const apiContext = await request.newContext({
    baseURL: 'http://localhost:8080'
  });

  // Get first course (Italian for Beginners)
  const coursesResponse = await apiContext.get('/api/courses');
  const courses = await coursesResponse.json();
  const course = courses[0];

  if (!course) {
    throw new Error('No courses found in seed data');
  }

  // Get lessons for that course
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

/**
 * Gets the first unlocked lesson (Lesson 0: Greetings)
 */
export async function getUnlockedLesson(): Promise<{ lessonId: string; title: string }> {
  const seedData = await getSeedData();
  const unlocked = seedData.lessons.find(l => !l.isLocked);

  if (!unlocked) {
    throw new Error('No unlocked lessons found');
  }

  return unlocked;
}

/**
 * Gets exercise IDs for a lesson
 */
export async function getExerciseIds(lessonId: string): Promise<string[]> {
  const apiContext = await request.newContext({
    baseURL: 'http://localhost:8080'
  });

  const response = await apiContext.get(`/api/exercises/lesson/${lessonId}`);
  const exercises = await response.json();

  await apiContext.dispose();

  return exercises.map((e: any) => e.id);
}
```

### Usage in Tests

```typescript
import { getSeedData, getUnlockedLesson } from '../helpers/test-data.helper';

test('should solve first lesson', async ({ page }) => {
  const lesson = await getUnlockedLesson();

  // Navigate to lesson
  await page.goto(`/lesson/${lesson.lessonId}`);

  // Lesson title should match seed data
  await expect(page.locator('h1')).toContainText(lesson.title);
});
```

## Test User Creation

### Unique Emails Per Test

**Strategy**: Generate unique email for each test run to avoid conflicts.

```typescript
/**
 * Generates unique test email for each test
 * Format: test-{sanitized-test-name}-{timestamp}@example.com
 */
export function generateTestEmail(testName: string): string {
  const timestamp = Date.now();
  const sanitized = testName.toLowerCase().replace(/\s+/g, '-');
  return `test-${sanitized}-${timestamp}@example.com`;
}

/**
 * Generates unique username
 * Format: TestUser_{timestamp}
 */
export function generateTestUsername(prefix: string = 'TestUser'): string {
  return `${prefix}_${Date.now()}`;
}
```

### Creating Test Users

Test users are created via backend `/api/auth/google-login` endpoint with mock Google token.

**See**: [authentication.md](./authentication.md) for complete implementation.

## Test Data Lifecycle

### Before Test Suite

1. Docker Compose starts
2. Backend runs EF Core migrations
3. DatabaseFixture seeds content hierarchy:
   - System user (excluded from cleanup)
   - Language → Course → 8 Lessons → 28 Exercises

### During Test

1. Test creates unique user via mock Google login
2. JWT token returned in AuthToken cookie
3. Test runs with authenticated user
4. User data accumulates in database

### After Test

**No cleanup** - Test users remain in database with unique emails.

**Rationale**:
- ✅ Fast (no cleanup overhead)
- ✅ Simple (no DELETE endpoint needed)
- ✅ Independent (parallel tests don't conflict)
- ✅ Debuggable (can inspect user data after failure)

**Cleanup Strategy** (if needed in future):
- Manual: `docker compose down -v` removes all data
- Automated: Add global teardown to delete users with email prefix `test-`

## Answer Lookup Strategy

### Test-Only Backend Endpoint

**Endpoint**: `GET /api/exercises/{id}/correct-answer`

**Purpose**: Allow tests to fetch correct answer without inspecting exercise details.

**Authorization**: Authenticated users only (any role).

**Response**:
```json
{
  "correctAnswer": "Buonasera"
}
```

**Implementation**: See [e2e-backend-changes.md](../backend/e2e-backend-changes.md)

### Usage in Tests

```typescript
import { getCorrectAnswer } from '../helpers/test-data.helper';

test('should submit correct answer', async ({ page, request }) => {
  const exerciseId = 'some-exercise-id';

  // Fetch correct answer from test endpoint
  const correctAnswer = await getCorrectAnswer(request, exerciseId);

  // Submit answer in UI
  await page.locator('[data-testid="answer-input"]').fill(correctAnswer);
  await page.locator('[data-testid="submit-btn"]').click();

  // Verify correct feedback
  await expect(page.locator('[data-testid="feedback"]'))
    .toContainText('Correct');
});
```

### Helper Implementation

```typescript
/**
 * Fetches correct answer for an exercise from test-only endpoint
 */
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

## Seed Data Stability

### What Persists

- ✅ System user
- ✅ Language (Italian)
- ✅ Course (Italian for Beginners)
- ✅ All 8 lessons
- ✅ All 28 exercises

### What Gets Cleared

- ❌ Test users (remain, but unique per test)
- ❌ User exercise progress (none by default)
- ❌ User avatars (none by default)

### Regenerating Seed Data

If seed data changes (new lessons, different exercises):

1. Update backend seed logic
2. Restart Docker Compose: `docker compose down -v && docker compose up --build`
3. E2E tests automatically discover new IDs via `getSeedData()`

No test code changes needed!
