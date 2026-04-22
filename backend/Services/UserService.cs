using Backend.Database;
using Microsoft.EntityFrameworkCore;

namespace Backend.Api.Services;

public class UserService(BackendDbContext context, AvatarService avatarService)
{
    private readonly BackendDbContext _context = context;
    private readonly AvatarService _avatarService = avatarService;

    public async Task<bool> UploadAvatarAsync(string userId, IFormFile file)
    {
        var (isValid, error) = AvatarService.ValidateAvatarFile(file);
        if (!isValid)
            return false;

        var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
        if (!userExists)
            return false;

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);

        await _avatarService.UpsertAvatarAsync(
            userId,
            memoryStream.ToArray(),
            AvatarService.GetContentType(file)
        );

        return true;
    }
}
