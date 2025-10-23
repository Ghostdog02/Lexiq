using Microsoft.AspNetCore.Identity;
using DevGuard.Api.Dtos;
using DevGuard.Database.Entities;

namespace DevGuard.Api.Services
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
