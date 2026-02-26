# Lexiq Database Entities Documentation

## Overview

This document provides comprehensive documentation for all database entities in the Lexiq language learning platform. The system is built on ASP.NET Core with Entity Framework Core and uses a hierarchical structure to organize language learning content.

## System Architecture

The Lexiq database follows a hierarchical content organization:

```
Language
  └── Course (multiple courses per language)
       └── Lesson (multiple lessons per course)
            └── Exercise (multiple exercises per lesson, polymorphic via TPH)
                 ├── MultipleChoiceExercise
                 │    └── ExerciseOption (multiple options)
                 ├── FillInBlankExercise
                 ├── ListeningExercise
                 └── TranslationExercise
```

Additionally:
- **Many-to-many** between Users and Languages through the `UserLanguage` junction table.
- **One-to-many** from User and Exercise to `UserExerciseProgress` for progress tracking.

---

## Core Entities

### User

**Purpose**: Represents application users with authentication capabilities.

**Inheritance**: Extends `IdentityUser` from ASP.NET Core Identity, inheriting standard authentication properties (Username, Email, PasswordHash, etc.).

**File**: [User.cs](backend/Database/Entities/User.cs)

#### Properties

| Property | Type | Description |
|----------|------|-------------|
| Id | string | Primary key (inherited from IdentityUser) |
| RegistrationDate | DateTime | Timestamp when user created their account |
| LastLoginDate | DateTime | Timestamp of user's most recent login |
| Avatar | string? | Profile picture URL; auto-populated from Google OAuth, overridable via `PUT /api/user/avatar` |
| TotalPointsEarned | int | Materialized XP aggregate; incremented on first correct exercise submission to avoid full re-aggregation for leaderboard queries |
| UserLanguages | List\<UserLanguage\> | Navigation property: Languages this user is learning |
| ExerciseProgress | List\<UserExerciseProgress\> | Navigation property: Exercise progress records for this user |

#### Relationships

- **One-to-Many** with UserLanguage: A user can learn multiple languages
- **One-to-Many** with UserExerciseProgress: A user has progress records per exercise

---

### Language

**Purpose**: Represents a language that can be learned on the platform (e.g., Italian, Spanish).

**File**: [Language.cs](backend/Database/Entities/Language.cs)

#### Properties

| Property | Type | Constraints | Description |
|----------|------|-------------|-------------|
| Id | string | Primary Key (Guid) | Unique identifier |
| Name | string | Required, MaxLength(100) | Language name (e.g., "Italian", "Spanish") |
| FlagIconUrl | string? | MaxLength(255), Optional | URL to flag icon for visual representation |
| CreatedAt | DateTime | Default: UtcNow | Timestamp when language was added |
| UserLanguages | List\<UserLanguage\> | Navigation | Users learning this language |
| Courses | List\<Course\> | Navigation | Courses available for this language |

#### Relationships

- **One-to-Many** with UserLanguage: Multiple users can learn this language
- **One-to-Many** with Course: A language can have multiple courses

---

### UserLanguage

**Purpose**: Junction table tracking which languages each user is learning (many-to-many relationship).

**File**: [UserLanguage.cs](backend/Database/Entities/UserLanguage.cs)

#### Properties

| Property | Type | Constraints | Description |
|----------|------|-------------|-------------|
| UserId | string | Required, Foreign Key | Reference to User |
| LanguageId | string | Required, Foreign Key | Reference to Language |
| EnrolledAt | DateTime | Default: UtcNow | When user started learning this language |
| User | User | Navigation | User entity |
| Language | Language | Navigation | Language entity |

#### Composite Primary Key

This entity uses a composite primary key consisting of `UserId` and `LanguageId`.

#### Relationships

- **Many-to-One** with User: Links to the user learning the language
- **Many-to-One** with Language: Links to the language being learned

---

## Content Hierarchy Entities

### Course

**Purpose**: Top-level learning content container for a specific language. Represents a complete learning path (e.g., "Italian for Beginners", "Business Italian").

**File**: [Course.cs](backend/Database/Entities/Course.cs)

#### Properties

