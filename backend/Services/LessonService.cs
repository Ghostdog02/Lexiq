using System.Linq.Expressions;
using Backend.Api.Dtos;
using Backend.Database;
using Backend.Database.Entities;
using Backend.Database.Entities.Exercises;
using Backend.Database.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace Backend.Api.Services;

public class LessonService(BackendDbContext context, ExerciseService exerciseService)
{
    private readonly BackendDbContext _context = context;
    private readonly ExerciseService _exerciseService = exerciseService;

    /// <summary>
    /// Lightweight projection used internally for cross-course navigation queries.
    /// Avoids loading full Course and Language entity graphs when only scalar FK values are needed.
    /// </summary>
    private record LessonCourseContext(
        string CourseId,
        int LessonOrderIndex,
        string LanguageId,
        int CourseOrderIndex
    );

    public async Task<Lesson?> GetNextLessonAsync(string currentLessonId)
    {
        var ctx = await _context
            .Lessons.Where(l => l.Id == currentLessonId)
            .Select(l => new LessonCourseContext(
                l.CourseId,
                l.OrderIndex,
                l.Course.LanguageId,
                l.Course.OrderIndex
            ))
            .FirstOrDefaultAsync();

        if (ctx == null)
            return null;

        var nextLessonInCourse = await _context
            .Lessons.Where(l => l.CourseId == ctx.CourseId && l.OrderIndex > ctx.LessonOrderIndex)
            .OrderBy(l => l.OrderIndex)
            .FirstOrDefaultAsync();

        if (nextLessonInCourse != null)
            return nextLessonInCourse;

        // Project only the ID — no need to materialize the full Course entity
        var nextCourseId = await _context
            .Courses.Where(c =>
                c.LanguageId == ctx.LanguageId && c.OrderIndex > ctx.CourseOrderIndex
            )
            .OrderBy(c => c.OrderIndex)
            .Select(c => c.Id)
            .FirstOrDefaultAsync();

        if (nextCourseId == null)
            return null;

        return await _context
            .Lessons.Where(l => l.CourseId == nextCourseId)
            .OrderBy(l => l.OrderIndex)
            .FirstOrDefaultAsync();
    }

