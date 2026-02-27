using Backend.Api.Dtos;
using Backend.Database;
using Microsoft.EntityFrameworkCore;

namespace Backend.Api.Services;

public class LeaderboardService(BackendDbContext context)
{
    private readonly BackendDbContext _context = context;
    private const int MaxLeaderboardEntries = 50;

    /// <summary>
    /// XP thresholds follow: threshold(n) = 50 * n * (n - 1)
    /// Level 1 = 0 XP, Level 2 = 100, Level 3 = 300, Level 4 = 600, Level 5 = 1000...
    /// </summary>
    public static int CalculateLevel(int totalXp)
    {
        if (totalXp <= 0)
            return 1;
        return (int)Math.Floor((1 + Math.Sqrt(1 + (double)totalXp / 25)) / 2);
    }

    /// <summary>
    /// Calculates current and longest streak from exercise completion dates.
    /// A streak counts consecutive UTC calendar days with at least one completed exercise.
    /// If no activity today, the streak is counted from yesterday (grace period until end of day).
    /// </summary>
    public async Task<(int CurrentStreak, int LongestStreak)> GetStreakAsync(string userId)
    {
        var activeDates = await _context
            .UserExerciseProgress.Where(p =>
                p.UserId == userId && p.IsCompleted && p.CompletedAt.HasValue
            )
            .Select(p => DateOnly.FromDateTime(p.CompletedAt!.Value))
            .Distinct()
            .OrderByDescending(d => d)
            .ToListAsync();

        if (activeDates.Count == 0)
            return (0, 0);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var currentStreak = 0;
        var longestStreak = 0;
        var streak = 1;
        var isCurrentStreak = activeDates[0] == today || activeDates[0] == today.AddDays(-1);

        for (var i = 1; i < activeDates.Count; i++)
        {
            if (activeDates[i - 1].DayNumber - activeDates[i].DayNumber == 1)
            {
                streak++;
            }
            else
            {
                if (isCurrentStreak && currentStreak == 0)
                    currentStreak = streak;

                longestStreak = Math.Max(longestStreak, streak);
                streak = 1;
                isCurrentStreak = false;
            }
        }

        // Handle the last streak in the list
        if (isCurrentStreak && currentStreak == 0)
            currentStreak = streak;
        longestStreak = Math.Max(longestStreak, streak);

        return (currentStreak, longestStreak);
    }

    public async Task<LeaderboardResponse> GetLeaderboardAsync(
        TimeFrame timeFrame,
        string? currentUserId
    )
    {
        var currentEntries = await GetRankedEntriesAsync(timeFrame);
        var previousEntries = await GetPreviousPeriodEntriesAsync(timeFrame);

        // Build a lookup of previous ranks by userId
        var previousRanks = new Dictionary<string, int>();
        for (var i = 0; i < previousEntries.Count; i++)
            previousRanks[previousEntries[i].UserId] = i + 1;

        // Enrich entries with streaks, levels, avatars, and rank change
        var enrichedEntries = new List<LeaderboardEntryDto>();
        LeaderboardEntryDto? currentUserEntry = null;

        for (var i = 0; i < currentEntries.Count; i++)
        {
            var entry = currentEntries[i];
            var currentRank = i + 1;
            var change = ComputeRankChange(entry.UserId, currentRank, previousRanks);

            var enriched = await EnrichEntryAsync(
                entry,
                currentRank,
                change,
                entry.UserId == currentUserId
            );
            enrichedEntries.Add(enriched);

            if (entry.UserId == currentUserId)
                currentUserEntry = enriched;
        }

        // If current user wasn't in the top entries, fetch their data separately
        if (currentUserId != null && currentUserEntry == null)
        {
            currentUserEntry = await GetUserEntryAsync(currentUserId, timeFrame, previousRanks);
        }

        return new LeaderboardResponse(enrichedEntries, currentUserEntry);
    }