| Property | Type | Constraints | Description |
|----------|------|-------------|-------------|
| Id | string | Primary Key (Guid) | Unique identifier |
| LanguageId | string | Required, Foreign Key | The language this course teaches |
| Title | string | Required, MaxLength(100) | Course title |
| Description | string? | MaxLength(1000), Optional | Detailed course description |
| EstimatedDurationHours | int? | Range(1, 300), Optional | Expected time to complete course |
| OrderIndex | int | Required | Position within the language (0, 1, 2, ...) |
| CreatedById | string | Required, Foreign Key | User who created this course |
| CreatedAt | DateTime | Default: UtcNow | Course creation timestamp |
| UpdatedAt | DateTime | Default: UtcNow | Last modification timestamp |
| Language | Language | Navigation | Parent language |
| CreatedBy | User | Navigation | Creator user |
| Lessons | List\<Lesson\> | Navigation | Child lessons (direct, no intermediate module layer) |

#### Relationships

- **Many-to-One** with Language: Course belongs to one language
- **Many-to-One** with User: Course has one creator
- **One-to-Many** with Lesson: Course directly contains multiple lessons

#### Business Rules

- Courses are ordered within a language using `OrderIndex`
- Duration can range from 1 to 300 hours
- Courses track creation and update timestamps for audit purposes

---

### Lesson

**Purpose**: Individual learning unit within a course, containing content and exercises (e.g., "Introduction to Pronouns", "Conjugating -ARE verbs").

**File**: [Lesson.cs](backend/Database/Entities/Lesson.cs)

#### Properties

| Property | Type | Constraints | Description |
|----------|------|-------------|-------------|
| Id | string | Primary Key (Guid) | Unique identifier |
| CourseId | string | Required, Foreign Key | Parent course |
| Title | string | Required, MaxLength(200) | Lesson title |
| Description | string? | MaxLength(1000), Optional | Lesson description |
| EstimatedDurationMinutes | int? | Range(10, 40), Optional | Expected completion time in minutes |
| OrderIndex | int | Required | Position within course (0, 1, 2, ...) |
| LessonContent | string | Required | Editor.js JSON content stored as text |
| IsLocked | bool | Required, Default: false | Whether lesson is accessible to the user |
| CreatedAt | DateTime | Default: UtcNow | Creation timestamp |
| Course | Course | Navigation | Parent course |
| Exercises | ICollection\<Exercise\> | Navigation | Child exercises |

#### Relationships

- **Many-to-One** with Course: Lesson belongs to one course
- **One-to-Many** with Exercise: Lesson contains multiple exercises

#### Business Rules

- Lessons are ordered within a course using `OrderIndex`
- Duration ranges from 10 to 40 minutes
- Lessons can be locked to enforce sequential learning
- Content is stored inline as Editor.js JSON (not via external URL)
- Completing 70%+ of a lesson's exercises unlocks the next lesson

---

### Exercise (Abstract Base)

**Purpose**: Abstract base class for all practice activities within a lesson. Uses Table-Per-Hierarchy (TPH) with a discriminator column to represent all exercise subtypes in a single `Exercises` table.

**File**: [Exercise.cs](backend/Database/Entities/Exercises/Exercise.cs)

#### Enumerations

**ExerciseType**: Discriminator that identifies the concrete exercise subtype
- `MultipleChoice` — Select from predefined options
- `FillInTheBlank` — Type the correct word(s) into a blank
- `Listening` — Listen to audio and provide an answer
- `Translation` — Translate text between languages

**DifficultyLevel**: Complexity level
- `Beginner` (0)
- `Intermediate` (1)
- `Advanced` (2)

#### Base Properties

| Property | Type | Constraints | Description |
|----------|------|-------------|-------------|
| Id | string | Primary Key (Guid) | Unique identifier |
| LessonId | string | Required, Foreign Key | Parent lesson |
| Title | string | Required, MaxLength(200) | Exercise title |
| Instructions | string? | MaxLength(1000), Optional | Instructions for completing the exercise |
| EstimatedDurationMinutes | int? | Range(5, 20), Optional | Expected completion time |
| DifficultyLevel | DifficultyLevel | Required | Complexity level |
| Points | int | Range(1, int.MaxValue) | Points earned for correct completion |
| OrderIndex | int | Required | Position within lesson |
| IsLocked | bool | Required, Default: true | Whether this exercise is accessible; first exercise unlocks with the lesson, rest unlock sequentially |
| Explanation | string? | MaxLength(1000), Optional | Educational feedback shown after answering |
| CreatedAt | DateTime | Default: UtcNow | Creation timestamp |
| Lesson | Lesson? | Navigation | Parent lesson |
| ExerciseProgress | List\<UserExerciseProgress\> | Navigation | User progress records for this exercise |

#### Relationships

