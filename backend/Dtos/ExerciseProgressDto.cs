namespace Backend.Api.Dtos;

public record SubmitAnswerRequest(string Answer);

public record ExerciseSubmitResult(
    bool IsCorrect,
    int PointsEarned,
    string? CorrectAnswer,
    string? Explanation
);

public record SubmitAnswerResponse(
    bool IsCorrect,
    int PointsEarned,
    string? CorrectAnswer,
    string? Explanation,
    LessonProgressSummary LessonProgress
);

public record LessonProgressSummary(
    int CompletedExercises,
    int TotalExercises,
    int EarnedXp,
    int TotalPossibleXp,
    double CompletionPercentage,
    bool MeetsCompletionThreshold
);

public record CompleteLessonResponse(
    string CurrentLessonId,
    bool IsCompleted,
    int EarnedXp,
    int TotalPossibleXp,
    double CompletionPercentage,
    double RequiredThreshold,
    bool IsLastInCourse,
    NextLessonInfo? NextLesson
);

public record NextLessonInfo(
    string Id,
    string Title,
    string CourseId,
    bool WasUnlocked,
    bool IsLocked
);

public record LessonProgressResult(
    LessonProgressSummary Summary,
    Dictionary<string, UserExerciseProgressDto> ExerciseProgress
);
