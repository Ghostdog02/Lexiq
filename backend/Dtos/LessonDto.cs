namespace Backend.Api.Dtos;

public record LessonDto(
    string LessonId,
    string CourseId,
    string CourseName,
    string Title,
    string? Description,
    int? EstimatedDurationMinutes,
    int OrderIndex,
    string LessonContent, // Editor.js JSON content
    bool IsLocked,
    int ExerciseCount,
    int? CompletedExercises = null,
    int? EarnedXp = null,
    int? TotalPossibleXp = null,
    bool? IsCompleted = null
);

public record CreateLessonDto(
    string CourseId,
    string Title,
    string? Description,
    int? EstimatedDurationMinutes,
    int OrderIndex,
    string Content // Editor.js JSON content
);

public record UpdateLessonDto(
    string? CourseId,
    string? Title,
    string? Description,
    int? EstimatedDurationMinutes,
    int? OrderIndex,
    string? LessonContent // Editor.js JSON content
);
