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
    /// 20 shared exercise IDs for use in UserExerciseProgress rows.
    /// Multiple IDs are needed because PK is (UserId, ExerciseId) —
    /// streak tests require one distinct ExerciseId per day of activity.
    /// </summary>
    public IReadOnlyList<string> ExerciseIds { get; } =
        Enumerable.Range(0, 20).Select(_ => Guid.NewGuid().ToString()).ToList();

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
            }
        );
        await ctx.SaveChangesAsync();

        var exercises = ExerciseIds.Select(
            (id, i) =>
                new FillInBlankExercise
                {
                    Id = id,
                    LessonId = lessonId,
                    Title = $"Exercise {i}",
                    Text = "Test",
                    CorrectAnswer = "answer",
                    CaseSensitive = false,
                    TrimWhitespace = true,
                    DifficultyLevel = DifficultyLevel.Beginner,
                    Points = 10,
                    OrderIndex = i,
                }
        );
        
        ctx.Exercises.AddRange(exercises);
        await ctx.SaveChangesAsync();
    }
}
