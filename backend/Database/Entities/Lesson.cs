using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Backend.Database.Entities.Exercises;

namespace Backend.Database.Entities;

public class Lesson
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public string CourseId { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public required string Title { get; set; }

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Range(10, 40)]
    public int? EstimatedDurationMinutes { get; set; }

    [Required]
    public int OrderIndex { get; set; } // Position within the module (0, 1, 2, ...)

    [MaxLength(255)]
    public List<string>? LessonMediaUrl { get; set; } // URL for video/audio lesson resources

    [MaxLength(255)]
    public string? LessonTextUrl { get; set; } // Optional URL for external lesson resources

    [Required]
    public required string LessonContent { get; set; } // Editor.js JSON content (stored as text)

    [Required]
    public bool IsLocked { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(CourseId))]
    public Course Course { get; set; } = null!;

    public List<Exercise> Exercises { get; set; } = [];
}
