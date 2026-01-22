using System.Security.Claims;
using Backend.Api.DTOs.Progress;
using Backend.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProgressController : ControllerBase
{
    private readonly IProgressService _progressService;
    private readonly ILogger<ProgressController> _logger;

    public ProgressController(IProgressService progressService, ILogger<ProgressController> logger)
    {
        _progressService = progressService;
        _logger = logger;
    }

    private string GetUserId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedAccessException("User ID not found in claims");

    /// <summary>
    /// Submit an exercise attempt and update progress
    /// </summary>
    /// <param name="exerciseId">The exercise ID</param>
    /// <param name="request">Attempt details</param>
    /// <returns>Updated exercise progress</returns>
    [HttpPost("exercises/{exerciseId:int}/attempts")]
    [ProducesResponseType(typeof(ExerciseProgressResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExerciseProgressResponse>> SubmitExerciseAttempt(
        int exerciseId,
        [FromBody] SubmitExerciseAttemptRequest request
    )
    {
        try
        {
            var userId = GetUserId();

            var success = await _progressService.RecordExerciseAttemptAsync(
                userId,
                exerciseId,
                request.Score,
                request.IsCompleted
            );

            if (!success)
            {
                return NotFound(new { message = "Exercise not found" });
            }

            var progress = await _progressService.GetExerciseProgressAsync(userId, exerciseId);

            return Ok(progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error submitting exercise attempt for exercise {ExerciseId}",
                exerciseId
            );
            return StatusCode(
                500,
                new { message = "An error occurred while processing your request" }
            );
        }
    }

    /// <summary>
    /// Get progress for a specific exercise
    /// </summary>
    [HttpGet("exercises/{exerciseId:int}")]
    [ProducesResponseType(typeof(ExerciseProgressResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExerciseProgressResponse>> GetExerciseProgress(int exerciseId)
    {
        try
        {
            var userId = GetUserId();
            var progress = await _progressService.GetExerciseProgressAsync(userId, exerciseId);

            if (progress == null)
            {
                return NotFound(new { message = "Progress not found" });
            }

            return Ok(progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving exercise progress for exercise {ExerciseId}",
                exerciseId
            );
            return StatusCode(
                500,
                new { message = "An error occurred while processing your request" }
            );
        }
    }

    /// <summary>
    /// Get progress for a specific lesson
    /// </summary>
    [HttpGet("lessons/{lessonId:int}")]
    [ProducesResponseType(typeof(LessonProgressResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LessonProgressResponse>> GetLessonProgress(int lessonId)
    {
        try
        {
            var userId = GetUserId();
            var progress = await _progressService.GetLessonProgressAsync(userId, lessonId);

            if (progress == null)
            {
                return NotFound(new { message = "Lesson not found" });
            }

            return Ok(progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving lesson progress for lesson {LessonId}",
                lessonId
            );
            return StatusCode(
                500,
                new { message = "An error occurred while processing your request" }
            );
        }
    }

    /// <summary>
    /// Get progress for a specific course
    /// </summary>
    [HttpGet("courses/{courseId:int}")]
    [ProducesResponseType(typeof(CourseProgressResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CourseProgressResponse>> GetCourseProgress(int courseId)
    {
        try
        {
            var userId = GetUserId();
            var progress = await _progressService.GetCourseProgressAsync(userId, courseId);

            if (progress == null)
            {
                return NotFound(new { message = "Course not found" });
            }

            return Ok(progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving course progress for course {CourseId}",
                courseId
            );
            return StatusCode(
                500,
                new { message = "An error occurred while processing your request" }
            );
        }
    }

    /// <summary>
    /// Get overall progress summary for the current user
    /// </summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(UserProgressSummaryResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<UserProgressSummaryResponse>> GetProgressSummary()
    {
        try
        {
            var userId = GetUserId();
            var summary = await _progressService.GetUserProgressSummaryAsync(userId);

            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving progress summary");
            return StatusCode(
                500,
                new { message = "An error occurred while processing your request" }
            );
        }
    }

    /// <summary>
    /// Get progress for all courses in a specific language
    /// </summary>
    [HttpGet("languages/{languageId:int}/courses")]
    [ProducesResponseType(typeof(List<CourseProgressResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<CourseProgressResponse>>> GetLanguageProgress(
        int languageId
    )
    {
        try
        {
            var userId = GetUserId();
            var courses = await _progressService.GetLanguageProgressAsync(userId, languageId);

            return Ok(courses);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving language progress for language {LanguageId}",
                languageId
            );
            return StatusCode(
                500,
                new { message = "An error occurred while processing your request" }
            );
        }
    }

    /// <summary>
    /// Reset progress for a specific exercise (for practice/review)
    /// </summary>
    [HttpDelete("exercises/{exerciseId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ResetExerciseProgress(int exerciseId)
    {
        try
        {
            var userId = GetUserId();
            var success = await _progressService.ResetExerciseProgressAsync(userId, exerciseId);

            if (!success)
            {
                return NotFound(new { message = "Progress not found" });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error resetting exercise progress for exercise {ExerciseId}",
                exerciseId
            );
            return StatusCode(
                500,
                new { message = "An error occurred while processing your request" }
            );
        }
    }
}
