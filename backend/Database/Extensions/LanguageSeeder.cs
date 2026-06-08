using Backend.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backend.Database.Extensions;

/// <summary>
/// Seeds the Italian language into the database.
/// Returns the language ID for use by downstream seeders.
/// </summary>
public static class LanguageSeeder
{
    public static async Task<string> SeedAsync(BackendDbContext context)
    {
        var italian = await context.Languages.FirstOrDefaultAsync(l => l.LanguageName == "Italian");

        if (italian != null)
        {
            return italian.LanguageId;
        }

        italian = new Language
        {
            LanguageName = "Italian",
            FlagIconUrl = "https://flagcdn.com/w40/it.png",
            CreatedAt = DateTime.UtcNow,
        };

        context.Languages.Add(italian);
        await context.SaveChangesAsync();

        return italian.LanguageId;
    }
}
