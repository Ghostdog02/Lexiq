using Backend.Database.Entities;
using Backend.Mapping;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;

namespace Backend.Services;

public interface IGoogleAuthService
{
    Task<GoogleJsonWebSignature.Payload?> ValidateGoogleTokenAsync(string idToken);
    Task<User?> LoginUser(GoogleJsonWebSignature.Payload payload);
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

    public async Task<User?> LoginUser(GoogleJsonWebSignature.Payload payload)
    {
        var user = await _userManager.FindByLoginAsync("Google", payload.Subject);

        if (user == null)
        {
            user = await _userManager.FindByEmailAsync(payload.Email);

            if (user == null)
            {
                user = _userManager.MapGooglePayloadToUser(payload);

                var result = await _userManager.CreateAsync(user);
                if (!result.Succeeded)
                {
                    Console.WriteLine(
                        $"User creation failed: {string.Join(", ", result.Errors.Select(e => e.Description))}"
                    );
                    
                    return null;
                }
            }

            var loginInfo = new UserLoginInfo("Google", payload.Subject, "Google");
            await _userManager.AddLoginAsync(user, loginInfo);
        }

        return user;
    }
}
