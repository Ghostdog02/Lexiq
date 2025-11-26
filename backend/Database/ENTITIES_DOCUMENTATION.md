# Lexiq Database Entities Documentation

## Overview

This document provides comprehensive documentation for all database entities in the Lexiq language learning platform. The system is built on ASP.NET Core with Entity Framework Core and uses a hierarchical structure to organize language learning content.

## System Architecture

The Lexiq database follows a hierarchical content organization:

```
Language
  └── Course (multiple courses per language)
       └── Module (multiple modules per course)
            └── Lesson (multiple lessons per module)
                 └── Exercise (multiple exercises per lesson)
                      └── Question (multiple questions per exercise)
                           └── QuestionOption (multiple options per question)
```

Additionally, there is a many-to-many relationship between Users and Languages through the UserLanguage junction table.

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
| UserLanguages | List\<UserLanguage\> | Navigation property: Languages this user is learning |

#### Relationships

- **One-to-Many** with UserLanguage: A user can learn multiple languages
- **One-to-Many** with Course: A user can create multiple courses (as CreatedBy)

---

### Language

**Purpose**: Represents a language that can be learned on the platform (e.g., Spanish, French, German).

**File**: [Language.cs](backend/Database/Entities/Language.cs)

#### Properties

| Property | Type | Constraints | Description |
|----------|------|-------------|-------------|
| Id | int | Primary Key | Unique identifier |
| Name | string | Required, MaxLength(100) | Language name (e.g., "Spanish", "French") |
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
| LanguageId | int | Required, Foreign Key | Reference to Language |
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

**Purpose**: Top-level learning content container for a specific language. Represents a complete learning path (e.g., "Spanish for Beginners", "Business French").

**File**: [Course.cs](backend/Database/Entities/Course.cs)

#### Properties

| Property | Type | Constraints | Description |
|----------|------|-------------|-------------|
| Id | int | Primary Key | Unique identifier |
| LanguageId | int | Required, Foreign Key | The language this course teaches |
| Title | string | Required, MaxLength(100) | Course title |
| Description | string? | MaxLength(1000), Optional | Detailed course description |
| EstimatedDurationHours | int? | Range(1, 300), Optional | Expected time to complete course |
| OrderIndex | int | Required | Position within the language (0, 1, 2, ...) |
| CreatedById | string | Required, Foreign Key | User who created this course |
| CreatedAt | DateTime | Default: UtcNow | Course creation timestamp |
| UpdatedAt | DateTime | Default: UtcNow | Last modification timestamp |
| Language | Language | Navigation | Parent language |
| CreatedBy | User | Navigation | Creator user |
| Modules | List\<Module\> | Navigation | Child modules |

#### Relationships

- **Many-to-One** with Language: Course belongs to one language
- **Many-to-One** with User: Course has one creator
- **One-to-Many** with Module: Course contains multiple modules

#### Business Rules

- Courses are ordered within a language using `OrderIndex`
- Duration can range from 1 to 300 hours
- Courses track creation and update timestamps for audit purposes

---

### Module

**Purpose**: A learning module within a course, representing a thematic section or skill level (e.g., "Basic Greetings", "Past Tense Verbs").

**File**: [Module.cs](backend/Database/Entities/Module.cs)

#### Enumerations

**DifficultyLevel**: Represents the complexity level of the module
- `Beginner` (0)
- `Intermediate` (1)
- `Advanced` (2)

#### Properties

| Property | Type | Constraints | Description |
|----------|------|-------------|-------------|
| Id | int | Primary Key | Unique identifier |
| CourseId | int | Required, Foreign Key | Parent course |
| Title | string | Required, MaxLength(100) | Module title |
| Description | string? | MaxLength(1000), Optional | Module description |
| DifficultyLevel | DifficultyLevel | Required | Complexity level |
| EstimatedDurationHours | int? | Range(1, 100), Optional | Expected completion time |
| OrderIndex | int | Required | Position within course (0, 1, 2, ...) |
| CreatedAt | DateTime | Default: UtcNow | Creation timestamp |
| Course | Course | Navigation | Parent course |
| Lessons | List\<Lesson\> | Navigation | Child lessons |

#### Relationships

- **Many-to-One** with Course: Module belongs to one course
- **One-to-Many** with Lesson: Module contains multiple lessons

#### Business Rules

- Modules are ordered within a course using `OrderIndex`
- Each module has an explicit difficulty level
- Duration can range from 1 to 100 hours

---

### Lesson

**Purpose**: Individual learning unit within a module, containing content and exercises (e.g., "Introduction to Pronouns", "Conjugating -AR verbs").

**File**: [Lesson.cs](backend/Database/Entities/Lesson.cs)

#### Properties

