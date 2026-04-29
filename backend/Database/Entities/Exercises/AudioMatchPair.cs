using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Backend.Database.Entities.Exercises;

[Index(nameof(AudioMatchingExerciseId))]
public class AudioMatchPair
{
    [Key]
    [MaxLength(36)]
    public string AudioMatchPairId { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [MaxLength(36)]
    public string AudioMatchingExerciseId { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public required string AudioUrl { get; set; }

    [Required]
    [MaxLength(500)]
    public required string ImageUrl { get; set; }

    [Required]
    [MaxLength(1000)]
    public required string Explanation { get; set; }

    [ForeignKey(nameof(AudioMatchingExerciseId))]
    public AudioMatchingExercise Exercise { get; set; } = null!;
}
