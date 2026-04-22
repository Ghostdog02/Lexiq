# Adding data-testid Attributes to Angular Components

## Overview

`data-testid` attributes provide stable, semantic selectors for E2E tests that are:
- ✅ **Resilient** - Survive UI refactoring, class name changes
- ✅ **Explicit** - Clear test intent, not coupling to implementation
- ✅ **Searchable** - Easy to find which tests use which elements

## Naming Convention

```typescript
// Pattern: {component}-{element-purpose}
data-testid="user-menu"
data-testid="logout-btn"
data-testid="course-card"
data-testid="lesson-status"
```

**Rules**:
- Use kebab-case (lowercase with hyphens)
- Be descriptive but concise
- Avoid implementation details (class names, IDs)
- Use semantic names that describe the element's purpose

## Where to Add data-testid

### Navigation / Header

**Component**: `app-header` or navigation component

```html
<!-- User menu dropdown -->
<div data-testid="user-menu" class="user-menu">
  <button data-testid="user-menu-btn">{{ userName }}</button>
  <div data-testid="user-menu-dropdown" class="dropdown">
    <a data-testid="profile-link" routerLink="/profile">Profile</a>
    <a data-testid="leaderboard-link" routerLink="/leaderboard">Leaderboard</a>
    <button data-testid="logout-btn" (click)="logout()">Logout</button>
  </div>
</div>
```

### Login Page

**Component**: `GoogleLoginComponent` (`frontend/src/app/auth/google-login/`)

```html
<div data-testid="login-page">
  <h1>Welcome to Lexiq</h1>

  <button
    data-testid="google-login-btn"
    (click)="loginWithGoogle()"
    class="google-login-button">
    <img src="google-icon.svg" alt="Google">
    Sign in with Google
  </button>
</div>
```

### Home Page (Courses & Lessons)

**Component**: `HomeComponent` (`frontend/src/app/home/`)

```html
<!-- Course cards -->
<div data-testid="course-card"
     *ngFor="let course of courses"
     class="course-card">
  <h3 data-testid="course-title">{{ course.title }}</h3>
  <p data-testid="course-description">{{ course.description }}</p>
</div>

<!-- Lesson cards -->
<div data-testid="lesson-card"
     *ngFor="let lesson of lessons"
     class="lesson-card"
     (click)="startLesson(lesson.lessonId)">
  <h4 data-testid="lesson-title">{{ lesson.title }}</h4>
  <span data-testid="lesson-duration">{{ lesson.duration }}</span>
  <span data-testid="lesson-status">{{ lesson.isLocked ? 'locked' : 'unlocked' }}</span>
</div>
```

### Exercise Page

**Component**: `ExerciseViewerComponent` (`frontend/src/app/exercise-viewer/`)

```html
<div data-testid="exercise-page">
  <!-- Progress indicator -->
  <div data-testid="progress-indicator">
    {{ currentExerciseIndex + 1 }}/{{ totalExercises }}
  </div>

  <!-- Question text -->
  <h2 data-testid="question-text">{{ currentExercise.title }}</h2>

  <!-- Multiple Choice -->
  @if (currentExercise.type === 'MultipleChoice') {
    <div data-testid="mc-option"
         *ngFor="let option of currentExercise.options"
         (click)="selectOption(option.id)">
      {{ option.optionText }}
    </div>
  }

  <!-- Fill in Blank -->
  @if (currentExercise.type === 'FillInBlank') {
    <input
      data-testid="fill-blank-input"
      [(ngModel)]="userAnswer"
      placeholder="Type your answer">
  }

  <!-- Translation -->
  @if (currentExercise.type === 'Translation') {
    <textarea
      data-testid="translation-input"
      [(ngModel)]="userAnswer"
      placeholder="Translate this sentence"></textarea>
  }

  <!-- Listening -->
  @if (currentExercise.type === 'Listening') {
    <button data-testid="play-audio-btn" (click)="playAudio()">
      Play Audio
    </button>
    <input
      data-testid="listening-input"
      [(ngModel)]="userAnswer"
      placeholder="What did you hear?">
  }

  <!-- Feedback message -->
  <div data-testid="feedback-message"
       *ngIf="showFeedback"
       [class.correct]="isCorrect"
       [class.incorrect]="!isCorrect">
    {{ feedbackText }}
  </div>

  <!-- Action buttons -->
  <button
    data-testid="submit-btn"
    (click)="submitAnswer()"
    [disabled]="!canSubmit">
    Submit
  </button>

  <button
    data-testid="next-btn"
    *ngIf="showNextButton"
    (click)="nextExercise()">
    Next Exercise
  </button>
</div>
```

### Leaderboard Page

**Component**: `LeaderboardComponent` (`frontend/src/app/leaderboard/`)

```html
<div data-testid="leaderboard-page">
  <h1>Leaderboard</h1>

  <!-- Time frame filter -->
  <select data-testid="timeframe-filter"
          [(ngModel)]="selectedTimeFrame"
          (change)="filterLeaderboard()">
    <option value="Weekly">This Week</option>
    <option value="Monthly">This Month</option>
    <option value="AllTime">All Time</option>
  </select>

  <!-- Leaderboard table -->
  <table data-testid="leaderboard-table">
    <tbody>
      <tr *ngFor="let entry of leaderboard; let i = index"
          [attr.data-testid]="'leaderboard-row-' + (i + 1)"
          [class.current-user]="entry.isCurrentUser">

        <td data-testid="rank">{{ entry.rank }}</td>

        <td data-testid="username">{{ entry.userName }}</td>

        <td data-testid="xp">{{ entry.totalXp }}</td>

        <td data-testid="avatar">
          <img [src]="entry.avatarUrl" alt="{{ entry.userName }}">
        </td>
      </tr>
    </tbody>
  </table>

  <!-- Current user row (if not in top N) -->
  <div data-testid="current-user-row"
       *ngIf="currentUserEntry && !currentUserInTopN">
    <span data-testid="rank">{{ currentUserEntry.rank }}</span>
    <span data-testid="username">{{ currentUserEntry.userName }}</span>
    <span data-testid="xp">{{ currentUserEntry.totalXp }}</span>
  </div>
</div>
```

