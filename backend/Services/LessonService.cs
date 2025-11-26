using Backend.Database;
using Backend.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services
{
    public class LessonService
    {
        private readonly BackendDbContext _context;

        public LessonService(BackendDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets the next lesson after the current one, even if it's in the next module
        /// </summary>
        /// <param name="currentLessonId">The ID of the current lesson</param>
        /// <returns>The next lesson, or null if this is the last lesson in the course</returns>
        public async Task<Lesson?> GetNextLessonAsync(int currentLessonId)
        {
            var currentLesson = await _context.Lessons
                .Include(l => l.Module)
                .ThenInclude(m => m.Course)
                .FirstOrDefaultAsync(l => l.Id == currentLessonId);

            if (currentLesson == null)
                return null;

            // Try to find the next lesson in the same module
            var nextLessonInModule = await _context.Lessons
                .Where(l => l.ModuleId == currentLesson.ModuleId
                         && l.OrderIndex > currentLesson.OrderIndex)
                .OrderBy(l => l.OrderIndex)
                .FirstOrDefaultAsync();

            if (nextLessonInModule != null)
                return nextLessonInModule;

            // If no next lesson in current module, find the first lesson of the next module
            var nextModule = await _context.Modules
                .Where(m => m.CourseId == currentLesson.Module.CourseId
                         && m.OrderIndex > currentLesson.Module.OrderIndex)
                .OrderBy(m => m.OrderIndex)
                .FirstOrDefaultAsync();

            if (nextModule == null)
                return null; // No next module, this is the last lesson in the course

            //TO DO: If last module in Course change to next course

            // Get the first lesson of the next module
            var firstLessonInNextModule = await _context.Lessons
                .Where(l => l.ModuleId == nextModule.Id)
                .OrderBy(l => l.OrderIndex)
                .FirstOrDefaultAsync();

            return firstLessonInNextModule;
        }

        /// <summary>
        /// Unlocks the next lesson after completing the current one
        /// </summary>
        /// <param name="currentLessonId">The ID of the lesson that was just completed</param>
        /// <returns>True if a next lesson was unlocked, false otherwise</returns>
        public async Task<bool> UnlockNextLessonAsync(int currentLessonId)
        {
            var nextLesson = await GetNextLessonAsync(currentLessonId);

            if (nextLesson == null)
                return false; // No next lesson to unlock

            if (!nextLesson.IsLocked)
                return false; // Already unlocked

            nextLesson.IsLocked = false;
            await _context.SaveChangesAsync();

            return true;
        }

        /// <summary>
        /// Checks if a lesson is the last one in its module
        /// </summary>
        /// <param name="lessonId">The ID of the lesson to check</param>
        /// <returns>True if this is the last lesson in the module</returns>
        public async Task<bool> IsLastLessonInModuleAsync(int lessonId)
        {
            var lesson = await _context.Lessons.FindAsync(lessonId);
            if (lesson == null)
                return false;

            var hasNextLessonInModule = await _context.Lessons
                .AnyAsync(l => l.ModuleId == lesson.ModuleId
                            && l.OrderIndex > lesson.OrderIndex);

            return !hasNextLessonInModule;
        }

        /// <summary>
        /// Gets the first lesson of a specific module
        /// </summary>
        /// <param name="moduleId">The ID of the module</param>
        /// <returns>The first lesson in the module</returns>
        public async Task<Lesson?> GetFirstLessonInModuleAsync(int moduleId)
        {
            return await _context.Lessons
                .Where(l => l.ModuleId == moduleId)
                .OrderBy(l => l.OrderIndex)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Gets all lessons for a specific module, ordered by OrderIndex
        /// </summary>
        /// <param name="moduleId">The ID of the module</param>
        /// <returns>List of lessons in the module</returns>
        public async Task<List<Lesson>> GetLessonsByModuleAsync(int moduleId)
        {
            return await _context.Lessons
                .Where(l => l.ModuleId == moduleId)
                .OrderBy(l => l.OrderIndex)
                .ToListAsync();
        }

        /// <summary>
        /// Unlocks a specific lesson
        /// </summary>
        /// <param name="lessonId">The ID of the lesson to unlock</param>
        public async Task UnlockLessonAsync(int lessonId)
        {
            var lesson = await _context.Lessons.FindAsync(lessonId);
            if (lesson != null && lesson.IsLocked)
            {
                lesson.IsLocked = false;
                await _context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Gets the lesson with full navigation properties
        /// </summary>
        /// <param name="lessonId">The ID of the lesson</param>
        /// <returns>The lesson with its module and course included</returns>
        public async Task<Lesson?> GetLessonWithDetailsAsync(int lessonId)
        {
            return await _context.Lessons
                .Include(l => l.Module)
                .ThenInclude(m => m.Course)
                .Include(l => l.Exercises)
                .FirstOrDefaultAsync(l => l.Id == lessonId);
        }
    }
}
