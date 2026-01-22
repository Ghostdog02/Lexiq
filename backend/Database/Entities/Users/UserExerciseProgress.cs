using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Backend.Database.Entities.Exercises;

namespace Backend.Database.Entities.Users;

public class UserExerciseProgress
{
    [Key]
    public int Id { get; set; }

    [Required]
    public required string UserId { get; set; }

    [Required]
    public int ExerciseId { get; set; }

    [Required]
    public bool IsCompleted { get; set; } = false;

    [Range(0, 100)]
    public int BestScore { get; set; } = 0; // Percentage (0-100)

    [Required]
    public int AttemptsCount { get; set; } = 0;

    public DateTime? FirstAttemptAt { get; set; }
    
    public DateTime? CompletedAt { get; set; } // When they first completed it

    public DateTime? LastAttemptAt { get; set; }

    [Required]
    public int PointsEarned { get; set; } = 0; // Actual points earned

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(ExerciseId))]
    public Exercise Exercise { get; set; } = null!;
}