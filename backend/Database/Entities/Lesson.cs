using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Database.Entities
{
    public class Lesson
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ModuleId { get; set; }

        [Required]
        [MaxLength(200)]
        public required string Title { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Range(10, 40)]
        public int? EstimatedDurationMinutes { get; set; }

        [Required]
        public int OrderIndex { get; set; } // Position within the module (0, 1, 2, ...)

        public List<string>? LessonMediaUrl { get; set; } // URL for video/audio lesson resources

        [Required]
        public required string LessonTextUrl { get; set; } // Markdown or HTML lesson url

        [Required]
        public bool IsLocked { get; set; } = false;

        public int ExercisesCount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(ModuleId))]
        public Module Module { get; set; } = null!;

        public ICollection<Exercise> Exercises { get; set; } = [];
    }
}
