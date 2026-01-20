using System.ComponentModel.DataAnnotations;

namespace Backend.Database.Entities.Exercises;

public class ListeningExercise : Exercise
{
    [Required]
    [MaxLength(500)]
    public required string AudioUrl { get; set; }

    [Required]
    [MaxLength(500)]
    public required string CorrectAnswer { get; set; }

    [MaxLength(1000)]
    public string? AcceptedAnswers { get; set; }

    [Required]
    public bool CaseSensitive { get; set; } = false;

    [Range(1, 10)]
    public int MaxReplays { get; set; } = 3;
}
