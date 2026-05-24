using System.ComponentModel.DataAnnotations;
using Backend.Database.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace Backend.Database.Entities;

[Index(nameof(LanguageName), IsUnique = true)]
public class Language
{
    [Key]
    public string LanguageId { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [MaxLength(100)]
    public required string LanguageName { get; set; }

    [MaxLength(255)]
    public string? FlagIconUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Course> Courses { get; set; } = [];

    public List<UserLanguage> UserLanguages { get; set; } = [];
}