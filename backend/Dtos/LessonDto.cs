namespace Backend.Api.Dtos;

public record LessonDto(
    int Id,
    int CourseId,
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
    int CourseId,
    string Title,
    string? Description,
    int? EstimatedDurationMinutes,
    int OrderIndex,
    List<string>? LessonMediaUrl,
    string LessonTextUrl
);

public record UpdateLessonDto(
    string? Title,
    string? Description,
    int? EstimatedDurationMinutes,
    int? OrderIndex,
    List<string>? LessonMediaUrl,
    string? LessonTextUrl
);