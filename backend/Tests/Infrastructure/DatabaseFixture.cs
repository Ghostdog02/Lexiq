using Backend.Database;
using Backend.Database.Entities;
using Backend.Database.Entities.Exercises;
using Backend.Database.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Testcontainers.MsSql;
using Xunit;

namespace Backend.Tests.Infrastructure;

/// <summary>
/// Spins up a real SQL Server 2022 container once per test class (IClassFixture).
/// Applies all EF Core migrations and seeds a permanent content hierarchy
/// (Language → Course → Lesson → 20 Exercises) used across all tests.
///
/// UserExerciseProgress.ExerciseId FK uses DeleteBehavior.NoAction and is
/// enforced on INSERT by SQL Server — real Exercise rows are required for
/// progress rows to be valid.
/// </summary>
public class DatabaseFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder(
        "mcr.microsoft.com/mssql/server:2022-latest"
    ).Build();

    public string ConnectionString => _container.GetConnectionString();

    /// <summary>
    /// ID of the system user created to satisfy Course.CreatedById FK.
    /// Excluded from per-test teardown so the content hierarchy survives.
    /// </summary>
    public string SystemUserId { get; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 40 shared exercise IDs for use in UserExerciseProgress rows.
    /// Multiple IDs are needed because PK is (UserId, ExerciseId) —
    /// streak tests require one distinct ExerciseId per day of activity.
    ///
    /// Distribution:
    /// - [0-9]: FillInBlank (10 exercises)
    /// - [10-19]: MultipleChoice (10 exercises)
    /// - [20-29]: Listening (10 exercises)
    /// - [30-39]: Translation (10 exercises)
    /// </summary>
    public IReadOnlyList<string> ExerciseIds { get; } =
        Enumerable.Range(0, 40).Select(_ => Guid.NewGuid().ToString()).ToList();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        await using var ctx = CreateDbContext();
        await ctx.Database.MigrateAsync();
        await SeedContentHierarchyAsync(ctx);
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    public BackendDbContext CreateDbContext() =>
        new(
            new DbContextOptionsBuilder<BackendDbContext>()
                .UseSqlServer(ConnectionString)
                .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
                .Options
        );

    private async ValueTask SeedContentHierarchyAsync(BackendDbContext ctx)
    {
        // System user satisfies Course.CreatedById FK — never deleted during tests
        var systemUser = new User
        {
            Id = SystemUserId,
            UserName = "_system_",
            NormalizedUserName = "_SYSTEM_",
            Email = "system@test.internal",
            NormalizedEmail = "SYSTEM@TEST.INTERNAL",
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            RegistrationDate = DateTime.UtcNow,
            LastLoginDate = DateTime.UtcNow,
            TotalPointsEarned = 0,
        };

        ctx.Users.Add(systemUser);
        await ctx.SaveChangesAsync();

        var languageId = Guid.NewGuid().ToString();
        ctx.Languages.Add(new Language { Id = languageId, Name = "Italian" });
        await ctx.SaveChangesAsync();

        var courseId = Guid.NewGuid().ToString();
        ctx.Courses.Add(
            new Course
            {
                Id = courseId,
                LanguageId = languageId,
                Title = "Test Course",
                CreatedById = SystemUserId,
                OrderIndex = 0,
            }
        );

        await ctx.SaveChangesAsync();

        var lessonId = Guid.NewGuid().ToString();
        ctx.Lessons.Add(
            new Lesson
            {
                Id = lessonId,
                CourseId = courseId,
                Title = "Test Lesson",
                LessonContent = "{}",
                OrderIndex = 0,
                IsLocked = false, // First lesson unlocked for E2E tests
            }
        );
        
        await ctx.SaveChangesAsync();

        // Seed 10 iterations of each exercise type (40 total)
        var exercises = new List<Exercise>();

        // Exercises 0-9: FillInBlank (10 exercises)
        for (var i = 0; i < 10; i++)
        {
            exercises.Add(
                new FillInBlankExercise
                {
                    Id = ExerciseIds[i],
                    LessonId = lessonId,
                    Title = $"FillInBlank {i}",
                    Text = $"Fill in the blank _",
                    CorrectAnswer = "answer",
                    AcceptedAnswers = "ans,solution",
                    CaseSensitive = false,
                    TrimWhitespace = true,
                    DifficultyLevel = DifficultyLevel.Beginner,
                    Points = 10,
                    OrderIndex = i,
                    IsLocked = i != 0, // Only first exercise unlocked
                }
            );
        }

        // Exercises 10-19: MultipleChoice (10 exercises with 3 options each)
        for (var i = 10; i < 20; i++)
        {
            var mcId = ExerciseIds[i];
            var mcExercise = new MultipleChoiceExercise
            {
                Id = mcId,
                LessonId = lessonId,
                Title = $"MultipleChoice {i}",
                Instructions = "Select the correct answer",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                OrderIndex = i,
                IsLocked = true,
                Options = new List<ExerciseOption>
                {
                    new()
                    {
                        Id = Guid.NewGuid().ToString(),
                        ExerciseId = mcId,
                        OptionText = $"Wrong answer A for MC {i}",
                        IsCorrect = false,
                        OrderIndex = 0,
                    },
                    new()
                    {
                        Id = Guid.NewGuid().ToString(),
                        ExerciseId = mcId,
                        OptionText = $"Correct answer for MC {i}",
                        IsCorrect = true,
                        OrderIndex = 1,
                    },
                    new()
                    {
                        Id = Guid.NewGuid().ToString(),
                        ExerciseId = mcId,
                        OptionText = $"Wrong answer B for MC {i}",
                        IsCorrect = false,
                        OrderIndex = 2,
                    },
                },
            };
            exercises.Add(mcExercise);
        }

        // Exercises 20-29: Listening (10 exercises)
        for (var i = 20; i < 30; i++)
        {
            exercises.Add(
                new ListeningExercise
                {
                    Id = ExerciseIds[i],
                    LessonId = lessonId,
                    Title = $"Listening {i}",
                    AudioUrl = $"https://example.com/audio{i}.mp3",
                    CorrectAnswer = "audio answer",
                    AcceptedAnswers = "audio,sound",
                    CaseSensitive = false,
                    MaxReplays = 3,
                    DifficultyLevel = DifficultyLevel.Beginner,
                    Points = 10,
                    OrderIndex = i,
                    IsLocked = true,
                }
            );
        }

        // Exercises 30-39: Translation (10 exercises)
        for (var i = 30; i < 40; i++)
        {
            exercises.Add(
                new TranslationExercise
                {
                    Id = ExerciseIds[i],
                    LessonId = lessonId,
                    Title = $"Translation {i}",
                    SourceText = "Hello",
                    TargetText = "Ciao",
                    SourceLanguageCode = "en",
                    TargetLanguageCode = "it",
                    MatchingThreshold = 0.85,
                    DifficultyLevel = DifficultyLevel.Beginner,
                    Points = 10,
                    OrderIndex = i,
                    IsLocked = true,
                }
            );
        }

        ctx.Exercises.AddRange(exercises);
        await ctx.SaveChangesAsync();
    }
}
