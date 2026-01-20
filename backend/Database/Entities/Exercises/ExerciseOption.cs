using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Database.Entities.Exercises;

public class ExerciseOption
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ExerciseId { get; set; }

    [Required]
    [MaxLength(500)]
    public required string OptionText { get; set; }

    [Required]
    public bool IsCorrect { get; set; } = false;

    [Required]
    public int OrderIndex { get; set; } // Display order (A, B, C, D)

    [ForeignKey(nameof(ExerciseId))]
    public Exercise Exercise { get; set; } = null!;
}
