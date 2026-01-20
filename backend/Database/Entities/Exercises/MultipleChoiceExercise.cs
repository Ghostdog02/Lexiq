namespace Backend.Database.Entities.Exercises;

public class MultipleChoiceExercise : Exercise
{
    public List<ExerciseOption> Options { get; set; } = [];
}