- **Many-to-One** with Lesson: Exercise belongs to one lesson
- **One-to-Many** with UserExerciseProgress: Exercise tracks progress per user

#### Business Rules

- Exercises are ordered within a lesson using `OrderIndex`
- Duration ranges from 5 to 20 minutes
- First exercise in a lesson unlocks when the lesson unlocks; subsequent exercises unlock sequentially on correct completion
- Wrong answers allow infinite retries without penalty
- Points system feeds leaderboard via `User.TotalPointsEarned`

---

### MultipleChoiceExercise

**Purpose**: Exercise subtype where the user selects the correct answer from a set of options.

**File**: [Exercises/MultipleChoiceExercise.cs](backend/Database/Entities/Exercises/MultipleChoiceExercise.cs)

#### Additional Properties

| Property | Type | Constraints | Description |
|----------|------|-------------|-------------|
| Options | List\<ExerciseOption\> | Navigation | Answer choices for this exercise |

#### Relationships

- **One-to-Many** with ExerciseOption: Exercise has multiple answer options

---

### FillInBlankExercise

**Purpose**: Exercise subtype where the user types the correct word or phrase into a blank.

**File**: [Exercises/FillInBlankExercise.cs](backend/Database/Entities/Exercises/FillInBlankExercise.cs)

#### Additional Properties

| Property | Type | Constraints | Description |
|----------|------|-------------|-------------|
| Text | string | Required | The sentence containing the blank to fill |
| CorrectAnswer | string | Required, MaxLength(500) | The expected answer |
| AcceptedAnswers | string? | MaxLength(1000), Optional | Comma-separated list of alternative accepted answers |
| CaseSensitive | bool | Required, Default: false | Whether answer matching is case-sensitive |
| TrimWhitespace | bool | Required, Default: true | Whether leading/trailing whitespace is ignored before comparison |

---

### ListeningExercise

**Purpose**: Exercise subtype where the user listens to audio and provides an answer (transcription or comprehension).

**File**: [Exercises/ListeningExercise.cs](backend/Database/Entities/Exercises/ListeningExercise.cs)

#### Additional Properties

| Property | Type | Constraints | Description |
|----------|------|-------------|-------------|
| AudioUrl | string | Required, MaxLength(500) | URL to the audio file |
| CorrectAnswer | string | Required, MaxLength(500) | Expected transcription or answer text |
| AcceptedAnswers | string? | MaxLength(1000), Optional | Comma-separated alternative accepted answers |
| CaseSensitive | bool | Default: false | Whether answer matching is case-sensitive |
| MaxReplays | int | Range(1, 10), Default: 3 | Maximum number of times the user may replay the audio |

---

### TranslationExercise

**Purpose**: Exercise subtype where the user translates text from one language to another.

**File**: [Exercises/TranslationExercise.cs](backend/Database/Entities/Exercises/TranslationExercise.cs)

#### Additional Properties

| Property | Type | Constraints | Description |
|----------|------|-------------|-------------|
| SourceText | string | Required, MaxLength(1000) | The text to translate |
| TargetText | string | Required, MaxLength(1000) | The correct translation |
| SourceLanguageCode | string | Required, MaxLength(10) | BCP 47 language code of the source (e.g., "bg", "en") |
| TargetLanguageCode | string | Required, MaxLength(10) | BCP 47 language code of the target (e.g., "it", "es") |
| MatchingThreshold | double | Range(0.0, 1.0), Default: 0.85 | Fuzzy-match tolerance; lower values accept less precise translations |

---

### ExerciseOption

**Purpose**: Individual answer choice for `MultipleChoiceExercise` questions.

**File**: [Exercises/ExerciseOption.cs](backend/Database/Entities/Exercises/ExerciseOption.cs)

#### Properties

| Property | Type | Constraints | Description |
|----------|------|-------------|-------------|
| Id | string | Primary Key (Guid) | Unique identifier |
| ExerciseId | string | Required, Foreign Key | Parent MultipleChoiceExercise |
| OptionText | string | Required, MaxLength(500) | Display text of this answer option |
| IsCorrect | bool | Required, Default: false | Whether this is the correct answer |
| OrderIndex | int | Required | Display order (0=A, 1=B, 2=C, 3=D) |
| Exercise | MultipleChoiceExercise | Navigation | Parent exercise |

#### Relationships

- **Many-to-One** with MultipleChoiceExercise: Option belongs to one exercise

#### Business Rules

