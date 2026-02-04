namespace Backend.Api.Dtos;

public record LessonDto(
    string LessonId,
    string CourseId,
    string CourseName,
    string Title,
    string? Description,
    int? EstimatedDurationMinutes,
    int OrderIndex,
    List<string>? LessonMediaUrl,
    string LessonContent, // Editor.js JSON content
    string? LessonTextUrl, // Optional external URL
    bool IsLocked,
    int ExerciseCount
);

public record CreateLessonDto(
    string CourseId,
    string Title,
    string? Description,
    int? EstimatedDurationMinutes,
    int OrderIndex,
    List<string>? LessonMediaUrl,
    string Content // Editor.js JSON content
);

public record UpdateLessonDto(
    string? CourseId,
    string? Title,
    string? Description,
    int? EstimatedDurationMinutes,
    int? OrderIndex,
    List<string>? LessonMediaUrl,
    string? LessonContent, // Editor.js JSON content
    string? LessonTextUrl // Optional external URL
);
