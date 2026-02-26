using Backend.Database;
using Microsoft.EntityFrameworkCore;

namespace Backend.Api.Services;

public class UserService(BackendDbContext context, IFileUploadsService fileUploadsService)
{
    private readonly BackendDbContext _context = context;
    private readonly IFileUploadsService _fileUploadsService = fileUploadsService;

    public async Task<string?> UploadAvatarAsync(string userId, IFormFile file, string baseUrl)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return null;

        var result = await _fileUploadsService.UploadFileAsync(file, "image", baseUrl);
        if (!result.IsSuccess)
            return null;

        user.Avatar = result.Url;
        await _context.SaveChangesAsync();
        return result.Url;
    }

    public async Task<string?> GetAvatarAsync(string userId)
    {
        return await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => u.Avatar)
            .FirstOrDefaultAsync();
    }
}
