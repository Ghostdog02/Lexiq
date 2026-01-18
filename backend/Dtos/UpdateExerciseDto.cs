using Backend.Database.Entities;

namespace Backend.Api.Dtos;

public class UpdateExerciseDto
{
    public string? Title { get; set; }
    public string? Instructions { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public DifficultyLevel? DifficultyLevel { get; set; }
    public int? Points { get; set; }
    public int? OrderIndex { get; set; }
}
