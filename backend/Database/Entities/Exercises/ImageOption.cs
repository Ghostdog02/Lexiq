using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Database.Entities.Exercises;

[Index(nameof(ImageChoiceExerciseId))]
public class ImageOption
{
    [Key]
    [MaxLength(36)]
    public string ImageOptionId { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [MaxLength(36)]
    public string ImageChoiceExerciseId { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public required string ImageUrl { get; set; }

    [Required]
    [MaxLength(200)]
    public required string AltText { get; set; }

    [Required]
    public bool IsCorrect { get; set; } = false;

    [Required]
    [MaxLength(1000)]
    public required string Explanation { get; set; }

    [ForeignKey(nameof(ImageChoiceExerciseId))]
    public ImageChoiceExercise Exercise { get; set; } = null!;
}
