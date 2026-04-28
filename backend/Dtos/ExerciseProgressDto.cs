namespace Backend.Api.Dtos;

public record SubmitAnswerRequest
{
    public required string Answer { get; init; }
}

public record ExerciseSubmitResult
{
    public required bool IsCorrect { get; init; }

    public required int PointsEarned { get; init; }

    public string? CorrectAnswer { get; init; }

    public string? Explanation { get; init; }
}

public record SubmitAnswerResponse
{
    public required bool IsCorrect { get; init; }

    public required int PointsEarned { get; init; }

    public string? CorrectAnswer { get; init; }

    public string? Explanation { get; init; }

    public required LessonProgressSummary LessonProgress { get; init; }
}

public record LessonProgressSummary
{
    public required int CompletedExercises { get; init; }

    public required int TotalExercises { get; init; }

    public required int EarnedXp { get; init; }

    public required int TotalPossibleXp { get; init; }

    public required double CompletionPercentage { get; init; }

    public required bool MeetsCompletionThreshold { get; init; }
}

public record CompleteLessonResponse
{
    public required string CurrentLessonId { get; init; }

    public required bool IsCompleted { get; init; }

    public required int EarnedXp { get; init; }

    public required int TotalPossibleXp { get; init; }

    public required double CompletionPercentage { get; init; }

    public required double RequiredThreshold { get; init; }

    public required bool IsLastInCourse { get; init; }

    public NextLessonInfo? NextLesson { get; init; }
}

public record NextLessonInfo
{
    public required string Id { get; init; }

    public required string Title { get; init; }

    public required string CourseId { get; init; }

    public required bool WasUnlocked { get; init; }

    public required bool IsLocked { get; init; }
}

public record LessonProgressResult
{
    public required LessonProgressSummary Summary { get; init; }

    public required Dictionary<string, UserExerciseProgressDto> ExerciseProgress { get; init; }
}
