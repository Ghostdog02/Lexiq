using Backend.Api.Dtos;
using Backend.Api.Mapping;
using Backend.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LessonController(LessonService lessonService) : ControllerBase
{
    private readonly LessonService _lessonService = lessonService;

    /// <summary>
    /// Marks a lesson as completed and unlocks the next lesson
    /// </summary>
    /// <param name="lessonId">The ID of the lesson that was completed</param>
    /// <returns>Information about the next lesson if available</returns>
    [HttpPost("{lessonId}/complete")]
    public async Task<IActionResult> CompleteLesson(int lessonId)
    {
        // Validate that the lesson exists
        var currentLesson = await _lessonService.GetLessonWithDetailsAsync(lessonId);
        if (currentLesson == null)
        {
            return NotFound(new { message = "Lesson not found" });
        }

        // Unlock the next lesson
        var wasUnlocked = await _lessonService.UnlockNextLessonAsync(lessonId);

        // Get the next lesson details
        var nextLesson = await _lessonService.GetNextLessonAsync(lessonId);

        // Check if this was the last lesson in the course
        var isLastInCourse = await _lessonService.IsLastLessonInCourseAsync(lessonId);

        return Ok(
            new
            {
                message = "Lesson completed successfully",
                currentLessonId = lessonId,
                isLastInCourse = isLastInCourse,
                nextLesson = nextLesson != null
                    ? new
                    {
                        id = nextLesson.Id,
                        title = nextLesson.Title,
                        courseId = nextLesson.CourseId,
                        wasUnlocked = wasUnlocked,
                        isLocked = nextLesson.IsLocked,
                    }
                    : null,
            }
        );
    }

    /// <summary>
    /// Gets the next lesson after the current one
    /// </summary>
    /// <param name="lessonId">The current lesson ID</param>
    [HttpGet("{lessonId}/next")]
    public async Task<ActionResult<LessonDto>> GetNextLesson(int lessonId)
    {
        var nextLesson = await _lessonService.GetNextLessonAsync(lessonId);

        if (nextLesson == null)
        {
            return Ok(new { message = "This is the last lesson in the language" });
        }

        return Ok(nextLesson.ToDto());
    }

    /// <summary>
    /// Gets all lessons for a specific course
    /// </summary>
    /// <param name="courseId">The course ID</param>
    [HttpGet("course/{courseId}")]
    public async Task<ActionResult<List<LessonDto>>> GetLessonsByCourse(int courseId)
    {
        var lessons = await _lessonService.GetLessonsByCourseAsync(courseId);
        return Ok(lessons.Select(l => l.ToDto()));
    }

    /// <summary>
    /// Gets a lesson with full details
    /// </summary>
    /// <param name="lessonId">The lesson ID</param>
    [HttpGet("{lessonId}")]
    public async Task<ActionResult<LessonDto>> GetLesson(int lessonId)
    {
        var lesson = await _lessonService.GetLessonWithDetailsAsync(lessonId);

        if (lesson == null)
        {
            return NotFound(new { message = "Lesson not found" });
        }

        return Ok(lesson.ToDto());
    }

    /// <summary>
    /// Manually unlock a specific lesson (for admin/testing purposes)
    /// </summary>
    /// <param name="lessonId">The lesson ID to unlock</param>
    [HttpPost("{lessonId}/unlock")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UnlockLesson(int lessonId)
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
    public async Task<ActionResult<LessonDto>> UpdateLesson(int lessonId, UpdateLessonDto dto)
    {
        var lesson = await _lessonService.UpdateLessonAsync(lessonId, dto);
        if (lesson == null)
            return NotFound();

        return Ok(lesson.ToDto());
    }

    [HttpDelete("{lessonId}")]
    [Authorize(Roles = "Admin,ContentCreator")]
    public async Task<IActionResult> DeleteLesson(int lessonId)
    {
        var result = await _lessonService.DeleteLessonAsync(lessonId);
        if (!result)
            return NotFound();

        return NoContent();
    }
}