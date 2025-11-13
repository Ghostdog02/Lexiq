using Backend.Database.Entities;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;

namespace Backend.Services
{
    public class UserService
    {
        public static async Task<IdentityResult> CreateUserFromGooglePayloadAsync(
            GoogleJsonWebSignature.Payload validPayload,
            UserManager<User> _userManager
        )
        {
            User newUser = new()
            {
                UserName = validPayload.Name,
                EmailConfirmed = true,
                NormalizedEmail = _userManager.NormalizeEmail(validPayload.Email),
                NormalizedUserName = _userManager.NormalizeEmail(validPayload.Name),
                LastLoginDate = DateTime.UtcNow,
                RegistrationDate = DateTime.UtcNow,
            };

            return await _userManager.CreateAsync(newUser);
        }
    }
}