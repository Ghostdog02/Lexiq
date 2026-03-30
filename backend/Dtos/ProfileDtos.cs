namespace Backend.Api.Dtos;

public record AchievementDto(
    string Id,
    string Name,
    string Description,
    int XpRequired,
    string Icon,
    bool IsUnlocked,
    DateTime? UnlockedAt
);

public record UserProfileDto(
    string UserId,
    string UserName,
    DateTime JoinDate,
    int TotalXp,
    int Level,
    int CurrentStreak,
    int LongestStreak,
    string? AvatarUrl,
    IReadOnlyList<AchievementDto> Achievements
);
