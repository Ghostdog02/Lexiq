using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Backend.Database.Entities.Users;

/// <summary>
/// Tracks which languages a user is enrolled in
/// </summary>
[PrimaryKey(nameof(UserId), nameof(LanguageId))]
[Index(nameof(UserId))]
public class UserLanguage
{
    [Required]
    [MaxLength(450)]
    public required string UserId { get; set; }

    [Required]
    [MaxLength(36)]
    public required string LanguageId { get; set; }

    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [ForeignKey(nameof(LanguageId))]
    public Language? Language { get; set; }
}
