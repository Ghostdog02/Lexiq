// using Backend.Api.DTOs.Progress;
// using Backend.Database;
// using Backend.Database.Entities.Users;
// using Microsoft.EntityFrameworkCore;

// namespace Backend.Api.Services;

// public class ProgressService : IProgressService
// {
//     private readonly BackendDbContext _context;

//     public ProgressService(BackendDbContext context)
//     {
//         _context = context;
//     }

//     public async Task<bool> RecordExerciseAttemptAsync(
//         string userId,
//         int exerciseId,
//         int score,
//         bool isCompleted
//     )
//     {
//         var exercise = await _context.Exercises.FindAsync(exerciseId);
//         if (exercise == null)
//             return false;

//         var progress = await _context.UserExerciseProgresses.FirstOrDefaultAsync(p =>
//             p.UserId == userId && p.ExerciseId == exerciseId
//         );

//         if (progress == null)
//         {
//             progress = new UserExerciseProgress
//             {
//                 UserId = userId,
//                 ExerciseId = exerciseId,
//                 FirstAttemptAt = DateTime.UtcNow,
//             };
//             _context.UserExerciseProgresses.Add(progress);
//         }

//         progress.AttemptsCount++;
//         progress.LastAttemptAt = DateTime.UtcNow;

//         if (score > progress.BestScore)
//         {
//             progress.BestScore = score;
//         }

//         if (isCompleted && !progress.IsCompleted)
//         {
//             progress.IsCompleted = true;
//             progress.CompletedAt = DateTime.UtcNow;
//             progress.PointsEarned = exercise.Points;
//         }

//         await _context.SaveChangesAsync();

//         // Update lesson progress
//         await UpdateLessonProgressAsync(userId, exerciseId);

//         // Update course progress
//         await UpdateCourseProgressAsync(userId, exercise.LessonId);

//         return true;
//     }

//     public async Task<ExerciseProgressResponse?> GetExerciseProgressAsync(
//         string userId,
//         int exerciseId
//     )
//     {
//         var progress = await _context
//             .UserExerciseProgresses.Include(p => p.Exercise)
//             .FirstOrDefaultAsync(p => p.UserId == userId && p.ExerciseId == exerciseId);

//         if (progress == null)
//         {
//             // Return default progress for exercises not yet attempted
//             var exercise = await _context.Exercises.FindAsync(exerciseId);
//             if (exercise == null)
//                 return null;

//             return new ExerciseProgressResponse
//             {
//                 ExerciseId = exerciseId,
//                 AttemptsCount = 0,
//                 BestScore = 0,
//                 IsCompleted = false,
//                 PointsEarned = 0,
//                 FirstAttemptAt = null,
//                 LastAttemptAt = null,
//                 CompletedAt = null
//             };
//         }

//         return new ExerciseProgressResponse
//         {
//             ExerciseId = progress.ExerciseId,
//             AttemptsCount = progress.AttemptsCount,
//             BestScore = progress.BestScore,
//             IsCompleted = progress.IsCompleted,
//             PointsEarned = progress.PointsEarned,
//             FirstAttemptAt = progress.FirstAttemptAt,
//             LastAttemptAt = progress.LastAttemptAt,
//             CompletedAt = progress.CompletedAt
//         };
//     }

//     public async Task<LessonProgressResponse?> GetLessonProgressAsync(
//         string userId,
//         int lessonId
//     )
//     {
//         var lesson = await _context
//             .Lessons.Include(l => l.Exercises)
//             .FirstOrDefaultAsync(l => l.Id == lessonId);

//         if (lesson == null)
//             return null;

//         var lessonProgress = await _context.UserLessonProgresses.FirstOrDefaultAsync(p =>
//             p.UserId == userId && p.LessonId == lessonId
//         );

//         var totalExercises = lesson.Exercises.Count;
//         var completedCount = await _context
//             .UserExerciseProgresses.Where(p =>
//                 p.UserId == userId && p.IsCompleted && p.Exercise.LessonId == lessonId
//             )
//             .CountAsync();

//         var totalPoints = lesson.Exercises.Sum(e => e.Points);
//         var earnedPoints = await _context
//             .UserExerciseProgresses.Where(p =>
//                 p.UserId == userId && p.IsCompleted && p.Exercise.LessonId == lessonId
//             )
//             .SumAsync(p => p.PointsEarned);

//         return new LessonProgressResponse
//         {
//             LessonId = lessonId,
//             CompletionPercentage = lessonProgress?.CompletionPercentage ?? 0,
//             IsCompleted = lessonProgress?.IsCompleted ?? false,
//             TotalExercises = totalExercises,
//             CompletedExercises = completedCount,
//             TotalPoints = totalPoints,
//             EarnedPoints = earnedPoints,
//             StartedAt = lessonProgress?.StartedAt,
//             CompletedAt = lessonProgress?.CompletedAt,
//             UpdatedAt = lessonProgress?.UpdatedAt
//         };
//     }

