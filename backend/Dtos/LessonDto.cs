namespace Backend.Api.Dtos;

public class LessonDto
{
    public int Id { get; set; }
    public int CourseId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public int OrderIndex { get; set; }
    public List<string>? LessonMediaUrl { get; set; }
    public required string LessonTextUrl { get; set; }
    public bool IsLocked { get; set; }
    public int ExerciseCount { get; set; }
}

public class CreateLessonDto
{
    public int CourseId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public int OrderIndex { get; set; }
    public List<string>? LessonMediaUrl { get; set; }
    public required string LessonTextUrl { get; set; }
}

public class UpdateLessonDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public int? OrderIndex { get; set; }
    public List<string>? LessonMediaUrl { get; set; }
    public string? LessonTextUrl { get; set; }
}
