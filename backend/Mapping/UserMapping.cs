using Backend.Api.Dtos;
using Backend.Database.Entities.Users;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;

namespace Backend.Api.Mapping;

public static class UserMapping
{
    public static UserDetailsDto MapUserToDto(this User user)
    {
        return new UserDetailsDto(
            user.Email!,
            user.LastLoginDate,
            user.UserName!,
            user.RegistrationDate
        );
    }

    public static IQueryable<UserDetailsDto> MapUsersToDtos(this IQueryable<User> users)
    {
        return users.Select(user => user.MapUserToDto());
    }

    public static User MapGooglePayloadToUser(
        this UserManager<User> userManager,
        GoogleJsonWebSignature.Payload validPayload
    )
    {
        return new User
        {
            UserName = CleanUsername(validPayload.Name),
            EmailConfirmed = true,
            NormalizedEmail = userManager.NormalizeEmail(validPayload.Email),
            NormalizedUserName = userManager.NormalizeEmail(validPayload.Name),
            RegistrationDate = DateTime.UtcNow,
        };
    }

    private static string CleanUsername(string username)
    {
        char[] charToRemove = ['-', ' ', '_', '*', '&'];
        return new string(username.Where(c => !charToRemove.Contains(c)).ToArray());
    }

    public static GoogleLoginDto ToGoogleLoginDto(this User user)
    {
        return new GoogleLoginDto(user.Email!, user.UserName!);
    }
}
