using System.ComponentModel.DataAnnotations;
namespace Backend.Database.Entities.Exercises;

public class ListeningExercise : Exercise
{
    [Required]
    [MaxLength(500)]
    public required string AudioUrl { get; set; }

    [Range(1, 10)]
    public int MaxReplays { get; set; } = 3;

    public List<ExerciseOption> Options { get; set; } = [];
}
