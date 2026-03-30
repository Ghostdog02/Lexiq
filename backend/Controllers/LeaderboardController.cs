using Backend.Api.Dtos;
using Backend.Api.Middleware;
using Backend.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LeaderboardController(LeaderboardService leaderboardService) : ControllerBase
{
    private readonly LeaderboardService _leaderboardService = leaderboardService;

    /// <summary>
    /// Get leaderboard rankings filtered by time frame.
    /// Returns top 50 users + current user's entry if authenticated.
    /// </summary>
    /// <param name="timeFrame">One of: Weekly, Monthly, AllTime (default: AllTime)</param>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<LeaderboardResponse>> GetLeaderboard(
        [FromQuery] TimeFrame timeFrame = TimeFrame.AllTime
    )
    {
        var currentUser = HttpContext.GetCurrentUser();
        var response = await _leaderboardService.GetLeaderboardAsync(timeFrame, currentUser?.Id);

        return Ok(response);
    }
}
