using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Database.Entities.Users;

/// <summary>
/// Tracks which languages a user has learned
/// </summary>
public class UserLanguage
{
    [Required]
    public required string UserId { get; set; }

    [Required]
    public int LanguageId { get; set; }

    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;

    [ForeignKey(nameof(LanguageId))]
    public Language Language { get; set; } = null!;
}
