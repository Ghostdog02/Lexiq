using Lexiq.Api.Dtos;
using Lexiq.Database.Entities;
using Microsoft.AspNetCore.Identity;

namespace Lexiq.Api.Services
{
    public static class UserExtensions
    {
        public static User UpdateUserCredentials(
            this User user,
            UpdatedUserDto dto,
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
    }
}
