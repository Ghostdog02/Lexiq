namespace Backend.Api.Dtos;

public record LessonDto
{
    public required string LessonId { get; init; }

    public required string CourseId { get; init; }

    public required string CourseTitle { get; init; }

    public required string Title { get; init; }

    public int? EstimatedDurationMinutes { get; init; }

    public required int OrderIndex { get; init; }

    public required string LessonContent { get; init; }

    public required bool IsLocked { get; init; }

    public required List<ExerciseDto> Exercises { get; init; }

    public int? CompletedExercises { get; init; }

    public int? EarnedXp { get; init; }

    public int? TotalPossibleXp { get; init; }

    public bool? IsCompleted { get; init; }
}

public record CreateLessonDto
{
    public required string CourseId { get; init; }

    public required string Title { get; init; }

    public int? EstimatedDurationMinutes { get; init; }

    public int? OrderIndex { get; init; }

    public required string Content { get; init; }

    public List<CreateExerciseDto>? Exercises { get; init; }
}

public record UpdateLessonDto
{
    public string? CourseId { get; init; }

    public string? Title { get; init; }

    public int? EstimatedDurationMinutes { get; init; }

    public int? OrderIndex { get; init; }

    public string? LessonContent { get; init; }
}

public enum UnlockStatus
{
    Unlocked,
    AlreadyUnlocked,
    NoNextLesson,
}