    public async Task<UnlockStatus> UnlockNextLessonAsync(string currentLessonId)
    {
        var nextLesson = await GetNextLessonAsync(currentLessonId);

        if (nextLesson == null)
            return UnlockStatus.NoNextLesson;

        if (!nextLesson.IsLocked)
            return UnlockStatus.AlreadyUnlocked;

        nextLesson.IsLocked = false;
        await _context.SaveChangesAsync();

        await _exerciseService.UnlockFirstExerciseInLessonAsync(nextLesson.Id);

        return UnlockStatus.Unlocked;
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
        if (lesson == null)
            return;

        if (lesson.IsLocked)
        {
            lesson.IsLocked = false;
            await _context.SaveChangesAsync();
        }

        // Always ensure the first exercise is unlocked — idempotent, safe to call
        // even when the lesson was already unlocked (repairs corrupt lock states).
        await _exerciseService.UnlockFirstExerciseInLessonAsync(lessonId);
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
                var exercise = _exerciseService.MapToEntity(
                    exerciseDto,
                    lesson.Id,
                    exerciseDto.OrderIndex ?? i
                );
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
            var course =
                await _context.Courses.FindAsync(dto.CourseId)
                ?? throw new ArgumentException($"Course with ID '{dto.CourseId}' not found.");
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

    public async Task<List<Exercise>> GetExercisesByLessonIdAsync(string lessonId)
    {
        return await _context
            .Exercises.Where(e => e.LessonId == lessonId)
            .Include(e => (e as MultipleChoiceExercise)!.Options)
            .OrderBy(e => e.OrderIndex)
            .ToListAsync();
    }

    public async Task<LessonProgressResult> GetFullLessonProgressAsync(
        string userId,
        string lessonId
    )
    {
        var data = await QueryExerciseProgressAsync(e => e.LessonId == lessonId, userId);

        var summary = BuildProgressSummary(data);

        var exerciseProgress = data.Where(d => d.Progress != null)
            .ToDictionary(
                d => d.ExerciseId,
                d => new UserExerciseProgressDto(
                    d.ExerciseId,
                    d.Progress!.IsCompleted,
                    d.Progress.PointsEarned,
                    d.Progress.CompletedAt
                )
            );

        return new LessonProgressResult(summary, exerciseProgress);
    }

    public async Task<Dictionary<string, LessonProgressSummary>> GetProgressForLessonsAsync(
        string userId,
        List<string> lessonIds
    )
    {
        var data = await QueryExerciseProgressAsync(e => lessonIds.Contains(e.LessonId), userId);

        return data.GroupBy(d => d.LessonId).ToDictionary(g => g.Key, g => BuildProgressSummary(g));
    }

    public async Task<List<SubmitAnswerResponse>> GetLessonSubmissionsAsync(
        string userId,
        string lessonId
    )
    {
        var exercises = await _context
            .Exercises.Where(e => e.LessonId == lessonId)
            .Include(e => (e as MultipleChoiceExercise)!.Options)
            .OrderBy(e => e.OrderIndex)
            .ToListAsync();

        var exerciseIds = exercises.Select(e => e.Id).ToList();

        var progressByExercise = await _context
            .UserExerciseProgress.Where(p =>
                p.UserId == userId && exerciseIds.Contains(p.ExerciseId)
            )
            .ToDictionaryAsync(p => p.ExerciseId);

        var fullProgress = await GetFullLessonProgressAsync(userId, lessonId);

        return exercises
            .Select(exercise =>
            {
                var hasProgress = progressByExercise.TryGetValue(exercise.Id, out var progress);

                return new SubmitAnswerResponse(
                    IsCorrect: hasProgress && progress!.IsCompleted,
                    PointsEarned: hasProgress ? progress!.PointsEarned : 0,
                    CorrectAnswer: hasProgress && !progress!.IsCompleted
                        ? GetCorrectAnswer(exercise)
                        : null,
                    Explanation: exercise.Explanation,
                    LessonProgress: fullProgress.Summary
                );
            })
            .ToList();
    }

    private async Task<List<ExerciseProgressRow>> QueryExerciseProgressAsync(
        Expression<Func<Exercise, bool>> filter,
        string userId
    )
    {
        return await _context
            .Exercises.Where(filter)
            .GroupJoin(
                _context.UserExerciseProgress.Where(p => p.UserId == userId),
                exercise => exercise.Id,
                progress => progress.ExerciseId,
                (exercise, progressGroup) =>
                    new ExerciseProgressRow
                    {
                        LessonId = exercise.LessonId,
                        ExerciseId = exercise.Id,
                        Points = exercise.Points,
                        Progress = progressGroup.FirstOrDefault(),
                    }
            )
            .ToListAsync();
    }

    private static LessonProgressSummary BuildProgressSummary(IEnumerable<ExerciseProgressRow> data)
    {
        var items = data.ToList();

        if (items.Count == 0)
            return new LessonProgressSummary(0, 0, 0, 0, 1.0, true);

        var completedCount = items.Count(d => d.Progress?.IsCompleted == true);
        var earnedXp = items.Sum(d => d.Progress?.PointsEarned ?? 0);
        var totalPossibleXp = items.Sum(d => d.Points);
        var completionPct = totalPossibleXp > 0 ? (double)earnedXp / totalPossibleXp : 1.0;

        return new LessonProgressSummary(
            CompletedExercises: completedCount,
            TotalExercises: items.Count,
            EarnedXp: earnedXp,
            TotalPossibleXp: totalPossibleXp,
            CompletionPercentage: Math.Round(completionPct, 2),
            MeetsCompletionThreshold: completionPct >= ExerciseProgressService.DefaultCompletionThreshold
        );
    }

    private class ExerciseProgressRow
    {
        public required string LessonId { get; init; }
        public required string ExerciseId { get; init; }
        public required int Points { get; init; }
        public UserExerciseProgress? Progress { get; init; }
    }

    private static string? GetCorrectAnswer(Exercise exercise)
    {
        return exercise switch
        {
            MultipleChoiceExercise mce => mce.Options.FirstOrDefault(o => o.IsCorrect)?.OptionText,
            FillInBlankExercise fib => fib.CorrectAnswer,
            TranslationExercise te => te.TargetText,
            ListeningExercise le => le.CorrectAnswer,
            _ => null,
        };
    }
}