- Options are ordered by `OrderIndex` for consistent display
- Exactly one option should have `IsCorrect=true` for single-answer questions
- `OrderIndex` determines letter labelling (0=A, 1=B, 2=C, 3=D)

---

### UserExerciseProgress

**Purpose**: Tracks each user's progress on individual exercises. Used for sequential exercise unlocking, lesson completion thresholds, and leaderboard XP aggregation.

**File**: [UserExerciseProgress.cs](backend/Database/Entities/UserExerciseProgress.cs)

#### Properties

| Property | Type | Constraints | Description |
|----------|------|-------------|-------------|
| UserId | string | Required, Foreign Key, Composite PK | Reference to User |
| ExerciseId | string | Required, Foreign Key, Composite PK | Reference to Exercise |
| IsCompleted | bool | Required | Whether the user has answered correctly at least once |
| PointsEarned | int | Required | Points awarded for correct completion (0 if not yet completed) |
| CompletedAt | DateTime? | Optional | Timestamp of first correct answer; null if not yet completed |
| User | User | Navigation | User entity |
| Exercise | Exercise | Navigation | Exercise entity |

#### Composite Primary Key

Uses a composite primary key consisting of `(UserId, ExerciseId)` — ensures one progress record per user per exercise.

#### Relationships

- **Many-to-One** with User: FK uses `DeleteBehavior.Cascade`
- **Many-to-One** with Exercise: FK uses `DeleteBehavior.NoAction` (avoids multiple cascade paths through the Lesson → Course → Language chain)

#### Business Rules

- Upsert pattern: first submission creates the record, subsequent correct submissions update it
- `CompletedAt` is set only once (on first correct answer) and never overwritten
- Lesson completion check: `SUM(PointsEarned) / totalLessonPoints >= 0.70`
- `User.TotalPointsEarned` is incremented at submission time to avoid full re-aggregation on leaderboard queries
- Streak calculation derives from distinct `CompletedAt` dates (consecutive days backward from today)

---

## Entity Relationship Diagram

```
User (1) ──────────────────< (M) UserLanguage (M) >──────────── (1) Language
  │                                                                       │
  │ CreatedBy                                                             │
  └──────< (M) Course (M) >───────────────────────────────────────────────┘
                   │
                   └──────< (M) Lesson
                                  │
                                  └──────< (M) Exercise  ←─────────────────────────── User (1)
                                               │  (TPH)                                    │
                                               ├─ MultipleChoiceExercise                   │
                                               │    └──────< (M) ExerciseOption             │
                                               ├─ FillInBlankExercise                      │
                                               ├─ ListeningExercise                        │
                                               └─ TranslationExercise                      │
                                                          │                                 │
                                                          └──< (M) UserExerciseProgress >───┘
```

---

## Key Design Patterns

### Flat Content Hierarchy
- The original `Module` layer between Course and Lesson was removed to simplify the content hierarchy
- Courses now contain Lessons directly, reducing join depth for common queries

### Table-Per-Hierarchy (TPH) for Exercises
- All exercise subtypes share the `Exercises` table with a discriminator column (`ExerciseType`)
- Type-specific columns are nullable for non-owning subtypes
- Enables polymorphic queries and eager loading via EF Core cast patterns:
  ```csharp
  .ThenInclude(e => (e as MultipleChoiceExercise)!.Options)
  ```

### UUID Primary Keys
- All entities use `string Id = Guid.NewGuid().ToString()` for primary keys
- Avoids sequential ID guessing, simplifies distributed generation, and is consistent across all entity types

### Ordering and Sequencing
- All hierarchical entities use `OrderIndex` to maintain consistent ordering
- OrderIndex starts at 0 and increments for each sibling
- Allows flexible reordering without renumbering all siblings
- Auto-calculated when not provided: `MAX(OrderIndex) + 1` within the parent

### Progressive Locking
- `Lesson.IsLocked` controls lesson accessibility (default: `false` at schema level, `true` in seed data)
- `Exercise.IsLocked` controls exercise accessibility (default: `true`)
- Hybrid unlock: first exercise in a lesson unlocks with the lesson; subsequent exercises unlock sequentially on correct completion
- All unlock methods are idempotent (check `IsLocked` before writing)

### XP Caching
- `User.TotalPointsEarned` is a materialized aggregate incremented at submission time
- Avoids `SELECT SUM(PointsEarned) FROM UserExerciseProgress WHERE UserId = @id` on every leaderboard query
- Written once per exercise (first correct answer only)

