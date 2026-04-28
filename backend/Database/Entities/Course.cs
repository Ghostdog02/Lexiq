using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Backend.Database.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace Backend.Database.Entities;

[Index(nameof(LanguageId), nameof(OrderIndex))]
public class Course
{
    [Key]
    [MaxLength(36)]
    public string CourseId { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [MaxLength(36)]
    public string LanguageId { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public required string Title { get; set; }

    [Required]
    [MaxLength(1000)]
    public required string Description { get; set; }

    [Range(1, 300)]
    public int EstimatedDurationHours { get; set; }
    
    [Required]
    public int OrderIndex { get; set; } // Position within the language (0, 1, 2, ...)

    [Required]
    [MaxLength(450)]
    public string CreatedById { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(LanguageId))]
    public Language Language { get; set; } = null!;

    [ForeignKey(nameof(CreatedById))]
    public User CreatedBy { get; set; } = null!;

    public List<Lesson> Lessons { get; set; } = [];
}
