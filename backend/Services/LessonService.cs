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
        /// Gets the next lesson after the current one, even if it's in the next course
        /// </summary>
        /// <param name="currentLessonId">The ID of the current lesson</param>
        /// <returns>The next lesson, or null if this is the last lesson in the language</returns>
        public async Task<Lesson?> GetNextLessonAsync(int currentLessonId)
        {
            var currentLesson = await _context
                .Lessons
                    .Include(l => l.Course)
                        .ThenInclude(c => c.LanguageId)
                .FirstOrDefaultAsync(l => l.Id == currentLessonId);

            if (currentLesson == null)
                return null;

            // Try to find the next lesson in the same course
            var nextLessonInCourse = await _context
                .Lessons.Where(l =>
                    l.CourseId == currentLesson.CourseId && l.OrderIndex > currentLesson.OrderIndex
                )
                .OrderBy(l => l.OrderIndex)
                .FirstOrDefaultAsync();

            if (nextLessonInCourse != null)
                return nextLessonInCourse;

            // If no next lesson in current course, find the first lesson of the next course
            var nextCourse = await _context
                .Courses.Where(c =>
                    c.LanguageId == currentLesson.Course.LanguageId
                    && c.OrderIndex > currentLesson.Course.OrderIndex
                )
                .OrderBy(c => c.OrderIndex)
                .FirstOrDefaultAsync();

            if (nextCourse == null)
                return null; // No next course, this is the last lesson in the language

            // Get the first lesson of the next course
            var firstLessonInNextCourse = await _context
                .Lessons.Where(l => l.CourseId == nextCourse.Id)
                .OrderBy(l => l.OrderIndex)
                .FirstOrDefaultAsync();

            return firstLessonInNextCourse;
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
        /// Checks if a lesson is the last one in its course
        /// </summary>
        /// <param name="lessonId">The ID of the lesson to check</param>
        /// <returns>True if this is the last lesson in the course</returns>
        public async Task<bool> IsLastLessonInCourseAsync(int lessonId)
        {
            var lesson = await _context.Lessons.FindAsync(lessonId);
            if (lesson == null)
                return false;

            var hasNextLessonInCourse = await _context.Lessons.AnyAsync(l =>
                l.CourseId == lesson.CourseId && l.OrderIndex > lesson.OrderIndex
            );

            return !hasNextLessonInCourse;
        }

        /// <summary>
        /// Gets the first lesson of a specific course
        /// </summary>
        /// <param name="courseId">The ID of the course</param>
        /// <returns>The first lesson in the course</returns>
        public async Task<Lesson?> GetFirstLessonInCourseAsync(int courseId)
        {
            return await _context
                .Lessons.Where(l => l.CourseId == courseId)
                .OrderBy(l => l.OrderIndex)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Gets all lessons for a specific course, ordered by OrderIndex
        /// </summary>
        /// <param name="courseId">The ID of the course</param>
        /// <returns>List of lessons in the course</returns>
        public async Task<List<Lesson>> GetLessonsByCourseAsync(int courseId)
        {
            return await _context
                .Lessons.Where(l => l.CourseId == courseId)
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
        /// <returns>The lesson with its course and language included</returns>
        public async Task<Lesson> CreateLessonAsync(Backend.Api.Dtos.CreateLessonDto dto)
        {
            var course = await _context.Courses.FirstOrDefaultAsync(c => c.Title == dto.CourseName);
            if (course == null)
            {
                throw new ArgumentException($"Course '{dto.CourseName}' not found.");
            }

            var lesson = new Lesson
            {
                CourseId = course.Id,
                Title = dto.Title,
                Description = dto.Description,
                EstimatedDurationMinutes = dto.EstimatedDurationMinutes,
                OrderIndex = dto.OrderIndex,
                LessonMediaUrl = dto.LessonMediaUrl,
                LessonTextUrl = dto.LessonTextUrl,
                IsLocked = true, // Default to locked
                CreatedAt = DateTime.UtcNow
            };

            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();
            return lesson;
        }

        public async Task<Lesson?> UpdateLessonAsync(int id, Backend.Api.Dtos.UpdateLessonDto dto)
        {
            var lesson = await _context.Lessons.FindAsync(id);
            if (lesson == null)
                return null;

            if (dto.Title != null) lesson.Title = dto.Title;
            if (dto.Description != null) lesson.Description = dto.Description;
            if (dto.EstimatedDurationMinutes.HasValue) lesson.EstimatedDurationMinutes = dto.EstimatedDurationMinutes.Value;
            if (dto.OrderIndex.HasValue) lesson.OrderIndex = dto.OrderIndex.Value;
            if (dto.LessonMediaUrl != null) lesson.LessonMediaUrl = dto.LessonMediaUrl;
            if (dto.LessonTextUrl != null) lesson.LessonTextUrl = dto.LessonTextUrl;

            await _context.SaveChangesAsync();
            return lesson;
        }

        public async Task<bool> DeleteLessonAsync(int lessonId)
        {
            var lesson = await _context.Lessons.FindAsync(lessonId);
            if (lesson == null)
                return false;

            _context.Lessons.Remove(lesson);
            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Gets the lesson with full navigation properties
        /// </summary>
        /// <param name="lessonId">The ID of the lesson</param>
        /// <returns>The lesson with its course and language included</returns>
        public async Task<Lesson?> GetLessonWithDetailsAsync(int lessonId)
        {
            return await _context
                .Lessons
                    .Include(l => l.Course)
                        .ThenInclude(c => c.LanguageId)
                    .Include(l => l.Exercises)
                .FirstOrDefaultAsync(l => l.Id == lessonId);
        }
    }
}
