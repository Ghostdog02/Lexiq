using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Backend.Database.Entities.Users;

[PrimaryKey(nameof(UserId), nameof(AchievementId))]
[Index(nameof(UserId))]
public class UserAchievement
{
    [Required]
    public required string UserId { get; set; }

    [Required]
    public required string AchievementId { get; set; }

    [Required]
    public DateTime UnlockedAt { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [ForeignKey(nameof(AchievementId))]
    public Achievement? Achievement { get; set; }
}
