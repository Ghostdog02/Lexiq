using Backend.Database.Entities;

namespace Backend.Api.Dtos;

public record CourseDto(
    int Id,
    int LanguageId,
    string LanguageName,
    string Title,
    string? Description,
    int? EstimatedDurationHours,
    int OrderIndex,
    int LessonCount
);

public record CreateCourseDto(
    int LanguageId,
    string Title,
    string? Description,
    int? EstimatedDurationHours,
    int OrderIndex
);

public record UpdateCourseDto(
    string? Title,
    string? Description,
    int? EstimatedDurationHours,
    int? OrderIndex
);