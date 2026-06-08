using Backend.Api.Dtos;
using Backend.Database;
using Backend.Database.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace Backend.Api.Services;

public class AchievementService(BackendDbContext context)
{
    private readonly BackendDbContext _context = context;

    /// <summary>
    /// Checks which achievements the user newly qualifies for (based on totalXp)
    /// and inserts UserAchievement unlock records. Called after TotalPointsEarned is incremented.
    /// </summary>
    public async Task CheckAndUnlockAchievementsAsync(string userId, int totalXp)
    {
        // Load all achievement IDs where xpRequired <= totalXp
        var qualifyingIds = await _context.Achievements
            .Where(a => a.XpRequired <= totalXp)
            .Select(a => a.AchievementId)
            .ToListAsync();

        if (qualifyingIds.Count == 0)
            return;

        // Load already-unlocked achievement IDs for this user
        var alreadyUnlockedIds = await _context.UserAchievements
            .Where(ua => ua.UserId == userId)
            .Select(ua => ua.AchievementId)
            .ToListAsync();

        var newlyUnlocked = qualifyingIds.Except(alreadyUnlockedIds).ToList();

        if (newlyUnlocked.Count == 0)
            return;

        var records = newlyUnlocked.Select(achievementId => new UserAchievement
        {
            UserId = userId,
            AchievementId = achievementId,
            UnlockedAt = DateTime.UtcNow,
            User = null!,
            Achievement = null!,
        });

        await _context.UserAchievements.AddRangeAsync(records);
        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Returns all achievement definitions merged with the user's unlock status.
    /// Used by the profile endpoint.
    /// </summary>
    public async Task<List<AchievementDto>> GetUserAchievementsAsync(string userId)
    {
        var definitions = await _context.Achievements
            .OrderBy(a => a.OrderIndex)
            .ToListAsync();

        var userUnlocks = await _context.UserAchievements
            .Where(ua => ua.UserId == userId)
            .Select(ua => new { ua.AchievementId, ua.UnlockedAt })
            .ToListAsync();

        var unlockMap = userUnlocks.ToDictionary(u => u.AchievementId, u => u.UnlockedAt);

        return definitions.Select(a =>
        {
            var isUnlocked = unlockMap.TryGetValue(a.AchievementId, out var unlockedAt);
            return new AchievementDto(
                Id: a.AchievementId,
                Name: a.AchievementName,
                Description: a.Description,
                XpRequired: a.XpRequired,
                Icon: a.Icon,
                IsUnlocked: isUnlocked,
                UnlockedAt: isUnlocked ? unlockedAt : null
            );
        }).ToList();
    }
}