| Property | Type | Constraints | Description |
|----------|------|-------------|-------------|
| Id | int | Primary Key | Unique identifier |
| ModuleId | int | Required, Foreign Key | Parent module |
| Title | string | Required, MaxLength(200) | Lesson title |
| Description | string? | MaxLength(1000), Optional | Lesson description |
| EstimatedDurationMinutes | int? | Range(10, 40), Optional | Expected completion time in minutes |
| OrderIndex | int | Required | Position within module (0, 1, 2, ...) |
| LessonMediaUrl | List\<string\>? | Optional | URLs for video/audio resources |
| LessonTextUrl | string | Required | URL to markdown or HTML lesson content |
| IsLocked | bool | Required, Default: false | Whether lesson is accessible to user |
| ExercisesCount | int | Default: 0 | Number of exercises in this lesson |
| CreatedAt | DateTime | Default: UtcNow | Creation timestamp |
| Module | Module | Navigation | Parent module |
| Exercises | ICollection\<Exercise\> | Navigation | Child exercises |

#### Relationships

- **Many-to-One** with Module: Lesson belongs to one module
- **One-to-Many** with Exercise: Lesson contains multiple exercises

#### Business Rules

- Lessons are ordered within a module using `OrderIndex`
- Duration ranges from 10 to 40 minutes
- Lessons can be locked to enforce sequential learning
- Content is stored externally (referenced by URLs)
- Supports multiple media types (text, video, audio)

---

### Exercise

**Purpose**: Practice activity within a lesson, containing questions to test comprehension (e.g., "Vocabulary Quiz", "Grammar Practice").

**File**: [Exercise.cs](backend/Database/Entities/Exercise.cs)

#### Properties

| Property | Type | Constraints | Description |
|----------|------|-------------|-------------|
| Id | int | Primary Key | Unique identifier |
| LessonId | int | Required, Foreign Key | Parent lesson |
| Title | string | Required, MaxLength(200) | Exercise title |
| Instructions | string? | MaxLength(1000), Optional | Instructions for completing exercise |
| EstimatedDurationMinutes | int? | Range(5, 20), Optional | Expected completion time |
| DifficultyLevel | DifficultyLevel? | Optional | Complexity level |
| Points | int | Default: 0 | Points earned for completion |
| OrderIndex | int | Required | Position within lesson |
| CreatedAt | DateTime | Default: UtcNow | Creation timestamp |
| Lesson | Lesson? | Navigation | Parent lesson |
| Questions | List\<Question\> | Navigation | Child questions |

#### Relationships

- **Many-to-One** with Lesson: Exercise belongs to one lesson
- **One-to-Many** with Question: Exercise contains multiple questions

#### Business Rules

- Exercises are ordered within a lesson using `OrderIndex`
- Duration ranges from 5 to 20 minutes
- Points system for gamification/progress tracking
- Optional difficulty level for adaptive learning

---

### Question

**Purpose**: Individual question within an exercise, testing specific knowledge or skills.

**File**: [Question.cs](backend/Database/Entities/Question.cs)

#### Enumerations

**QuestionType**: Defines the type of question and expected answer format
- `MultipleChoice` (0): Select from predefined options
- `FillInBlank` (1): Type the correct answer
- `Translation` (2): Translate text between languages

#### Properties

| Property | Type | Constraints | Description |
|----------|------|-------------|-------------|
| Id | int | Primary Key | Unique identifier |
| ExerciseId | int | Required, Foreign Key | Parent exercise |
| QuestionText | string | Required, MaxLength(1000) | The question prompt |
| QuestionAudioUrl | string? | MaxLength(500), Optional | Audio file for listening exercises |
| QuestionImageUrl | string? | MaxLength(500), Optional | Image for visual questions |
| CorrectAnswer | string? | MaxLength(500), Optional | Expected answer (for FillInBlank) |
| OrderIndex | int | Required | Position within exercise (1, 2, 3...) |
| Points | int | Default: 0 | Points awarded for correct answer |
| ExerciseType | QuestionType | Required | Type of question |
| Explanation | string? | MaxLength(1000), Optional | Educational feedback shown after answering |
| CreatedAt | DateTime | Default: UtcNow | Creation timestamp |
| Exercise | Exercise | Navigation | Parent exercise |
| QuestionOptions | List\<QuestionOption\> | Navigation | Answer options (for multiple choice) |

#### Relationships

- **Many-to-One** with Exercise: Question belongs to one exercise
- **One-to-Many** with QuestionOption: Question can have multiple options

#### Business Rules

- Questions are ordered within an exercise using `OrderIndex`
- Question type determines validation logic:
  - **MultipleChoice**: Requires QuestionOptions
  - **FillInBlank**: Requires CorrectAnswer property
  - **Translation**: May use CorrectAnswer or QuestionOptions
- Supports multimodal questions (text, audio, images)
- Explanations provide learning feedback

---

### QuestionOption

**Purpose**: Individual answer choice for multiple-choice questions.

**File**: [QuestionOption.cs](backend/Database/Entities/QuestionOption.cs)

#### Properties

