using Backend.Database;
using Backend.Database.Entities;
using Backend.Database.Entities.Exercises;
using Backend.Database.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests.Helpers;

public static class DbSeeder
{
    public static async Task AddUserAsync(BackendDbContext ctx, User user)
    {
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();
    }

    public static async Task AddUsersAsync(BackendDbContext ctx, IEnumerable<User> users)
    {
        ctx.Users.AddRange(users);
        await ctx.SaveChangesAsync();
    }

    /// <summary>
    /// Adds a single UserExerciseProgress row. The exerciseId must reference
    /// an existing Exercise — use one of DatabaseFixture.ExerciseIds.
    /// </summary>
    public static async Task AddProgressAsync(
        BackendDbContext ctx,
        string userId,
        string exerciseId,
        bool isCompleted,
        int pointsEarned,
        DateTime? completedAt
    )
    {
        ctx.UserExerciseProgress.Add(
            new UserExerciseProgress
            {
                UserId = userId,
                ExerciseId = exerciseId,
                IsCompleted = isCompleted,
                PointsEarned = pointsEarned,
                CompletedAt = completedAt,
            }
        );
        await ctx.SaveChangesAsync();
    }

    /// <summary>
    /// Adds multiple consecutive days of completed activity for a user.
    /// Cycles through the supplied exerciseIds (one row per day).
    /// </summary>
    public static async Task AddConsecutiveDaysActivityAsync(
        BackendDbContext ctx,
        string userId,
        IReadOnlyList<string> exerciseIds,
        int days,
        int startDaysAgo = 0,
        int pointsPerDay = 10
    )
    {
        for (var i = 0; i < days; i++)
        {
            ctx.UserExerciseProgress.Add(
                new UserExerciseProgress
                {
                    UserId = userId,
                    ExerciseId = exerciseIds[i],
                    IsCompleted = true,
                    PointsEarned = pointsPerDay,
                    CompletedAt = DateTime.UtcNow.Date.AddDays(-(startDaysAgo + i)),
                }
            );
        }
        await ctx.SaveChangesAsync();
    }

    public static async Task AddAvatarAsync(BackendDbContext ctx, string userId)
    {
        ctx.UserAvatars.Add(
            new UserAvatar
            {
                UserId = userId,
                Data = [0xFF, 0xD8],
                ContentType = "image/jpeg",
            }
        );
        await ctx.SaveChangesAsync();
    }

    /// <summary>
    /// Creates a FillInBlank exercise. All exercises accept "answer" as the correct response for test simplicity.
    /// </summary>
    public static async Task<string> CreateFillInBlankExerciseAsync(
        BackendDbContext ctx,
        string lessonId,
        int orderIndex,
        bool isLocked = true,
        int points = 10
    )
    {
        var exerciseId = Guid.NewGuid().ToString();
        ctx.Exercises.Add(
            new FillInBlankExercise
            {
                Id = exerciseId,
                LessonId = lessonId,
                Title = $"FillInBlank {orderIndex}",
                Text = "Fill in the blank: _",
                CorrectAnswer = "answer",
                AcceptedAnswers = "ans",
                CaseSensitive = false,
                TrimWhitespace = true,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = points,
                OrderIndex = orderIndex,
                IsLocked = isLocked,
            }
        );
        await ctx.SaveChangesAsync();
        return exerciseId;
    }

    /// <summary>
    /// Creates a MultipleChoice exercise with "answer" as the correct option text.
    /// </summary>
    public static async Task<string> CreateMultipleChoiceExerciseAsync(
        BackendDbContext ctx,
        string lessonId,
        int orderIndex,
        bool isLocked = true,
        int points = 10
    )
    {
        var exerciseId = Guid.NewGuid().ToString();
        ctx.Exercises.Add(
            new MultipleChoiceExercise
            {
                Id = exerciseId,
                LessonId = lessonId,
                Title = $"MultipleChoice {orderIndex}",
                Instructions = "Select the correct answer",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = points,
                OrderIndex = orderIndex,
                IsLocked = isLocked,
                Options = new List<ExerciseOption>
                {
                    new()
                    {
                        Id = Guid.NewGuid().ToString(),
                        ExerciseId = exerciseId,
                        OptionText = "wrong1",
                        IsCorrect = false,
                        OrderIndex = 0,
                    },
                    new()
                    {
                        Id = Guid.NewGuid().ToString(),
                        ExerciseId = exerciseId,
                        OptionText = "answer",
                        IsCorrect = true,
                        OrderIndex = 1,
                    },
                    new()
                    {
                        Id = Guid.NewGuid().ToString(),
                        ExerciseId = exerciseId,
                        OptionText = "wrong2",
                        IsCorrect = false,
                        OrderIndex = 2,
                    },
                },
            }
        );
        await ctx.SaveChangesAsync();
        return exerciseId;
    }

