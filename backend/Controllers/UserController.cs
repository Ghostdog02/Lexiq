using Backend.Api.Dtos;
using Backend.Api.Middleware;
using Backend.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController(
    UserXpService userXpService,
    UserService userService,
    AvatarService avatarService
) : ControllerBase
{
    private readonly UserXpService _userXpService = userXpService;
    private readonly UserService _userService = userService;
    private readonly AvatarService _avatarService = avatarService;

    /// <summary>
    /// Get current authenticated user's total XP and stats
    /// </summary>
    [HttpGet("xp")]
    [Authorize]
    public async Task<ActionResult<UserXpDto>> GetCurrentUserXp()
    {
        var currentUser = HttpContext.GetCurrentUser();
        if (currentUser == null)
            return Unauthorized(new { message = "User is not authorized." });

        var xpData = await _userXpService.GetUserXpAsync(currentUser.Id);
        if (xpData == null)
            return NotFound(new { message = "User not found." });

        return Ok(xpData);
    }

    /// <summary>
    /// Get any user's total XP and stats by user ID (public for leaderboard)
    /// </summary>
    /// <param name="userId">The user's ID</param>
    [HttpGet("{userId}/xp")]
    [AllowAnonymous]
    public async Task<ActionResult<UserXpDto>> GetUserXp(string userId)
    {
        var xpData = await _userXpService.GetUserXpAsync(userId);
        if (xpData == null)
            return NotFound(new { message = "User not found." });

        return Ok(xpData);
    }

    /// <summary>
    /// Get a user's avatar image
    /// </summary>
    [HttpGet("{userId}/avatar")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAvatar(string userId)
    {
        var (data, contentType) = await _avatarService.GetAvatarAsync(userId);

        if (data == null)
            return NotFound();

        Response.Headers["Cache-Control"] = "public, max-age=86400";
        return File(data, contentType!);
    }

    /// <summary>
    /// Upload a new avatar image for the current user
    /// </summary>
    [HttpPut("avatar")]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateAvatar(IFormFile file)
    {
        var currentUser = HttpContext.GetCurrentUser();
        if (currentUser == null)
            return Unauthorized(new { message = "User is not authorized." });

        var success = await _userService.UploadAvatarAsync(currentUser.Id, file);

        if (!success)
            return BadRequest(new { message = "Avatar upload failed." });

        var avatarUrl = $"/api/user/{currentUser.Id}/avatar";
        return Ok(new { avatarUrl });
    }
}
