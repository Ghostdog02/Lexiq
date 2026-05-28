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

    public async Task<LessonProgressResult> GetFullLessonProgressAsync(
        string userId,
        string lessonId
    )
    {
        var data = await QueryExerciseProgressAsync(e => e.LessonId == lessonId, userId);

        var user = await _userManager.FindByIdAsync(userId);
        var summary = BuildProgressSummary(data, user?.Hearts > 0);

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

        var user = await _userManager.FindByIdAsync(userId);
        var hasHearts = user?.Hearts > 0;

        return data.GroupBy(d => d.LessonId).ToDictionary(g => g.Key, g => BuildProgressSummary(g, hasHearts));
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
            .UserExerciseProgress.Where(p =>
                p.UserId == userId && exerciseIds.Contains(p.ExerciseId)
            )
            .ToDictionaryAsync(p => p.ExerciseId);

        var fullProgress = await GetFullLessonProgressAsync(userId, lessonId);

        return exercises
            .Select(exercise =>
            {
                var hasProgress = progressByExercise.TryGetValue(exercise.ExerciseId, out var progress);

                var explanation = exercise switch
                {
                    TrueFalseExercise tf => tf.Options.FirstOrDefault(o => o.IsCorrect)?.Explanation,
                    _ => null
                };

                return new SubmitAnswerResponse(
                    IsCorrect: hasProgress && progress!.IsCompleted,
                    PointsEarned: hasProgress ? progress!.PointsEarned : 0,
                    CorrectOptionId: hasProgress && !progress!.IsCompleted
                        ? GetCorrectOptionIdForExercise(exercise)
                        : null,
                    Explanation: explanation,
                    LessonProgress: fullProgress.Summary
                );
            })
            .ToList();
    }

    public async Task<LessonSubmitResult> SubmitLessonAsync(
        string userId,
        string lessonId,
        IReadOnlyList<ExerciseAnswerDto> answers
    )
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

        if (!canBypassLocks && lesson.IsLocked)
            throw new InvalidOperationException("Lesson is locked");

        if (!canBypassLocks && user.Hearts <= 0)
            throw new NoHeartsException();

        var exerciseLookup = exercises.ToDictionary(e => e.ExerciseId);
        var exerciseResults = new List<ExerciseResultDto>();
        var isCompleted = true;

        // Load existing progress for all exercises in one query
        var exerciseIds = exercises.Select(e => e.ExerciseId).ToList();
        var existingProgress = await _context
            .UserExerciseProgress.Where(p => p.UserId == userId && exerciseIds.Contains(p.ExerciseId))
            .ToDictionaryAsync(p => p.ExerciseId);

        foreach (var answer in answers)
        {
            if (!exerciseLookup.TryGetValue(answer.ExerciseId, out var exercise))
                continue;

            var selectedOptionId = answer.SelectedOptionId ?? string.Empty;
            var isCorrect = ValidateAnswer(exercise, selectedOptionId);
            var pointsEarned = isCorrect ? exercise.Points : 0;

            var wasAlreadyCompleted = existingProgress.TryGetValue(exercise.ExerciseId, out var existing)
                && existing.IsCompleted;

            if (existing == null)
            {
                var newProgress = new UserExerciseProgress
                {
                    UserId = userId,
                    ExerciseId = exercise.ExerciseId,
                    IsCompleted = isCorrect,
                    PointsEarned = pointsEarned,
                    CompletedAt = isCorrect ? _clock.UtcNow : null,
                    User = null!,
                    Exercise = null!,
                };
                _context.UserExerciseProgress.Add(newProgress);
            }
            else
            {
                existing.IsCompleted = isCorrect;
                existing.PointsEarned = pointsEarned;
                existing.CompletedAt = isCorrect ? (existing.CompletedAt ?? _clock.UtcNow) : null;
            }

            // Accumulate XP only on first correct submission
            if (isCorrect && !wasAlreadyCompleted)
            {
                user.TotalPointsEarned += exercise.Points;
            }

            var correctOptionId = GetCorrectOptionIdForExercise(exercise);
            var explanation = ResolveOptionExplanation(exercise, selectedOptionId, isCorrect);

            exerciseResults.Add(new ExerciseResultDto(
                ExerciseId: exercise.ExerciseId,
                IsCorrect: isCorrect,
                PointsEarned: pointsEarned,
                CorrectOptionId: correctOptionId,
                Explanation: explanation
            ));

            if (!isCorrect && !canBypassLocks)
            {
                _heartsService.DecrementHearts(user, 1);
                if (user.Hearts <= 0)
                {
                    isCompleted = false;
                    break;
                }
            }
        }

        await _context.SaveChangesAsync();

        await _achievementService.CheckAndUnlockAchievementsAsync(userId, user.TotalPointsEarned);

        // Recompute progress summary after saving
        var progressData = await QueryExerciseProgressAsync(e => e.LessonId == lessonId, userId);
        var summary = BuildProgressSummary(progressData, isCompleted);

        // Upsert UserLessonProgress
        var lessonProgress = await _context.UserLessonProgress
            .FirstOrDefaultAsync(ulp => ulp.UserId == userId && ulp.LessonId == lessonId);

        if (lessonProgress == null)
        {
            lessonProgress = new UserLessonProgress
            {
                UserId = userId,
                LessonId = lessonId,
            };
            _context.UserLessonProgress.Add(lessonProgress);
        }

        lessonProgress.CompletedExercises = summary.CompletedExercises;
        lessonProgress.TotalExercises = summary.TotalExercises;
        lessonProgress.EarnedXp = summary.EarnedXp;
        lessonProgress.TotalPossibleXp = summary.TotalPossibleXp;
        lessonProgress.CompletionPercentage = summary.CompletionPercentage;
        lessonProgress.IsCompleted = summary.IsCompleted;
        lessonProgress.CompletedAt = summary.IsCompleted
            ? lessonProgress.CompletedAt ?? _clock.UtcNow
            : null;
        lessonProgress.UpdatedAt = _clock.UtcNow;

        await _context.SaveChangesAsync();

        if (summary.IsCompleted)
            await _lessonService.UnlockNextLessonAsync(lessonId);

        return new LessonSubmitResult(exerciseResults, summary, user.Hearts);
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

    private static LessonProgressSummary BuildProgressSummary(IEnumerable<ExerciseProgressRow> data, bool isCompleted = false)
    {
        var items = data.ToList();

        if (items.Count == 0)
            return new LessonProgressSummary(0, 0, 0, 0, 1.0, isCompleted);

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
            IsCompleted: isCompleted
        );
    }

    private class ExerciseProgressRow
    {
        public required string LessonId { get; init; }
        public required string ExerciseId { get; init; }
        public required int Points { get; init; }
        public UserExerciseProgress? Progress { get; init; }
    }

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
            FillInBlankExercise fib => ValidateOptionBased(fib.Options, selectedOptionId),
            ListeningExercise le => ValidateOptionBased(le.Options, selectedOptionId),
            TrueFalseExercise tf => ValidateOptionBased(tf.Options, selectedOptionId),
            ImageChoiceExercise ice => ValidateImageChoice(ice, selectedOptionId),
            AudioMatchingExercise ame => ValidateAudioMatching(ame, selectedOptionId),
            Exercise e => ValidateOptionBased(e.Options, selectedOptionId),
        };
    }

    private static bool ValidateOptionBased(List<ExerciseOption> options, string selectedOptionId)
    {
        var selectedOption = options.FirstOrDefault(o => o.ExerciseOptionId == selectedOptionId);
        return selectedOption?.IsCorrect ?? false;
    }

    private static bool ValidateImageChoice(ImageChoiceExercise exercise, string selectedImageOptionId)
    {
        var selectedOption = exercise.Options.FirstOrDefault(o => o.ImageOptionId == selectedImageOptionId);
        return selectedOption?.IsCorrect ?? false;
    }

    private static bool ValidateAudioMatching(AudioMatchingExercise exercise, string selectedPairId)
    {
        var selected = exercise.Pairs.FirstOrDefault(p => p.AudioMatchPairId == selectedPairId);
        return selected?.IsCorrect ?? false;
    }

    private static string? ResolveOptionExplanation(Exercise exercise, string selectedOptionId, bool isCorrect)
    {
        return exercise switch
        {
            TrueFalseExercise tf => ResolveOptionExplanation(
                tf.Options, selectedOptionId, isCorrect,
                o => o.ExerciseOptionId, o => o.Explanation, o => o.IsCorrect),
            FillInBlankExercise fib => ResolveOptionExplanation(
                fib.Options, selectedOptionId, isCorrect,
                o => o.ExerciseOptionId, o => o.Explanation, o => o.IsCorrect),
            ListeningExercise le => ResolveOptionExplanation(
                le.Options, selectedOptionId, isCorrect,
                o => o.ExerciseOptionId, o => o.Explanation, o => o.IsCorrect),
            ImageChoiceExercise ice => ResolveOptionExplanation(
                ice.Options, selectedOptionId, isCorrect,
                o => o.ImageOptionId, o => o.Explanation, o => o.IsCorrect),
            AudioMatchingExercise ame => ResolveOptionExplanation(
                ame.Pairs, selectedOptionId, isCorrect,
                p => p.AudioMatchPairId, p => p.Explanation, p => p.IsCorrect),
            _ => null,
        };
    }

    private static string? ResolveOptionExplanation<T>(
        IEnumerable<T> options,
        string optionId,
        bool isCorrect,
        Func<T, string> idSelector,
        Func<T, string> explanationSelector,
        Func<T, bool> isCorrectSelector
    )
        where T : class
    {
        var materialized = options as IList<T> ?? options.ToList();

        var chosen = materialized.FirstOrDefault(o => idSelector(o) == optionId);
        if (chosen != null)
            return explanationSelector(chosen);

        if (isCorrect)
            return null;

        var correct = materialized.FirstOrDefault(isCorrectSelector);
        return correct != null ? explanationSelector(correct) : null;
    }
}
