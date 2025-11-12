using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Database.Entities
{

    public class Exercise
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int LessonId { get; set; }

        [Required]
        [MaxLength(200)]
        public required string Title { get; set; }

        [MaxLength(1000)]
        public string? Instructions { get; set; }

        [Range(5, 20)]
        public int? EstimatedDurationMinutes { get; set; }

        public DifficultyLevel? DifficultyLevel { get; set; }

        public int Points { get; set; } = 0; // Points earned for completion

        [Required]
        public int OrderIndex { get; set; } // Position in Lesson

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(LessonId))]
        public Lesson? Lesson { get; set; }

        public List<Question> Questions { get; set; } = [];
    }
}