//     public async Task<CourseProgressResponse?> GetCourseProgressAsync(string userId, int courseId)
//     {
//         var course = await _context
//             .Courses.Include(c => c.Lessons)
//             .ThenInclude(l => l.Exercises)
//             .FirstOrDefaultAsync(c => c.Id == courseId);

//         if (course == null)
//             return null;

//         var courseProgress = await _context.UserCourseProgresses.FirstOrDefaultAsync(p =>
//             p.UserId == userId && p.CourseId == courseId
//         );

//         var totalLessons = course.Lessons.Count;
//         var completedLessons = await _context
//             .UserLessonProgresses.Where(p =>
//                 p.UserId == userId && p.IsCompleted && p.Lesson.CourseId == courseId
//             )
//             .CountAsync();

//         var totalExercises = course.Lessons.SelectMany(l => l.Exercises).Count();
//         var completedExercises = await _context
//             .UserExerciseProgresses.Where(p =>
//                 p.UserId == userId && p.IsCompleted && p.Exercise.Lesson.CourseId == courseId
//             )
//             .CountAsync();

//         var totalPoints = course.Lessons.SelectMany(l => l.Exercises).Sum(e => e.Points);
//         var earnedPoints = await _context
//             .UserExerciseProgresses.Where(p =>
//                 p.UserId == userId && p.IsCompleted && p.Exercise.Lesson.CourseId == courseId
//             )
//             .SumAsync(p => p.PointsEarned);

//         var completionPercentage =
//             totalExercises > 0 ? (int)((completedExercises / (double)totalExercises) * 100) : 0;

//         return new CourseProgressResponse
//         {
//             CourseId = courseId,
//             CompletionPercentage = completionPercentage,
//             IsCompleted = courseProgress?.IsCompleted ?? false,
//             TotalLessons = totalLessons,
//             CompletedLessons = completedLessons,
//             TotalExercises = totalExercises,
//             CompletedExercises = completedExercises,
//             TotalPoints = totalPoints,
//             EarnedPoints = earnedPoints,
//             StartedAt = courseProgress?.StartedAt,
//             CompletedAt = courseProgress?.CompletedAt,
//             UpdatedAt = courseProgress?.UpdatedAt
//         };
//     }

//     public async Task<UserProgressSummaryResponse> GetUserProgressSummaryAsync(string userId)
//     {
//         var totalExercisesCompleted = await _context
//             .UserExerciseProgresses.Where(p => p.UserId == userId && p.IsCompleted)
//             .CountAsync();

//         var totalPoints = await _context
//             .UserExerciseProgresses.Where(p => p.UserId == userId && p.IsCompleted)
//             .SumAsync(p => p.PointsEarned);

//         var totalLessonsCompleted = await _context
//             .UserLessonProgresses.Where(p => p.UserId == userId && p.IsCompleted)
//             .CountAsync();

//         var totalCoursesCompleted = await _context
//             .UserCourseProgresses.Where(p => p.UserId == userId && p.IsCompleted)
//             .CountAsync();

//         var coursesInProgress = await _context
//             .UserCourseProgresses.Where(p => p.UserId == userId && !p.IsCompleted)
//             .CountAsync();

//         var currentStreak = await CalculateCurrentStreakAsync(userId);
//         var longestStreak = await CalculateLongestStreakAsync(userId);

//         return new UserProgressSummaryResponse
//         {
//             TotalExercisesCompleted = totalExercisesCompleted,
//             TotalPoints = totalPoints,
//             TotalLessonsCompleted = totalLessonsCompleted,
//             TotalCoursesCompleted = totalCoursesCompleted,
//             CoursesInProgress = coursesInProgress,
//             CurrentStreak = currentStreak,
//             LongestStreak = longestStreak
//         };
//     }

//     public async Task<List<CourseProgressResponse>> GetLanguageProgressAsync(
//         string userId,
//         int languageId
//     )
//     {
//         var courses = await _context
//             .Courses.Where(c => c.LanguageId == languageId)
//             .OrderBy(c => c.Order)
//             .ToListAsync();

//         var progressList = new List<CourseProgressResponse>();

//         foreach (var course in courses)
//         {
//             var progress = await GetCourseProgressAsync(userId, course.Id);
//             if (progress != null)
//             {
//                 progressList.Add(progress);
//             }
//         }

//         return progressList;
//     }

//     public async Task<bool> ResetExerciseProgressAsync(string userId, int exerciseId)
//     {
//         var progress = await _context.UserExerciseProgresses.FirstOrDefaultAsync(p =>
//             p.UserId == userId && p.ExerciseId == exerciseId
//         );

//         if (progress == null)
//             return false;

//         _context.UserExerciseProgresses.Remove(progress);
//         await _context.SaveChangesAsync();

//         // Recalculate lesson progress
//         var exercise = await _context.Exercises.FindAsync(exerciseId);
//         if (exercise != null)
//         {
//             await UpdateLessonProgressAsync(userId, exerciseId);
//             await UpdateCourseProgressAsync(userId, exercise.LessonId);
//         }

