using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Database.Entities.Exercises;

[Index(nameof(ExerciseId))]
public class ExerciseOption
{
    [Key]
    [MaxLength(36)]
    public string ExerciseOptionId { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [MaxLength(36)]
    public string ExerciseId { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public required string OptionText { get; set; }

    [Required]
    public bool IsCorrect { get; set; } = false;

    [Required]
    [MaxLength(1000)]
    public required string Explanation { get; set; }

    [ForeignKey(nameof(ExerciseId))]
    public Exercise Exercise { get; set; } = null!;
}
