using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Backend.Database.Entities.Exercises;
using Backend.Database.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace Backend.Database.Entities;

[PrimaryKey(nameof(UserId), nameof(ExerciseId))]
[Index(nameof(UserId))]
[Index(nameof(ExerciseId))]
public class UserExerciseProgress
{
    [Required]
    [MaxLength(450)]
    public required string UserId { get; set; }

    [Required]
    [MaxLength(36)]
    public required string ExerciseId { get; set; }

    [Required]
    public bool IsCompleted { get; set; }

    [Required]
    [Range(0, int.MaxValue)]
    public int PointsEarned { get; set; }

    public DateTime? CompletedAt { get; set; }

    // SM-2 spaced repetition fields
    [Range(1.3, 2.5)]
    public double EaseFactor { get; set; } = 2.5;

    [Range(0, int.MaxValue)]
    public int Interval { get; set; } = 0;

    [Range(0, int.MaxValue)]
    public int Repetitions { get; set; } = 0;

    public DateTime? NextReviewDate { get; set; }

    public DateTime? LastReviewedAt { get; set; }

    [Required]
    [ForeignKey(nameof(UserId))]
    public required User User { get; set; }

    [Required]
    [ForeignKey(nameof(ExerciseId))]
    public required Exercise Exercise { get; set; }
}
