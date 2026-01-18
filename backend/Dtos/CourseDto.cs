using Backend.Database.Entities;

namespace Backend.Api.Dtos;

public class CourseDto
{
    public int Id { get; set; }
    public int LanguageId { get; set; }
    public string LanguageName { get; set; } = string.Empty;
    public required string Title { get; set; }
    public string? Description { get; set; }
    public int? EstimatedDurationHours { get; set; }
    public int OrderIndex { get; set; }
    public int LessonCount { get; set; }
}

public class CreateCourseDto
{
    public int LanguageId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public int? EstimatedDurationHours { get; set; }
    public int OrderIndex { get; set; }
}

public class UpdateCourseDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public int? EstimatedDurationHours { get; set; }
    public int? OrderIndex { get; set; }
}
