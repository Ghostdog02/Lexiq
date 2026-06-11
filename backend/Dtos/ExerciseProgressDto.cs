namespace Backend.Api.Dtos;

public record SubmitAnswerRequest(string SelectedOptionId);

public record ExerciseSubmitResult(
    bool IsCorrect,
    int PointsEarned,
    string? CorrectOptionId,
    string? Explanation
);

public record SubmitAnswerResponse(
    bool IsCorrect,
    int PointsEarned,
    string? CorrectOptionId,
    string? Explanation,
    LessonProgressSummary LessonProgress
);

public record LessonProgressSummary(
    int CompletedExercises,
    int TotalExercises,
    int EarnedXp,
    int TotalPossibleXp,
    double CompletionPercentage,
    bool IsCompleted
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
    Dictionary<string, UserExerciseProgressDto> ExerciseProgress,
    bool IsLocked
);

public record SubmitLessonRequest(List<ExerciseAnswerDto> Answers);

public record ExerciseAnswerDto(string ExerciseId, string? SelectedOptionId);

public record LessonSubmitResult(
    List<ExerciseResultDto> Exercises,
    LessonProgressSummary Summary,
    int HeartsRemaining
);

public record ExerciseResultDto(
    string ExerciseId,
    bool IsCorrect,
    int PointsEarned,
    string? CorrectOptionId,
    string? Explanation
);
