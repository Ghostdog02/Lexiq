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
                 ├── FillInBlankExercise
                 │    └── ExerciseOption (options with IsCorrect flag)
                 ├── ListeningExercise
                 │    └── ExerciseOption (options with IsCorrect flag)
                 ├── TrueFalseExercise
                 │    └── ExerciseOption (True / False options)
                 ├── ImageChoiceExercise
                 │    └── ImageOption (image-based answer choices)
                 └── AudioMatchingExercise
                      └── AudioMatchPair (audio-to-image pairs)
```

Additionally:
- **Many-to-many** between Users and Languages through the `UserLanguage` junction table.
- **One-to-many** from User to `UserExerciseProgress` (per-exercise tracking).
- **One-to-many** from User/Lesson to `UserLessonProgress` (per-lesson summary).
- **One-to-one** between User and `UserAvatar` (shared PK: `UserId`); avatar binary stored separately to avoid loading bytes via `UserContextMiddleware` on every request.
- **Many-to-many** between Users and Achievements through `UserAchievement`.

---

## Core Entities

### User

**Purpose**: Represents application users with authentication capabilities.

**Inheritance**: Extends `IdentityUser` from ASP.NET Core Identity, inheriting standard authentication properties (Username, Email, PasswordHash, etc.).

**File**: [User.cs](../Database/Entities/Users/User.cs)

#### Properties

| Property            | Type                         | Default       | Description                                                                                                                                                                                                                                                     |
|---------------------|------------------------------|---------------|-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Id                  | string                       | —             | Primary key (inherited from IdentityUser)                                                                                                                                                                                                                       |
| RegistrationDate    | DateTime                     | —             | Timestamp when user created their account                                                                                                                                                                                                                       |
| LastLoginDate       | DateTime                     | —             | Timestamp of user's most recent login                                                                                                                                                                                                                           |
| TotalPointsEarned   | int                          | 0             | Materialized XP aggregate; incremented on first correct exercise submission to avoid full re-aggregation for leaderboard queries                                                                                                                                |
| Hearts              | int                          | 5             | Current heart count (0–5). Decremented on wrong answers; blocked at 0. Refilled automatically every 4 hours                                                                                                                                                   |
| LastHeartResetAt    | DateTime                     | UtcNow        | Timestamp of the last heart refill cycle; used by `HeartsService` to compute elapsed time                                                                                                                                                                      |
| TimesOnTop          | int                          | 0             | Counter incremented each time the user holds the #1 rank on the AllTime leaderboard                                                                                                                                                                            |
| LastTimesOnTopAt    | DateTime?                    | null          | Timestamp of the most recent #1 rank increment                                                                                                                                                                                                                  |
| Avatar              | UserAvatar?                  | null          | Navigation property to `UserAvatars` table (1:1, shared PK). Binary avatar data stored separately to avoid loading bytes on every request. Served via `GET /api/user/{id}/avatar`. Auto-populated from Google on login; overridable via `PUT /api/user/avatar`. |
| UserLanguages       | List\<UserLanguage\>         | []            | Navigation property: Languages this user is learning                                                                                                                                                                                                            |
| ExerciseProgress    | List\<UserExerciseProgress\> | []            | Navigation property: Exercise progress records for this user                                                                                                                                                                                                    |

#### Relationships

- **One-to-Many** with UserLanguage: A user can learn multiple languages
- **One-to-Many** with UserExerciseProgress: A user has progress records per exercise
- **One-to-One** with UserAvatar: A user optionally has a stored avatar (shared PK: `UserId`)
- **Many-to-Many** with Achievement via UserAchievement

---

### UserAvatar

**Purpose**: Stores a user's avatar image as binary data in a dedicated table. Kept separate from `User` to prevent loading the `varbinary(max)` payload on every request through `UserContextMiddleware`.

**File**: [Entities/Users/UserAvatar.cs](../Database/Entities/Users/UserAvatar.cs)

#### Properties

| Property    | Type   | Constraints                                    | Description                                                    |
|-------------|--------|------------------------------------------------|----------------------------------------------------------------|
| UserId      | string | Primary Key, Foreign Key → User                | Shared PK with `User`; identifies the owning user              |
| User        | User   | Navigation                                     | Parent user entity                                             |
| Data        | byte[] | Required, `varbinary(max)`                     | Raw avatar image bytes                                         |
| ContentType | string | Required, MaxLength(50), Default: "image/jpeg" | MIME type of the stored image (e.g. `image/jpeg`, `image/png`) |

#### Relationships

- **One-to-One** with User: Shares the `UserId` primary key; a user may have zero or one avatar

#### Business Rules

- Downloaded from Google profile picture URL on every login via `AvatarService.DownloadAvatarAsync` (upsert — overwrites previous)
- Manually overridable via `PUT /api/user/avatar` (`IFormFile`); replaces existing bytes on upsert
- Served via `GET /api/user/{id}/avatar` (`[AllowAnonymous]`, `Cache-Control: public, max-age=86400`)
- Leaderboard queries batch-check existence via `AvatarService.GetUsersWithAvatarsAsync` (returns a `HashSet<string>`) without loading `Data` bytes — only IDs are fetched
- If avatar download or upsert fails, login still succeeds (avatar is non-critical)

---

### Achievement

**Purpose**: Defines an achievement or badge that users can unlock by reaching an XP milestone.

**File**: [Entities/Users/Achievement.cs](../Database/Entities/Users/Achievement.cs)

#### Properties

| Property        | Type                       | Constraints                    | Description                                   |
|-----------------|----------------------------|--------------------------------|-----------------------------------------------|
| AchievementId   | string                     | Primary Key (Guid)             | Unique identifier                             |
| AchievementName | string                     | Required, MaxLength(100)       | Display name                                  |
| Description     | string                     | Required, MaxLength(500)       | Description of how to earn the achievement    |
| XpRequired      | int                        | Required, Range(0, int.MaxValue) | XP threshold to unlock                       |
| Icon            | string                     | Required, MaxLength(10)        | Emoji or short icon string                    |
| OrderIndex      | int                        | Required                       | Display order                                 |
| UserAchievements | ICollection\<UserAchievement\> | Navigation                | Junction records for users who earned this    |

---

### UserAchievement

**Purpose**: Junction table recording which achievements each user has unlocked.

**File**: [Entities/Users/UserAchievement.cs](../Database/Entities/Users/UserAchievement.cs)

#### Properties

| Property      | Type        | Constraints                     | Description                        |
|---------------|-------------|---------------------------------|------------------------------------|
| UserId        | string      | Required, Composite PK, FK → User | User who earned the achievement  |
| AchievementId | string      | Required, Composite PK, FK → Achievement | The achievement earned      |
| UnlockedAt    | DateTime    | Required                        | When the achievement was unlocked  |
| User          | User?       | Navigation                      | Owning user                        |
| Achievement   | Achievement? | Navigation                     | Earned achievement                 |

#### Composite Primary Key

Composite PK on `(UserId, AchievementId)`.

---

### Language

**Purpose**: Represents a language that can be learned on the platform (e.g., Italian, Spanish).

**File**: [Language.cs](../Database/Entities/Language.cs)

#### Properties

| Property      | Type                 | Constraints              | Description                                |
|---------------|----------------------|--------------------------|--------------------------------------------|
| Id            | string               | Primary Key (Guid)       | Unique identifier                          |
| Name          | string               | Required, MaxLength(100) | Language name (e.g., "Italian", "Spanish") |
| FlagIconUrl   | string?              | MaxLength(255), Optional | URL to flag icon for visual representation |
| CreatedAt     | DateTime             | Default: UtcNow          | Timestamp when language was added          |
| UserLanguages | List\<UserLanguage\> | Navigation               | Users learning this language               |
| Courses       | List\<Course\>       | Navigation               | Courses available for this language        |

#### Relationships

- **One-to-Many** with UserLanguage: Multiple users can learn this language
- **One-to-Many** with Course: A language can have multiple courses

---

### UserLanguage

**Purpose**: Junction table tracking which languages each user is learning (many-to-many relationship).

**File**: [UserLanguage.cs](../Database/Entities/Users/UserLanguage.cs)

#### Properties

| Property   | Type     | Constraints           | Description                              |
|------------|----------|-----------------------|------------------------------------------|
| UserId     | string   | Required, Foreign Key | Reference to User                        |
| LanguageId | string   | Required, Foreign Key | Reference to Language                    |
| EnrolledAt | DateTime | Default: UtcNow       | When user started learning this language |
| User       | User     | Navigation            | User entity                              |
| Language   | Language | Navigation            | Language entity                          |

#### Composite Primary Key

Composite PK on `(UserId, LanguageId)`.

---

## Content Hierarchy Entities

### Course

**Purpose**: Top-level learning content container for a specific language. Represents a complete learning path (e.g., "Italian for Beginners", "Business Italian").

**File**: [Course.cs](../Database/Entities/Course.cs)

#### Properties

| Property               | Type           | Constraints               | Description                                          |
|------------------------|----------------|---------------------------|------------------------------------------------------|
| Id                     | string         | Primary Key (Guid)        | Unique identifier                                    |
| LanguageId             | string         | Required, Foreign Key     | The language this course teaches                     |
| Title                  | string         | Required, MaxLength(100)  | Course title                                         |
| Description            | string?        | MaxLength(1000), Optional | Detailed course description                          |
| EstimatedDurationHours | int?           | Range(1, 300), Optional   | Expected time to complete course                     |
| OrderIndex             | int            | Required                  | Position within the language (0, 1, 2, ...)          |
| CreatedById            | string         | Required, Foreign Key     | User who created this course                         |
| CreatedAt              | DateTime       | Default: UtcNow           | Course creation timestamp                            |
| UpdatedAt              | DateTime       | Default: UtcNow           | Last modification timestamp                          |
| Language               | Language       | Navigation                | Parent language                                      |
| CreatedBy              | User           | Navigation                | Creator user                                         |
| Lessons                | List\<Lesson\> | Navigation                | Child lessons (direct, no intermediate module layer) |

#### Business Rules

- Courses are ordered within a language using `OrderIndex`
- Duration can range from 1 to 300 hours

---

### Lesson

**Purpose**: Individual learning unit within a course, containing content and exercises.

**File**: [Lesson.cs](../Database/Entities/Lesson.cs)

#### Properties

| Property                 | Type                    | Constraints               | Description                              |
|--------------------------|-------------------------|---------------------------|------------------------------------------|
| LessonId                 | string                  | Primary Key (Guid)        | Unique identifier                        |
| CourseId                 | string                  | Required, Foreign Key     | Parent course                            |
| Title                    | string                  | Required, MaxLength(200)  | Lesson title                             |
| EstimatedDurationMinutes | int                     | Range(10, 120)            | Expected completion time in minutes      |
| OrderIndex               | int                     | Required                  | Position within course (0, 1, 2, ...)    |
| LessonContent            | string                  | Required, nvarchar(max)   | Editor.js JSON content stored as text    |
| IsLocked                 | bool                    | Required, Default: true   | Whether lesson is accessible to the user |
| CreatedAt                | DateTime                | Default: UtcNow           | Creation timestamp                       |
| Course                   | Course                  | Navigation                | Parent course                            |
| Exercises                | List\<Exercise\>        | Navigation                | Child exercises                          |

#### Business Rules

- Lessons are ordered within a course using `OrderIndex`
- Duration ranges from 10 to 120 minutes
- `IsLocked = true` by default; first lesson in a course is unlocked by the admin
- Lesson completion is determined by `UserLessonProgress.IsCompleted` (hearts-based — see Hearts System below)

---

## Exercise Entities

### Exercise (Base)

**Purpose**: Base class for all practice activities within a lesson. Uses Table-Per-Hierarchy (TPH) with a discriminator column (`ExerciseType`) to store all subtypes in a single `Exercises` table.

**File**: [Exercise.cs](../Database/Entities/Exercises/Exercise.cs)

#### Enumerations

**ExerciseType** — discriminator that identifies the concrete subtype:
- `FillInBlank` — Fill in a blank in a sentence; correct answer via ExerciseOption
- `Listening` — Listen to audio and select/type an answer; correct answer via ExerciseOption
- `TrueFalse` — True/False statement; options are pre-filled True and False ExerciseOptions
- `ImageChoice` — Select the correct image from a set of ImageOptions
- `AudioMatching` — Match audio clips to images via AudioMatchPairs

**DifficultyLevel**:
- `Beginner` (0)
- `Intermediate` (1)
- `Advanced` (2)

#### Base Properties

| Property        | Type                         | Constraints               | Description                                      |
|-----------------|------------------------------|---------------------------|--------------------------------------------------|
| ExerciseId      | string                       | Primary Key (Guid)        | Unique identifier                                |
| LessonId        | string                       | Required, Foreign Key     | Parent lesson                                    |
| Instructions    | string                       | Required, MaxLength(2000) | Instructions for completing the exercise         |
| DifficultyLevel | DifficultyLevel              | Required                  | Complexity level                                 |
| Points          | int                          | Required, Range(1, ∞)     | Points earned for correct completion             |
| CreatedAt       | DateTime                     | Default: UtcNow           | Creation timestamp                               |
| Lesson          | Lesson?                      | Navigation                | Parent lesson                                    |
| Options         | List\<ExerciseOption\>       | Navigation                | Answer options (used by FillInBlank, Listening, TrueFalse) |
| ExerciseProgress | List\<UserExerciseProgress\> | Navigation                | User progress records for this exercise          |

#### Business Rules

- Access is gated at the lesson level via `Lesson.IsLocked`; there is no per-exercise lock
- Points feed the leaderboard via `User.TotalPointsEarned`
- Wrong answers decrement the user's hearts; submissions blocked when `Hearts = 0`

---

### FillInBlankExercise

**Purpose**: User completes a sentence by selecting or typing the correct word. The sentence with a blank is stored in `Text`; correct/incorrect answers are modelled as `ExerciseOption` records on the base `Options` collection.

**File**: [Exercises/FillInBlankExercise.cs](../Database/Entities/Exercises/FillInBlankExercise.cs)

#### Additional Properties

| Property | Type   | Constraints               | Description                               |
|----------|--------|---------------------------|-------------------------------------------|
| Text     | string | Required, MaxLength(5000) | The sentence containing the blank to fill |

---

### ListeningExercise

**Purpose**: User listens to audio and selects the correct answer from `ExerciseOption` records.

**File**: [Exercises/ListeningExercise.cs](../Database/Entities/Exercises/ListeningExercise.cs)

#### Additional Properties

| Property   | Type   | Constraints               | Description                                           |
|------------|--------|---------------------------|-------------------------------------------------------|
| AudioUrl   | string | Required, MaxLength(500)  | URL to the audio file (stored in `/static/uploads/audio`) |
| MaxReplays | int    | Range(1, 10), Default: 3  | Maximum number of times the user may replay the audio |

---

### TrueFalseExercise

**Purpose**: User evaluates a statement and selects True or False. The two answer options are stored as `ExerciseOption` records (one with `IsCorrect = true`).

**File**: [Exercises/TrueFalseExercise.cs](../Database/Entities/Exercises/TrueFalseExercise.cs)

#### Additional Properties

| Property  | Type    | Constraints               | Description                                  |
|-----------|---------|---------------------------|----------------------------------------------|
| Statement | string  | Required, MaxLength(1000) | The statement to evaluate as true or false   |
| ImageUrl  | string? | MaxLength(500), Optional  | Optional supporting image URL                |

---

### ImageChoiceExercise

**Purpose**: User selects the correct image from a set of image options. Uses `ImageOption` instead of `ExerciseOption`.

**File**: [Exercises/ImageChoiceExercise.cs](../Database/Entities/Exercises/ImageChoiceExercise.cs)

#### Additional Properties

| Property | Type                  | Description                                          |
|----------|-----------------------|------------------------------------------------------|
| Options  | List\<ImageOption\>   | Image-based answer choices (shadows base `Options`)  |

---

### AudioMatchingExercise

**Purpose**: User matches audio clips to corresponding images. Uses `AudioMatchPair` records.

**File**: [Exercises/AudioMatchingExercise.cs](../Database/Entities/Exercises/AudioMatchingExercise.cs)

#### Additional Properties

| Property | Type                   | Description                     |
|----------|------------------------|---------------------------------|
| Pairs    | List\<AudioMatchPair\> | Audio-to-image matching pairs   |

---

### ExerciseOption

**Purpose**: Individual answer choice for `FillInBlankExercise`, `ListeningExercise`, and `TrueFalseExercise`. FK points to the base `Exercise`.

**File**: [Exercises/ExerciseOption.cs](../Database/Entities/Exercises/ExerciseOption.cs)

#### Properties

| Property         | Type      | Constraints              | Description                                    |
|------------------|-----------|--------------------------|------------------------------------------------|
| ExerciseOptionId | string    | Primary Key (Guid)       | Unique identifier                              |
| ExerciseId       | string    | Required, Foreign Key    | Parent Exercise (base class FK)                |
| OptionText       | string    | Required, MaxLength(500) | Display text of this answer option             |
| IsCorrect        | bool      | Required, Default: false | Whether this is the correct answer             |
| Explanation      | string    | Required, MaxLength(1000)| Feedback shown after the user answers          |
| Exercise         | Exercise  | Navigation               | Parent exercise                                |

#### Business Rules

- Exactly one option per exercise should have `IsCorrect = true`
- `Explanation` is shown to the user after they answer (correct or incorrect)

---

### ImageOption

**Purpose**: Image-based answer choice for `ImageChoiceExercise`.

**File**: [Exercises/ImageOption.cs](../Database/Entities/Exercises/ImageOption.cs)

#### Properties

| Property              | Type                 | Constraints               | Description                                      |
|-----------------------|----------------------|---------------------------|--------------------------------------------------|
| ImageOptionId         | string               | Primary Key (Guid)        | Unique identifier                                |
| ImageChoiceExerciseId | string               | Required, Foreign Key     | Parent ImageChoiceExercise                       |
| ImageUrl              | string               | Required, MaxLength(500)  | URL of the image to display                      |
| AltText               | string               | Required, MaxLength(200)  | Accessibility alt text                           |
| IsCorrect             | bool                 | Required, Default: false  | Whether this is the correct option               |
| Explanation           | string               | Required, MaxLength(1000) | Feedback shown after the user answers            |
| Exercise              | ImageChoiceExercise  | Navigation                | Parent exercise                                  |

#### Business Rules

- Indexed on `ImageChoiceExerciseId` for efficient FK queries
- Exactly one option per exercise should have `IsCorrect = true`

---

### AudioMatchPair

**Purpose**: A single audio-to-image pairing within an `AudioMatchingExercise`. Each pair has an audio clip and an image; `IsCorrect` marks the pair(s) the user should select.

**File**: [Exercises/AudioMatchPair.cs](../Database/Entities/Exercises/AudioMatchPair.cs)

#### Properties

| Property               | Type                  | Constraints               | Description                                    |
|------------------------|-----------------------|---------------------------|------------------------------------------------|
| AudioMatchPairId       | string                | Primary Key (Guid)        | Unique identifier                              |
| AudioMatchingExerciseId | string               | Required, Foreign Key     | Parent AudioMatchingExercise                   |
| AudioUrl               | string                | Required, MaxLength(500)  | URL of the audio clip                          |
| ImageUrl               | string                | Required, MaxLength(500)  | URL of the image to match                      |
| IsCorrect              | bool                  | Required, Default: false  | Whether this pair is a correct match           |
| Explanation            | string                | Required, MaxLength(1000) | Feedback shown after the user answers          |
| Exercise               | AudioMatchingExercise | Navigation                | Parent exercise                                |

#### Business Rules

- Indexed on `AudioMatchingExerciseId` for efficient FK queries

---

## Progress Entities

### UserExerciseProgress

**Purpose**: Tracks each user's progress on individual exercises, including spaced-repetition scheduling data.

**File**: [UserExerciseProgress.cs](../Database/Entities/UserExerciseProgress.cs)

#### Properties

| Property       | Type      | Constraints                         | Description                                                    |
|----------------|-----------|-------------------------------------|----------------------------------------------------------------|
| UserId         | string    | Required, Composite PK, FK → User   | Reference to User                                              |
| ExerciseId     | string    | Required, Composite PK, FK → Exercise | Reference to Exercise                                        |
| IsCompleted    | bool      | Required                            | Whether the user has answered correctly at least once          |
| PointsEarned   | int       | Required, Range(0, ∞)               | Points awarded for correct completion (0 if not yet completed) |
| CompletedAt    | DateTime? | Optional                            | Timestamp of first correct answer; null if not yet completed   |
| EaseFactor     | double    | Range(1.3, 2.5), Default: 2.5       | SM-2 ease factor for spaced repetition scheduling             |
| Interval       | int       | Range(0, ∞), Default: 0             | SM-2 interval in days until next review                       |
| Repetitions    | int       | Range(0, ∞), Default: 0             | SM-2 consecutive correct repetition count                     |
| NextReviewDate | DateTime? | Optional                            | Scheduled next review date (SM-2)                             |
| LastReviewedAt | DateTime? | Optional                            | Timestamp of the most recent review attempt                   |
| User           | User?     | Navigation                          | User entity                                                    |
| Exercise       | Exercise? | Navigation                          | Exercise entity                                                |

#### Composite Primary Key

`(UserId, ExerciseId)` — one progress record per user per exercise.

#### Relationships

- **Many-to-One** with User: FK uses `DeleteBehavior.Cascade`
- **Many-to-One** with Exercise: FK uses `DeleteBehavior.NoAction` (avoids multiple cascade paths through the Lesson → Course → Language chain)

#### Business Rules

- Upsert pattern: first submission creates the record, subsequent correct submissions update it
- `CompletedAt` is set only once (on first correct answer) and never overwritten
- `User.TotalPointsEarned` is incremented at submission time to avoid full re-aggregation on leaderboard queries
- Streak calculation derives from distinct `CompletedAt` dates (consecutive days backward from today)
- SM-2 fields (`EaseFactor`, `Interval`, `Repetitions`, `NextReviewDate`, `LastReviewedAt`) are reserved for future spaced-repetition scheduling

---

### UserLessonProgress

**Purpose**: Per-user, per-lesson summary of completion state. Upserted by `LessonProgressService` on every lesson-load and lesson-submit call.

**File**: [UserLessonProgress.cs](../Database/Entities/UserLessonProgress.cs)

#### Properties

| Property             | Type      | Constraints                        | Description                                                  |
|----------------------|-----------|------------------------------------|--------------------------------------------------------------|
| UserId               | string    | Required, Composite PK, FK → User  | Reference to User                                            |
| LessonId             | string    | Required, Composite PK, FK → Lesson | Reference to Lesson                                         |
| CompletedExercises   | int       | —                                  | Number of exercises answered correctly                       |
| TotalExercises       | int       | —                                  | Total exercises in the lesson                                |
| EarnedXp             | int       | —                                  | XP earned in this lesson                                     |
| TotalPossibleXp      | int       | —                                  | Maximum XP available in this lesson                          |
| CompletionPercentage | double    | —                                  | `CompletedExercises / TotalExercises * 100`                  |
| IsCompleted          | bool      | —                                  | True when user still has hearts at lesson submission time    |
| CompletedAt          | DateTime? | Optional                           | Timestamp when `IsCompleted` first became true               |
| UpdatedAt            | DateTime  | Default: UtcNow                    | Last update timestamp                                        |
| User                 | User?     | Navigation                         | User entity                                                  |
| Lesson               | Lesson?   | Navigation                         | Lesson entity                                                |

#### Composite Primary Key

`(UserId, LessonId)`.

#### Business Rules

- `IsCompleted = true` when the user submits a lesson with at least 1 heart remaining
- Unlocking the next lesson is gated on `IsCompleted = true`
- Indexed separately on `UserId` and `LessonId`

---

## Entity Relationship Diagram

```
User (1) ──────────────────< (M) UserLanguage (M) >──────────── (1) Language
  │  │                                                                    │
  │  └── (1:1) UserAvatar  [shared PK: UserId]                           │
  │  └──────< (M) UserAchievement (M) >──── (1) Achievement              │
  │                                                                       │
  │ CreatedBy                                                             │
  └──────< (M) Course (M) >───────────────────────────────────────────────┘
                   │
                   └──────< (M) Lesson
                                  │
                                  └──< (M) UserLessonProgress >── (1) User
                                  │
                                  └──────< (M) Exercise  ←─────────────────── User (1)
                                               │  (TPH)                            │
                                               ├─ FillInBlankExercise              │
                                               │    └──────< (M) ExerciseOption    │
                                               ├─ ListeningExercise                │
                                               │    └──────< (M) ExerciseOption    │
                                               ├─ TrueFalseExercise                │
                                               │    └──────< (M) ExerciseOption    │
                                               ├─ ImageChoiceExercise              │
                                               │    └──────< (M) ImageOption       │
                                               └─ AudioMatchingExercise            │
                                                    └──────< (M) AudioMatchPair    │
                                                          │                        │
                                                          └──< (M) UserExerciseProgress >─┘
