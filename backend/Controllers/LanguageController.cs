using Backend.Api.Dtos;
using Backend.Api.Mapping;
using Backend.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LanguageController(LanguageService languageService, ContentMapping mapper) : ControllerBase
{
    private readonly LanguageService _languageService = languageService;
    private readonly ContentMapping _mapper = mapper;

    [HttpGet]
    public async Task<ActionResult<List<LanguageDto>>> GetAllLanguages()
    {
        var languages = await _languageService.GetAllLanguagesAsync();
        return Ok(languages.Select(l => _mapper.MapToDto(l)));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<LanguageDto>> GetLanguage(string id)
    {
        var language = await _languageService.GetLanguageByIdAsync(id);
        if (language == null)
            return NotFound();

        return Ok(_mapper.MapToDto(language));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<LanguageDto>> CreateLanguage(CreateLanguageDto dto)
    {
        var language = await _languageService.CreateLanguageAsync(dto);
        return CreatedAtAction(nameof(GetLanguage), new { id = language.LanguageId }, _mapper.MapToDto(language));
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<LanguageDto>> UpdateLanguage(string id, CreateLanguageDto dto)
    {
        var language = await _languageService.UpdateLanguageAsync(id, dto);
        if (language == null)
            return NotFound();

        return Ok(_mapper.MapToDto(language));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteLanguage(string id)
    {
        var result = await _languageService.DeleteLanguageAsync(id);
        if (!result)
            return NotFound();

        return NoContent();
    }
}
