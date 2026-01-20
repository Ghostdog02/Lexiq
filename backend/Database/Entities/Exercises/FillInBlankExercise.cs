using System.ComponentModel.DataAnnotations;

namespace Backend.Database.Entities.Exercises;

public class FillInBlankExercise : Exercise
{
    [Required]
    [MaxLength(500)]
    public required string CorrectAnswer { get; set; }

    [MaxLength(1000)]
    public string? AcceptedAnswers { get; set; }

    [Required]
    public bool CaseSensitive { get; set; } = false;

    [Required]
    public bool TrimWhitespace { get; set; } = true;
}
