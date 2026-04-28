using Backend.Api.Dtos;
using Backend.Database.Entities.Users;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Riok.Mapperly.Abstractions;

namespace Backend.Api.Mapping;

#pragma warning disable RMG020 // User inherits IdentityUser — many source properties intentionally unmapped
[Mapper]
public partial class UserMapping
{
    [MapProperty(nameof(User.UserName), nameof(UserDetailsDto.FullName))]
    public partial UserDetailsDto MapToDto(User user);

    public partial GoogleLoginDto MapToGoogleLoginDto(User user);
}
#pragma warning restore RMG020

public static class UserMappingExtensions
{
    // IQueryable projection — must stay as a LINQ expression for EF Core SQL translation
    public static IQueryable<UserDetailsDto> MapUsersToDtos(this IQueryable<User> users) =>
        users.Select(u => new UserDetailsDto(u.Email!, u.LastLoginDate, u.UserName!, u.RegistrationDate));

    // Requires UserManager + custom logic — cannot be expressed as a pure Mapperly mapping
    public static User MapGooglePayloadToUser(
        this UserManager<User> userManager,
        GoogleJsonWebSignature.Payload validPayload
    ) =>
        new()
        {
            UserName = CleanUsername(validPayload.Name),
            Email = validPayload.Email,
            EmailConfirmed = true,
            NormalizedEmail = userManager.NormalizeEmail(validPayload.Email),
            NormalizedUserName = userManager.NormalizeEmail(validPayload.Name),
            RegistrationDate = DateTime.UtcNow,
        };

    private static string CleanUsername(string username)
    {
        char[] charToRemove = ['-', ' ', '_', '*', '&'];
        return new string(username.Where(c => !charToRemove.Contains(c)).ToArray());
    }
}
