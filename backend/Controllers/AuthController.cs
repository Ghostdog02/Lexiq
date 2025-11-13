using Backend.Database.Entities;
using Backend.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IGoogleAuthService _googleAuthService;
        private readonly SignInManager<User> _signInManager;

        public AuthController(
            IGoogleAuthService googleAuthService,
            SignInManager<User> signInManager
        )
        {
            _googleAuthService = googleAuthService;
            _signInManager = signInManager;
        }

        [HttpPost("google")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            if (string.IsNullOrEmpty(request.IdToken))
            {
                return BadRequest(new { message = "ID token is required" });
            }

            // Validate Google token
            var payload = await _googleAuthService.ValidateGoogleTokenAsync(request.IdToken);
            if (payload == null)
            {
                return Unauthorized(new { message = "Invalid Google token" });
            }

            // Get or create user
            var user = await _googleAuthService.GetOrCreateUserFromGoogleAsync(payload);
            if (user == null)
            {
                return BadRequest(new { message = "Failed to create user" });
            }

            // Sign in user and create cookie
            await _signInManager.SignInAsync(user, isPersistent: true);

            // Configure cookie options
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = true, // Use HTTPS only
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddDays(30),
            };

            Response.Cookies.Append("AuthToken", "authenticated", cookieOptions);

            return Ok(
                new
                {
                    message = "Login successful",
                    user = new
                    {
                        id = user.Id,
                        email = user.Email,
                        firstName = user.FirstName,
                        lastName = user.LastName,
                    },
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
        public string IdToken { get; set; } = string.Empty;
    }
}
