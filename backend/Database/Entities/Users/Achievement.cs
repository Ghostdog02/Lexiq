using System.ComponentModel.DataAnnotations;

namespace Backend.Database.Entities.Users;

public class Achievement
{
    [Key]
    [MaxLength(36)]
    public string AchievementId { get; set; } = Guid.NewGuid().ToString();

    [Required]
    [MaxLength(100)]
    public required string AchievementName { get; set; }

    [Required]
    [MaxLength(500)]
    public required string Description { get; set; }

    [Required]
    [Range(0, int.MaxValue)]
    public int XpRequired { get; set; }

    [Required]
    [MaxLength(10)]
    public required string Icon { get; set; }

    [Required]
    public int OrderIndex { get; set; }

    public ICollection<UserAchievement> UserAchievements { get; set; } = [];
}
