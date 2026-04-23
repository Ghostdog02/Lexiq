# Page Object Model

## Overview

Page objects encapsulate UI interactions for each page/component, making tests:
- ✅ **Maintainable** - UI changes only require updating page objects
- ✅ **Readable** - Test code expresses user intent, not implementation details
- ✅ **Reusable** - Common actions shared across tests

## Base Page Object

**File**: `e2e/pages/base.page.ts`

```typescript
import { Page, Locator } from '@playwright/test';

export abstract class BasePage {
  constructor(protected page: Page) {}

  /** Navigate to this page */
  abstract goto(): Promise<void>;

  /** Verify page is loaded */
  abstract waitForLoad(): Promise<void>;

  // Common elements across all pages

  get navBar(): Locator {
    return this.page.locator('[data-testid="nav-bar"]');
  }

  get userMenu(): Locator {
    return this.page.locator('[data-testid="user-menu"]');
  }

  get logoutButton(): Locator {
    return this.page.locator('[data-testid="logout-btn"]');
  }

  /** Click logout in navigation */
  async clickLogout(): Promise<void> {
    await this.userMenu.click();
    await this.logoutButton.click();
  }
}
```

## Login Page

**File**: `e2e/pages/login.page.ts`

```typescript
import { Page, Locator, expect } from '@playwright/test';
import { BasePage } from './base.page';

export class LoginPage extends BasePage {
  readonly googleLoginButton: Locator;
  readonly welcomeHeading: Locator;

  constructor(page: Page) {
    super(page);
    this.googleLoginButton = page.locator('[data-testid="google-login-btn"]');
    this.welcomeHeading = page.locator('h1');
  }

  async goto(): Promise<void> {
    await this.page.goto('/google-login');
  }

  async waitForLoad(): Promise<void> {
    await expect(this.googleLoginButton).toBeVisible();
  }

  /** Verify login page is displayed */
  async expectLoginPage(): Promise<void> {
    await expect(this.googleLoginButton).toBeVisible();
    await expect(this.welcomeHeading).toBeVisible();
  }
}
```

## Home Page (Courses & Lessons)

**File**: `e2e/pages/home.page.ts`

```typescript
import { Page, Locator, expect } from '@playwright/test';
import { BasePage } from './base.page';

export class HomePage extends BasePage {
  readonly pageHeading: Locator;
  readonly courseCards: Locator;
  readonly lessonCards: Locator;

  constructor(page: Page) {
    super(page);
    this.pageHeading = page.locator('h1');
    this.courseCards = page.locator('[data-testid="course-card"]');
    this.lessonCards = page.locator('[data-testid="lesson-card"]');
  }

  async goto(): Promise<void> {
    await this.page.goto('/');
  }

  async waitForLoad(): Promise<void> {
    await expect(this.pageHeading).toBeVisible();
  }

  /** Get course card by name */
  getCourseByName(courseName: string): Locator {
    return this.page.locator(
      `[data-testid="course-card"]:has-text("${courseName}")`
    );
  }

  /** Get lesson card by name */
  getLessonByName(lessonName: string): Locator {
    return this.page.locator(
      `[data-testid="lesson-card"]:has-text("${lessonName}")`
    );
  }

  /** Click on a lesson to start exercises */
  async selectLesson(lessonName: string): Promise<void> {
    const lessonCard = this.getLessonByName(lessonName);
    await lessonCard.click();
  }

  /** Verify home page is loaded with courses */
  async expectHomePageLoaded(): Promise<void> {
    await expect(this.courseCards.first()).toBeVisible();
  }

  /** Get lesson status badge */
  getLessonStatus(lessonName: string): Locator {
    return this.getLessonByName(lessonName)
      .locator('[data-testid="lesson-status"]');
  }

  /** Verify lesson is marked as completed */
  async expectLessonCompleted(lessonName: string): Promise<void> {
    const status = this.getLessonStatus(lessonName);
    await expect(status).toHaveText('completed');
  }
}
```

