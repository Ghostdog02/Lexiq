namespace Backend.Database.Entities.Exercises;

public class ImageChoiceExercise : Exercise
{
    public new List<ImageOption> Options { get; set; } = [];
}
