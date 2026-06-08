using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Backend.Database.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace Backend.Database.Entities;

[Index(nameof(UserId))]
[Index(nameof(LessonId))]
public class UserLessonProgress
{
    [Key, Column(Order = 0)]
    public string UserId { get; set; } = string.Empty;

    [Key, Column(Order = 1)]
    public string LessonId { get; set; } = string.Empty;

    public int CompletedExercises { get; set; }
    public int TotalExercises { get; set; }
    public int EarnedXp { get; set; }
    public int TotalPossibleXp { get; set; }
    public double CompletionPercentage { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [ForeignKey(nameof(LessonId))]
    public Lesson? Lesson { get; set; }
}