## Exercise Page

**File**: `e2e/pages/exercise.page.ts`

```typescript
import { Page, Locator, expect } from '@playwright/test';
import { BasePage } from './base.page';

export class ExercisePage extends BasePage {
  readonly questionText: Locator;
  readonly submitButton: Locator;
  readonly nextButton: Locator;
  readonly progressIndicator: Locator;
  readonly feedbackMessage: Locator;

  constructor(page: Page) {
    super(page);
    this.questionText = page.locator('[data-testid="question-text"]');
    this.submitButton = page.locator('[data-testid="submit-btn"]');
    this.nextButton = page.locator('[data-testid="next-btn"]');
    this.progressIndicator = page.locator('[data-testid="progress-indicator"]');
    this.feedbackMessage = page.locator('[data-testid="feedback-message"]');
  }

  async goto(lessonId: string): Promise<void> {
    await this.page.goto(`/lesson/${lessonId}`);
  }

  async waitForLoad(): Promise<void> {
    await expect(this.questionText).toBeVisible();
  }

  // --- Multiple Choice ---

  /** Select multiple choice option by text */
  async selectMultipleChoiceOption(optionText: string): Promise<void> {
    const option = this.page.locator(
      `[data-testid="mc-option"]:has-text("${optionText}")`
    );
    await option.click();
  }

  /** Get all multiple choice options */
  get multipleChoiceOptions(): Locator {
    return this.page.locator('[data-testid="mc-option"]');
  }

  // --- Fill in Blank ---

  /** Fill in the blank with text */
  async fillInBlank(text: string): Promise<void> {
    const input = this.page.locator('[data-testid="fill-blank-input"]');
    await input.fill(text);
  }

  // --- Translation ---

  /** Type translation answer */
  async typeTranslation(text: string): Promise<void> {
    const input = this.page.locator('[data-testid="translation-input"]');
    await input.fill(text);
  }

  // --- Actions ---

  /** Submit current exercise answer */
  async submitAnswer(): Promise<void> {
    await this.submitButton.click();
    // Wait for feedback to appear
    await expect(this.feedbackMessage).toBeVisible();
  }

  /** Click next exercise button */
  async clickNext(): Promise<void> {
    await this.nextButton.click();
    // Wait for new question to load
    await this.waitForLoad();
  }

  // --- Assertions ---

  /** Verify correct feedback is shown */
  async expectCorrectFeedback(): Promise<void> {
    await expect(this.feedbackMessage).toContainText(/correct/i);
  }

  /** Verify incorrect feedback is shown */
  async expectIncorrectFeedback(): Promise<void> {
    await expect(this.feedbackMessage).toContainText(/incorrect/i);
  }

  /** Get current progress text (e.g., "3/10") */
  async getProgress(): Promise<string> {
    return await this.progressIndicator.textContent() || '';
  }

  /** Verify submit button is enabled */
  async expectSubmitEnabled(): Promise<void> {
    await expect(this.submitButton).toBeEnabled();
  }

  /** Verify next button is visible */
  async expectNextButtonVisible(): Promise<void> {
    await expect(this.nextButton).toBeVisible();
  }
}
```

## Leaderboard Page

**File**: `e2e/pages/leaderboard.page.ts`

