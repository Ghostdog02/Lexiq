namespace Backend.Api.Dtos;

public record AchievementDto
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Description { get; init; }

    public required int XpRequired { get; init; }

    public required string Icon { get; init; }

    public required bool IsUnlocked { get; init; }

    public DateTime? UnlockedAt { get; init; }
}

public record UserProfileDto
{
    public required string UserId { get; init; }

    public required string UserName { get; init; }

    public required DateTime JoinDate { get; init; }

    public required int TotalXp { get; init; }

    public required int Level { get; init; }

    public required int CurrentStreak { get; init; }

    public required int LongestStreak { get; init; }

    public string? AvatarUrl { get; init; }

    public required IReadOnlyList<AchievementDto> Achievements { get; init; }
}
