using System.Security.Claims;
using Backend.Api.Dtos;
using Backend.Api.Mapping;
using Backend.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UserLanguageController(UserLanguageService userLanguageService) : ControllerBase
{
    private readonly UserLanguageService _userLanguageService = userLanguageService;

    [HttpGet]
    public async Task<ActionResult<List<UserLanguageDto>>> GetMyLanguages()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var userLanguages = await _userLanguageService.GetUserLanguagesAsync(userId);
        return Ok(userLanguages.Select(ul => ul.ToDto()));
    }

    [HttpPost("enroll")]
    public async Task<ActionResult<UserLanguageDto>> Enroll([FromBody] EnrollLanguageDto dto)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var userLanguage = await _userLanguageService.EnrollUserAsync(userId, dto.LanguageId);
        if (userLanguage == null)
            return BadRequest(new { message = "Failed to enroll. Language might not exist." });

        return Ok(userLanguage.ToDto());
    }

    [HttpDelete("{languageId}")]
    public async Task<IActionResult> Unenroll(int languageId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var result = await _userLanguageService.UnenrollUserAsync(userId, languageId);
        if (!result)
            return NotFound();

        return NoContent();
    }
}
