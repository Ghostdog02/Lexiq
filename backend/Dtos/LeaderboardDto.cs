using System.Text.Json.Serialization;

namespace Backend.Api.Dtos;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TimeFrame
{
    Weekly,
    Monthly,
    AllTime
}

public record LeaderboardEntryDto(
    int Rank,
    string UserId,
    string UserName,
    string? Avatar,
    int TotalXp,
    int CurrentStreak,
    int LongestStreak,
    int Level,
    int Change,
    bool IsCurrentUser
);

public record LeaderboardResponse(
    List<LeaderboardEntryDto> Entries,
    LeaderboardEntryDto? CurrentUserEntry
);
