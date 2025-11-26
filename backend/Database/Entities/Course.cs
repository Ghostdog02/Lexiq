using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Database.Entities
{
    public class Course
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int LanguageId { get; set; }

        [Required]
        [MaxLength(100)]
        public required string Title { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        [Range(1, 300)]
        public int? EstimatedDurationHours { get; set; }
        
        [Required]
        public int OrderIndex { get; set; } // Position within the language (0, 1, 2, ...)

        [Required]
        public required string CreatedById { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(LanguageId))]
        public Language Language { get; set; } = null!;

        [ForeignKey(nameof(CreatedById))]
        public User CreatedBy { get; set; } = null!;

        public List<Lesson> Lessons { get; set; } = [];
    }
}
