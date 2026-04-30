using System.ComponentModel.DataAnnotations;

namespace Backend.Database.Entities.Exercises;

public class TrueFalseExercise : Exercise
{
    [Required]
    [MaxLength(1000)]
    public required string Statement { get; set; }

    [Required]
    public required bool CorrectAnswer { get; set; }

    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    [Required]
    [MaxLength(1000)]
    public required string Explanation { get; set; }
}
