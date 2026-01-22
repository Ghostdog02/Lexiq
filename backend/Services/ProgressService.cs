using Backend.Database;
using Backend.Database.Entities.Users;

namespace Backend.Api.Services;

public class ProgressService
{
    private readonly BackendDbContext _context;

    public ProgressService(BackendDbContext context)
    {
        _context = context;
    }

    public async Task<bool> RecordExerciseAttempt(
        string userId,
        int exerciseId,
        int score,
        bool isCompleted
    )
    {
        var progress = await _context.UserExerciseProgress.FirstOrDefaultAsync(p =>
            p.UserId == userId && p.ExerciseId == exerciseId
        );

        if (progress == null)
        {
            progress = new UserExerciseProgress
            {
                UserId = userId,
                ExerciseId = exerciseId,
                FirstAttemptAt = DateTime.UtcNow,
            };
            _context.UserExerciseProgress.Add(progress);
        }

        progress.AttemptsCount++;
        progress.LastAttemptAt = DateTime.UtcNow;

        if (score > progress.BestScore)
        {
            progress.BestScore = score;
        }

        if (isCompleted && !progress.IsCompleted)
        {
            progress.IsCompleted = true;
            progress.CompletedAt = DateTime.UtcNow;

            var exercise = await _context.Exercises.FindAsync(exerciseId);
            progress.PointsEarned = exercise.Points;
        }

        await _context.SaveChangesAsync();

        // Update lesson progress
        await UpdateLessonProgress(userId, exerciseId);

        return true;
    }

    private async Task UpdateLessonProgress(string userId, int exerciseId)
    {
        var exercise = await _context
            .Exercises.Include(e => e.Lesson)
            .ThenInclude(l => l.Exercises)
            .FirstOrDefaultAsync(e => e.Id == exerciseId);

        if (exercise?.Lesson == null)
            return;

        var lessonId = exercise.LessonId;
        var totalExercises = exercise.Lesson.Exercises.Count;

        var completedCount = await _context
            .UserExerciseProgress.Where(p =>
                p.UserId == userId && p.IsCompleted && p.Exercise.LessonId == lessonId
            )
            .CountAsync();

        var lessonProgress = await _context.UserLessonProgress.FirstOrDefaultAsync(p =>
            p.UserId == userId && p.LessonId == lessonId
        );

        if (lessonProgress == null)
        {
            lessonProgress = new UserLessonProgress
            {
                UserId = userId,
                LessonId = lessonId,
                StartedAt = DateTime.UtcNow,
            };
            _context.UserLessonProgress.Add(lessonProgress);
        }

        lessonProgress.CompletionPercentage = (int)(
            (completedCount / (double)totalExercises) * 100
        );
        lessonProgress.IsCompleted = completedCount == totalExercises;
        lessonProgress.UpdatedAt = DateTime.UtcNow;

        if (lessonProgress.IsCompleted && lessonProgress.CompletedAt == null)
        {
            lessonProgress.CompletedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }
}