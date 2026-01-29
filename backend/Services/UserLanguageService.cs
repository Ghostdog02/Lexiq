using Backend.Database;
using Backend.Database.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace Backend.Api.Services;

public class UserLanguageService(BackendDbContext context)
{
    private readonly BackendDbContext _context = context;

    public async Task<List<UserLanguage>> GetUserLanguagesAsync(string userId)
    {
        return await _context
            .UserLanguages.Where(ul => ul.UserId == userId)
            .Include(ul => ul.Language)
            .ToListAsync();
    }

    public async Task<UserLanguage?> EnrollUserAsync(string userId, string languageId)
    {
        // Check if already enrolled
        var existing = await _context.UserLanguages.FindAsync(userId, languageId);
        if (existing != null)
            return existing;

        var language = await _context.Languages.FindAsync(languageId);
        if (language == null)
            return null;

        var userLanguage = new UserLanguage
        {
            UserId = userId,
            LanguageId = languageId,
            EnrolledAt = DateTime.UtcNow
        };

        _context.UserLanguages.Add(userLanguage);
        await _context.SaveChangesAsync();
        return userLanguage;
    }

    public async Task<bool> UnenrollUserAsync(string userId, int languageId)
    {
        var userLanguage = await _context.UserLanguages.FindAsync(userId, languageId);
        if (userLanguage == null)
            return false;

        _context.UserLanguages.Remove(userLanguage);
        await _context.SaveChangesAsync();
        return true;
    }
}
