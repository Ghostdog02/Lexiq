using System.ComponentModel.DataAnnotations;

namespace Backend.Database.Entities
{
    public class Language
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public required string Name { get; set; }

        [MaxLength(255)]
        public string? FlagIconUrl { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<Course> Courses { get; set; } = [];

        public List<UserLanguage> UserLanguages { get; set; } = [];
    }
}