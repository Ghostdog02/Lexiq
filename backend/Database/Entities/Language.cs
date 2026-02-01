using System.ComponentModel.DataAnnotations;
using Backend.Database.Entities.Users;

namespace Backend.Database.Entities;

public class Language
{
    [Key]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [MaxLength(100)]
    public required string Name { get; set; }

    [MaxLength(255)]
    public string? FlagIconUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Course> Courses { get; set; } = [];

    public List<UserLanguage> UserLanguages { get; set; } = [];
}