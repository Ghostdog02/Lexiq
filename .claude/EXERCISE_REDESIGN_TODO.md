# Exercise Redesign — What's Left

## Immediate: Fix Build Errors (Backend Tests)

The backend compiles but test files still reference removed types. Fix these before running tests.

### `AdminContentManagementJourneyTests.cs`
- [ ] Line 78: Add `WordBank: "answer,wrong1,wrong2,wrong3"` to `CreateFillInBlankExerciseDto`
- [ ] Line 349: Add `WordBank: "one,two,three,four"` to `CreateFillInBlankExerciseDto`
- [ ] Line 384: Add `WordBank: "two,three,four,five"` to `CreateFillInBlankExerciseDto`

### `ExerciseSubmissionSecurityTests.cs`
- [ ] Line 244: Add `WordBank = "answer,wrong1,wrong2,wrong3"` to inline `FillInBlankExercise` object
- [ ] Lines 278–319: Replace `Student_SubmitsCorrectMultipleChoiceAnswer_Success` test
  - Use `DbSeeder.CreateListeningExerciseWithOptionsAsync` instead of `CreateMultipleChoiceExerciseAsync`
  - Replace `(e as MultipleChoiceExercise)!.Options` cast with `(e as ListeningExercise)!.Options`
  - Remove `MultipleChoiceExercise` cast; use `ListeningExercise`

### `ExerciseValidationTests.cs`
- [ ] Line 643: Add `WordBank = "Answer,wrong1,wrong2,wrong3"` to inline `FillInBlankExercise`
- [ ] Remove/replace `Translation_*` tests (5 tests) — `TranslationExercise` deleted
  - Replace with `TrueFalse_*` tests: correct answer `"true"`/`"false"`, wrong answer rejected
- [ ] Rewrite `CreateAndSaveTranslationAsync` helper → `CreateAndSaveTrueFalseAsync`
- [ ] Rewrite `CreateAndSaveListeningAsync` helper — old fields removed (`CorrectAnswer`, `AcceptedAnswers`, `CaseSensitive`); now uses `Options`
  - New signature: `CreateAndSaveListeningAsync(string audioUrl)` returning `(exerciseId, correctOptionId)`
- [ ] Rewrite `Listening_*` tests (4 tests) — submit option ID instead of text string
- [ ] Rename `CreateAndSaveMultipleChoiceAsync` → `CreateAndSaveListeningAsync` and fix `MultipleChoice_*` tests (3 tests)
  - Tests become `Listening_CorrectOptionId_ReturnsTrue`, etc.

### `LessonCrudTests.cs`
- [ ] Line 128: Add `WordBank: "meowing,barking,running,sleeping"` to first `CreateFillInBlankExerciseDto`
- [ ] Line 143: Add `WordBank: "barking,meowing,running,sleeping"` to second `CreateFillInBlankExerciseDto`

### `LessonQueryTests.cs`
- [ ] Lines 158–197: Replace `MultipleChoiceExercise` test with `ListeningExercise`
  - Use `ListeningExercise` with `Options` list instead of `MultipleChoiceExercise`
  - Fix cast on line 194: `result.Exercises[0] as ListeningExercise`
- [ ] Lines 259–300: Fix `GetLessonWithDetailsAsync_ReturnsMixedExerciseTypes` test
  - Add `WordBank` to `FillInBlankExercise`
  - Replace `ListeningExercise` creation (remove `CorrectAnswer`, `AcceptedAnswers`, `CaseSensitive`; add `Options` list)
  - Replace `TranslationExercise` with `TrueFalseExercise`
  - Fix assertions on lines 314–320 for new types

### `LessonUnlockingTests.cs`
- [ ] Line 108: Add `WordBank = "answer,wrong1,wrong2,wrong3"` to inline `FillInBlankExercise`
- [ ] Line 245: Add `WordBank = "answer,wrong1,wrong2,wrong3"` to first inline `FillInBlankExercise`
- [ ] Line 259: Add `WordBank = "answer,wrong1,wrong2,wrong3"` to second inline `FillInBlankExercise`

---

## EF Core Migration

- [ ] Create migration for all Phase B schema changes:
  ```bash
  cd backend
  dotnet ef migrations add AddNewExerciseTypes --project Database/Backend.Database.csproj
  ```
  Should cover:
  - New column: `Exercises.WordBank` (FillInBlank)
  - New SM-2 columns on `UserExerciseProgress`
  - New tables: `ImageOptions`, `AudioMatchPairs`
  - TPH discriminator entries for `TrueFalseExercise`, `ImageChoiceExercise`, `AudioMatchingExercise`
  - Drop removed columns from old `ListeningExercise` (`CorrectAnswer`, `AcceptedAnswers`, `CaseSensitive`)

---

## SM-2 Spaced Repetition Algorithm

- [ ] Implement SM-2 update logic in `ExerciseProgressService.SubmitAnswerAsync`
  - After saving progress, compute new `EaseFactor`, `Interval`, `Repetitions`, `NextReviewDate`
  - Correct answer: `Interval = max(1, prevInterval * EaseFactor)`, ease factor increases slightly
  - Wrong answer: `Interval = 1`, `Repetitions = 0`, ease factor decreases (floor 1.3)
- [ ] Add `GET /api/exercises/due-for-review` endpoint
  - Returns exercises where `NextReviewDate <= DateTime.UtcNow` for the current user

---

## Frontend Phase (Angular)

### Setup
- [ ] Install Angular CDK: `npm install @angular/cdk`

### New Exercise Components
- [ ] `fill-in-blank-exercise` — word tile bank with CDK drag-and-drop into blank slots
- [ ] `true-false-exercise` — two card layout with flip animation on selection
- [ ] `image-choice-exercise` — image grid, highlight selection, submit on click
- [ ] `audio-matching-exercise` — audio play buttons + image drop targets (drag image to audio)
- [ ] `listening-exercise` — audio player + option cards (replaces old MC + Listening)

### Refactor
- [ ] `exercise-viewer` — dispatch to per-type components instead of monolithic template
- [ ] Remove old MultipleChoice and Translation exercise rendering code
- [ ] Update `exercise.interface.ts` to match backend DTO changes:
  - Remove `MultipleChoiceExercise`, `TranslationExercise` interfaces
  - Add `TrueFalseExercise`, `ImageChoiceExercise`, `AudioMatchingExercise` interfaces
  - Update `ListeningExercise` interface (remove text-answer fields, add `options`)
  - Add `wordBank` to `FillInBlankExercise` interface

### Service / API Layer
- [ ] Update `ExerciseService` (Angular) answer submission for new types
  - Listening: send selected option ID
  - TrueFalse: send `"true"` or `"false"`
  - ImageChoice: send selected option ID
  - AudioMatching: send `"pairId:imageUrl,..."` pairs

---

## Optional / Future

- [ ] Progressive difficulty — adaptive ordering based on user performance history
- [ ] Review queue UI — dedicated "due for review" session flow
- [ ] Content creator tooling — UI for uploading image/audio assets for new exercise types
