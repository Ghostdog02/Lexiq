using Backend.Api.Dtos;
using Backend.Database;
using Backend.Database.Entities;
using Backend.Database.Entities.Exercises;
using Microsoft.EntityFrameworkCore;

namespace Backend.Api.Services;

public class ExerciseProgressService(BackendDbContext context, LessonService lessonService)
{
    private readonly BackendDbContext _context = context;
    private readonly LessonService _lessonService = lessonService;

    public const double DefaultCompletionThreshold = 0.70;

    public async Task<SubmitAnswerResponse> SubmitAnswerAsync(
        string userId, string exerciseId, string answer)
    {
        var exercise = await _context.Exercises
            .Include(e => (e as MultipleChoiceExercise)!.Options)
            .Include(e => e.Lesson)
            .FirstOrDefaultAsync(e => e.Id == exerciseId)
            ?? throw new ArgumentException("Exercise not found");

        if (exercise.Lesson?.IsLocked == true)
            throw new InvalidOperationException("Cannot submit answers for a locked lesson");

        var isCorrect = ValidateAnswer(exercise, answer);
        var pointsEarned = isCorrect ? exercise.Points : 0;

        // Upsert progress
        var progress = await _context.UserExerciseProgress
            .FirstOrDefaultAsync(p => p.UserId == userId && p.ExerciseId == exerciseId);

        if (progress == null)
        {
            progress = new UserExerciseProgress
            {
                UserId = userId,
                ExerciseId = exerciseId,
                IsCompleted = isCorrect,
                PointsEarned = pointsEarned,
                CompletedAt = isCorrect ? DateTime.UtcNow : null
            };
            _context.UserExerciseProgress.Add(progress);
        }
        else
        {
            progress.IsCompleted = isCorrect;
            progress.PointsEarned = pointsEarned;
            progress.CompletedAt = isCorrect ? (progress.CompletedAt ?? DateTime.UtcNow) : null;
        }

        await _context.SaveChangesAsync();

        var lessonProgress = await GetLessonProgressAsync(userId, exercise.LessonId);

        return new SubmitAnswerResponse(
            IsCorrect: isCorrect,
            PointsEarned: pointsEarned,
            CorrectAnswer: isCorrect ? null : GetCorrectAnswer(exercise),
            Explanation: exercise.Explanation,
            LessonProgress: lessonProgress
        );
    }

    public async Task<CompleteLessonResponse> CompleteLessonAsync(
        string userId, string lessonId)
    {
        var lesson = await _context.Lessons
            .Include(l => l.Exercises)
            .FirstOrDefaultAsync(l => l.Id == lessonId)
            ?? throw new ArgumentException("Lesson not found");

        var lessonProgress = await GetLessonProgressAsync(userId, lessonId);
        var meetsThreshold = lessonProgress.MeetsCompletionThreshold;

        var wasUnlocked = false;
        if (meetsThreshold)
        {
            wasUnlocked = await _lessonService.UnlockNextLessonAsync(lessonId);
        }

        var isLastInCourse = await _lessonService.IsLastLessonInCourseAsync(lessonId);
        var nextLesson = await _lessonService.GetNextLessonAsync(lessonId);

        return new CompleteLessonResponse(
            CurrentLessonId: lessonId,
            IsCompleted: meetsThreshold,
            EarnedXp: lessonProgress.EarnedXp,
            TotalPossibleXp: lessonProgress.TotalPossibleXp,
            CompletionPercentage: lessonProgress.CompletionPercentage,
            RequiredThreshold: DefaultCompletionThreshold,
            IsLastInCourse: isLastInCourse,
            NextLesson: nextLesson != null
                ? new NextLessonInfo(
                    nextLesson.Id,
                    nextLesson.Title,
                    nextLesson.CourseId,
                    wasUnlocked,
                    nextLesson.IsLocked)
                : null
        );
    }

    public async Task<LessonProgressSummary> GetLessonProgressAsync(
        string userId, string lessonId)
    {
        var exercises = await _context.Exercises
            .Where(e => e.LessonId == lessonId)
            .Select(e => new { e.Id, e.Points })
            .ToListAsync();

        if (exercises.Count == 0)
        {
            return new LessonProgressSummary(0, 0, 0, 0, 1.0, true);
        }

        var exerciseIds = exercises.Select(e => e.Id).ToList();

        var progressRecords = await _context.UserExerciseProgress
            .Where(p => p.UserId == userId && exerciseIds.Contains(p.ExerciseId))
            .ToListAsync();

        var completedCount = progressRecords.Count(p => p.IsCompleted);
        var earnedXp = progressRecords.Sum(p => p.PointsEarned);
        var totalPossibleXp = exercises.Sum(e => e.Points);
        var completionPercentage = totalPossibleXp > 0
            ? (double)earnedXp / totalPossibleXp
            : 1.0;

        return new LessonProgressSummary(
            CompletedExercises: completedCount,
            TotalExercises: exercises.Count,
            EarnedXp: earnedXp,
            TotalPossibleXp: totalPossibleXp,
            CompletionPercentage: Math.Round(completionPercentage, 2),
            MeetsCompletionThreshold: completionPercentage >= DefaultCompletionThreshold
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
            _ => false
        };
    }

    private static bool ValidateMultipleChoice(MultipleChoiceExercise exercise, string answer)
    {
        var selectedOption = exercise.Options.FirstOrDefault(o => o.Id == answer);
        return selectedOption?.IsCorrect ?? false;
    }

    private static bool ValidateFillInBlank(FillInBlankExercise exercise, string answer)
    {
        var userInput = answer;
        var correctAnswer = exercise.CorrectAnswer;

        if (exercise.TrimWhitespace)
        {
            userInput = userInput.Trim();
            correctAnswer = correctAnswer.Trim();
        }

        if (!exercise.CaseSensitive)
        {
            userInput = userInput.ToLowerInvariant();
            correctAnswer = correctAnswer.ToLowerInvariant();
        }

        if (userInput == correctAnswer)
            return true;

        if (!string.IsNullOrEmpty(exercise.AcceptedAnswers))
        {
            var alternatives = exercise.AcceptedAnswers
                .Split(',')
                .Select(a =>
                {
                    var alt = exercise.TrimWhitespace ? a.Trim() : a;
                    return exercise.CaseSensitive ? alt : alt.ToLowerInvariant();
                });

            return alternatives.Contains(userInput);
        }

        return false;
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
        var userInput = answer.Trim();
        var correctAnswer = exercise.CorrectAnswer.Trim();

        if (!exercise.CaseSensitive)
        {
            userInput = userInput.ToLowerInvariant();
            correctAnswer = correctAnswer.ToLowerInvariant();
        }

        if (userInput == correctAnswer)
            return true;

        if (!string.IsNullOrEmpty(exercise.AcceptedAnswers))
        {
            var alternatives = exercise.AcceptedAnswers
                .Split(',')
                .Select(a =>
                {
                    var alt = a.Trim();
                    return exercise.CaseSensitive ? alt : alt.ToLowerInvariant();
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

    private static string? GetCorrectAnswer(Exercise exercise)
    {
        return exercise switch
        {
            MultipleChoiceExercise mce =>
                mce.Options.FirstOrDefault(o => o.IsCorrect)?.OptionText,
            FillInBlankExercise fib => fib.CorrectAnswer,
            TranslationExercise te => te.TargetText,
            ListeningExercise le => le.CorrectAnswer,
            _ => null
        };
    }
}
