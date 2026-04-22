# Course Entity

**Namespace:** `Backend.Database.Entities`
**Table:** `Courses`

## Overview

Represents a language learning course within a specific language. Courses contain multiple lessons and are organized by `OrderIndex` for sequential progression.

## Properties

| Property | Type | Constraints | Description |
|----------|------|-------------|-------------|
| `Id` | `string` | Primary Key, GUID | Unique identifier |
| `LanguageId` | `string` | Foreign Key, Required | Reference to parent Language |
| `Title` | `string` | Required, MaxLength(200) | Course display name |
| `Description` | `string?` | Optional, MaxLength(1000) | Course overview |
| `EstimatedDurationHours` | `int?` | Optional | Expected completion time |
| `OrderIndex` | `int` | Required | Display order within language (0-based) |
| `CreatedById` | `string` | Foreign Key, Required | User who created the course |
| `CreatedAt` | `DateTime` | Required | UTC timestamp of creation |
| `UpdatedAt` | `DateTime` | Required | UTC timestamp of last modification |

## Navigation Properties

| Property | Type | Relationship | Description |
|----------|------|--------------|-------------|
| `Language` | `Language` | Many-to-One | Parent language |
| `Lessons` | `List<Lesson>` | One-to-Many | Child lessons (cascade delete) |
| `CreatedBy` | `User` | Many-to-One | Creator user |

## Business Rules

### Creation
- `LanguageId` must reference an existing Language
- `CreatedById` must reference an existing User
- `OrderIndex` is required (no auto-increment)
- `CreatedAt` and `UpdatedAt` set to `DateTime.UtcNow`

### Update
- Partial updates supported via `UpdateCourseDto`
- Null DTO fields preserve original values
- `UpdatedAt` automatically updated on every change
- Cannot change `LanguageId` after creation

### Delete
- Cascade deletes all child `Lessons`
- Cascade deletes propagate to `Exercises` → `UserExerciseProgress`
- Returns `false` if course doesn't exist

### Ordering
- `GetAllCoursesAsync()` returns courses ordered by `OrderIndex` ascending
- Multiple courses can share the same language
- No unique constraint on `OrderIndex` within a language

## DTOs

### CreateCourseDto
```csharp
public record CreateCourseDto(
    string LanguageName,        // Language name (not ID)
    string Title,
    string? Description,
    int? EstimatedDurationHours,
    int OrderIndex
);
```

**Validation:**
- `LanguageName` must exist → throws `ArgumentException` if not found
- `Title` is required
- `OrderIndex` is required (must be provided by caller)

### UpdateCourseDto
```csharp
public record UpdateCourseDto(
    string LanguageName,        // Not used (cannot change language)
    string? Title,
    string? Description,
    int? EstimatedDurationHours,
    int? OrderIndex
);
```

**Partial Update Behavior:**
- All fields except `LanguageName` are nullable
- `null` = "don't change this field"
- Only non-null fields are updated

### CourseDto
```csharp
public record CourseDto(
    string CourseId,
    string LanguageName,
    string Title,
    string? Description,
    int? EstimatedDurationHours,
    int OrderIndex,
    int LessonCount
);
```

**Mapping:**
- `LessonCount` derived from `Lessons.Count`
- `LanguageName` from `Language.Name`

## Service Methods

### CourseService

| Method | Returns | Description |
|--------|---------|-------------|
| `GetAllCoursesAsync()` | `List<Course>` | All courses ordered by `OrderIndex`, includes `Language` |
| `GetCourseByIdAsync(id)` | `Course?` | Single course with `Language` and `Lessons`, or null |
| `CreateCourseAsync(dto, userId)` | `Course` | Creates new course, throws if language not found |
| `UpdateCourseAsync(id, dto)` | `Course?` | Updates existing course, null if not found |
| `DeleteCourseAsync(id)` | `bool` | Deletes course and cascades, false if not found |

## Test Coverage

**File:** `backend/Tests/Services/CourseCrudTests.cs` (16 tests)

**Categories:**
- DTO validation (invalid language, null fields, timestamps)
- Read operations (ordering, eager loading)
- Update operations (partial updates, timestamp changes)
- Delete operations (cascade deletes, non-existent handling)

**Key Test Cases:**
- `CreateCourseAsync_InvalidLanguageName_ThrowsArgumentException`
- `UpdateCourse_PartialFields_OnlyUpdatesProvidedFields`
- `DeleteCourse_WithLessons_CascadeDeletes`
- `GetAllCourses_ReturnsCoursesOrderedByOrderIndex`

## Common Patterns

### Creating a Course
```csharp
var dto = new CreateCourseDto(
    LanguageName: "Italian",
    Title: "Beginner Italian",
    Description: "Learn the basics",
    EstimatedDurationHours: 40,
    OrderIndex: 0
);

var course = await courseService.CreateCourseAsync(dto, currentUser.Id);
```

### Partial Update
```csharp
var updateDto = new UpdateCourseDto(
    LanguageName: "Italian",  // Ignored
    Title: "Updated Title",
    Description: null,        // Preserves original
    EstimatedDurationHours: null,  // Preserves original
    OrderIndex: 5            // Updates to 5
);

var updated = await courseService.UpdateCourseAsync(courseId, updateDto);
```

### Cascade Delete Behavior
```csharp
// Deleting course also deletes:
// - All Lessons in the course
// - All Exercises in those lessons
// - All UserExerciseProgress for those exercises
await courseService.DeleteCourseAsync(courseId);
```

## Related Entities

- **Parent:** [Language](./Language.md)
- **Child:** [Lesson](./Lesson.md)
- **References:** User (via `CreatedById`)

## Database Schema

```sql
CREATE TABLE [Courses] (
    [Id] nvarchar(450) NOT NULL PRIMARY KEY,
    [LanguageId] nvarchar(450) NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [Description] nvarchar(1000) NULL,
    [EstimatedDurationHours] int NULL,
    [OrderIndex] int NOT NULL,
    [CreatedById] nvarchar(450) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NOT NULL,
    CONSTRAINT [FK_Courses_Languages] FOREIGN KEY ([LanguageId])
        REFERENCES [Languages]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_Courses_Users] FOREIGN KEY ([CreatedById])
        REFERENCES [Users]([Id]) ON DELETE NO ACTION
);
```

## Migration History

- Initial creation: Migration `AddCourseEntity`
- Added `EstimatedDurationHours`: Migration `AddCourseDuration`
- Added `CreatedById` FK: Migration `AddCourseCreator`
