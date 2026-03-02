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
            new() { Id = Guid.NewGuid().ToString(), Name = "First Steps", Description = "Earned your first 100 XP", XpRequired = 100, Icon = "🌱", OrderIndex = 0 },
            new() { Id = Guid.NewGuid().ToString(), Name = "Getting Started", Description = "Reached 500 XP", XpRequired = 500, Icon = "🚀", OrderIndex = 1 },
            new() { Id = Guid.NewGuid().ToString(), Name = "Dedicated Learner", Description = "Accumulated 1,000 XP", XpRequired = 1000, Icon = "📚", OrderIndex = 2 },
            new() { Id = Guid.NewGuid().ToString(), Name = "Rising Star", Description = "Reached 2,500 XP", XpRequired = 2500, Icon = "⭐", OrderIndex = 3 },
            new() { Id = Guid.NewGuid().ToString(), Name = "Expert Explorer", Description = "Achieved 5,000 XP", XpRequired = 5000, Icon = "🏆", OrderIndex = 4 },
            new() { Id = Guid.NewGuid().ToString(), Name = "Master Linguist", Description = "Accumulated 10,000 XP", XpRequired = 10000, Icon = "👑", OrderIndex = 5 },
            new() { Id = Guid.NewGuid().ToString(), Name = "Legend", Description = "Reached 25,000 XP", XpRequired = 25000, Icon = "💎", OrderIndex = 6 },
            new() { Id = Guid.NewGuid().ToString(), Name = "Polyglot Pro", Description = "Earned 50,000 XP", XpRequired = 50000, Icon = "🌟", OrderIndex = 7 },
        };

        await context.Achievements.AddRangeAsync(achievements);
        await context.SaveChangesAsync();
    }
}
