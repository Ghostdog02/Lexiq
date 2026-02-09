using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Backend.Api.Dtos;
using Backend.Api.Mapping;
using Backend.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LessonController(LessonService lessonService, ExerciseProgressService progressService)
    : ControllerBase
{
    private readonly LessonService _lessonService = lessonService;
    private readonly ExerciseProgressService _progressService = progressService;

    /// <summary>
    /// Attempts to complete a lesson. Requires meeting the XP threshold (70%).
    /// If met, unlocks the next lesson automatically.
    /// </summary>
    /// <param name="lessonId">The ID of the lesson to complete</param>
    [HttpPost("{lessonId}/complete")]
    public async Task<ActionResult<CompleteLessonResponse>> CompleteLesson(string lessonId)
    {
        var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (userId == null)
            return Unauthorized();

        try
        {
            var result = await _progressService.CompleteLessonAsync(userId, lessonId);
            return Ok(result);
        }
        
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Gets the next lesson after the current one
    /// </summary>
    /// <param name="lessonId">The current lesson ID</param>
    [HttpGet("{lessonId}/next")]
    public async Task<ActionResult<LessonDto>> GetNextLesson(string lessonId)
    {
        var nextLesson = await _lessonService.GetNextLessonAsync(lessonId);

        if (nextLesson == null)
        {
            return Ok(new { message = "This is the last lesson in the language" });
        }

        return Ok(nextLesson.ToDto());
    }

    /// <summary>
    /// Gets all lessons for a specific course, including user progress
    /// </summary>
    /// <param name="courseId">The course ID</param>
    [HttpGet("course/{courseId}")]
    public async Task<ActionResult<List<LessonDto>>> GetLessonsByCourse(string courseId)
    {
        var lessons = await _lessonService.GetLessonsByCourseAsync(courseId);
        if (lessons == null)
            return NotFound(new { message = $"Course '{courseId}' not found" });

        var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (userId == null)
            return Ok(lessons.Select(l => l.ToDto()));

        var result = new List<LessonDto>();
        foreach (var lesson in lessons)
        {
            var progress = await _progressService.GetLessonProgressAsync(userId, lesson.Id);
            result.Add(lesson.ToDto(progress));
        }
        return Ok(result);
    }

    /// <summary>
    /// Gets a lesson with full details, including user progress
    /// </summary>
    /// <param name="lessonId">The lesson ID</param>
    [HttpGet("{lessonId}")]
    public async Task<ActionResult<LessonDto>> GetLesson(string lessonId)
    {
        var lesson = await _lessonService.GetLessonWithDetailsAsync(lessonId);

        if (lesson == null)
        {
            return NotFound(new { message = "Lesson not found" });
        }

        var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        if (userId == null)
            return Ok(lesson.ToDto());

        var progress = await _progressService.GetLessonProgressAsync(userId, lessonId);
        return Ok(lesson.ToDto(progress));
    }

    /// <summary>
    /// Manually unlock a specific lesson (for admin/testing purposes)
    /// </summary>
    /// <param name="lessonId">The lesson ID to unlock</param>
    [HttpPost("{lessonId}/unlock")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UnlockLesson(string lessonId)
    {
        await _lessonService.UnlockLessonAsync(lessonId);
        return Ok(new { message = "Lesson unlocked successfully" });
    }

    [HttpPost]
    [Authorize(Roles = "Admin,ContentCreator")]
    public async Task<ActionResult<LessonDto>> CreateLesson(CreateLessonDto dto)
    {
        var lesson = await _lessonService.CreateLessonAsync(dto);
        return CreatedAtAction(nameof(GetLesson), new { lessonId = lesson.Id }, lesson.ToDto());
    }

    [HttpPut("{lessonId}")]
    [Authorize(Roles = "Admin,ContentCreator")]
    public async Task<ActionResult<LessonDto>> UpdateLesson(string lessonId, UpdateLessonDto dto)
    {
        var lesson = await _lessonService.UpdateLessonAsync(lessonId, dto);
        if (lesson == null)
            return NotFound();

        return Ok(lesson.ToDto());
    }

    [HttpDelete("{lessonId}")]
    [Authorize(Roles = "Admin,ContentCreator")]
    public async Task<IActionResult> DeleteLesson(string lessonId)
    {
        var result = await _lessonService.DeleteLessonAsync(lessonId);
        if (!result)
            return NotFound();

        return NoContent();
    }
}
