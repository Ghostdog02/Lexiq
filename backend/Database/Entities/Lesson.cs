using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Database.Entities
{
    public class Lesson
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CourseId { get; set; }

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

        [Required]
        [MaxLength(255)]
        public required string LessonTextUrl { get; set; } // Markdown or HTML lesson url

        [Required]
        public bool IsLocked { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(CourseId))]
        public Course Course { get; set; } = null!;

        public List<Exercise> Exercises { get; set; } = [];
    }
}
