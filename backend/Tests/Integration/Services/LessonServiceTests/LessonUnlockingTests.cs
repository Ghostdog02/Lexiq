using Backend.Api.Dtos;
using Backend.Api.Services;
using Backend.Database;
using Backend.Database.Entities;
using Backend.Database.Entities.Exercises;
using Backend.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend.Tests.Services.LessonServiceTests;

/// <summary>
/// Tests for lesson unlocking: automatic and manual unlocking with exercise cascade behavior.
/// </summary>
public class LessonUnlockingTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private BackendDbContext _ctx = null!;
    private LessonService _sut = null!;

    private string _courseId = null!;
    private string _secondCourseId = null!;
    private string _thirdCourseId = null!;
    private string _languageId = null!;

    public LessonUnlockingTests(DatabaseFixture fixture) => _fixture = fixture;

    public async ValueTask InitializeAsync()
    {
        _ctx = _fixture.CreateDbContext();

        var exerciseService = new ExerciseService(_ctx);
        _sut = new LessonService(_ctx, exerciseService);

        var language = await _ctx.Languages.FirstAsync(TestContext.Current.CancellationToken);
        _languageId = language.LanguageId;

        var course = await _ctx.Courses.FirstAsync(TestContext.Current.CancellationToken);
        _courseId = course.CourseId;

        _secondCourseId = Guid.NewGuid().ToString();
        _ctx.Courses.Add(
            new Course
            {
                CourseId = _secondCourseId,
                LanguageId = _languageId,
                Title = "Second Course",
                Description = "Second test course for lesson unlocking tests.",
                CreatedById = _fixture.SystemUserId,
                OrderIndex = 1,
            }
        );

        _thirdCourseId = Guid.NewGuid().ToString();
        _ctx.Courses.Add(
            new Course
            {
                CourseId = _thirdCourseId,
                LanguageId = _languageId,
                Title = "Third Course",
                Description = "Third test course for lesson unlocking tests.",
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
                LessonId = firstLessonId,
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
                LessonId = secondLessonId,
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
                ExerciseId = exerciseId,
                LessonId = secondLessonId,
                Instructions = "Exercise 1",
                Text = "Test",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                IsLocked = true,
                Options =
                [
                    new ExerciseOption
                    {
                        ExerciseId = exerciseId,
                        OptionText = "answer",
                        IsCorrect = true,
                        Explanation = "Correct answer explanation.",
                    },
                ],
            }
        );
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.UnlockNextLessonAsync(firstLessonId);

        // Assert
        result.Should().Be(UnlockStatus.Unlocked);

        var unlockedLesson = await _ctx.Lessons.FindAsync(
            [secondLessonId],
            TestContext.Current.CancellationToken
        );
        unlockedLesson.Should().NotBeNull();
        unlockedLesson.IsLocked.Should().BeFalse();

        var unlockedExercise = await _ctx.Exercises.FindAsync(
            [exerciseId],
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
                LessonId = firstLessonId,
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
                LessonId = secondLessonId,
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
                LessonId = lastLessonId,
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
                LessonId = lessonId,
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
                ExerciseId = exercise1Id,
                LessonId = lessonId,
                Instructions = "Exercise 1",
                Text = "Test",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                IsLocked = true,
                Options =
                [
                    new ExerciseOption
                    {
                        ExerciseId = exercise1Id,
                        OptionText = "answer",
                        IsCorrect = true,
                        Explanation = "Correct answer explanation.",
                    },
                ],
            },
            new FillInBlankExercise
            {
                ExerciseId = exercise2Id,
                LessonId = lessonId,
                Instructions = "Exercise 2",
                Text = "Test",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                IsLocked = true,
                Options =
                [
                    new ExerciseOption
                    {
                        ExerciseId = exercise2Id,
                        OptionText = "answer",
                        IsCorrect = true,
                        Explanation = "Correct answer explanation.",
                    },
                ],
            }
        );
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        await _sut.UnlockLessonAsync(lessonId);

        // Assert
        var lesson = await _ctx.Lessons.FindAsync(
            [lessonId],
            TestContext.Current.CancellationToken
        );
        lesson.Should().NotBeNull();
        lesson.IsLocked.Should().BeFalse();

        var exercise1 = await _ctx.Exercises.FindAsync(
            [exercise1Id],
            TestContext.Current.CancellationToken
        );
        exercise1.Should().NotBeNull();
        exercise1.IsLocked.Should().BeFalse();

        var exercise2 = await _ctx.Exercises.FindAsync(
            [exercise2Id],
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
                LessonId = lessonId,
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
            [lessonId],
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