```typescript
import { Page, Locator, expect } from '@playwright/test';
import { BasePage } from './base.page';

export class LeaderboardPage extends BasePage {
  readonly pageTitle: Locator;
  readonly timeFrameFilter: Locator;
  readonly leaderboardTable: Locator;

  constructor(page: Page) {
    super(page);
    this.pageTitle = page.locator('h1');
    this.timeFrameFilter = page.locator('[data-testid="timeframe-filter"]');
    this.leaderboardTable = page.locator('[data-testid="leaderboard-table"]');
  }

  async goto(): Promise<void> {
    await this.page.goto('/leaderboard');
  }

  async waitForLoad(): Promise<void> {
    await expect(this.pageTitle).toBeVisible();
  }

  /** Select time frame filter */
  async selectTimeFrame(timeFrame: 'Weekly' | 'Monthly' | 'AllTime'): Promise<void> {
    await this.timeFrameFilter.selectOption(timeFrame);
    // Wait for table to reload
    await this.page.waitForLoadState('networkidle');
  }

  /** Get leaderboard row by rank */
  getRowByRank(rank: number): Locator {
    return this.page.locator(`[data-testid="leaderboard-row-${rank}"]`);
  }

  /** Get current user's row (highlighted) */
  get currentUserRow(): Locator {
    return this.page.locator('[data-testid="current-user-row"]');
  }

  /** Get current user's rank */
  async getCurrentUserRank(): Promise<number | null> {
    if (await this.currentUserRow.isVisible()) {
      const rankText = await this.currentUserRow
        .locator('[data-testid="rank"]')
        .textContent();
      return rankText ? parseInt(rankText) : null;
    }
    return null;
  }

  /** Verify leaderboard is populated */
  async expectLeaderboardVisible(): Promise<void> {
    await expect(this.leaderboardTable).toBeVisible();
    await expect(this.getRowByRank(1)).toBeVisible();
  }

  /** Verify user is highlighted in leaderboard */
  async expectCurrentUserHighlighted(): Promise<void> {
    await expect(this.currentUserRow).toBeVisible();
  }
}
```

## Profile Page

**File**: `e2e/pages/profile.page.ts`

```typescript
import { Page, Locator, expect } from '@playwright/test';
import { BasePage } from './base.page';

export class ProfilePage extends BasePage {
  readonly userName: Locator;
  readonly levelDisplay: Locator;
  readonly xpDisplay: Locator;
  readonly streakDisplay: Locator;
  readonly avatarImage: Locator;

  constructor(page: Page) {
    super(page);
    this.userName = page.locator('[data-testid="profile-username"]');
    this.levelDisplay = page.locator('[data-testid="level"]');
    this.xpDisplay = page.locator('[data-testid="xp"]');
    this.streakDisplay = page.locator('[data-testid="streak"]');
    this.avatarImage = page.locator('[data-testid="avatar"]');
  }

  async goto(): Promise<void> {
    await this.page.goto('/profile');
  }

  async waitForLoad(): Promise<void> {
    await expect(this.userName).toBeVisible();
  }

  /** Get current level */
  async getLevel(): Promise<number> {
    const text = await this.levelDisplay.textContent();
    const match = text?.match(/\d+/);
    return match ? parseInt(match[0]) : 0;
  }

  /** Get current XP */
  async getXP(): Promise<number> {
    const text = await this.xpDisplay.textContent();
    const match = text?.match(/\d+/);
    return match ? parseInt(match[0]) : 0;
  }

  /** Get current streak */
  async getStreak(): Promise<number> {
    const text = await this.streakDisplay.textContent();
    const match = text?.match(/\d+/);
    return match ? parseInt(match[0]) : 0;
  }

  /** Verify profile is loaded */
  async expectProfileVisible(): Promise<void> {
    await expect(this.userName).toBeVisible();
    await expect(this.xpDisplay).toBeVisible();
  }
}
```

## Selector Strategy

### Priority Order

1. **`data-testid` attributes** (Recommended) - Resilient to UI changes
2. **ARIA labels** - Semantic and accessible
3. **Component selectors** - Angular component tags (app-*)
4. **CSS classes** - Last resort, fragile

### Why data-testid?

```typescript
// ✅ GOOD - Explicit test selector
page.locator('[data-testid="submit-btn"]')

// ❌ AVOID - Fragile class names
page.locator('.btn.btn-primary.submit-button')

// ⚠️ OK - ARIA label (changes with i18n)
page.locator('button[aria-label="Submit answer"]')
```

## See Also

- [data-testid-guide.md](./data-testid-guide.md) - Adding data-testid to Angular components
- [test-suites.md](./test-suites.md) - Complete test specifications using these page objects
