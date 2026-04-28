using System.Text.Json.Serialization;

namespace Backend.Api.Dtos;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TimeFrame
{
    Weekly,
    Monthly,
    AllTime
}

public record LeaderboardEntryDto
{
    public required int Rank { get; init; }

    public required string UserId { get; init; }

    public required string UserName { get; init; }

    public string? Avatar { get; init; }

    public required int TotalXp { get; init; }

    public required int CurrentStreak { get; init; }

    public required int LongestStreak { get; init; }

    public required int Level { get; init; }

    public required int Change { get; init; }

    public required bool IsCurrentUser { get; init; }
}

public record LeaderboardResponse
{
    public required List<LeaderboardEntryDto> Entries { get; init; }

    public LeaderboardEntryDto? CurrentUserEntry { get; init; }
}
