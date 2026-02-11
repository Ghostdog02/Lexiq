using Backend.Api.Dtos;
using Backend.Database.Entities.Users;
using Microsoft.AspNetCore.Identity;

namespace Backend.Api.Services;

public static class UserExtensions
{
    public static User UpdateUserCredentials(
        this User user,
        UpdateUserDto dto,
        UserManager<User> userManager
    )
    {
        ArgumentNullException.ThrowIfNull(user, $"user cannot be null");
        ArgumentNullException.ThrowIfNull(dto, $"dto cannot be null");

        user.UserName = dto.FullName;
        user.NormalizedUserName = userManager.NormalizeName(dto.FullName);
        user.PhoneNumber = dto.PhoneNumber;
        user.PhoneNumberConfirmed = !string.IsNullOrWhiteSpace(dto.PhoneNumber);

        return user;
    }

    public static void UpdateLastLoginDate(this User user)
    {
        user.LastLoginDate = DateTime.Now;
    }

    /// <summary>
    /// Check if user can bypass lesson/exercise locks (Admin or ContentCreator role).
    /// </summary>
    public static async Task<bool> CanBypassLocksAsync(
        this User user,
        UserManager<User> userManager
    )
    {
        var roles = await userManager.GetRolesAsync(user);
        return roles.Contains("Admin") || roles.Contains("ContentCreator");
    }
}
