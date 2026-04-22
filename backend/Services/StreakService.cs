using Backend.Database;
using Microsoft.EntityFrameworkCore;

namespace Backend.Api.Services;

public class StreakService(BackendDbContext context)
{
    private readonly BackendDbContext _context = context;

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
}
