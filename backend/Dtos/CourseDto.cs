using Backend.Database.Entities;

namespace Backend.Api.Dtos;

public record CourseDto(
    string LanguageName,
    string Title,
    string? Description,
    int? EstimatedDurationHours,
    int OrderIndex,
    int LessonCount
);

public record CreateCourseDto(
    string LanguageName,
    string Title,
    string? Description,
    int? EstimatedDurationHours,
    int OrderIndex
);

public record UpdateCourseDto(
    string LanguageName,
    string? Title,
    string? Description,
    int? EstimatedDurationHours,
    int? OrderIndex
);