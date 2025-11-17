using Backend.Database.Entities;
using Backend.Dtos;
using Backend.Mapping;
using Backend.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IGoogleAuthService googleAuthService, SignInManager<User> signInManager)
    : ControllerBase
{
    private readonly IGoogleAuthService _googleAuthService = googleAuthService;
    private readonly SignInManager<User> _signInManager = signInManager;

    [HttpPost("google-login")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
    {
        if (string.IsNullOrEmpty(request.IdToken))
        {
            return BadRequest(new { message = "ID token is required" });
        }

        var payload = await _googleAuthService.ValidateGoogleTokenAsync(request.IdToken);
        if (payload == null)
        {
            return Unauthorized(new { message = "Invalid Google token" });
        }

        var user = await _googleAuthService.LoginUser(payload);
        if (user == null)
        {
            return BadRequest(new { message = "Failed to create user" });
        }

        await _signInManager.SignInAsync(user, isPersistent: true);

        return Ok(
            new
            {
                message = "Login successful",
                user = user.ToGoogleLoginDto(),
                payload.ExpirationTimeSeconds,
            }
        );
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        Response.Cookies.Delete("AuthToken");
        return Ok(new { message = "Logout successful" });
    }
}

public class GoogleLoginRequest
{
    /// <summary>
    /// JWT token from Google (with or without 'Bearer ' prefix)
    /// </summary>
    /// <example>Bearer eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJodHRwczovL2FjY291bnRzLmdvb2dsZS5jb20iLCJhenAiOiI...</example>
    public string IdToken { get; set; } = string.Empty;
}
