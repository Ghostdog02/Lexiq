using Backend.Database.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace Backend.Database.Extensions;

public class AchievementSeeder
{
    public static async Task SeedAsync(BackendDbContext context)
    {
        if (await context.Achievements.AnyAsync())
            return;

        var achievements = new List<Achievement>
        {
            new() { AchievementName = "First Steps", Description = "Earned your first 100 XP", XpRequired = 100, Icon = "🌱", OrderIndex = 0 },
            new() { AchievementName = "Getting Started", Description = "Reached 500 XP", XpRequired = 500, Icon = "🚀", OrderIndex = 1 },
            new() { AchievementName = "Dedicated Learner", Description = "Accumulated 1,000 XP", XpRequired = 1000, Icon = "📚", OrderIndex = 2 },
            new() { AchievementName = "Rising Star", Description = "Reached 2,500 XP", XpRequired = 2500, Icon = "⭐", OrderIndex = 3 },
            new() { AchievementName = "Expert Explorer", Description = "Achieved 5,000 XP", XpRequired = 5000, Icon = "🏆", OrderIndex = 4 },
            new() { AchievementName = "Master Linguist", Description = "Accumulated 10,000 XP", XpRequired = 10000, Icon = "👑", OrderIndex = 5 },
            new() { AchievementName = "Legend", Description = "Reached 25,000 XP", XpRequired = 25000, Icon = "💎", OrderIndex = 6 },
            new() { AchievementName = "Polyglot Pro", Description = "Earned 50,000 XP", XpRequired = 50000, Icon = "🌟", OrderIndex = 7 },
        };

        await context.Achievements.AddRangeAsync(achievements);
        await context.SaveChangesAsync();
    }
}
