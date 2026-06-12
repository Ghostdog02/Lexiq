namespace Backend.Api.Dtos;

public record CoursesWithProgressDto(
    List<HomeCourseDto> Courses,
    int TotalXp,
    int Hearts,
    DateTime? NextHeartRefillAt
);

public record HomeCourseDto(
    string CourseId,
    string LanguageName,
    string Title,
    string? Description,
    int? EstimatedDurationHours,
    int OrderIndex,
    int LessonCount,
    List<HomeLessonDto> Lessons
);

public record HomeLessonDto(
    string LessonId,
    string CourseId,
    string CourseName,
    string Title,
    int? EstimatedDurationMinutes,
    int OrderIndex,
    bool IsLocked,
    int? CompletedExercises,
    int? EarnedXp,
    int? TotalPossibleXp,
    bool? IsCompleted
);
