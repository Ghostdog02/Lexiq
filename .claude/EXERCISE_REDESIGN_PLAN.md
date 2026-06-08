# Exercise Redesign Plan

## Overview

Full redesign of the exercise system to remove test-blank aesthetics and introduce interactive,
gamified exercise types with spaced repetition.

## Decisions Made

| Topic | Decision |
|-------|----------|
| Word bank for FillInBlank | **CHANGED**: Uses `List<ExerciseOption>` (not comma-separated string) — content creators provide options list like Listening |
| MultipleChoice → Listening | Absorb MC design into Listening (even without audio) |
| SM-2 spaced repetition | Full algorithm — EaseFactor, Interval, Repetitions, NextReviewDate, LastReviewedAt |
| New type priority | AudioMatching → TrueFalse → ImageChoice |
| Migrated data | Hard delete — no backwards compatibility |
| AudioMatching | Images only matched to audio clips (not words) |
| Primary key naming | New entities use `<EntityName>Id` pattern (ExerciseId, ImageOptionId, etc.) |

## Types Removed

- **MultipleChoice** — design absorbed into Listening (option-based selection)
- **Translation** — removed entirely (free-text similarity not interactive enough)

## Types Kept / Modified

| Type | Changes |
|------|---------|
| FillInBlank | **CHANGED**: Now uses `List<ExerciseOption>` (not WordBank string) — provides word bank as structured options, drag-and-drop word tiles in UI |
| Listening | Removed CorrectAnswer/AcceptedAnswers/CaseSensitive fields; now uses `List<ExerciseOption>` (like old MC) |

## Types Added

| Type | Answer Format | Notes |
|------|--------------|-------|
| TrueFalse | `"true"` or `"false"` string | Optional `ImageUrl`, card-flip UI |
| ImageChoice | Option ID (GUID string) | `ImageOption` child table with ImageUrl + AltText |
| AudioMatching | `"pairId1:imageUrl1,pairId2:imageUrl2,..."` | `AudioMatchPair` child table; images matched to audio |

---

## Phase A — Cleanup & Rename (COMPLETED)

### Goals
- Rename `Instructions` → `Question` on Exercise base entity
- Rename enum value `FillInTheBlank` → `FillInBlank`
- Update all downstream code

### Files Changed
- `Database/Entities/Exercises/Exercise.cs` — renamed property + enum value
- `Database/Migrations/20260426140035_RenameInstructionsToQuestion.cs` — EF migration
- All DTOs, services, mappings, seeders, test files updated for rename

---

## Phase B — New Exercise Types (IN PROGRESS)

### Backend Entity Changes

**Modified entities:**
- `FillInBlankExercise` — removed text-answer fields (CorrectAnswer, AcceptedAnswers, CaseSensitive, TrimWhitespace); added `List<ExerciseOption> Options`
- `ListeningExercise` — removed text-answer fields (CorrectAnswer, AcceptedAnswers, CaseSensitive); added `List<ExerciseOption> Options`

**New entities:**
- `TrueFalseExercise` — Statement, CorrectAnswer (bool), optional ImageUrl
- `ImageChoiceExercise` + `ImageOption` — ImageUrl, AltText, IsCorrect, OrderIndex
- `AudioMatchingExercise` + `AudioMatchPair` — AudioUrl, ImageUrl, OrderIndex

**SM-2 fields on `UserExerciseProgress`:**
- `EaseFactor` (double, default 2.5)
- `Interval` (int, default 0)
- `Repetitions` (int, default 0)
- `NextReviewDate` (DateTime?)
- `LastReviewedAt` (DateTime?)

### Backend Service Changes

**ExerciseService** — MapToEntity, GetById includes updated for new types  
**ExerciseProgressService** — ValidateAnswer switch updated:
  - FillInBlank: text comparison (unchanged logic)
  - Listening: match by option ID (like old MC)
  - TrueFalse: `bool.TryParse`, compare to `CorrectAnswer`
  - ImageChoice: match by option ID against `ImageOption` table
  - AudioMatching: parse `"pairId:imageUrl"` pairs, validate all match

**LessonService** — All three Include chains + `GetCorrectAnswer` updated  
**ExerciseController** — `GetCorrectAnswerForExercise` updated  
**ExerciseSeeder** — All seeded exercises updated (MC → Listening with Options, Translation removed, WordBank added)

### Backend DTO Changes

**Removed:** `MultipleChoiceExerciseDto`, `TranslationExerciseDto`, `CreateMultipleChoiceExerciseDto`, `CreateTranslationExerciseDto`  
**Added:** `TrueFalseExerciseDto`, `ImageChoiceExerciseDto`, `AudioMatchingExerciseDto`, `ImageOptionDto`, `AudioMatchPairDto`, `ExerciseOptionDto` + Create variants  
**Modified:** `ListeningExerciseDto` (options list), `FillInBlankExerciseDto` (options list, removed text-answer fields)

### EF Migration Needed

One migration covering:
- New SM-2 columns on `UserExerciseProgress`: `EaseFactor`, `Interval`, `Repetitions`, `NextReviewDate`, `LastReviewedAt`
- New tables: `TrueFalseExercises`, `ImageChoiceExercises`, `ImageOptions`, `AudioMatchingExercises`, `AudioMatchPairs`
- Removed columns from `FillInBlankExercise`: `CorrectAnswer`, `AcceptedAnswers`, `CaseSensitive`, `TrimWhitespace`
- Removed columns from `ListeningExercise`: `CorrectAnswer`, `AcceptedAnswers`, `CaseSensitive`
- Removed tables: `MultipleChoiceExercises`, `TranslationExercises`
- `ExerciseOption` now shared by both `FillInBlankExercise` and `ListeningExercise` (polymorphic FK to Exercise base)

---

## Phase C — Frontend Redesign (NOT STARTED)

### Goals
- Install Angular CDK for drag-and-drop
- Redesign exercise viewer with type-specific interactive components
- Word tile drag-and-drop for FillInBlank
- Card-flip animation for TrueFalse
- Image grid selection for ImageChoice and AudioMatching
- Audio player integration for Listening and AudioMatching

### Components to Create/Modify
- `exercise-viewer` — refactor to dispatch to per-type components
- `fill-in-blank-exercise` — word tile bank, drag-and-drop slots
- `true-false-exercise` — two-card flip layout
- `image-choice-exercise` — image grid with selection state
- `audio-matching-exercise` — audio player + image drag targets
- `listening-exercise` — audio player + option cards

---

## Phase D — SM-2 Spaced Repetition (NOT STARTED)

### Algorithm
Full SM-2: after each answer, update `EaseFactor`, `Interval`, `Repetitions`, `NextReviewDate`.
- Correct: increase interval, adjust ease factor upward
- Incorrect: reset to interval 1, decrease ease factor (floor 1.3)

### Backend Service
New `SpacedRepetitionService` or extend `ExerciseProgressService.SubmitAnswerAsync`
to call SM-2 update after saving progress.

### Review Queue Endpoint
`GET /api/exercises/due-for-review` — returns exercises where `NextReviewDate <= now`
ordered by urgency.

---

## Phase E — Progressive Difficulty (NOT STARTED)

Serve easier exercises first within a lesson (already partially handled by `OrderIndex`).
Potential future: adaptive difficulty based on user performance history.