```

---

## Key Design Patterns

### Flat Content Hierarchy
- The original `Module` layer between Course and Lesson was removed to simplify the content hierarchy
- Courses contain Lessons directly, reducing join depth for common queries

### Table-Per-Hierarchy (TPH) for Exercises
- All exercise subtypes share the `Exercises` table with a discriminator column (`ExerciseType`)
- Type-specific columns are nullable for non-owning subtypes
- Enables polymorphic queries and eager loading via EF Core cast patterns:
  ```csharp
  .ThenInclude(e => (e as ImageChoiceExercise)!.Options)
  .ThenInclude(e => (e as AudioMatchingExercise)!.Pairs)
  ```

### UUID Primary Keys
- All entities use `string Id = Guid.NewGuid().ToString()` for primary keys
- Avoids sequential ID guessing, simplifies distributed generation, and is consistent across all entity types

### Ordering and Sequencing
- All hierarchical entities use `OrderIndex` to maintain consistent ordering
- Auto-calculated when not provided: `MAX(OrderIndex) + 1` within the parent
- Exercises within a lesson do **not** use `OrderIndex` on the entity — ordering is determined by insertion/API order

### Progressive Locking
- `Lesson.IsLocked` controls lesson accessibility (default: `true`)
- Exercise access is gated solely by the parent lesson's `IsLocked` flag — there is no per-exercise lock
- All unlock methods are idempotent (check `IsLocked` before mutation)
- Admins and ContentCreators bypass the lock via `UserExtensions.CanBypassLocksAsync`

### Hearts System
- `User.Hearts` starts at 5 (max); decremented by `HeartsService.DecrementHearts` on each wrong answer
- When `Hearts = 0`, all submissions (including correct answers) are blocked with `NoHeartsException`
- Refill formula: `floor(elapsedHours / 4)` hearts per interval, capped at `MaxHearts - currentHearts`
- Refill timer freezes when `Hearts == MaxHearts` (no-op until a wrong answer is submitted)
- `LastHeartResetAt` advances by `granted * 4` hours to maintain the refill schedule
- Admins and ContentCreators bypass the hearts check

### Lesson Completion (Hearts-Based)
- A lesson is marked `IsCompleted = true` in `UserLessonProgress` when the user submits with `Hearts > 0`
- The next lesson is unlocked on `IsCompleted = true`
- `CompletionPercentage` is tracked for UI display but does **not** gate completion

### XP Caching
- `User.TotalPointsEarned` is a materialized aggregate incremented at submission time
- Avoids `SELECT SUM(PointsEarned) FROM UserExerciseProgress WHERE UserId = @id` on every leaderboard query
- Written once per exercise (first correct answer only)

### Timestamps
- Most entities include `CreatedAt` for audit trails
- Course includes `UpdatedAt` for tracking content modifications
- `UserExerciseProgress.CompletedAt` records the first correct answer timestamp (used for streak calculation)
- `UserLessonProgress.UpdatedAt` is refreshed on every upsert

### Inline Content Storage
- `Lesson.LessonContent` stores Editor.js JSON directly as a database `nvarchar(max)` column
- Avoids external URL dependencies; content updates go through the API

### Audio File Management
- `ListeningExercise.AudioUrl` and `AudioMatchPair.AudioUrl` point to files in `/static/uploads/audio`
- `FileUploadsService.DeleteOrphanedAudioAsync(graceWindow)` removes audio files not referenced by any exercise, respecting a grace window for recently uploaded files
- Cascade delete: deleting an exercise triggers `FileUploadsService` to remove the associated audio file before the DB row is deleted

---

## Validation Constraints Summary

### String Length Constraints
- **10 characters**: Language codes (BCP 47)
- **100 characters**: Language.Name, Course.Title, Achievement.AchievementName
- **200 characters**: Lesson.Title, Exercise base fields, ImageOption.AltText
- **255 characters**: Language.FlagIconUrl
- **500 characters**: ExerciseOption.OptionText, AudioUrl/ImageUrl fields, Achievement.Description
- **1000 characters**: Explanation fields (ExerciseOption, ImageOption, AudioMatchPair), Instructions (up to 2000), TrueFalseExercise.Statement
- **2000 characters**: Exercise.Instructions
- **5000 characters**: FillInBlankExercise.Text

### Numeric Range Constraints
- **Course Duration**: 1–300 hours
- **Lesson Duration**: 10–120 minutes
- **Exercise Points**: 1–int.MaxValue
- **ListeningExercise.MaxReplays**: 1–10
- **User.Hearts**: 0–5 (enforced by service, not DB constraint)
- **Achievement.XpRequired**: 0–int.MaxValue
- **SM-2 EaseFactor**: 1.3–2.5

### Required Fields
- All primary keys and foreign keys
- Titles across all content entities
- Course.CreatedById (audit trail)
- Lesson.LessonContent (inline content must exist)
- Exercise.DifficultyLevel, Exercise.Instructions (required for all subtypes)
- ExerciseOption.OptionText, ExerciseOption.Explanation
- ImageOption.ImageUrl, ImageOption.AltText, ImageOption.Explanation
- AudioMatchPair.AudioUrl, AudioMatchPair.ImageUrl, AudioMatchPair.Explanation
- TrueFalseExercise.Statement
- ListeningExercise.AudioUrl
- FillInBlankExercise.Text

---

## Database Considerations

### Indexing
- Foreign keys (configured automatically by EF Core)
- `Lesson (CourseId, OrderIndex)` — composite index for sort performance
- `Exercise (LessonId)` — index for lesson exercise queries
- `ImageOption (ImageChoiceExerciseId)` — index for child lookup
- `AudioMatchPair (AudioMatchingExerciseId)` — index for child lookup
- `UserExerciseProgress (UserId)` and `(ExerciseId)` — separate indexes
- `UserLessonProgress (UserId)` and `(LessonId)` — separate indexes
- `UserAchievement (UserId)` — index for user achievement queries
- `UserExerciseProgress.CompletedAt` for streak and leaderboard date range queries

### Cascade Delete Behavior
- Deleting a Language cascades to UserLanguage records
- Deleting a Course cascades to Lessons
- Deleting a Lesson cascades to Exercises
- Deleting an Exercise triggers `FileUploadsService` to remove associated audio files, then cascades to ExerciseOptions / ImageOptions / AudioMatchPairs
- Deleting a User cascades to UserExerciseProgress and UserLessonProgress (`DeleteBehavior.Cascade` on UserId FK)
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

**Last Updated**: 2026-05-29
**Database Version**: 3.0
**EF Core Version**: Compatible with EF Core 10.0+
