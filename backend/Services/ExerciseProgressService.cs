using Backend.Api.Dtos;
using Backend.Database;
using Backend.Database.Entities;
using Backend.Database.Entities.Exercises;
using Backend.Database.Entities.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Backend.Api.Services;

public class ExerciseProgressService(
    BackendDbContext context,
    LessonService lessonService,
    ExerciseService exerciseService,
    UserManager<User> userManager,
    AchievementService achievementService
)
{
    private readonly BackendDbContext _context = context;
    private readonly LessonService _lessonService = lessonService;
    private readonly ExerciseService _exerciseService = exerciseService;
    private readonly UserManager<User> _userManager = userManager;
    private readonly AchievementService _achievementService = achievementService;

    public const double DefaultCompletionThreshold = 0.70;

    public async Task<ExerciseSubmitResult> SubmitAnswerAsync(
        string userId,
        string exerciseId,
        string answer
    )
    {
        var exercise =
            await _context
                .Exercises
                .Include(e => (e as FillInBlankExercise)!.Options)
                .Include(e => (e as ListeningExercise)!.Options)
                .Include(e => (e as ImageChoiceExercise)!.Options)
                .Include(e => (e as AudioMatchingExercise)!.Pairs)
                .Include(e => e.Lesson)
                .FirstOrDefaultAsync(e => e.ExerciseId == exerciseId)
            ?? throw new ArgumentException("Exercise not found");

        // Check if user can bypass locks (Admin or ContentCreator)
        var user = await _userManager.FindByIdAsync(userId);
        var canBypassLocks = user != null && await user.CanBypassLocksAsync(_userManager);

        if (exercise.Lesson?.IsLocked == true && !canBypassLocks)
            throw new InvalidOperationException("Cannot submit answers for a locked lesson");

        if (exercise.IsLocked && !canBypassLocks)
            throw new InvalidOperationException("Cannot submit answers for a locked exercise");

        var isCorrect = ValidateAnswer(exercise, answer);
        var pointsEarned = isCorrect ? exercise.Points : 0;

        // Check hearts (don't allow submission if hearts depleted, unless bypass)
        if (!canBypassLocks && user != null && user.Hearts <= 0)
            throw new InvalidOperationException("No hearts remaining. Cannot submit answer.");

        // Decrement hearts on wrong answer (but not for admins/creators)
        if (!isCorrect && !canBypassLocks && user != null)
        {
            user.Hearts = Math.Max(0, user.Hearts - 1);
        }

        // Upsert progress
        var progress = await _context.UserExerciseProgress.FirstOrDefaultAsync(p =>
            p.UserId == userId && p.ExerciseId == exerciseId
        );

        var wasAlreadyCompleted = progress?.IsCompleted == true;

        if (progress == null)
        {
            progress = new UserExerciseProgress
            {
                UserId = userId,
                ExerciseId = exerciseId,
                IsCompleted = isCorrect,
                PointsEarned = pointsEarned,
                CompletedAt = isCorrect ? DateTime.UtcNow : null,
                User = null!,
                Exercise = null!,
            };

            _context.UserExerciseProgress.Add(progress);
        }
        else
        {
            progress.IsCompleted = isCorrect;
            progress.PointsEarned = pointsEarned;
            progress.CompletedAt = isCorrect ? (progress.CompletedAt ?? DateTime.UtcNow) : null;
        }

        // Update cached TotalPointsEarned only on first correct submission
        if (isCorrect && !wasAlreadyCompleted && user != null)
        {
            user.TotalPointsEarned += pointsEarned;
        }

        await _context.SaveChangesAsync();

        // Unlock any newly earned achievements after XP is persisted
        if (isCorrect && !wasAlreadyCompleted && user != null)
        {
            await _achievementService.CheckAndUnlockAchievementsAsync(
                user.Id,
                user.TotalPointsEarned
            );
        }

        // Unlock the next exercise if this one was completed successfully
        var nextExerciseUnlocked = false;
        if (isCorrect)
        {
            nextExerciseUnlocked = await _exerciseService.UnlockNextExerciseAsync(exerciseId);
        }

        var correctOptionId = exercise switch
        {
            FillInBlankExercise fib => fib.Options.FirstOrDefault(o => o.IsCorrect)
                ?.ExerciseOptionId,
            ListeningExercise le => le.Options.FirstOrDefault(o => o.IsCorrect)
                ?.ExerciseOptionId,
            ImageChoiceExercise ice => ice.Options.FirstOrDefault(o => o.IsCorrect)
                ?.ImageOptionId,
            AudioMatchingExercise ame => ame.Pairs.FirstOrDefault(p => p.IsCorrect)
                ?.AudioMatchPairId,
            _ => null,
        };

        var explanation = exercise switch
        {
            TrueFalseExercise tf => tf.Explanation,
            FillInBlankExercise fib => ResolveOptionExplanation(
                fib.Options, answer, isCorrect,
                o => o.ExerciseOptionId, o => o.Explanation, o => o.IsCorrect
            ),
            ListeningExercise le => ResolveOptionExplanation(
                le.Options, answer, isCorrect,
                o => o.ExerciseOptionId, o => o.Explanation, o => o.IsCorrect
            ),
            ImageChoiceExercise ice => ResolveOptionExplanation(
                ice.Options, answer, isCorrect,
                o => o.ImageOptionId, o => o.Explanation, o => o.IsCorrect
            ),
            AudioMatchingExercise ame => ResolveOptionExplanation(
                ame.Pairs, answer, isCorrect,
                p => p.AudioMatchPairId, p => p.Explanation, p => p.IsCorrect
            ),
            _ => null,
        };

        return new ExerciseSubmitResult(
            IsCorrect: isCorrect,
            PointsEarned: pointsEarned,
            CorrectOptionId: correctOptionId,
            Explanation: explanation
        );
    }

    private static string? ResolveOptionExplanation<T>(
        IEnumerable<T> options,
        string answer,
        bool isCorrect,
        Func<T, string> idSelector,
        Func<T, string> explanationSelector,
        Func<T, bool> isCorrectSelector
    )
        where T : class
    {
        var materialized = options as IList<T> ?? options.ToList();

        if (isCorrect)
        {
            var chosen = materialized.FirstOrDefault(o => idSelector(o) == answer);
            return chosen != null ? explanationSelector(chosen) : null;
        }

        var picked = materialized.FirstOrDefault(o => idSelector(o) == answer);
        if (picked != null)
            return explanationSelector(picked);

        var correct = materialized.FirstOrDefault(isCorrectSelector);
        return correct != null ? explanationSelector(correct) : null;
    }

    public async Task<CompleteLessonResponse> CompleteLessonAsync(string userId, string lessonId)
    {
        var lesson =
            await _context
                .Lessons.Include(l => l.Exercises)
                .FirstOrDefaultAsync(l => l.LessonId == lessonId)
            ?? throw new ArgumentException("Lesson not found");

        var fullProgress = await _lessonService.GetFullLessonProgressAsync(userId, lessonId);
        var meetsThreshold = fullProgress.Summary.MeetsCompletionThreshold;

        var unlockStatus = meetsThreshold
            ? await _lessonService.UnlockNextLessonAsync(lessonId)
            : UnlockStatus.NoNextLesson;

        var isLastInCourse = await _lessonService.IsLastLessonInCourseAsync(lessonId);
        var nextLesson = await _lessonService.GetNextLessonAsync(lessonId);

        return new CompleteLessonResponse(
            CurrentLessonId: lessonId,
            IsCompleted: meetsThreshold,
            EarnedXp: fullProgress.Summary.EarnedXp,
            TotalPossibleXp: fullProgress.Summary.TotalPossibleXp,
            CompletionPercentage: fullProgress.Summary.CompletionPercentage,
            RequiredThreshold: DefaultCompletionThreshold,
            IsLastInCourse: isLastInCourse,
            NextLesson: nextLesson != null
                ? new NextLessonInfo(
                    nextLesson.LessonId,
                    nextLesson.Title,
                    nextLesson.CourseId,
                    unlockStatus == UnlockStatus.Unlocked,
                    nextLesson.IsLocked
                )
                : null
        );
    }

    private static bool ValidateAnswer(Exercise exercise, string answer)
    {
        return exercise switch
        {
            FillInBlankExercise fib => ValidateOptionBased(fib.Options, answer),
            ListeningExercise le => ValidateOptionBased(le.Options, answer),
            TrueFalseExercise tf => ValidateTrueFalse(tf, answer),
            ImageChoiceExercise ice => ValidateImageChoice(ice, answer),
            AudioMatchingExercise ame => ValidateAudioMatching(ame, answer),
            _ => false,
        };
    }

    private static bool ValidateOptionBased(List<ExerciseOption> options, string answer)
    {
        var selectedOption = options.FirstOrDefault(o => o.ExerciseOptionId == answer);
        return selectedOption?.IsCorrect ?? false;
    }

    private static bool ValidateTrueFalse(TrueFalseExercise exercise, string answer)
    {
        if (!bool.TryParse(answer, out var userAnswer))
            return false;

        return userAnswer == exercise.CorrectAnswer;
    }

    private static bool ValidateImageChoice(ImageChoiceExercise exercise, string answer)
    {
        var selectedOption = exercise.Options.FirstOrDefault(o => o.ImageOptionId == answer);
        return selectedOption?.IsCorrect ?? false;
    }

    private static bool ValidateAudioMatching(AudioMatchingExercise exercise, string answer)
    {
        var selected = exercise.Pairs.FirstOrDefault(p => p.AudioMatchPairId == answer);
        return selected?.IsCorrect ?? false;
    }
}
