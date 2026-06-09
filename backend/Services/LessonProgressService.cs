using System.Linq.Expressions;
using Backend.Api.Dtos;
using Backend.Api.Services.Clock;
using Backend.Database;
using Backend.Database.Entities;
using Backend.Database.Entities.Exercises;
using Backend.Database.Entities.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Backend.Api.Services;

public class LessonProgressService(
    BackendDbContext context,
    LessonService lessonService,
    UserManager<User> userManager,
    AchievementService achievementService,
    IClock clock,
    HeartsService heartsService
)
{
    private readonly BackendDbContext _context = context;
    private readonly LessonService _lessonService = lessonService;
    private readonly UserManager<User> _userManager = userManager;
    private readonly AchievementService _achievementService = achievementService;
    private readonly IClock _clock = clock;
    private readonly HeartsService _heartsService = heartsService;

    // ── Public API ────────────────────────────────────────────────────────────

    public async Task<LessonProgressResult> GetFullLessonProgressAsync(
        string userId,
        string lessonId
    )
    {
        var data = await QueryExerciseProgressAsync(e => e.LessonId == lessonId, userId);
        var isCompleted = await GetLessonCompletionAsync(userId, lessonId);
        var summary = BuildExerciseMetrics(data) with { IsCompleted = isCompleted };

        var exerciseProgress = data
            .Where(d => d.Progress != null)
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
        var completions = await GetLessonCompletionsAsync(userId, lessonIds);

        return data.GroupBy(d => d.LessonId).ToDictionary(
            g => g.Key,
            g => BuildExerciseMetrics(g) with { IsCompleted = completions.GetValueOrDefault(g.Key) }
        );
    }

    public async Task<List<SubmitAnswerResponse>> GetLessonSubmissionsAsync(
        string userId,
        string lessonId
    )
    {
        var exercises = await _context
            .Exercises.Where(e => e.LessonId == lessonId)
            .OrderBy(e => e.CreatedAt)
            .Include(e => (e as FillInBlankExercise)!.Options)
            .Include(e => (e as ListeningExercise)!.Options)
            .Include(e => (e as ImageChoiceExercise)!.Options)
            .Include(e => (e as AudioMatchingExercise)!.Pairs)
            .ToListAsync();

        var exerciseIds = exercises.Select(e => e.ExerciseId).ToList();

        var progressByExercise = await _context
            .UserExerciseProgress.Where(p => p.UserId == userId && exerciseIds.Contains(p.ExerciseId))
            .ToDictionaryAsync(p => p.ExerciseId);

        var fullProgress = await GetFullLessonProgressAsync(userId, lessonId);

        return exercises
            .Select(exercise =>
            {
                var hasProgress = progressByExercise.TryGetValue(exercise.ExerciseId, out var progress);

                return new SubmitAnswerResponse(
                    IsCorrect: hasProgress && progress!.IsCompleted,
                    PointsEarned: hasProgress ? progress!.PointsEarned : 0,
                    CorrectOptionId: hasProgress && !progress!.IsCompleted
                        ? GetCorrectOptionIdForExercise(exercise)
                        : null,
                    Explanation: exercise is TrueFalseExercise tf
                        ? tf.Options.FirstOrDefault(o => o.IsCorrect)?.Explanation
                        : null,
                    LessonProgress: fullProgress.Summary
                );
            })
            .ToList();
    }

    public async Task<HashSet<string>> GetUnlockedLessonIdsAsync(string userId, List<string> lessonIds)
    {
        return await _context.UserLessonProgress
            .Where(ulp => ulp.UserId == userId && lessonIds.Contains(ulp.LessonId) && !ulp.IsLocked)
            .Select(ulp => ulp.LessonId)
            .ToHashSetAsync();
    }

    public async Task EnsureFirstLessonUnlockedAsync(string userId, string courseId)
    {
        var firstLesson = await _lessonService.GetFirstLessonInCourseAsync(courseId);
        if (firstLesson == null)
            return;

        var exists = await _context.UserLessonProgress
            .AnyAsync(ulp => ulp.UserId == userId && ulp.LessonId == firstLesson.LessonId && !ulp.IsLocked);

        if (exists)
            return;

        _context.UserLessonProgress.Add(new UserLessonProgress
        {
            UserId = userId,
            LessonId = firstLesson.LessonId,
            IsLocked = false,
        });
        await _context.SaveChangesAsync();
    }

    public async Task<LessonSubmitResult> SubmitLessonAsync(
        string userId,
        string lessonId,
        IReadOnlyList<ExerciseAnswerDto> answers
    )
    {
        var ctx = await LoadSubmissionContextAsync(userId, lessonId);
        var (exerciseResults, isCompleted) = ProcessAnswers(ctx, answers);
        await SaveExerciseProgressAsync(userId, exerciseResults, ctx.ExistingProgress);
        await _context.SaveChangesAsync();

        await _achievementService.CheckAndUnlockAchievementsAsync(userId, ctx.User.TotalPointsEarned);

        var progressData = await QueryExerciseProgressAsync(e => e.LessonId == lessonId, userId);
        var summary = BuildExerciseMetrics(progressData) with { IsCompleted = isCompleted };

        await UpsertLessonProgressAsync(userId, lessonId, summary);
        await _context.SaveChangesAsync();

        if (isCompleted)
            await _lessonService.UnlockNextLessonAsync(lessonId, userId);

        return new LessonSubmitResult(exerciseResults, summary, ctx.User.Hearts);
    }

    // ── Submission pipeline ───────────────────────────────────────────────────

    private record SubmissionContext(
        List<Exercise> Exercises,
        User User,
        bool CanBypassLocks,
        Dictionary<string, Exercise> ExerciseLookup,
        Dictionary<string, UserExerciseProgress> ExistingProgress
    );

    private async Task<SubmissionContext> LoadSubmissionContextAsync(string userId, string lessonId)
    {
        var exercises = await _context
            .Exercises.Where(e => e.LessonId == lessonId)
            .Include(e => (e as FillInBlankExercise)!.Options)
            .Include(e => (e as ListeningExercise)!.Options)
            .Include(e => (e as TrueFalseExercise)!.Options)
            .Include(e => (e as ImageChoiceExercise)!.Options)
            .Include(e => (e as AudioMatchingExercise)!.Pairs)
            .Include(e => e.Lesson)
            .ToListAsync();

        if (exercises.Count == 0)
            throw new ArgumentException($"Lesson '{lessonId}' not found or has no exercises");

        var lesson = exercises[0].Lesson
            ?? await _context.Lessons.FindAsync(lessonId)
            ?? throw new ArgumentException($"Lesson '{lessonId}' not found");

        var user = await _userManager.FindByIdAsync(userId)
            ?? throw new ArgumentException($"User '{userId}' not found");

        var canBypassLocks = await user.CanBypassLocksAsync(_userManager);

        if (!canBypassLocks)
        {
            var isUnlockedForUser = await _context.UserLessonProgress
                .AnyAsync(ulp => ulp.UserId == userId && ulp.LessonId == lesson.LessonId && !ulp.IsLocked);
            if (!isUnlockedForUser)
                throw new InvalidOperationException("Lesson is locked");
        }

        if (!canBypassLocks && user.Hearts <= 0)
            throw new NoHeartsException();

        var exerciseIds = exercises.Select(e => e.ExerciseId).ToList();
        var existingProgress = await _context.UserExerciseProgress
            .Where(p => p.UserId == userId && exerciseIds.Contains(p.ExerciseId))
            .ToDictionaryAsync(p => p.ExerciseId);

        return new SubmissionContext(
            exercises,
            user,
            canBypassLocks,
            exercises.ToDictionary(e => e.ExerciseId),
            existingProgress
        );
    }

    private (List<ExerciseResultDto> Results, bool IsCompleted) ProcessAnswers(
        SubmissionContext ctx,
        IReadOnlyList<ExerciseAnswerDto> answers
    )
    {
        var results = new List<ExerciseResultDto>();
        var isCompleted = true;

        foreach (var answer in answers)
        {
            if (!ctx.ExerciseLookup.TryGetValue(answer.ExerciseId, out var exercise))
                continue;

            var selectedOptionId = answer.SelectedOptionId ?? string.Empty;
            var isCorrect = ValidateAnswer(exercise, selectedOptionId);
            var pointsEarned = isCorrect ? exercise.Points : 0;

            if (isCorrect)
                ctx.User.TotalPointsEarned += exercise.Points;

            results.Add(new ExerciseResultDto(
                ExerciseId: exercise.ExerciseId,
                IsCorrect: isCorrect,
                PointsEarned: pointsEarned,
                CorrectOptionId: GetCorrectOptionIdForExercise(exercise),
                Explanation: ResolveOptionExplanation(exercise, selectedOptionId, isCorrect)
            ));

            if (!isCorrect && !ctx.CanBypassLocks)
            {
                _heartsService.DecrementHearts(ctx.User, 1);
                if (ctx.User.Hearts <= 0)
                {
                    isCompleted = false;
                    break;
                }
            }
        }

        return (results, isCompleted);
    }

    private async Task SaveExerciseProgressAsync(
        string userId,
        List<ExerciseResultDto> results,
        Dictionary<string, UserExerciseProgress> existingProgress
    )
    {
        foreach (var result in results)
        {
            if (existingProgress.TryGetValue(result.ExerciseId, out var existing))
            {
                existing.IsCompleted = result.IsCorrect;
                existing.PointsEarned = result.PointsEarned;
                existing.CompletedAt = result.IsCorrect ? (existing.CompletedAt ?? _clock.UtcNow) : null;
            }
            else
            {
                _context.UserExerciseProgress.Add(new UserExerciseProgress
                {
                    UserId = userId,
                    ExerciseId = result.ExerciseId,
                    IsCompleted = result.IsCorrect,
                    PointsEarned = result.PointsEarned,
                    CompletedAt = result.IsCorrect ? _clock.UtcNow : null,
                    User = null!,
                    Exercise = null!,
                });
            }
        }
    }

    private async Task UpsertLessonProgressAsync(
        string userId,
        string lessonId,
        LessonProgressSummary summary
    )
    {
        var record = await _context.UserLessonProgress
            .FirstOrDefaultAsync(ulp => ulp.UserId == userId && ulp.LessonId == lessonId);

        if (record == null)
        {
            record = new UserLessonProgress { UserId = userId, LessonId = lessonId };
            _context.UserLessonProgress.Add(record);
        }

        record.CompletedExercises = summary.CompletedExercises;
        record.TotalExercises = summary.TotalExercises;
        record.EarnedXp = summary.EarnedXp;
        record.TotalPossibleXp = summary.TotalPossibleXp;
        record.CompletionPercentage = summary.CompletionPercentage;
        record.IsCompleted = summary.IsCompleted;
        record.IsLocked = false;
        record.CompletedAt = summary.IsCompleted ? record.CompletedAt ?? _clock.UtcNow : null;
        record.UpdatedAt = _clock.UtcNow;
    }

    // ── Progress queries ──────────────────────────────────────────────────────

    private async Task<List<ExerciseProgressRow>> QueryExerciseProgressAsync(
        Expression<Func<Exercise, bool>> filter,
        string userId
    )
    {
        return await _context
            .Exercises.Where(filter)
            .GroupJoin(
                _context.UserExerciseProgress.Where(p => p.UserId == userId),
                exercise => exercise.ExerciseId,
                progress => progress.ExerciseId,
                (exercise, progressGroup) =>
                    new ExerciseProgressRow
                    {
                        LessonId = exercise.LessonId,
                        ExerciseId = exercise.ExerciseId,
                        Points = exercise.Points,
                        Progress = progressGroup.FirstOrDefault(),
                    }
            )
            .ToListAsync();
    }

    private async Task<bool> GetLessonCompletionAsync(string userId, string lessonId)
    {
        return await _context.UserLessonProgress
            .Where(ulp => ulp.UserId == userId && ulp.LessonId == lessonId)
            .Select(ulp => ulp.IsCompleted)
            .FirstOrDefaultAsync();
    }

    private async Task<Dictionary<string, bool>> GetLessonCompletionsAsync(
        string userId,
        List<string> lessonIds
    )
    {
        return await _context.UserLessonProgress
            .Where(ulp => ulp.UserId == userId && lessonIds.Contains(ulp.LessonId))
            .ToDictionaryAsync(ulp => ulp.LessonId, ulp => ulp.IsCompleted);
    }

    // ── Metrics ───────────────────────────────────────────────────────────────

    private static LessonProgressSummary BuildExerciseMetrics(IEnumerable<ExerciseProgressRow> data)
    {
        var items = data.ToList();

        if (items.Count == 0)
            return new LessonProgressSummary(0, 0, 0, 0, 1.0, false);

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
            IsCompleted: false
        );
    }

    private class ExerciseProgressRow
    {
        public required string LessonId { get; init; }
        public required string ExerciseId { get; init; }
        public required int Points { get; init; }
        public UserExerciseProgress? Progress { get; init; }
    }

    // ── Answer validation ─────────────────────────────────────────────────────

    private static string? GetCorrectOptionIdForExercise(Exercise exercise)
    {
        return exercise switch
        {
            FillInBlankExercise fib => fib.Options.FirstOrDefault(o => o.IsCorrect)?.ExerciseOptionId,
            ListeningExercise le => le.Options.FirstOrDefault(o => o.IsCorrect)?.ExerciseOptionId,
            TrueFalseExercise tf => tf.Options.FirstOrDefault(o => o.IsCorrect)?.ExerciseOptionId,
            ImageChoiceExercise ice => ice.Options.FirstOrDefault(o => o.IsCorrect)?.ImageOptionId,
            AudioMatchingExercise ame => ame.Pairs.FirstOrDefault(p => p.IsCorrect)?.AudioMatchPairId,
            Exercise e => e.Options.FirstOrDefault(o => o.IsCorrect)?.ExerciseOptionId,
        };
    }

    private static bool ValidateAnswer(Exercise exercise, string selectedOptionId)
    {
        return exercise switch
        {
            FillInBlankExercise fib => fib.Options.Any(o => o.ExerciseOptionId == selectedOptionId && o.IsCorrect),
            ListeningExercise le => le.Options.Any(o => o.ExerciseOptionId == selectedOptionId && o.IsCorrect),
            TrueFalseExercise tf => tf.Options.Any(o => o.ExerciseOptionId == selectedOptionId && o.IsCorrect),
            ImageChoiceExercise ice => ice.Options.Any(o => o.ImageOptionId == selectedOptionId && o.IsCorrect),
            AudioMatchingExercise ame => ame.Pairs.Any(p => p.AudioMatchPairId == selectedOptionId && p.IsCorrect),
            Exercise e => e.Options.Any(o => o.ExerciseOptionId == selectedOptionId && o.IsCorrect),
        };
    }

    // ── Explanation resolution ────────────────────────────────────────────────

    private sealed record OptionInfo(string Id, string? Explanation, bool IsCorrect);

    private static string? ResolveOptionExplanation(Exercise exercise, string selectedOptionId, bool isCorrect)
    {
        var infos = exercise switch
        {
            TrueFalseExercise tf => tf.Options.Select(o => new OptionInfo(o.ExerciseOptionId, o.Explanation, o.IsCorrect)),
            FillInBlankExercise fib => fib.Options.Select(o => new OptionInfo(o.ExerciseOptionId, o.Explanation, o.IsCorrect)),
            ListeningExercise le => le.Options.Select(o => new OptionInfo(o.ExerciseOptionId, o.Explanation, o.IsCorrect)),
            ImageChoiceExercise ice => ice.Options.Select(o => new OptionInfo(o.ImageOptionId, o.Explanation, o.IsCorrect)),
            AudioMatchingExercise ame => ame.Pairs.Select(p => new OptionInfo(p.AudioMatchPairId, p.Explanation, p.IsCorrect)),
            _ => [],
        };

        var items = infos.ToList();
        var chosen = items.FirstOrDefault(o => o.Id == selectedOptionId);
        if (chosen is not null)
            return chosen.Explanation;
        if (isCorrect)
            return null;
        return items.FirstOrDefault(o => o.IsCorrect)?.Explanation;
    }
}
