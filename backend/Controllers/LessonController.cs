using Backend.Api.Dtos;
using Backend.Api.Mapping;
using Backend.Api.Middleware;
using Backend.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers;

[ApiController]
[Route("api/[controller]s")]
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
        var currentUser = HttpContext.GetCurrentUser()!;
        var result = await _progressService.CompleteLessonAsync(currentUser.Id, lessonId);
        return Ok(result);
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

        var currentUser = HttpContext.GetCurrentUser()!;
        var lessonIds = lessons.Select(l => l.Id).ToList();
        var progressMap = await _lessonService.GetProgressForLessonsAsync(
            currentUser.Id,
            lessonIds
        );

        var result = lessons.Select(l => l.ToDto(progressMap.GetValueOrDefault(l.Id))).ToList();

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

        var currentUser = HttpContext.GetCurrentUser()!;
        var progress = await _lessonService.GetFullLessonProgressAsync(currentUser.Id, lessonId);

        return OkPolymorphic<LessonDto>(lesson.ToDto(progress.Summary, progress.ExerciseProgress));
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
        var result = CreatedAtAction(
            nameof(GetLesson),
            new { lessonId = lesson.Id },
            lesson.ToDto()
        );
        result.DeclaredType = typeof(LessonDto);

        return result;
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

    /// <summary>
    /// Gets all exercises for a specific lesson
    /// </summary>
    /// <param name="lessonId">The lesson ID</param>
    [HttpGet("{lessonId}/exercises")]
    public async Task<ActionResult<List<ExerciseDto>>> GetExercisesByLesson(string lessonId)
    {
        var exercises = await _lessonService.GetExercisesByLessonIdAsync(lessonId);

        return OkPolymorphic<List<ExerciseDto>>(exercises.Select(e => e.ToDto()).ToList());
    }

    /// <summary>
    /// Gets user progress for all exercises in a lesson
    /// </summary>
    /// <param name="lessonId">The lesson ID</param>
    [HttpGet("{lessonId}/progress")]
    public async Task<ActionResult<List<UserExerciseProgressDto>>> GetLessonProgress(
        string lessonId
    )
    {
        var user = HttpContext.GetCurrentUser()!;
        var fullProgress = await _lessonService.GetFullLessonProgressAsync(user.Id, lessonId);
        return Ok(fullProgress.ExerciseProgress.Values.ToList());
    }

    /// <summary>
    /// Gets all exercise submissions for a lesson with correct answers for incorrect attempts
    /// </summary>
    /// <param name="lessonId">The lesson ID</param>
    [HttpGet("{lessonId}/submissions")]
    public async Task<ActionResult<List<SubmitAnswerResponse>>> GetLessonSubmissions(
        string lessonId
    )
    {
        var user = HttpContext.GetCurrentUser()!;
        var submissions = await _lessonService.GetLessonSubmissionsAsync(user.Id, lessonId);
        return Ok(submissions);
    }

    /// <summary>
    /// Returns 200 OK with DeclaredType set to T, ensuring System.Text.Json serializes
    /// through the base type and includes [JsonPolymorphic] type discriminators.
    /// </summary>
    private static OkObjectResult OkPolymorphic<T>(T value) =>
        new(value) { DeclaredType = typeof(T) };
}
