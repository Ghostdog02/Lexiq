using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Database.Entities.Users;

public class UserLessonProgress
{
    [Key]
    public int Id { get; set; }

    [Required]
    public required string UserId { get; set; }

    [Required]
    public int LessonId { get; set; }

    [Required]
    public bool IsCompleted { get; set; } = false;

    [Required]
    [Range(0, 100)]
    public int CompletionPercentage { get; set; } = 0; // % of exercises completed

    public DateTime? StartedAt { get; set; }
    
    public DateTime? CompletedAt { get; set; }

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(LessonId))]
    public Lesson Lesson { get; set; } = null!;
}