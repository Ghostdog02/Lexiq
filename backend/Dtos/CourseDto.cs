namespace Backend.Api.Dtos;

public record CourseDto
{
    public required string CourseId { get; init; }

    public required string LanguageName { get; init; }

    public required string Title { get; init; }

    public string? Description { get; init; }

    public int? EstimatedDurationHours { get; init; }

    public required int OrderIndex { get; init; }

    public required int LessonCount { get; init; }
}

public record CreateCourseDto
{
    public required string LanguageName { get; init; }

    public required string Title { get; init; }

    public string? Description { get; init; }

    public int? EstimatedDurationHours { get; init; }

    public required int OrderIndex { get; init; }
}

public record UpdateCourseDto
{
    public string? LanguageName { get; init; }

    public string? Title { get; init; }

    public string? Description { get; init; }

    public int? EstimatedDurationHours { get; init; }

    public int? OrderIndex { get; init; }
}