    private async Task<List<RawLeaderboardEntry>> GetRankedEntriesAsync(TimeFrame timeFrame)
    {
        return timeFrame switch
        {
            TimeFrame.Weekly => await GetTimeFilteredLeaderboardAsync(
                DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-7)
            ),
            TimeFrame.Monthly => await GetTimeFilteredLeaderboardAsync(
                DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30)
            ),
            _ => await GetAllTimeLeaderboardAsync(),
        };
    }

    /// <summary>
    /// Gets the previous period's rankings for rank change comparison.
    /// Weekly: days 8-14 ago. Monthly: days 31-60 ago. AllTime: 8-14 days ago window.
    /// </summary>
    private async Task<List<RawLeaderboardEntry>> GetPreviousPeriodEntriesAsync(TimeFrame timeFrame)
    {
        return timeFrame switch
        {
            TimeFrame.Weekly => await GetTimeWindowedLeaderboardAsync(
                DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-14),
                DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-7)
            ),
            TimeFrame.Monthly => await GetTimeWindowedLeaderboardAsync(
                DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-60),
                DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30)
            ),
            _ => await GetTimeWindowedLeaderboardAsync(
                DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-14),
                DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-7)
            ),
        };
    }

    private async Task<List<RawLeaderboardEntry>> GetTimeWindowedLeaderboardAsync(
        DateOnly from,
        DateOnly to
    )
    {
        var fromDateTime = from.ToDateTime(TimeOnly.MinValue);
        var toDateTime = to.ToDateTime(TimeOnly.MinValue);

        return await _context
            .UserExerciseProgress.Where(p =>
                p.IsCompleted
                && p.CompletedAt.HasValue
                && p.CompletedAt >= fromDateTime
                && p.CompletedAt < toDateTime
            )
            .Join(
                _context.Users,
                p => p.UserId,
                u => u.Id,
                (p, u) =>
                    new
                    {
                        p.UserId,
                        p.PointsEarned,
                        u.UserName,
                        u.Email,
                        u.Avatar,
                    }
            )
            .GroupBy(x => new
            {
                x.UserId,
                x.UserName,
                x.Email,
                x.Avatar,
            })
            .Select(g => new
            {
                g.Key.UserId,
                g.Key.UserName,
                g.Key.Email,
                g.Key.Avatar,
                TotalXp = g.Sum(x => x.PointsEarned),
            })
            .OrderByDescending(e => e.TotalXp)
            .Take(MaxLeaderboardEntries)
            .Select(e => new RawLeaderboardEntry(
                e.UserId,
                e.UserName ?? e.Email ?? "Unknown",
                e.Avatar,
                e.TotalXp
            ))
            .ToListAsync();
    }

    private async Task<List<RawLeaderboardEntry>> GetAllTimeLeaderboardAsync()
    {
        return await _context
            .Users.OrderByDescending(u => u.TotalPointsEarned)
            .Take(MaxLeaderboardEntries)
            .Select(u => new RawLeaderboardEntry(
                u.Id,
                u.UserName ?? u.Email ?? "Unknown",
                u.Avatar,
                u.TotalPointsEarned
            ))
            .ToListAsync();
    }

    private async Task<List<RawLeaderboardEntry>> GetTimeFilteredLeaderboardAsync(DateOnly since)
    {
        var sinceDateTime = since.ToDateTime(TimeOnly.MinValue);

        return await _context
            .UserExerciseProgress.Where(p =>
                p.IsCompleted && p.CompletedAt.HasValue && p.CompletedAt >= sinceDateTime
            )
            .Join(
                _context.Users,
                p => p.UserId,
                u => u.Id,
                (p, u) => new
                    {
                        p.UserId,
                        p.PointsEarned,
                        u.UserName,
                        u.Email,
                        u.Avatar,
                    }
            )
            .GroupBy(x => new
            {
                x.UserId,
                x.UserName,
                x.Email,
                x.Avatar,
            })
            .Select(g => new RawLeaderboardEntry(
                g.Key.UserId,
                g.Key.UserName ?? g.Key.Email ?? "Unknown",
                g.Key.Avatar,
                g.Sum(x => x.PointsEarned)
            ))
            .OrderByDescending(e => e.TotalXp)
            .Take(MaxLeaderboardEntries)
            .ToListAsync();
    }

    /// <summary>
    /// Computes rank change: positive = moved up, negative = moved down, 0 = no change.
    /// If user wasn't in previous period, change is 0 (new entry).
    /// </summary>
    private static int ComputeRankChange(
        string userId,
        int currentRank,
        Dictionary<string, int> previousRanks
    )
    {
        if (!previousRanks.TryGetValue(userId, out var previousRank))
            return 0; // New entry, no previous rank to compare

        return previousRank - currentRank; // positive = moved up
    }

    private async Task<LeaderboardEntryDto> EnrichEntryAsync(
        RawLeaderboardEntry entry,
        int rank,
        int change,
        bool isCurrentUser
    )
    {
        var (currentStreak, longestStreak) = await GetStreakAsync(entry.UserId);
        var level = CalculateLevel(entry.TotalXp);

        return new LeaderboardEntryDto(
            Rank: rank,
            UserId: entry.UserId,
            UserName: entry.UserName,
            Avatar: entry.Avatar,
            TotalXp: entry.TotalXp,
            CurrentStreak: currentStreak,
            LongestStreak: longestStreak,
            Level: level,
            Change: change,
            IsCurrentUser: isCurrentUser
        );
    }

    private async Task<LeaderboardEntryDto?> GetUserEntryAsync(
        string userId,
        TimeFrame timeFrame,
        Dictionary<string, int> previousRanks
    )
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return null;

        int totalXp;
        if (timeFrame == TimeFrame.AllTime)
        {
            totalXp = user.TotalPointsEarned;
        }
        else
        {
            var since =
                timeFrame == TimeFrame.Weekly
                    ? DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-7)
                    : DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30);
            var sinceDateTime = since.ToDateTime(TimeOnly.MinValue);

            totalXp = await _context
                .UserExerciseProgress.Where(p =>
                    p.UserId == userId
                    && p.IsCompleted
                    && p.CompletedAt.HasValue
                    && p.CompletedAt >= sinceDateTime
                )
                .SumAsync(p => p.PointsEarned);
        }

        var (currentStreak, longestStreak) = await GetStreakAsync(userId);
        var level = CalculateLevel(totalXp);

        // Calculate rank: count users with more XP + 1
        int rank;
        if (timeFrame == TimeFrame.AllTime)
        {
            rank = await _context.Users.CountAsync(u => u.TotalPointsEarned > totalXp) + 1;
        }
        else
        {
            var since =
                timeFrame == TimeFrame.Weekly
                    ? DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-7)
                    : DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30);
            var sinceDateTime = since.ToDateTime(TimeOnly.MinValue);

            rank =
                await _context
                    .UserExerciseProgress.Where(p =>
                        p.IsCompleted && p.CompletedAt.HasValue && p.CompletedAt >= sinceDateTime
                    )
                    .GroupBy(p => p.UserId)
                    .CountAsync(g => g.Sum(p => p.PointsEarned) > totalXp) + 1;
        }

        var change = ComputeRankChange(userId, rank, previousRanks);

        return new LeaderboardEntryDto(
            Rank: rank,
            UserId: userId,
            UserName: user.UserName ?? user.Email ?? "Unknown",
            Avatar: user.Avatar,
            TotalXp: totalXp,
            CurrentStreak: currentStreak,
            LongestStreak: longestStreak,
            Level: level,
            Change: change,
            IsCurrentUser: true
        );
    }

    private record RawLeaderboardEntry(string UserId, string UserName, string? Avatar, int TotalXp);
}
