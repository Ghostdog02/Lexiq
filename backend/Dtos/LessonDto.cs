namespace Backend.Api.Dtos;

public record LessonDto(
    string CourseName,
    string Title,
    string? Description,
    int? EstimatedDurationMinutes,
    int OrderIndex,
    List<string>? LessonMediaUrl,
    string LessonTextUrl,
    bool IsLocked,
    int ExerciseCount
);

public record CreateLessonDto(
    string CourseName,
    string Title,
    string? Description,
    int? EstimatedDurationMinutes,
    int OrderIndex,
    List<string>? LessonMediaUrl,
    string LessonTextUrl
);

public record UpdateLessonDto(
    string CourseName,
    string? Title,
    string? Description,
    int? EstimatedDurationMinutes,
    int? OrderIndex,
    List<string>? LessonMediaUrl,
    string? LessonTextUrl
);