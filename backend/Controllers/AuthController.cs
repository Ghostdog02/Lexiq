using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Backend.Api.Dtos;
using Backend.Api.Mapping;
using Backend.Api.Services;
using Backend.Database.Entities.Users;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace Backend.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(
    IGoogleAuthService googleAuthService,
    SignInManager<User> signInManager,
    ILogger<AuthController> logger
) : ControllerBase
{
    private readonly IGoogleAuthService _googleAuthService = googleAuthService;
    private readonly SignInManager<User> _signInManager = signInManager;
    private readonly ILogger<AuthController> _logger = logger;

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
                issuedAt = payload.IssuedAtTimeSeconds,
            }
        );
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        try
        {
            ClearAuthCookies();

            return Ok(new { message = "Logout successful" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, new { message = "An error occurred during logout" });
        }
    }

    [HttpGet("auth-status")]
    public async Task<IActionResult> GetAuthStatus()
    {
        try
        {
            string cookie = Request.Cookies["AuthToken"] ?? "";

            bool isValid = await ValidateJwtToken(cookie);

            return Ok(
                new AuthStatusResponse { Message = "Successful auth status check", IsLogged = true }
            );
        }
        
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking auth status");
            return Ok(
                new AuthStatusResponse { Message = "Error checking status", IsLogged = false }
            );
        }
    }

    private void ClearAuthCookies()
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddDays(-1),
        };

        Response.Cookies.Append("AuthToken", "", cookieOptions);
    }

    public async Task<bool> ValidateJwtToken(string token)
    {
        try
        {
            if (!string.IsNullOrEmpty(token))
            {
                GoogleJsonWebSignature.Payload isValid = await GoogleJsonWebSignature.ValidateAsync(
                    token
                );

                return true;
            }
            else
            {
                throw new ArgumentException($"Passed token is null or empty");
            }
        }
        catch (InvalidJwtException message)
        {
            throw new InvalidJwtException($"The passed jwt token is invalid. {message}");
        }
        catch (Exception message)
        {
            throw new InvalidJwtException(
                $"Error occurred during jwt token validation with google. {message}"
            );
        }
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

public class AuthStatusResponse
{
    public string Message { get; set; } = string.Empty;
    public bool IsLogged { get; set; }
}
