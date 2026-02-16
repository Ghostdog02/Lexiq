using Backend.Api.Dtos;
using Backend.Database;
using Backend.Database.Entities;
using Backend.Database.Entities.Exercises;
using Microsoft.EntityFrameworkCore;

namespace Backend.Api.Services;

public class LessonService(BackendDbContext context, ExerciseService exerciseService)
{
    private readonly BackendDbContext _context = context;
    private readonly ExerciseService _exerciseService = exerciseService;

    public async Task<Lesson?> GetNextLessonAsync(string currentLessonId)
    {
        var currentLesson = await _context
            .Lessons.Include(l => l.Course)
            .ThenInclude(c => c.Language)
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

    public async Task<bool> UnlockNextLessonAsync(string currentLessonId)
    {
        var nextLesson = await GetNextLessonAsync(currentLessonId);

        if (nextLesson == null)
            return false; // No next lesson to unlock

        if (!nextLesson.IsLocked)
            return false; // Already unlocked

        nextLesson.IsLocked = false;
        await _context.SaveChangesAsync();

        // Unlock the first exercise in this lesson
        await _exerciseService.UnlockFirstExerciseInLessonAsync(nextLesson.Id);

        return true;
    }

    public async Task<bool> IsLastLessonInCourseAsync(string lessonId)
    {
        var lesson = await _context.Lessons.FindAsync(lessonId);
        if (lesson == null)
            return false;

        var hasNextLessonInCourse = await _context.Lessons.AnyAsync(l =>
            l.CourseId == lesson.CourseId && l.OrderIndex > lesson.OrderIndex
        );

        return !hasNextLessonInCourse;
    }

    public async Task<Lesson?> GetFirstLessonInCourseAsync(string courseId)
    {
        return await _context
            .Lessons.Where(l => l.CourseId == courseId)
            .OrderBy(l => l.OrderIndex)
            .FirstOrDefaultAsync();
    }

    public async Task<List<Lesson>?> GetLessonsByCourseAsync(string courseId)
    {
        var courseExists = await _context.Courses.AnyAsync(c => c.Id == courseId);
        if (!courseExists)
            return null;

        return await _context
            .Lessons.Where(l => l.CourseId == courseId)
            .OrderBy(l => l.OrderIndex)
            .ToListAsync();
    }

    public async Task UnlockLessonAsync(string lessonId)
    {
        var lesson = await _context.Lessons.FindAsync(lessonId);
        if (lesson != null && lesson.IsLocked)
        {
            lesson.IsLocked = false;
            await _context.SaveChangesAsync();

            // Also unlock the first exercise
            await _exerciseService.UnlockFirstExerciseInLessonAsync(lessonId);
        }
    }

    public async Task<Lesson> CreateLessonAsync(CreateLessonDto dto)
    {
        var course =
            await _context.Courses.FindAsync(dto.CourseId)
            ?? throw new ArgumentException($"Course with ID '{dto.CourseId}' not found.");

        // Auto-calculate OrderIndex if not provided
        int orderIndex = dto.OrderIndex ?? await GetNextOrderIndexForCourseAsync(dto.CourseId);

        var lesson = new Lesson
        {
            CourseId = course.Id,
            Title = dto.Title,
            Description = dto.Description,
            EstimatedDurationMinutes = dto.EstimatedDurationMinutes,
            OrderIndex = orderIndex,
            LessonContent = dto.Content,
            IsLocked = true,
            CreatedAt = DateTime.UtcNow,
        };

        _context.Lessons.Add(lesson);

        if (dto.Exercises is { Count: > 0 })
        {
            for (var i = 0; i < dto.Exercises.Count; i++)
            {
                var exerciseDto = dto.Exercises[i];
                var exercise = _exerciseService.MapToEntity(exerciseDto, lesson.Id, exerciseDto.OrderIndex ?? i);
                exercise.IsLocked = i != 0; // First exercise unlocked, rest locked
                _context.Exercises.Add(exercise);
            }
        }

        await _context.SaveChangesAsync();
        return lesson;
    }

    public async Task<Lesson?> UpdateLessonAsync(string id, UpdateLessonDto dto)
    {
        var lesson = await _context.Lessons.FindAsync(id);
        if (lesson == null)
            return null;

        if (dto.CourseId != null)
        {
            var course = await _context.Courses.FindAsync(dto.CourseId);
            if (course == null)
                throw new ArgumentException($"Course with ID '{dto.CourseId}' not found.");
            lesson.CourseId = dto.CourseId;
        }
        if (dto.Title != null)
            lesson.Title = dto.Title;
        if (dto.Description != null)
            lesson.Description = dto.Description;
        if (dto.EstimatedDurationMinutes.HasValue)
            lesson.EstimatedDurationMinutes = dto.EstimatedDurationMinutes.Value;
        if (dto.OrderIndex.HasValue)
            lesson.OrderIndex = dto.OrderIndex.Value;
        if (dto.LessonContent != null)
            lesson.LessonContent = dto.LessonContent; // Update Editor.js content

        await _context.SaveChangesAsync();
        return lesson;
    }

    public async Task<bool> DeleteLessonAsync(string lessonId)
    {
        var lesson = await _context.Lessons.FindAsync(lessonId);
        if (lesson == null)
            return false;

        _context.Lessons.Remove(lesson);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<Lesson?> GetLessonWithDetailsAsync(string lessonId)
    {
        return await _context
            .Lessons.Include(l => l.Course)
            .ThenInclude(c => c.Language)
            .Include(l => l.Exercises)
            .ThenInclude(e => (e as MultipleChoiceExercise)!.Options)
            .FirstOrDefaultAsync(l => l.Id == lessonId);
    }

    private async Task<int> GetNextOrderIndexForCourseAsync(string courseId)
    {
        var maxOrderIndex = await _context
            .Lessons.Where(l => l.CourseId == courseId)
            .MaxAsync(l => (int?)l.OrderIndex);

        return (maxOrderIndex ?? -1) + 1;
    }
}
