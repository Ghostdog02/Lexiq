using Backend.Api.DTOs.Progress;

namespace Backend.Api.Services;

public interface IProgressService
{
    Task<bool> RecordExerciseAttemptAsync(
        string userId,
        int exerciseId,
        int score,
        bool isCompleted
    );
    
    Task<ExerciseProgressResponse?> GetExerciseProgressAsync(string userId, int exerciseId);

    Task<LessonProgressResponse?> GetLessonProgressAsync(string userId, int lessonId);

    Task<CourseProgressResponse?> GetCourseProgressAsync(string userId, int courseId);

    Task<UserProgressSummaryResponse> GetUserProgressSummaryAsync(string userId);

    Task<List<CourseProgressResponse>> GetLanguageProgressAsync(string userId, int languageId);

    Task<bool> ResetExerciseProgressAsync(string userId, int exerciseId);
}
