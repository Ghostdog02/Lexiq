using Backend.Api.Dtos;
using Backend.Api.Services;
using Backend.Database;
using Backend.Database.Entities;
using Backend.Database.Entities.Exercises;
using Backend.Tests.Helpers;
using Backend.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend.Tests.Services.LessonServiceTests;

/// <summary>
/// Tests for lesson unlocking: automatic and manual unlocking with exercise cascade behavior.
/// </summary>
public class LessonUnlockingTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly DatabaseFixture _fixture = fixture;
    private BackendDbContext _ctx = null!;
    private LessonService _sut = null!;

    private string _courseId = null!;
    private string _secondCourseId = null!;
    private string _thirdCourseId = null!;
    private string _languageId = null!;

    public async ValueTask InitializeAsync()
    {
        _ctx = _fixture.CreateDbContext();

        // Clean up state from previous tests
        await DbSeeder.ClearLeaderboardDataAsync(_ctx, _fixture.SystemUserId);
        await ClearTestCoursesAndLessonsAsync();

        var exerciseService = new ExerciseService(_ctx);
        _sut = new LessonService(_ctx, exerciseService);

        var language = await _ctx.Languages.FirstAsync(TestContext.Current.CancellationToken);
        _languageId = language.Id;

        var course = await _ctx.Courses.FirstAsync(TestContext.Current.CancellationToken);
        _courseId = course.Id;

        _secondCourseId = Guid.NewGuid().ToString();
        _ctx.Courses.Add(
            new Course
            {
                Id = _secondCourseId,
                LanguageId = _languageId,
                Title = "Second Course",
                CreatedById = _fixture.SystemUserId,
                OrderIndex = 1,
            }
        );

        _thirdCourseId = Guid.NewGuid().ToString();
        _ctx.Courses.Add(
            new Course
            {
                Id = _thirdCourseId,
                LanguageId = _languageId,
                Title = "Third Course",
                CreatedById = _fixture.SystemUserId,
                OrderIndex = 2,
            }
        );

        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _ctx.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Deletes all lessons except the fixture's base lesson, and all courses except the fixture's base course.
    /// Required because ClearLeaderboardDataAsync only deletes exercises, not lessons or courses.
    /// </summary>
    private async Task ClearTestCoursesAndLessonsAsync()
    {
        // Delete all lessons except the fixture's base lesson
        await _ctx
            .Lessons.Where(l => l.Id != _fixture.LessonId)
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);

        // Delete all courses except the fixture's base course
        await _ctx
            .Courses.Where(c => c.Id != _fixture.CourseId)
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
    }

    #region UnlockNextLessonAsync

    [Fact]
    public async Task UnlockNextLessonAsync_LockedNextLesson_UnlocksLessonAndFirstExercise()
    {
        // Arrange
        var firstLessonId = Guid.NewGuid().ToString();
        var secondLessonId = Guid.NewGuid().ToString();
        var exerciseId = Guid.NewGuid().ToString();

        _ctx.Lessons.Add(
            new Lesson
            {
                Id = firstLessonId,
                CourseId = _courseId,
                Title = "Lesson 1",
                LessonContent = "{}",
                OrderIndex = 0,
                IsLocked = false,
            }
        );
        _ctx.Lessons.Add(
            new Lesson
            {
                Id = secondLessonId,
                CourseId = _courseId,
                Title = "Lesson 2",
                LessonContent = "{}",
                OrderIndex = 1,
                IsLocked = true,
            }
        );
        _ctx.Exercises.Add(
            new FillInBlankExercise
            {
                Id = exerciseId,
                LessonId = secondLessonId,
                Title = "Exercise 1",
                Text = "Test",
                CorrectAnswer = "answer",
                CaseSensitive = false,
                TrimWhitespace = true,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                OrderIndex = 0,
                IsLocked = true,
            }
        );
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.UnlockNextLessonAsync(firstLessonId);

        // Assert
        result.Should().Be(UnlockStatus.Unlocked);

        var unlockedLesson = await _ctx.Lessons.FindAsync(
            new object[] { secondLessonId },
            TestContext.Current.CancellationToken
        );
        unlockedLesson.Should().NotBeNull();
        unlockedLesson.IsLocked.Should().BeFalse();

        var unlockedExercise = await _ctx.Exercises.FindAsync(
            new object[] { exerciseId },
            TestContext.Current.CancellationToken
        );
        unlockedExercise.Should().NotBeNull();
        unlockedExercise.IsLocked.Should().BeFalse();
    }

    [Fact]
    public async Task UnlockNextLessonAsync_NextLessonAlreadyUnlocked_ReturnsAlreadyUnlocked()
    {
        // Arrange
        var firstLessonId = Guid.NewGuid().ToString();
        var secondLessonId = Guid.NewGuid().ToString();

        _ctx.Lessons.Add(
            new Lesson
            {
                Id = firstLessonId,
                CourseId = _courseId,
                Title = "Lesson 1",
                LessonContent = "{}",
                OrderIndex = 0,
                IsLocked = false,
            }
        );
        _ctx.Lessons.Add(
            new Lesson
            {
                Id = secondLessonId,
                CourseId = _courseId,
                Title = "Lesson 2",
                LessonContent = "{}",
                OrderIndex = 1,
                IsLocked = false, // Already unlocked
            }
        );
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.UnlockNextLessonAsync(firstLessonId);

        // Assert
        result.Should().Be(UnlockStatus.AlreadyUnlocked);
    }

    [Fact]
    public async Task UnlockNextLessonAsync_NoNextLesson_ReturnsNoNextLesson()
    {
        // Arrange
        var lastLessonId = Guid.NewGuid().ToString();
        _ctx.Lessons.Add(
            new Lesson
            {
                Id = lastLessonId,
                CourseId = _thirdCourseId,
                Title = "Last Lesson",
                LessonContent = "{}",
                OrderIndex = 0,
            }
        );
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.UnlockNextLessonAsync(lastLessonId);

        // Assert
        result.Should().Be(UnlockStatus.NoNextLesson);
    }

    [Fact]
    public async Task UnlockNextLessonAsync_InvalidLessonId_ReturnsNoNextLesson()
    {
        // Arrange
        var invalidId = Guid.NewGuid().ToString();

        // Act
        var result = await _sut.UnlockNextLessonAsync(invalidId);

        // Assert
        result.Should().Be(UnlockStatus.NoNextLesson);
    }

    #endregion

    #region UnlockLessonAsync

    [Fact]
    public async Task UnlockLessonAsync_LockedLesson_UnlocksLessonAndFirstExercise()
    {
        // Arrange
        var lessonId = Guid.NewGuid().ToString();
        var exercise1Id = Guid.NewGuid().ToString();
        var exercise2Id = Guid.NewGuid().ToString();

        _ctx.Lessons.Add(
            new Lesson
            {
                Id = lessonId,
                CourseId = _courseId,
                Title = "Test Lesson",
                LessonContent = "{}",
                OrderIndex = 0,
                IsLocked = true,
            }
        );
        _ctx.Exercises.AddRange(
            new FillInBlankExercise
            {
                Id = exercise1Id,
                LessonId = lessonId,
                Title = "Exercise 1",
                Text = "Test",
                CorrectAnswer = "answer",
                CaseSensitive = false,
                TrimWhitespace = true,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                OrderIndex = 0,
                IsLocked = true,
            },
            new FillInBlankExercise
            {
                Id = exercise2Id,
                LessonId = lessonId,
                Title = "Exercise 2",
                Text = "Test",
                CorrectAnswer = "answer",
                CaseSensitive = false,
                TrimWhitespace = true,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                OrderIndex = 1,
                IsLocked = true,
            }
        );
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await _sut.UnlockLessonAsync(lessonId);

        // Assert
        var lesson = await _ctx.Lessons.FindAsync(
            new object[] { lessonId },
            TestContext.Current.CancellationToken
        );
        lesson.Should().NotBeNull();
        lesson.IsLocked.Should().BeFalse();

        var exercise1 = await _ctx.Exercises.FindAsync(
            new object[] { exercise1Id },
            TestContext.Current.CancellationToken
        );
        exercise1.Should().NotBeNull();
        exercise1.IsLocked.Should().BeFalse();

        var exercise2 = await _ctx.Exercises.FindAsync(
            new object[] { exercise2Id },
            TestContext.Current.CancellationToken
        );
        exercise2.Should().NotBeNull();
        exercise2.IsLocked.Should().BeTrue("second exercise should remain locked");
    }

    [Fact]
    public async Task UnlockLessonAsync_AlreadyUnlockedLesson_IsIdempotent()
    {
        // Arrange
        var lessonId = Guid.NewGuid().ToString();
        _ctx.Lessons.Add(
            new Lesson
            {
                Id = lessonId,
                CourseId = _courseId,
                Title = "Test Lesson",
                LessonContent = "{}",
                OrderIndex = 0,
                IsLocked = false,
            }
        );
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await _sut.UnlockLessonAsync(lessonId);
        await _sut.UnlockLessonAsync(lessonId);

        // Assert
        var lesson = await _ctx.Lessons.FindAsync(
            new object[] { lessonId },
            TestContext.Current.CancellationToken
        );
        lesson.Should().NotBeNull();
        lesson.IsLocked.Should().BeFalse();
    }

    [Fact]
    public async Task UnlockLessonAsync_InvalidLessonId_DoesNotThrow()
    {
        // Arrange
        var invalidId = Guid.NewGuid().ToString();

        // Act
        var act = async () => await _sut.UnlockLessonAsync(invalidId);

        // Assert
        await act.Should().NotThrowAsync();
    }

    #endregion
}
