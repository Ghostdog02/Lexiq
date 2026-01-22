using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Backend.Database.Entities.Users;

namespace Backend.Database.Entities.Exercises;

public enum DifficultyLevel
{
    Beginner,
    Intermediate,
    Advanced,
}

public enum ExerciseType
{
    MultipleChoice,
    FillInTheBlank,
    Listening,
    Translation,
}

public abstract class Exercise
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int LessonId { get; set; }

    [Required]
    [MaxLength(200)]
    public required string Title { get; set; }

    [MaxLength(1000)]
    public string? Instructions { get; set; }

    [Range(5, 20)]
    public int? EstimatedDurationMinutes { get; set; }

    [Required]
    public DifficultyLevel DifficultyLevel { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int Points { get; set; } = 0; // Points earned for completion

    [MaxLength(1000)]
    public string? Explanation { get; set; }

    [Required]
    public int OrderIndex { get; set; } // Position in Lesson

    [Required]
    [DataType(DataType.DateTime)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(LessonId))]
    public Lesson? Lesson { get; set; }

    public List<UserExerciseProgress> UserProgress { get; set; } = [];
}
