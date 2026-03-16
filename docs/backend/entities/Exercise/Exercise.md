# Exercise Entity Overview

**Namespace:** `Backend.Database.Entities.Exercises`
**Table:** `Exercises` (Table-Per-Hierarchy)
**Pattern:** Abstract base class with polymorphic subtypes

## Architecture

Lexiq uses **Table-Per-Hierarchy (TPH)** inheritance for exercises, storing all types in a single `Exercises` table with a `Discriminator` column.

### Four Exercise Types

1. **[FillInBlankExercise](./FillInBlank.md)** - Complete sentences with missing words
2. **[TranslationExercise](./Translation.md)** - Translate text with fuzzy matching
3. **[ListeningExercise](./Listening.md)** - Transcribe audio clips
4. **[MultipleChoiceExercise](./MultipleChoice.md)** - Select correct option from choices

## Base Properties

| Property | Type | Constraints | Description |
|----------|------|-------------|-------------|
| `Id` | `string` | PK, GUID | Unique identifier |
| `LessonId` | `string` | FK, Required | Parent lesson |
| `Title` | `string` | Required, Max 200 | Display name |
| `Instructions` | `string?` | Optional, Max 1000 | Exercise instructions |
| `EstimatedDurationMinutes` | `int?` | Optional, Range(5,20) | Expected time |
| `DifficultyLevel` | `enum` | Required | Beginner/Intermediate/Advanced |
| `Points` | `int` | Required | XP awarded for correct answer |
| `OrderIndex` | `int` | Required | Position in lesson (0-based) |
| `IsLocked` | `bool` | Required | Access control flag |
| `Explanation` | `string?` | Optional | Post-submission explanation |

## Navigation Properties

| Property | Type | Relationship |
|----------|------|--------------|
| `Lesson` | `Lesson` | Many-to-One |
| `ExerciseProgress` | `List<UserExerciseProgress>` | One-to-Many |

## Business Rules

### Sequential Unlocking
- First exercise in lesson unlocked when lesson unlocks
- Completing exercise N unlocks exercise N+1 (by `OrderIndex`)
- Admin/ContentCreator bypass locks via `CanBypassLocksAsync()`

### Progress Tracking
- One `UserExerciseProgress` per (user, exercise) pair
- First correct submission awards XP and updates `User.TotalPointsEarned`
- Resubmitting correct answer is idempotent (no double XP)
- Wrong answers allow infinite retries

### Lock Validation
```csharp
// Dual lock check in ExerciseProgressService.SubmitAnswerAsync
if (exercise.Lesson.IsLocked && !canBypass)
    throw InvalidOperationException("Cannot submit to locked lesson");

if (exercise.IsLocked && !canBypass)
    throw InvalidOperationException("Cannot submit to locked exercise");
```

## Answer Submission Flow

1. User submits answer via `POST /api/exercises/{id}/submit`
2. `ExerciseProgressService.SubmitAnswerAsync()` validates:
   - Exercise exists
   - Lesson not locked (or user can bypass)
   - Exercise not locked (or user can bypass)
3. Type-specific validation determines correctness
4. Upsert `UserExerciseProgress` record
5. Award XP on first correct submission only
6. Unlock next exercise if answer correct
7. Return `ExerciseSubmitResult`

## Response DTO

```csharp
public record ExerciseSubmitResult(
    bool IsCorrect,
    int PointsEarned,
    string? CorrectAnswer,  // Only revealed if wrong
    string? Explanation
);
```

## Service Methods

### ExerciseProgressService
- `SubmitAnswerAsync(userId, exerciseId, answer)` - Validate and track progress
- `CompleteLessonAsync(userId, lessonId)` - Check 70% threshold

### ExerciseService
- `GetExerciseByIdAsync(id)` - Fetch with type-specific includes
- `CreateExerciseAsync(dto)` - Polymorphic creation
- `UpdateExerciseAsync(id, dto)` - Update exercise
- `DeleteExerciseAsync(id)` - Delete with cascade
- `UnlockNextExerciseAsync(id)` - Sequential unlock

## Test Coverage

**File:** `backend/Tests/Services/ExerciseValidationTests.cs` (23 tests)

Tests cover all four exercise types with focus on validation edge cases.

## Type-Specific Documentation

- **[FillInBlank](./FillInBlank.md)** - Case sensitivity, whitespace, AcceptedAnswers (9 tests)
- **[Translation](./Translation.md)** - Levenshtein distance, fuzzy matching (8 tests)
- **[Listening](./Listening.md)** - Audio transcription (4 tests)
- **[MultipleChoice](./MultipleChoice.md)** - Option selection (3 tests)

## Related Entities

- **Parent:** [Lesson](./Lesson.md)
- **Progress:** UserExerciseProgress
- **Options:** ExerciseOption (MultipleChoice only)
