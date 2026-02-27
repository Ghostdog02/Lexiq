using Backend.Database;
using Backend.Database.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace Backend.Api.Services;

public class AvatarService(
    BackendDbContext context,
    IHttpClientFactory httpClientFactory,
    ILogger<AvatarService> logger
)
{
    private readonly BackendDbContext _context = context;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ILogger<AvatarService> _logger = logger;

    private const int MaxAvatarSizeBytes = 1 * 1024 * 1024; // 1MB

    private static readonly HashSet<string> AllowedExtensions =
    [
        ".jpg",
        ".jpeg",
        ".png",
        ".gif",
        ".webp",
    ];

    private static readonly Dictionary<string, string> ExtensionToContentType = new()
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
    };

    public async Task<(byte[]? Data, string? ContentType)> DownloadAvatarAsync(string url)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("GoogleAvatar");
            var response = await client.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to download avatar from {Url}: {Status}",
                    url,
                    response.StatusCode
                );
                return (null, null);
            }

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "image/jpeg";
            var data = await response.Content.ReadAsByteArrayAsync();

            if (data.Length > MaxAvatarSizeBytes)
            {
                _logger.LogWarning(
                    "Avatar from {Url} exceeds size limit ({Size} bytes)",
                    url,
                    data.Length
                );
                return (null, null);
            }

            return (data, contentType);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error downloading avatar from {Url}", url);
            return (null, null);
        }
    }

    public async Task UpsertAvatarAsync(string userId, byte[] data, string contentType)
    {
        var existing = await _context.UserAvatars.FindAsync(userId);

        if (existing != null)
        {
            existing.Data = data;
            existing.ContentType = contentType;
        }
        else
        {
            _context.UserAvatars.Add(
                new UserAvatar
                {
                    UserId = userId,
                    Data = data,
                    ContentType = contentType,
                }
            );
        }

        await _context.SaveChangesAsync();
    }

    public async Task<(byte[]? Data, string? ContentType)> GetAvatarAsync(string userId)
    {
        var avatar = await _context.UserAvatars.FindAsync(userId);
        return avatar != null ? (avatar.Data, avatar.ContentType) : (null, null);
    }

    public static (bool IsValid, string? Error) ValidateAvatarFile(IFormFile file)
    {
        if (file.Length == 0)
            return (false, "File is empty");

        if (file.Length > MaxAvatarSizeBytes)
            return (false, $"File exceeds {MaxAvatarSizeBytes / (1024 * 1024)}MB limit");

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(extension))
            return (false, $"Invalid file type. Allowed: {string.Join(", ", AllowedExtensions)}");

        return (true, null);
    }

    public static string GetContentType(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        return ExtensionToContentType.GetValueOrDefault(extension, "image/jpeg");
    }

    public async Task<bool> HasAvatarAsync(string userId)
    {
        return await _context.UserAvatars.AnyAsync(a => a.UserId == userId);
    }

    public async Task<HashSet<string>> GetUsersWithAvatarsAsync(List<string> userIds)
    {
        var list = await _context
            .UserAvatars.Where(a => userIds.Contains(a.UserId))
            .Select(a => a.UserId)
            .ToListAsync();

        return list.ToHashSet();
    }
}
