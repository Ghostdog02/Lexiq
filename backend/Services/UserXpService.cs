using Backend.Api.Dtos;
using Backend.Database;
using Microsoft.EntityFrameworkCore;

namespace Backend.Api.Services;

public class UserXpService(BackendDbContext context)
{
    private readonly BackendDbContext _context = context;

    /// <summary>
    /// Calculates total XP for a user by summing all PointsEarned from their ExerciseProgress
    /// </summary>
    /// <param name="userId">The user's ID</param>
    /// <returns>UserXpDto with total XP and stats, or null if user not found</returns>
    public async Task<UserXpDto?> GetUserXpAsync(string userId)
    {
        var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
        if (!userExists)
            return null;

        var progressData = await _context.UserExerciseProgress
            .Where(p => p.UserId == userId)
            .Select(p => new
            {
                p.PointsEarned,
                p.IsCompleted,
                p.CompletedAt
            })
            .ToListAsync();

        var totalXp = progressData.Sum(p => p.PointsEarned);
        var completedCount = progressData.Count(p => p.IsCompleted);
        var lastActivity = progressData
            .Where(p => p.CompletedAt.HasValue)
            .Max(p => p.CompletedAt);

        return new UserXpDto(
            UserId: userId,
            TotalXp: totalXp,
            CompletedExercises: completedCount,
            LastActivityAt: lastActivity
        );
    }
}
