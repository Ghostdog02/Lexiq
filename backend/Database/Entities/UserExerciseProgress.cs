using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Backend.Database.Entities.Exercises;
using Backend.Database.Entities.Users;

namespace Backend.Database.Entities;

public class UserExerciseProgress
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string ExerciseId { get; set; } = string.Empty;

    [Required]
    public bool IsCompleted { get; set; }

    [Required]
    public int PointsEarned { get; set; }

    public DateTime? CompletedAt { get; set; }

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(ExerciseId))]
    public Exercise Exercise { get; set; } = null!;
}
