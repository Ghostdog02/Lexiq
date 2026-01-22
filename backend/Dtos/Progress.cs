using System.ComponentModel.DataAnnotations;

namespace Backend.Api.DTOs.Progress;

// Request DTOs
public record SubmitExerciseAttemptRequest
{
    [Required]
    [Range(0, 100)]
    public int Score { get; init; }

    [Required]
    public bool IsCompleted { get; init; }
}

// Response DTOs
public record ExerciseProgressResponse
{
    public int ExerciseId { get; init; }
    public string Title { get; init; } = string.Empty;
    public bool IsCompleted { get; init; }
    public int BestScore { get; init; }
    public int AttemptsCount { get; init; }
    public int PointsEarned { get; init; }
    public DateTime? CompletedAt { get; init; }
    public DateTime? LastAttemptAt { get; init; }
}

public record LessonProgressResponse
{
    public int LessonId { get; init; }
    public string Title { get; init; } = string.Empty;
    public bool IsCompleted { get; init; }
    public int CompletionPercentage { get; init; }
    public int TotalExercises { get; init; }
    public int CompletedExercises { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public List<ExerciseProgressResponse> Exercises { get; init; } = [];
}

public record CourseProgressResponse
{
    public int CourseId { get; init; }
    public string Title { get; init; } = string.Empty;
    public bool IsCompleted { get; init; }
    public int CompletionPercentage { get; init; }
    public int TotalPointsEarned { get; init; }
    public int TotalLessons { get; init; }
    public int CompletedLessons { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public List<LessonProgressResponse> Lessons { get; init; } = [];
}

public record UserProgressSummaryResponse
{
    public string UserId { get; init; } = string.Empty;
    public int TotalPointsEarned { get; init; }
    public int TotalExercisesCompleted { get; init; }
    public int TotalLessonsCompleted { get; init; }
    public int TotalCoursesCompleted { get; init; }
    public List<CourseProgressResponse> CourseProgress { get; init; } = [];
}