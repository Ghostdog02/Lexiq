using System.Security.Claims;
using Backend.Api.Dtos;
using Backend.Api.Mapping;
using Backend.Api.Middleware;
using Backend.Api.Services;
using Backend.Database.Entities.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ExerciseController(
    ExerciseService exerciseService,
    ExerciseProgressService progressService,
    UserManager<User> userManager
) : ControllerBase
{
    private readonly ExerciseService _exerciseService = exerciseService;
    private readonly ExerciseProgressService _progressService = progressService;
    private readonly UserManager<User> _userManager = userManager;

    [HttpGet("lesson/{lessonId}")]
    public async Task<ActionResult<List<ExerciseDto>>> GetExercisesByLesson(string lessonId)
    {
        var exercises = await _exerciseService.GetExercisesByLessonIdAsync(lessonId);

        return Ok(exercises.Select(e => e.ToDto()));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ExerciseDto>> GetExercise(string id)
    {
        var exercise = await _exerciseService.GetExerciseByIdAsync(id);
        if (exercise == null)
            return NotFound();

        // Check if user can bypass locks
        var user = HttpContext.GetCurrentUser();
        var canBypassLocks = user != null && await user.CanBypassLocksAsync(_userManager);

        if (exercise.IsLocked == true && !canBypassLocks)
            return StatusCode(
                403,
                new { message = "Exercise is locked. Complete previous exercises to unlock." }
            );

        return Ok(exercise.ToDto());
    }

    [HttpPost]
    [Authorize(Roles = "Admin,ContentCreator")]
    public async Task<ActionResult<ExerciseDto>> CreateExercise(CreateExerciseDto dto)
    {
        var exercise = await _exerciseService.CreateExerciseAsync(dto);
        return CreatedAtAction(nameof(GetExercise), new { id = exercise.Id }, exercise.ToDto());
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,ContentCreator")]
    public async Task<ActionResult<ExerciseDto>> UpdateExercise(string id, UpdateExerciseDto dto)
    {
        var exercise = await _exerciseService.UpdateExerciseAsync(id, dto);
        if (exercise == null)
            return NotFound();

        return Ok(exercise.ToDto());
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,ContentCreator")]
    public async Task<IActionResult> DeleteExercise(string id)
    {
        var result = await _exerciseService.DeleteExerciseAsync(id);
        if (!result)
            return NotFound();

        return NoContent();
    }

    /// <summary>
    /// Submit an answer for an exercise and get backend validation result
    /// </summary>
    [HttpPost("{exerciseId}/submit")]
    public async Task<ActionResult<SubmitAnswerResponse>> SubmitAnswer(
        string exerciseId,
        SubmitAnswerRequest request
    )
    {
        if (string.IsNullOrWhiteSpace(request.Answer))
            return BadRequest(new { message = "Answer cannot be empty" });

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        try
        {
            SubmitAnswerResponse result = await _progressService.SubmitAnswerAsync(
                userId,
                exerciseId,
                request.Answer
            );

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(403, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get user's exercise progress for a specific lesson
    /// </summary>
    [HttpGet("lesson/{lessonId}/progress")]
    public async Task<ActionResult<List<UserExerciseProgressDto>>> GetLessonProgress(
        string lessonId
    )
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var fullProgress = await _progressService.GetFullLessonProgressAsync(userId, lessonId);
        return Ok(fullProgress.ExerciseProgress.Values.ToList());
    }

    /// <summary>
    /// Get all submission results for exercises in a lesson, ordered by exercise OrderIndex.
    /// Used by the frontend to restore previous progress state.
    /// </summary>
    [HttpGet("lesson/{lessonId}/submissions")]
    public async Task<ActionResult<List<SubmitAnswerResponse>>> GetLessonSubmissions(
        string lessonId
    )
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
            return Unauthorized();

        var submissions = await _progressService.GetLessonSubmissionsAsync(userId, lessonId);
        return Ok(submissions);
    }
}
