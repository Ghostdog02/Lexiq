using Backend.Database.Entities;
using Backend.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LessonController : ControllerBase
    {
        private readonly LessonService _lessonService;

        public LessonController(LessonService lessonService)
        {
            _lessonService = lessonService;
        }

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

            // Check if this was the last lesson in the module
            var isLastInModule = await _lessonService.IsLastLessonInModuleAsync(lessonId);

            return Ok(new
            {
                message = "Lesson completed successfully",
                currentLessonId = lessonId,
                isLastInModule = isLastInModule,
                nextLesson = nextLesson != null ? new
                {
                    id = nextLesson.Id,
                    title = nextLesson.Title,
                    moduleId = nextLesson.ModuleId,
                    wasUnlocked = wasUnlocked,
                    isLocked = nextLesson.IsLocked
                } : null
            });
        }

        /// <summary>
        /// Gets the next lesson after the current one
        /// </summary>
        /// <param name="lessonId">The current lesson ID</param>
        [HttpGet("{lessonId}/next")]
        public async Task<IActionResult> GetNextLesson(int lessonId)
        {
            var nextLesson = await _lessonService.GetNextLessonAsync(lessonId);

            if (nextLesson == null)
            {
                return Ok(new { message = "This is the last lesson in the course" });
            }

            return Ok(nextLesson);
        }

        /// <summary>
        /// Gets all lessons for a specific module
        /// </summary>
        /// <param name="moduleId">The module ID</param>
        [HttpGet("module/{moduleId}")]
        public async Task<IActionResult> GetLessonsByModule(int moduleId)
        {
            var lessons = await _lessonService.GetLessonsByModuleAsync(moduleId);
            return Ok(lessons);
        }

        /// <summary>
        /// Gets a lesson with full details
        /// </summary>
        /// <param name="lessonId">The lesson ID</param>
        [HttpGet("{lessonId}")]
        public async Task<IActionResult> GetLesson(int lessonId)
        {
            var lesson = await _lessonService.GetLessonWithDetailsAsync(lessonId);

            if (lesson == null)
            {
                return NotFound(new { message = "Lesson not found" });
            }

            return Ok(lesson);
        }

        /// <summary>
        /// Manually unlock a specific lesson (for admin/testing purposes)
        /// </summary>
        /// <param name="lessonId">The lesson ID to unlock</param>
        [HttpPost("{lessonId}/unlock")]
        public async Task<IActionResult> UnlockLesson(int lessonId)
        {
            await _lessonService.UnlockLessonAsync(lessonId);
            return Ok(new { message = "Lesson unlocked successfully" });
        }
    }
}
