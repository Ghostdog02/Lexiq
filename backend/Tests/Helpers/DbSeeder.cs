using Backend.Database;
using Backend.Database.Entities;
using Backend.Database.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests.Helpers;

public static class DbSeeder
{
    public static async Task AddUserAsync(BackendDbContext ctx, User user)
    {
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();
    }

    public static async Task AddUsersAsync(BackendDbContext ctx, IEnumerable<User> users)
    {
        ctx.Users.AddRange(users);
        await ctx.SaveChangesAsync();
    }

    /// <summary>
    /// Adds a single UserExerciseProgress row. The exerciseId must reference
    /// an existing Exercise — use one of DatabaseFixture.ExerciseIds.
    /// </summary>
    public static async Task AddProgressAsync(
        BackendDbContext ctx,
        string userId,
        string exerciseId,
        bool isCompleted,
        int pointsEarned,
        DateTime? completedAt
    )
    {
        ctx.UserExerciseProgress.Add(
            new UserExerciseProgress
            {
                UserId = userId,
                ExerciseId = exerciseId,
                IsCompleted = isCompleted,
                PointsEarned = pointsEarned,
                CompletedAt = completedAt,
            }
        );
        await ctx.SaveChangesAsync();
    }

    /// <summary>
    /// Adds multiple consecutive days of completed activity for a user.
    /// Cycles through the supplied exerciseIds (one row per day).
    /// </summary>
    public static async Task AddConsecutiveDaysActivityAsync(
        BackendDbContext ctx,
        string userId,
        IReadOnlyList<string> exerciseIds,
        int days,
        int startDaysAgo = 0,
        int pointsPerDay = 10
    )
    {
        for (var i = 0; i < days; i++)
        {
            ctx.UserExerciseProgress.Add(
                new UserExerciseProgress
                {
                    UserId = userId,
                    ExerciseId = exerciseIds[i],
                    IsCompleted = true,
                    PointsEarned = pointsPerDay,
                    CompletedAt = DateTime.UtcNow.Date.AddDays(-(startDaysAgo + i)),
                }
            );
        }
        await ctx.SaveChangesAsync();
    }

    public static async Task AddAvatarAsync(BackendDbContext ctx, string userId)
    {
        ctx.UserAvatars.Add(
            new UserAvatar
            {
                UserId = userId,
                Data = [0xFF, 0xD8],
                ContentType = "image/jpeg",
            }
        );
        await ctx.SaveChangesAsync();
    }

    /// <summary>
    /// Clears all leaderboard-related test data between tests.
    /// Preserves the system user and content hierarchy (Language/Course/Lesson/Exercises).
    ///
    /// Deletion order respects FK constraints:
    /// 1. UserExerciseProgress (FK → Users Cascade, FK → Exercises NoAction)
    /// 2. UserAvatars (FK → Users Cascade)
    /// 3. Identity junction tables (no DbSet; cleared via raw SQL)
    /// 4. Test users (excluding system user who owns the content hierarchy)
    /// </summary>
    public static async Task ClearLeaderboardDataAsync(BackendDbContext ctx, string systemUserId)
    {
        await ctx.UserExerciseProgress.ExecuteDeleteAsync();
        await ctx.UserAvatars.ExecuteDeleteAsync();

        await ctx.Database.ExecuteSqlRawAsync("DELETE FROM UserClaims");
        await ctx.Database.ExecuteSqlRawAsync("DELETE FROM UserLogins");
        await ctx.Database.ExecuteSqlRawAsync("DELETE FROM UserRoles");
        await ctx.Database.ExecuteSqlRawAsync("DELETE FROM UserTokens");

        await ctx.Users.Where(u => u.Id != systemUserId).ExecuteDeleteAsync();
    }
}
