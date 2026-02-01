using Backend.Api.Dtos;
using Backend.Database;
using Backend.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backend.Api.Services;

public class LanguageService(BackendDbContext context)
{
    private readonly BackendDbContext _context = context;

    public async Task<List<Language>> GetAllLanguagesAsync()
    {
        return await _context.Languages.Include(l => l.Courses).ToListAsync();
    }

    public async Task<Language?> GetLanguageByIdAsync(string id)
    {
        return await _context
            .Languages.Include(l => l.Courses)
            .FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<Language> CreateLanguageAsync(CreateLanguageDto dto)
    {
        var language = new Language
        {
            Name = dto.Name,
            FlagIconUrl = dto.FlagIconUrl,
            CreatedAt = DateTime.UtcNow
        };

        _context.Languages.Add(language);
        await _context.SaveChangesAsync();

        return language;
    }

    public async Task<Language?> UpdateLanguageAsync(string id, CreateLanguageDto dto)
    {
        var language = await _context.Languages.FindAsync(id);
        if (language == null)
            return null;

        language.Name = dto.Name;
        language.FlagIconUrl = dto.FlagIconUrl;

        await _context.SaveChangesAsync();
        return language;
    }

    public async Task<bool> DeleteLanguageAsync(string id)
    {
        var language = await _context.Languages.FindAsync(id);
        if (language == null)
            return false;

        _context.Languages.Remove(language);
        await _context.SaveChangesAsync();
        return true;
    }
}
