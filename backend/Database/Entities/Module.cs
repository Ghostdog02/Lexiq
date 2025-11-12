using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Database.Entities
{
    public enum DifficultyLevel
    {
        Beginner,
        Intermediate,
        Advanced
    }

    public class Module
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CourseId { get; set; }

        [Required]
        [MaxLength(100)]
        public required string Title { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }
        
        [Required]
        public DifficultyLevel DifficultyLevel { get; set; }

        [Range(1, 100)]
        public int? EstimatedDurationHours { get; set; }

        [Required]
        public int OrderIndex { get; set; } // Position within the course (0, 1, 2, ...)

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(CourseId))]
        public Course Course { get; set; } = null!;

        public List<Lesson> Lessons { get; set; } = [];
    }
}
