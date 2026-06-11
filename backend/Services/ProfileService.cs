using Backend.Api.Dtos;
using Backend.Database;
using Backend.Database.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Backend.Api.Services;

public class ProfileService(BackendDbContext context, StreakService streakService, IMemoryCache cache)
{
    private readonly BackendDbContext _context = context;
    private readonly StreakService _streakService = streakService;
    private readonly IMemoryCache _cache = cache;

    public async Task<UserProfileDto?> GetUserProfileAsync(string userId)
    {
        // Query 1: user data + avatar existence in one round trip
        var user = await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => new
            {
                u.Id,
                u.UserName,
                u.RegistrationDate,
                u.TotalPointsEarned,
                HasAvatar = _context.UserAvatars.Any(a => a.UserId == u.Id),
            })
            .FirstOrDefaultAsync();

        if (user == null)
            return null;

        // Query 2: streak (requires in-memory date computation)
        var (currentStreak, longestStreak) = await _streakService.GetStreakAsync(userId);

        // Query 3a: achievement definitions from cache (miss → SQL, hit → free)
        var definitions = await _cache.GetOrCreateAsync("achievement_defs", async entry =>
        {
            entry.SlidingExpiration = TimeSpan.FromHours(24);
            return await _context.Achievements.OrderBy(a => a.OrderIndex).ToListAsync();
        }) ?? [];

        // Query 3b: this user's unlocked achievements only
        var userUnlocks = await _context.UserAchievements
            .Where(ua => ua.UserId == userId)
            .ToDictionaryAsync(ua => ua.AchievementId);

        var achievements = definitions
            .Select(a =>
            {
                var unlocked = userUnlocks.TryGetValue(a.AchievementId, out var ua);
                return new AchievementDto(
                    a.AchievementId,
                    a.AchievementName,
                    a.Description,
                    a.XpRequired,
                    a.Icon,
                    unlocked,
                    unlocked ? (DateTime?)ua!.UnlockedAt : null
                );
            })
            .ToList();

        return new UserProfileDto(
            UserId: user.Id,
            UserName: user.UserName ?? "Unknown",
            JoinDate: user.RegistrationDate,
            TotalXp: user.TotalPointsEarned,
            Level: LeaderboardService.CalculateLevel(user.TotalPointsEarned),
            CurrentStreak: currentStreak,
            LongestStreak: longestStreak,
            AvatarUrl: user.HasAvatar ? $"/api/user/{userId}/avatar" : null,
            Achievements: achievements
        );
    }
}
