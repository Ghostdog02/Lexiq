using Backend.Api.Dtos;
using Backend.Database;
using Microsoft.EntityFrameworkCore;

namespace Backend.Api.Services;

public class ProfileService(
    BackendDbContext context,
    StreakService streakService,
    AchievementService achievementService,
    AvatarService avatarService
)
{
    private readonly BackendDbContext _context = context;
    private readonly StreakService _streakService = streakService;
    private readonly AchievementService _achievementService = achievementService;
    private readonly AvatarService _avatarService = avatarService;

    public async Task<UserProfileDto?> GetUserProfileAsync(string userId)
    {
        var user = await _context
            .Users.Where(u => u.Id == userId)
            .Select(u => new
            {
                u.Id,
                u.UserName,
                u.RegistrationDate,
                u.TotalPointsEarned,
            })
            .FirstOrDefaultAsync();

        if (user == null)
            return null;

        var (currentStreak, longestStreak) = await _streakService.GetStreakAsync(userId);
        var level = LeaderboardService.CalculateLevel(user.TotalPointsEarned);
        var hasAvatar = await _avatarService.HasAvatarAsync(userId);
        var avatarUrl = hasAvatar ? $"/api/user/{userId}/avatar" : null;
        var achievements = await _achievementService.GetUserAchievementsAsync(userId);

        return new UserProfileDto(
            UserId: user.Id,
            UserName: user.UserName ?? "Unknown",
            JoinDate: user.RegistrationDate,
            TotalXp: user.TotalPointsEarned,
            Level: level,
            CurrentStreak: currentStreak,
            LongestStreak: longestStreak,
            AvatarUrl: avatarUrl,
            Achievements: achievements
        );
    }
}
