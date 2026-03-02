namespace Backend.Database.Entities.Users;

public class Achievement
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int XpRequired { get; set; }
    public string Icon { get; set; } = string.Empty;
    public int OrderIndex { get; set; }
    public ICollection<UserAchievement> UserAchievements { get; set; } = [];
}