### Profile Page

**Component**: `ProfileComponent` (`frontend/src/app/profile/`)

```html
<div data-testid="profile-page">
  <h1>Profile</h1>

  <!-- Avatar -->
  <img data-testid="avatar"
       [src]="user.avatarUrl"
       alt="Profile picture">

  <!-- Username -->
  <h2 data-testid="profile-username">{{ user.userName }}</h2>

  <!-- Stats -->
  <div data-testid="stats">
    <div data-testid="level">
      Level: {{ user.level }}
    </div>

    <div data-testid="xp">
      XP: {{ user.totalXp }}
    </div>

    <div data-testid="streak">
      Streak: {{ user.streak }} days
    </div>
  </div>
</div>
```

## Dynamic data-testid with Indices

For lists, append index or unique identifier:

```html
<!-- ✅ GOOD - Unique per item -->
<div *ngFor="let lesson of lessons; let i = index"
     [attr.data-testid]="'lesson-card-' + i">
  {{ lesson.title }}
</div>

<!-- ✅ ALSO GOOD - Semantic ID -->
<div *ngFor="let lesson of lessons"
     [attr.data-testid]="'lesson-' + lesson.lessonId">
  {{ lesson.title }}
</div>
```

## Implementation Checklist

### Navigation
- [ ] `data-testid="nav-bar"` - Main navigation container
- [ ] `data-testid="user-menu"` - User menu dropdown
- [ ] `data-testid="logout-btn"` - Logout button

### Login Page
- [ ] `data-testid="google-login-btn"` - Google sign-in button

### Home Page
- [ ] `data-testid="course-card"` - Course cards
- [ ] `data-testid="lesson-card"` - Lesson cards
- [ ] `data-testid="lesson-status"` - Lock/unlock status
- [ ] `data-testid="lesson-title"` - Lesson title

### Exercise Page
- [ ] `data-testid="question-text"` - Exercise question
- [ ] `data-testid="progress-indicator"` - Progress (e.g., "3/10")
- [ ] `data-testid="mc-option"` - Multiple choice options
- [ ] `data-testid="fill-blank-input"` - Fill in blank input
- [ ] `data-testid="translation-input"` - Translation textarea
- [ ] `data-testid="listening-input"` - Listening input
- [ ] `data-testid="submit-btn"` - Submit button
- [ ] `data-testid="next-btn"` - Next exercise button
- [ ] `data-testid="feedback-message"` - Correct/incorrect feedback

### Leaderboard Page
- [ ] `data-testid="timeframe-filter"` - Week/Month/AllTime dropdown
- [ ] `data-testid="leaderboard-table"` - Leaderboard table
- [ ] `data-testid="leaderboard-row-{rank}"` - Individual rows
- [ ] `data-testid="current-user-row"` - Highlighted current user

### Profile Page
- [ ] `data-testid="profile-username"` - Username display
- [ ] `data-testid="level"` - User level
- [ ] `data-testid="xp"` - Total XP
- [ ] `data-testid="streak"` - Current streak
- [ ] `data-testid="avatar"` - Profile avatar

## Best Practices

### ✅ DO

```html
<!-- Descriptive, semantic names -->
<button data-testid="submit-btn">Submit</button>
<input data-testid="email-input" type="email">

<!-- Stable across translations -->
<h1 data-testid="page-title">{{ t('welcome') }}</h1>

<!-- Unique identifiers for lists -->
<div *ngFor="let item of items" [attr.data-testid]="'item-' + item.id">
```

### ❌ DON'T

```html
<!-- Don't use implementation details -->
<button data-testid="btn-primary-submit">Submit</button>

<!-- Don't duplicate class names -->
<div data-testid="user-card" class="user-card">

<!-- Don't use changing content -->
<div data-testid="exercise-{{ currentExercise.title }}">
```

## Testing Your data-testid Attributes

### In Browser DevTools

```javascript
// Verify selector works
document.querySelector('[data-testid="submit-btn"]')

// Count elements
document.querySelectorAll('[data-testid="lesson-card"]').length
```

### In Playwright Tests

```typescript
// Verify element exists
await expect(page.locator('[data-testid="submit-btn"]')).toBeVisible();

// Count elements
const count = await page.locator('[data-testid="lesson-card"]').count();
```

## Questions to Consider

Before implementing, please confirm:

1. **Component file locations**: Are these the correct paths?
   - `frontend/src/app/auth/google-login/google-login.component.html`
   - `frontend/src/app/home/home.component.html`
   - `frontend/src/app/exercise-viewer/exercise-viewer.component.html`
   - `frontend/src/app/leaderboard/leaderboard.component.html`
   - `frontend/src/app/profile/profile.component.html`

2. **Template syntax**: Your components use `@if` and `@switch` (Angular 17+), correct?

3. **Navigation component**: Where is the user menu / logout button? Separate header component?

4. **Lesson card location**: Are lesson cards in `HomeComponent` or a separate component?

Let me know if any paths or assumptions are incorrect!
