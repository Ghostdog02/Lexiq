using Backend.Api.Mapping;
using Backend.Database.Entities.Users;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;

namespace Backend.Api.Services;

public interface IGoogleAuthService
{
    Task<GoogleJsonWebSignature.Payload?> ValidateGoogleTokenAsync(string idToken);
    Task<User?> LoginUser(GoogleJsonWebSignature.Payload payload);
}

public class GoogleAuthService(
    UserManager<User> userManager,
    ILogger<GoogleAuthService> logger
) : IGoogleAuthService
{
    private readonly UserManager<User> _userManager = userManager;
    private readonly ILogger<GoogleAuthService> _logger = logger;

    public async Task<GoogleJsonWebSignature.Payload?> ValidateGoogleTokenAsync(string idToken)
    {
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience =
                [
                    Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID")
                        ?? throw new InvalidOperationException("GOOGLE_CLIENT_ID not found"),
                ],
                IssuedAtClockTolerance = TimeSpan.FromSeconds(1600),
            };

            return await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogWarning(ex, "Google token validation failed");
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
                    _logger.LogError(
                        "User creation failed: {Errors}",
                        string.Join(", ", result.Errors.Select(e => e.Description))
                    );
                    return null;
                }

                var roleResult = await _userManager.AddToRoleAsync(user, "Student");
                if (!roleResult.Succeeded)
                {
                    _logger.LogError(
                        "Role assignment failed: {Errors}",
                        string.Join(", ", roleResult.Errors.Select(e => e.Description))
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
