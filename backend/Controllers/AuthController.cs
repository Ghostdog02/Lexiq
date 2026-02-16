using Backend.Api.Dtos;
using Backend.Api.Mapping;
using Backend.Api.Middleware;
using Backend.Api.Services;
using Backend.Database.Entities.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    IGoogleAuthService googleAuthService,
    IJwtService jwtService,
    UserManager<User> userManager
) : ControllerBase
{
    private readonly IGoogleAuthService _googleAuthService = googleAuthService;
    private readonly IJwtService _jwtService = jwtService;
    private readonly UserManager<User> _userManager = userManager;

    [HttpPost("google-login")]
    public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequestDto request)
    {
        if (string.IsNullOrEmpty(request.IdToken))
            return BadRequest(new { message = "ID token is required" });

        var payload = await _googleAuthService.ValidateGoogleTokenAsync(request.IdToken);
        if (payload == null)
            return Unauthorized(new { message = "Invalid Google token" });

        var user = await _googleAuthService.LoginUser(payload);
        if (user == null)
            return BadRequest(new { message = "Failed to create user" });

        var roles = await _userManager.GetRolesAsync(user);
        var token = _jwtService.GenerateToken(user, roles);

        SetAuthCookie(token, DateTime.UtcNow.AddHours(_jwtService.ExpirationHours));

        return Ok(new { message = "Login successful", user = user.ToGoogleLoginDto() });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        SetAuthCookie("", DateTime.UtcNow.AddDays(-1));

        return Ok(new { message = "Logout successful" });
    }

    [HttpGet("auth-status")]
    public IActionResult GetAuthStatus()
    {
        var isAuthenticated = HttpContext.User.Identity?.IsAuthenticated ?? false;

        return Ok(new AuthStatusResponseDto(
            Message: isAuthenticated ? "Authenticated" : "Not authenticated",
            IsLogged: isAuthenticated
        ));
    }

    /// <summary>
    /// Check if the current user has admin or content creator privileges.
    /// </summary>
    [HttpGet("is-admin")]
    [Authorize]
    public async Task<IActionResult> IsAdmin()
    {
        var user = HttpContext.GetCurrentUser();
        if (user == null)
            return Unauthorized(new { message = "User not found" });

        var roles = await _userManager.GetRolesAsync(user);
        var canBypassLocks = roles.Contains("Admin") || roles.Contains("ContentCreator");

        return Ok(new IsAdminResponseDto(canBypassLocks, roles.ToList()));
    }

    private void SetAuthCookie(string token, DateTime expires)
    {
        Response.Cookies.Append(
            "AuthToken",
            token,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = HttpContext.Request.IsHttps,
                SameSite = SameSiteMode.Lax,
                Path = "/",
                Expires = expires,
            }
        );
    }
}
