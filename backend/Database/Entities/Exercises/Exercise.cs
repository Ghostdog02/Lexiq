using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Backend.Database.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace Backend.Database.Entities.Exercises;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DifficultyLevel
{
    Beginner,
    Intermediate,
    Advanced,
}

public enum ExerciseType
{
    FillInBlank,
    Listening,
    TrueFalse,
    ImageChoice,
    AudioMatching,
}

[Index(nameof(LessonId))]
public abstract class Exercise
{
    [Key]
    [MaxLength(36)]
    public string ExerciseId { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [MaxLength(36)]
    public string LessonId { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public required string Instructions { get; set; }

    [Required]
    [MaxLength(450)]
    public required string CreatedById { get; set; }

    [Required]
    public DifficultyLevel DifficultyLevel { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int Points { get; set; } = 0; // Points earned for completion

    public bool IsLocked { get; set; } = true;

    [Required]
    [DataType(DataType.DateTime)]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    [ForeignKey(nameof(LessonId))]
    public required Lesson Lesson { get; set; }

    [Required]
    [ForeignKey(nameof(CreatedById))]
    public required User CreatedBy { get; set; }

    public List<UserExerciseProgress> ExerciseProgress { get; set; } = [];
}
