using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Backend.Database.Entities.Exercises;
using Microsoft.EntityFrameworkCore;

namespace Backend.Database.Entities;

[Index(nameof(CourseId), nameof(OrderIndex))]
public class Lesson
{
    [Key]
    [MaxLength(36)]
    public string LessonId { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [MaxLength(36)]
    public string CourseId { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public required string Title { get; set; }

    [Range(10, 120)]
    public int EstimatedDurationMinutes { get; set; }

    [Required]
    public int OrderIndex { get; set; } // Position within the course (0, 1, 2, ...)

    [Required]
    [Column(TypeName = "nvarchar(max)")]
    public required string LessonContent { get; set; } // Editor.js JSON content

    [Required]
    public bool IsLocked { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(CourseId))]
    public Course Course { get; set; } = null!;

    public List<Exercise> Exercises { get; set; } = [];
}
