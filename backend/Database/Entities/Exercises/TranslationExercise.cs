using System.ComponentModel.DataAnnotations;

namespace Backend.Database.Entities.Exercises;

public class TranslationExercise : Exercise
{
    [Required]
    [MaxLength(1000)]
    public required string SourceText { get; set; }

    [Required]
    [MaxLength(1000)]
    public required string TargetText { get; set; }

    [Required]
    [MaxLength(10)]
    public required string SourceLanguageCode { get; set; } // e.g., "en", "es"

    [Required]
    [MaxLength(10)]
    public required string TargetLanguageCode { get; set; } // e.g., "en", "es"

    /// <summary>
    /// How strict the matching should be (0.0 to 1.0)
    /// Lower = more lenient
    /// </summary>
    [Range(0.0, 1.0)]
    public double MatchingThreshold { get; set; } = 0.85;
}