    /// <summary>
    /// Creates a Listening exercise with "answer" accepted.
    /// </summary>
    public static async Task<string> CreateListeningExerciseAsync(
        BackendDbContext ctx,
        string lessonId,
        int orderIndex,
        bool isLocked = true,
        int points = 10
    )
    {
        var exerciseId = Guid.NewGuid().ToString();
        ctx.Exercises.Add(
            new ListeningExercise
            {
                Id = exerciseId,
                LessonId = lessonId,
                Title = $"Listening {orderIndex}",
                AudioUrl = $"https://example.com/audio{orderIndex}.mp3",
                CorrectAnswer = "answer",
                AcceptedAnswers = "ans",
                CaseSensitive = false,
                MaxReplays = 3,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = points,
                OrderIndex = orderIndex,
                IsLocked = isLocked,
            }
        );
        
        await ctx.SaveChangesAsync();
        return exerciseId;
    }

    /// <summary>
    /// Creates a Translation exercise with "answer" as the target text.
    /// </summary>
    public static async Task<string> CreateTranslationExerciseAsync(
        BackendDbContext ctx,
        string lessonId,
        int orderIndex,
        bool isLocked = true,
        int points = 10
    )
    {
        var exerciseId = Guid.NewGuid().ToString();
        ctx.Exercises.Add(
            new TranslationExercise
            {
                Id = exerciseId,
                LessonId = lessonId,
                Title = $"Translation {orderIndex}",
                SourceText = "Test",
                TargetText = "answer",
                SourceLanguageCode = "en",
                TargetLanguageCode = "it",
                MatchingThreshold = 0.85,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = points,
                OrderIndex = orderIndex,
                IsLocked = isLocked,
            }
        );
        await ctx.SaveChangesAsync();
        return exerciseId;
    }

    /// <summary>
    /// Resets all exercise lock states to their initial seeded values.
    /// Only the first exercise (OrderIndex 0) is unlocked; all others are locked.
    /// Call this after ClearLeaderboardDataAsync to ensure clean test state.
    /// </summary>
    public static async Task ResetExerciseLocksAsync(BackendDbContext ctx)
    {
        // Unlock only the first exercise (OrderIndex 0)
        await ctx
            .Exercises.Where(e => e.OrderIndex == 0)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.IsLocked, false));

        // Lock all other exercises
        await ctx
            .Exercises.Where(e => e.OrderIndex > 0)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.IsLocked, true));
    }

    /// <summary>
    /// Clears all leaderboard-related test data between tests.
    /// Preserves the system user and content hierarchy (Language/Course/Lesson).
    /// Deletes all exercises to avoid accumulation across tests.
    ///
    /// Deletion order respects FK constraints:
    /// 1. UserExerciseProgress (FK → Users Cascade, FK → Exercises NoAction)
    /// 2. Exercises (must delete after progress due to NoAction FK)
    /// 3. UserAvatars (FK → Users Cascade)
    /// 4. Identity junction tables (no DbSet; cleared via raw SQL)
    /// 5. Test users (excluding system user who owns the content hierarchy)
    /// </summary>
    public static async Task ClearLeaderboardDataAsync(BackendDbContext ctx, string systemUserId)
    {
        await ctx.UserExerciseProgress.ExecuteDeleteAsync();
        await ctx.Exercises.ExecuteDeleteAsync();
        await ctx.UserAvatars.ExecuteDeleteAsync();
        await ctx.Database.ExecuteSqlRawAsync("DELETE FROM UserClaims");
        await ctx.Database.ExecuteSqlRawAsync("DELETE FROM UserLogins");
        await ctx.Database.ExecuteSqlRawAsync("DELETE FROM UserRoles");
        await ctx.Database.ExecuteSqlRawAsync("DELETE FROM UserTokens");

        await ctx.Users.Where(u => u.Id != systemUserId).ExecuteDeleteAsync();
    }
}
