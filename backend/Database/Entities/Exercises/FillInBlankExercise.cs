using System.ComponentModel.DataAnnotations;

namespace Backend.Database.Entities.Exercises;

public class FillInBlankExercise : Exercise
{
    [Required]
    [MaxLength(5000)]
    public required string Text { get; set; }

    public List<ExerciseOption> Options { get; set; } = [];
}
