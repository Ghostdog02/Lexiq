namespace Backend.Database.Entities.Users;

public class UserAchievement
{
    public string UserId { get; set; } = string.Empty;

    public string AchievementId { get; set; } = string.Empty;

    public DateTime UnlockedAt { get; set; }

    public User User { get; set; } = null!;
    
    public Achievement Achievement { get; set; } = null!;
}
