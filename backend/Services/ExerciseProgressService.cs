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
                .Exercises.Include(e => (e as MultipleChoiceExercise)!.Options)
                .Include(e => e.Lesson)
                .FirstOrDefaultAsync(e => e.Id == exerciseId)
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

        var correctAnswer = isCorrect
            ? null
            : exercise switch
            {
                MultipleChoiceExercise mce => mce.Options.FirstOrDefault(o => o.IsCorrect)
                    ?.OptionText,
                FillInBlankExercise fib => fib.CorrectAnswer,
                TranslationExercise te => te.TargetText,
                ListeningExercise le => le.CorrectAnswer,
                _ => null,
            };

        return new ExerciseSubmitResult(
            IsCorrect: isCorrect,
            PointsEarned: pointsEarned,
            CorrectAnswer: correctAnswer,
            Explanation: exercise.Explanation
        );
    }

    public async Task<CompleteLessonResponse> CompleteLessonAsync(string userId, string lessonId)
    {
        var lesson =
            await _context
                .Lessons.Include(l => l.Exercises)
                .FirstOrDefaultAsync(l => l.Id == lessonId)
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
                    nextLesson.Id,
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
            MultipleChoiceExercise mce => ValidateMultipleChoice(mce, answer),
            FillInBlankExercise fib => ValidateFillInBlank(fib, answer),
            TranslationExercise te => ValidateTranslation(te, answer),
            ListeningExercise le => ValidateListening(le, answer),
            _ => false,
        };
    }

    private static bool ValidateMultipleChoice(MultipleChoiceExercise exercise, string answer)
    {
        var selectedOption = exercise.Options.FirstOrDefault(o => o.Id == answer);
        return selectedOption?.IsCorrect ?? false;
    }

    private static bool ValidateFillInBlank(FillInBlankExercise exercise, string answer)
    {
        return ValidateTextAnswer(
            answer,
            exercise.CorrectAnswer,
            exercise.AcceptedAnswers,
            exercise.CaseSensitive,
            exercise.TrimWhitespace
        );
    }

    private static bool ValidateTranslation(TranslationExercise exercise, string answer)
    {
        var userInput = answer.Trim().ToLowerInvariant();
        var target = exercise.TargetText.Trim().ToLowerInvariant();

        if (userInput.Length == 0 && target.Length == 0)
            return true;

        var similarity = CalculateSimilarity(userInput, target);
        return similarity >= exercise.MatchingThreshold;
    }

    private static bool ValidateListening(ListeningExercise exercise, string answer)
    {
        return ValidateTextAnswer(
            answer,
            exercise.CorrectAnswer,
            exercise.AcceptedAnswers,
            exercise.CaseSensitive,
            trimWhitespace: true
        );
    }

    private static bool ValidateTextAnswer(
        string answer,
        string correctAnswer,
        string? acceptedAnswers,
        bool caseSensitive,
        bool trimWhitespace
    )
    {
        var userInput = trimWhitespace ? answer.Trim() : answer;
        var expected = trimWhitespace ? correctAnswer.Trim() : correctAnswer;

        if (!caseSensitive)
        {
            userInput = userInput.ToLowerInvariant();
            expected = expected.ToLowerInvariant();
        }

        if (userInput == expected)
            return true;

        if (!string.IsNullOrEmpty(acceptedAnswers))
        {
            var alternatives = acceptedAnswers
                .Split(',')
                .Select(a =>
                {
                    var alt = trimWhitespace ? a.Trim() : a;
                    return caseSensitive ? alt : alt.ToLowerInvariant();
                });

            return alternatives.Contains(userInput);
        }

        return false;
    }

    private static double CalculateSimilarity(string s1, string s2)
    {
        var longer = s1.Length >= s2.Length ? s1 : s2;
        var shorter = s1.Length >= s2.Length ? s2 : s1;

        if (longer.Length == 0)
            return 1.0;

        var distance = LevenshteinDistance(longer, shorter);
        return (double)(longer.Length - distance) / longer.Length;
    }

    private static int LevenshteinDistance(string s1, string s2)
    {
        var matrix = new int[s2.Length + 1, s1.Length + 1];

        for (var i = 0; i <= s2.Length; i++)
            matrix[i, 0] = i;

        for (var j = 0; j <= s1.Length; j++)
            matrix[0, j] = j;

        for (var i = 1; i <= s2.Length; i++)
        {
            for (var j = 1; j <= s1.Length; j++)
            {
                var cost = s2[i - 1] == s1[j - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost
                );
            }
        }

        return matrix[s2.Length, s1.Length];
    }
}