### Timestamps
- Most entities include `CreatedAt` for audit trails
- Course includes `UpdatedAt` for tracking content modifications
- `UserExerciseProgress.CompletedAt` records the first correct answer timestamp (used for streak calculation)

### Inline Content Storage
- `Lesson.LessonContent` stores Editor.js JSON directly as a database text column
- Avoids external URL dependencies; content updates go through the API

### Fuzzy Answer Matching
- `TranslationExercise.MatchingThreshold` (default 0.85) allows slight variations
- `FillInBlankExercise` and `ListeningExercise` support comma-separated `AcceptedAnswers` for alternative correct responses
- Case sensitivity and whitespace trimming are configurable per exercise

---

## Validation Constraints Summary

### String Length Constraints
- **10 characters**: Language codes (SourceLanguageCode, TargetLanguageCode)
- **100 characters**: Language.Name, Course.Title
- **200 characters**: Lesson.Title, Exercise.Title
- **255 characters**: Language.FlagIconUrl
- **500 characters**: ExerciseOption.OptionText, CorrectAnswer fields, Audio/Image URLs
- **1000 characters**: Description fields, Explanation, Instructions, AcceptedAnswers, SourceText, TargetText

### Numeric Range Constraints
- **Course Duration**: 1–300 hours
- **Lesson Duration**: 10–40 minutes
- **Exercise Duration**: 5–20 minutes
- **Exercise Points**: 1–int.MaxValue
- **ListeningExercise.MaxReplays**: 1–10
- **TranslationExercise.MatchingThreshold**: 0.0–1.0

### Required Fields
- All primary keys and foreign keys
- Titles across all content entities
- Course.CreatedById (audit trail)
- Lesson.LessonContent (inline content must exist)
- Exercise.DifficultyLevel (required for all subtypes)
- Exercise.ExerciseType (TPH discriminator)
- ExerciseOption.IsCorrect (answer correctness flag)

---

## Database Considerations

### Indexing Recommendations
- Foreign keys (configured automatically by EF Core)
- `OrderIndex` fields for sorting performance
- `User.Email` and `User.UserName` (inherited from IdentityUser)
- `Language.Name` for lookups
- `Course.LanguageId` and `Course.CreatedById` for filtering
- `UserExerciseProgress.(UserId, ExerciseId)` composite index (composite PK covers this)
- `UserExerciseProgress.CompletedAt` for streak and leaderboard date range queries

### Cascade Delete Behavior
- Deleting a Language cascades to UserLanguage records
- Deleting a Course cascades to Lessons
- Deleting a Lesson cascades to Exercises
- Deleting a MultipleChoiceExercise cascades to ExerciseOptions
- Deleting a User cascades to UserExerciseProgress (`DeleteBehavior.Cascade` on UserId FK)
- Deleting an Exercise does **not** cascade to UserExerciseProgress (`DeleteBehavior.NoAction` on ExerciseId FK — SQL Server multiple cascade path constraint)

### Performance Considerations
- Use eager loading for navigation properties when loading content hierarchies
- Use `GetProgressForLessonsAsync(userId, lessonIds)` for batch progress loading (avoids N+1)
- Use EF Core `GroupJoin` for left-joins between exercises and user progress in a single round-trip
- Cache Language entities (rarely change)
- Use `ICollection` instead of `List` for large navigation properties

### EF Core TPH Gotcha
- `.WithMany()` without a navigation property reference creates a shadow FK (e.g. `ExerciseId1`)
- Always pass the inverse navigation explicitly: `.WithMany(e => e.ExerciseProgress)`

---

## Future Extension Points

The current schema supports future enhancements:

1. **Course Enrollment**: Formal enrollment system for access control beyond `UserLanguage`
2. **Achievements/Badges**: Leverage `UserExerciseProgress` and `TotalPointsEarned` for gamification milestones
3. **Social Features**: Comments, ratings, and user-generated content layers
4. **Adaptive Learning**: Use `DifficultyLevel` and per-user performance to personalise exercise selection
5. **Content Versioning**: Track `Course.UpdatedAt` for content change management
6. **Media Library**: Centralise media asset management instead of per-exercise URLs
7. **Certification**: Track lesson/course completion for certificates using `UserExerciseProgress`
8. **Streak Persistence**: Materialise daily streaks to avoid recalculating from `CompletedAt` dates on every leaderboard query

---

**Last Updated**: 2026-02-26
**Database Version**: 2.0
**EF Core Version**: Compatible with EF Core 9.0+
