namespace Backend.Database.Entities.Exercises;

public class AudioMatchingExercise : Exercise
{
    public List<AudioMatchPair> Pairs { get; set; } = [];
}
