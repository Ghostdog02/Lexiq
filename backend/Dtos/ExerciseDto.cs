using Backend.Database.Entities;

namespace Backend.Api.Dtos;

public class ExerciseDto
{
    public int Id { get; set; }
    public int LessonId { get; set; }
    public required string Title { get; set; }
    public string? Instructions { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public DifficultyLevel? DifficultyLevel { get; set; }
    public int Points { get; set; }
    public int OrderIndex { get; set; }
    public int QuestionCount { get; set; }
}

public class CreateExerciseDto
{
    public int LessonId { get; set; }
    public required string Title { get; set; }
    public string? Instructions { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public DifficultyLevel? DifficultyLevel { get; set; }
    public int Points { get; set; }
    public int OrderIndex { get; set; }
}