//         return true;
//     }

//     private async Task UpdateLessonProgressAsync(string userId, int exerciseId)
//     {
//         var exercise = await _context
//             .Exercises.Include(e => e.Lesson)
//             .ThenInclude(l => l.Exercises)
//             .FirstOrDefaultAsync(e => e.Id == exerciseId);

//         if (exercise?.Lesson == null)
//             return;

//         var lessonId = exercise.LessonId;
//         var totalExercises = exercise.Lesson.Exercises.Count;

//         var completedCount = await _context
//             .UserExerciseProgresses.Where(p =>
//                 p.UserId == userId && p.IsCompleted && p.Exercise.LessonId == lessonId
//             )
//             .CountAsync();

//         var lessonProgress = await _context.UserLessonProgresses.FirstOrDefaultAsync(p =>
//             p.UserId == userId && p.LessonId == lessonId
//         );

//         if (lessonProgress == null)
//         {
//             lessonProgress = new UserLessonProgress
//             {
//                 UserId = userId,
//                 LessonId = lessonId,
//                 StartedAt = DateTime.UtcNow,
//             };
//             _context.UserLessonProgresses.Add(lessonProgress);
//         }

//         lessonProgress.CompletionPercentage = (int)(
//             (completedCount / (double)totalExercises) * 100
//         );
//         lessonProgress.IsCompleted = completedCount == totalExercises;
//         lessonProgress.UpdatedAt = DateTime.UtcNow;

//         if (lessonProgress.IsCompleted && lessonProgress.CompletedAt == null)
//         {
//             lessonProgress.CompletedAt = DateTime.UtcNow;
//         }
//         else if (!lessonProgress.IsCompleted)
//         {
//             lessonProgress.CompletedAt = null;
//         }

//         await _context.SaveChangesAsync();
//     }

//     private async Task UpdateCourseProgressAsync(string userId, int lessonId)
//     {
//         var lesson = await _context
//             .Lessons.Include(l => l.Course)
//             .ThenInclude(c => c.Lessons)
//             .FirstOrDefaultAsync(l => l.Id == lessonId);

//         if (lesson?.Course == null)
//             return;

//         var courseId = lesson.CourseId;
//         var totalLessons = lesson.Course.Lessons.Count;

//         var completedCount = await _context
//             .UserLessonProgresses.Where(p =>
//                 p.UserId == userId && p.IsCompleted && p.Lesson.CourseId == courseId
//             )
//             .CountAsync();

//         var courseProgress = await _context.UserCourseProgresses.FirstOrDefaultAsync(p =>
//             p.UserId == userId && p.CourseId == courseId
//         );

//         if (courseProgress == null)
//         {
//             courseProgress = new UserCourseProgress
//             {
//                 UserId = userId,
//                 CourseId = courseId,
//                 StartedAt = DateTime.UtcNow,
//             };
//             _context.UserCourseProgresses.Add(courseProgress);
//         }

//         courseProgress.CompletionPercentage = (int)(
//             (completedCount / (double)totalLessons) * 100
//         );
//         courseProgress.IsCompleted = completedCount == totalLessons;
//         courseProgress.UpdatedAt = DateTime.UtcNow;

//         if (courseProgress.IsCompleted && courseProgress.CompletedAt == null)
//         {
//             courseProgress.CompletedAt = DateTime.UtcNow;
//         }
//         else if (!courseProgress.IsCompleted)
//         {
//             courseProgress.CompletedAt = null;
//         }

//         await _context.SaveChangesAsync();
//     }

//     private async Task<int> CalculateCurrentStreakAsync(string userId)
//     {
//         var today = DateTime.UtcNow.Date;
//         var streak = 0;

//         for (var date = today; date >= today.AddDays(-365); date = date.AddDays(-1))
//         {
//             var hasActivity = await _context
//                 .UserExerciseProgresses.AnyAsync(p =>
//                     p.UserId == userId && p.LastAttemptAt.HasValue && p.LastAttemptAt.Value.Date == date
//                 );

//             if (hasActivity)
//             {
//                 streak++;
//             }
//             else if (date != today)
//             {
//                 break;
//             }
//         }

//         return streak;
//     }

//     private async Task<int> CalculateLongestStreakAsync(string userId)
//     {
//         var attempts = await _context
//             .UserExerciseProgresses.Where(p => p.UserId == userId && p.LastAttemptAt.HasValue)
//             .Select(p => p.LastAttemptAt!.Value.Date)
//             .Distinct()
//             .OrderBy(d => d)
//             .ToListAsync();

//         if (!attempts.Any())
//             return 0;

//         var longestStreak = 1;
//         var currentStreak = 1;

//         for (var i = 1; i < attempts.Count; i++)
//         {
//             if ((attempts[i] - attempts[i - 1]).Days == 1)
//             {
//                 currentStreak++;
//                 longestStreak = Math.Max(longestStreak, currentStreak);
//             }
//             else
//             {
//                 currentStreak = 1;
//             }
//         }

//         return longestStreak;
//     }
// }