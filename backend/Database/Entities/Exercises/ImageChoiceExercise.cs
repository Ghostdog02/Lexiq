namespace Backend.Database.Entities.Exercises;

public class ImageChoiceExercise : Exercise
{
    public List<ImageOption> Options { get; set; } = [];
}
