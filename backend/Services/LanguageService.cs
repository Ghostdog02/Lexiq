using Backend.Api.Dtos;
using Backend.Database;
using Backend.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace Backend.Api.Services;

public class LanguageService(BackendDbContext context, IMemoryCache cache)
{
    private readonly BackendDbContext _context = context;
    private readonly IMemoryCache _cache = cache;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromHours(24);

    public async Task<List<Language>> GetAllLanguagesAsync()
    {
        return await _cache.GetOrCreateAsync("languages", async entry =>
        {
            entry.SlidingExpiration = CacheTtl;
            return await _context.Languages.Include(l => l.Courses).ToListAsync();
        }) ?? [];
    }

    public async Task<Language?> GetLanguageByIdAsync(string id)
    {
        return await _cache.GetOrCreateAsync($"language:{id}", async entry =>
        {
            entry.SlidingExpiration = CacheTtl;
            return await _context.Languages.Include(l => l.Courses).FirstOrDefaultAsync(l => l.LanguageId == id);
        });
    }

    public async Task<Language> CreateLanguageAsync(CreateLanguageDto dto)
    {
        var language = new Language
        {
            LanguageName = dto.Name,
            FlagIconUrl = dto.FlagIconUrl,
            CreatedAt = DateTime.UtcNow,
        };

        _context.Languages.Add(language);
        await _context.SaveChangesAsync();

        _cache.Remove("languages");
        return language;
    }

    public async Task<Language?> UpdateLanguageAsync(string id, CreateLanguageDto dto)
    {
        var language = await _context.Languages.FindAsync(id);
        if (language == null)
            return null;

        language.LanguageName = dto.Name;
        language.FlagIconUrl = dto.FlagIconUrl;

        await _context.SaveChangesAsync();

        _cache.Remove("languages");
        _cache.Remove($"language:{id}");
        return language;
    }

    public async Task<bool> DeleteLanguageAsync(string id)
    {
        var language = await _context.Languages.FindAsync(id);
        if (language == null)
            return false;

        _context.Languages.Remove(language);
        await _context.SaveChangesAsync();

        _cache.Remove("languages");
        _cache.Remove($"language:{id}");
        return true;
    }
}
