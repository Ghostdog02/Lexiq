using System.Text;
using Backend.Api.Dtos;
using Backend.Database.Entities.Users;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;

namespace Backend.Api.Mapping;

public static class UserMapping
{
    public static UserDetailsDto MapUserToDto(this User user)
    {
        UserDetailsDto dto = new(
            user.Email!,
            user.LastLoginDate,
            user.UserName!,
            user.RegistrationDate
        );

        return dto;
    }

    public static IQueryable<UserDetailsDto> MapUsersToDtos(this IQueryable<User> users)
    {
        return users.Select(user => user.MapUserToDto());
    }

    public static User MapGooglePayloadToUser(
        this UserManager<User> _userManager,
        GoogleJsonWebSignature.Payload validPayload
    )
    {
        User newUser = new()
        {
            UserName = CleanUsername(validPayload.Name),
            EmailConfirmed = true,
            NormalizedEmail = _userManager.NormalizeEmail(validPayload.Email),
            NormalizedUserName = _userManager.NormalizeEmail(validPayload.Name),
            RegistrationDate = DateTime.UtcNow,
        };

        return newUser;
    }

    private static string CleanUsername(string username)
    {
        char[] charToRemove = ['-', ' ', '_', '*', '&'];
        return new string(username.Where(c => !charToRemove.Contains(c)).ToArray());
    }

    public static User ToEntity(this CreateUserDto dto)
    {
        string cleanFullName = dto.FullName.RemoveInvalidCharacters();

        return new User
        {
            Email = dto.Email,
            UserName = cleanFullName,
            SecurityStamp = dto.SecurityStamp,
            ConcurrencyStamp = dto.ConcurrencyStamp,
            PhoneNumber = dto.PhoneNumber,
            PhoneNumberConfirmed = dto.PhoneNumber != null || dto.PhoneNumber != string.Empty,
            RegistrationDate = dto.RegistrationDate,
            NormalizedEmail = dto.Email.Normalize(),
            NormalizedUserName = cleanFullName.Normalize(),
        };
    }

    private static string RemoveInvalidCharacters(this string name)
    {
        StringBuilder builder = new();

        foreach (var character in name)
        {
            if (char.IsDigit(character) || char.IsLetter(character))
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    public static GoogleLoginDto ToGoogleLoginDto(this User user)
    {
        return new GoogleLoginDto(user.Email!, user.UserName!);
    }

    public static User ToEntity(this UpdateUserDto dto, string id)
    {
        return new User
        {
            Id = id,
            UserName = dto.FullName,
            PhoneNumber = dto.PhoneNumber,
            PhoneNumberConfirmed = dto.PhoneNumber != null || dto.PhoneNumber != string.Empty,
        };
    }
}
