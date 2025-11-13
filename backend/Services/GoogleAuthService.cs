using Backend.Database.Entities;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;

namespace Backend.Services
{
    public interface IGoogleAuthService
    {
        Task<GoogleJsonWebSignature.Payload?> ValidateGoogleTokenAsync(string idToken);
        Task<User?> GetOrCreateUserFromGoogleAsync(GoogleJsonWebSignature.Payload payload);
    }

    public class GoogleAuthService(UserManager<User> userManager, IConfiguration configuration)
        : IGoogleAuthService
    {
        private readonly UserManager<User> _userManager = userManager;
        private readonly IConfiguration _configuration = configuration;

        public async Task<GoogleJsonWebSignature.Payload?> ValidateGoogleTokenAsync(string idToken)
        {
            try
            {
                var settings = new GoogleJsonWebSignature.ValidationSettings
                {
                    Audience =
                    [
                        Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID")
                            ?? throw new Exception("GOOGLE_CLIENT_ID not found"),
                    ],
                    IssuedAtClockTolerance = TimeSpan.FromSeconds(1600), //1 hour
                };

                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
                return payload;
            }

            catch (InvalidJwtException ex)
            {
                Console.WriteLine($"Token validation failed: {ex.Message}");
                return null;
            }
        }

        public async Task<User?> GetOrCreateUserFromGoogleAsync(
            GoogleJsonWebSignature.Payload payload
        )
        {
            // Try to find user by Google ID
            var user = await _userManager.FindByLoginAsync("Google", payload.Subject);

            if (user == null)
            {
                // Try to find by email
                user = await _userManager.FindByEmailAsync(payload.Email);

                if (user == null)
                {
                    // Create new user
                    user = new User
                    {
                        UserName = payload.Email,
                        Email = payload.Email,
                        EmailConfirmed = payload.EmailVerified,
                        FirstName = payload.GivenName,
                        LastName = payload.FamilyName,
                        // Add other properties as needed
                    };

                    var result = await _userManager.CreateAsync(user);
                    if (!result.Succeeded)
                    {
                        return null;
                    }
                }

                // Add Google login
                var loginInfo = new UserLoginInfo("Google", payload.Subject, "Google");
                await _userManager.AddLoginAsync(user, loginInfo);
            }

            return user;
        }
    }
}