| Property | Type | Constraints | Description |
|----------|------|-------------|-------------|
| Id | int | Primary Key | Unique identifier |
| QuestionId | int | Required, Foreign Key | Parent question |
| OptionText | string | Required, MaxLength(500) | Text of this answer option |
| IsCorrect | bool | Required, Default: false | Whether this is the correct answer |
| OrderIndex | int | Required | Display order (corresponds to A, B, C, D) |
| Question | Question | Navigation | Parent question |

#### Relationships

- **Many-to-One** with Question: Option belongs to one question

#### Business Rules

- Options are ordered using `OrderIndex` for consistent display
- Each question should have exactly one option marked `IsCorrect=true` for single-answer questions
- Multiple options can be marked correct for multi-select questions
- OrderIndex determines display order (typically 0=A, 1=B, 2=C, 3=D)

---

## Entity Relationship Diagram (Text)

```
User (1) ────────< (M) UserLanguage (M) >──────── (1) Language
  │                                                       │
  │ CreatedBy                                            │
  └──────< (M) Course (M) >─────────────────────────────┘
                   │
                   │
            (1) Course
                   │
                   └──────< (M) Module
                                  │
                                  └──────< (M) Lesson
                                               │
                                               └──────< (M) Exercise
                                                            │
                                                            └──────< (M) Question
                                                                         │
                                                                         └──────< (M) QuestionOption
```

## Key Design Patterns

### Ordering and Sequencing
- All hierarchical entities use `OrderIndex` to maintain consistent ordering
- OrderIndex starts at 0 and increments for each item at the same level
- This allows flexible reordering without renumbering all items

### Timestamps
- Most entities include `CreatedAt` for audit trails
- Course includes `UpdatedAt` for tracking modifications

### Soft Locking
- Lessons have `IsLocked` property for progressive unlocking
- Supports gamified learning paths where content unlocks sequentially

### Flexible Content Storage
- Lesson content stored externally (via URLs)
- Supports multiple media formats without database bloat
- Allows content updates without schema changes

### Points and Gamification
- Exercises and Questions have point values
- Enables progress tracking and leaderboards
- Motivates learners through achievement systems

### Difficulty Progression
- Modules have required difficulty levels
- Exercises have optional difficulty levels
- Supports adaptive learning and skill-based progression

### Multimodal Learning
- Questions support text, audio, and images
- Lessons support video, audio, and text content
- Accommodates different learning styles

## Validation Constraints Summary

### String Length Constraints
- **100 characters**: Language.Name, Course.Title, Module.Title
- **200 characters**: Lesson.Title, Exercise.Title
- **255 characters**: Language.FlagIconUrl
- **500 characters**: QuestionOption.OptionText, Question URLs, Question.CorrectAnswer
- **1000 characters**: Description fields, Question.QuestionText, Instructions, Explanations

### Numeric Range Constraints
- **Course Duration**: 1-300 hours
- **Module Duration**: 1-100 hours
- **Lesson Duration**: 10-40 minutes
- **Exercise Duration**: 5-20 minutes

### Required Fields
- All primary keys and foreign keys are required
- Titles are required across all content entities
- User.CreatedById is required (audit trail)
- Lesson.LessonTextUrl is required (content must exist)
- Question.ExerciseType is required (determines validation)

## Database Considerations

### Indexing Recommendations
- Foreign keys (automatic in most EF Core configurations)
- OrderIndex fields for sorting performance
- User.Email and User.UserName (inherited from IdentityUser)
- Language.Name for lookups
- Course.LanguageId and Course.CreatedById for filtering

### Cascade Delete Behavior
Consider cascade delete rules for the hierarchy:
- Deleting a Language should handle UserLanguage records
- Deleting a Course should cascade to Modules
- Deleting a Module should cascade to Lessons
- Deleting a Lesson should cascade to Exercises
- Deleting an Exercise should cascade to Questions
- Deleting a Question should cascade to QuestionOptions

### Performance Considerations
- Use eager loading for navigation properties when loading content hierarchies
- Consider pagination for large collections (courses, modules, lessons)
- Cache Language entities (rarely change)
- Use `ICollection` instead of `List` for large navigation properties

## Future Extension Points

The current schema supports future enhancements:

1. **User Progress Tracking**: Add entities to track lesson completion, exercise scores, and question attempts
2. **Course Enrollment**: Add enrollment system for course access control
3. **Achievements/Badges**: Leverage points system for gamification
4. **Social Features**: Add comments, ratings, and user-generated content
5. **Adaptive Learning**: Use DifficultyLevel and user performance for personalized paths
6. **Content Versioning**: Track Course.UpdatedAt for content change management
7. **Media Library**: Centralize media management instead of direct URLs
8. **Certification**: Track course completion for certificates

---

**Last Updated**: 2025-11-12
**Database Version**: 1.0
**EF Core Version**: Compatible with EF Core 6.0+
