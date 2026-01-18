using Backend.Api.Dtos;
using Backend.Api.Mapping;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers;

[ApiController]
[Route("api/languages")]
public class LanguageController(LanguageService languageService) : ControllerBase
{
    private readonly LanguageService _languageService = languageService;

    [HttpGet]
    public async Task<ActionResult<List<LanguageDto>>> GetAllLanguages()
    {
        var languages = await _languageService.GetAllLanguagesAsync();
        return Ok(languages.Select(l => l.ToDto()));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<LanguageDto>> GetLanguage(int id)
    {
        var language = await _languageService.GetLanguageByIdAsync(id);
        if (language == null)
            return NotFound();

        return Ok(language.ToDto());
    }

    [HttpPost]
    [Authorize(Roles = "Admin")] // Assuming Admin role exists or will be used
    public async Task<ActionResult<LanguageDto>> CreateLanguage(CreateLanguageDto dto)
    {
        var language = await _languageService.CreateLanguageAsync(dto);
        return CreatedAtAction(nameof(GetLanguage), new { id = language.Id }, language.ToDto());
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<LanguageDto>> UpdateLanguage(int id, CreateLanguageDto dto)
    {
        var language = await _languageService.UpdateLanguageAsync(id, dto);
        if (language == null)
            return NotFound();

        return Ok(language.ToDto());
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteLanguage(int id)
    {
        var result = await _languageService.DeleteLanguageAsync(id);
        if (!result)
            return NotFound();

        return NoContent();
    }
}
