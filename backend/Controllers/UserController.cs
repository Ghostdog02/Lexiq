using Backend.Api.Dtos;
using Backend.Api.Middleware;
using Backend.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController(UserXpService userXpService) : ControllerBase
{
    private readonly UserXpService _userXpService = userXpService;

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
}
