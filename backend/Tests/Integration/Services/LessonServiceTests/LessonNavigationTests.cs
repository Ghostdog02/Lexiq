using Backend.Api.Services;
using Backend.Database;
using Backend.Database.Entities;
using Backend.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace Backend.Tests.Integration.Services.LessonServiceTests;

/// <summary>
/// Tests for lesson navigation: finding next lessons, first lessons, and boundary detection.
/// </summary>
public class LessonNavigationTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private BackendDbContext _ctx = null!;
    private LessonService _sut = null!;

    private string _courseId = null!;
    private string _secondCourseId = null!;
    private string _thirdCourseId = null!;
    private string _languageId = null!;

    public async ValueTask InitializeAsync()
    {
        _ctx = fixture.CreateDbContext();

        var exerciseService = new ExerciseService(_ctx, new Moq.Mock<Backend.Api.Services.IFileUploadsService>().Object);
        _sut = new LessonService(_ctx, exerciseService, new Backend.Api.Services.Clock.SystemClock(), new MemoryCache(new MemoryCacheOptions()));

        // Derive course and language from the fixture lesson — immune to leftover courses from other test classes
        var fixtureLesson = await _ctx
            .Lessons.Where(l => l.LessonId == fixture.LessonId)
            .Select(l => new { l.CourseId, l.Course.LanguageId })
            .FirstAsync(TestContext.Current.CancellationToken);
        _courseId = fixtureLesson.CourseId;
        _languageId = fixtureLesson.LanguageId;

        // Clean up test data from previous runs — keep only fixture course and its seeded lesson
        await _ctx
            .Lessons.Where(l => l.LessonId != fixture.LessonId)
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await _ctx
            .Courses.Where(c => c.CourseId != _courseId)
            .ExecuteDeleteAsync(TestContext.Current.CancellationToken);

        _secondCourseId = Guid.NewGuid().ToString();
        _ctx.Courses.Add(
            new Course
            {
                CourseId = _secondCourseId,
                LanguageId = _languageId,
                Title = "Second Course",
                Description = "Second test course for lesson navigation tests.",
                EstimatedDurationHours = 10,
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
                Description = "Third test course for lesson navigation tests.",
                EstimatedDurationHours = 10,
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

    #region GetNextLessonAsync

    [Fact]
    public async Task GetNextLessonAsync_NextLessonInSameCourse_ReturnsNextLesson()
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
            }
        );
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.GetNextLessonAsync(firstLessonId);

        // Assert
        result.Should().NotBeNull();
        result.LessonId.Should().Be(secondLessonId);
        result.Title.Should().Be("Lesson 2");
    }

    [Fact]
    public async Task GetNextLessonAsync_LastLessonInCourse_ReturnsFirstLessonOfNextCourse()
    {
        // Arrange
        var lastLessonSecondCourseId = Guid.NewGuid().ToString();
        var firstLessonThirdCourseId = Guid.NewGuid().ToString();

        // Use _secondCourseId and _thirdCourseId which don't have pre-existing lessons
        _ctx.Lessons.Add(
            new Lesson
            {
                LessonId = lastLessonSecondCourseId,
                CourseId = _secondCourseId,
                Title = "Last Lesson Course 2",
                LessonContent = "{}",
                OrderIndex = 0,
            }
        );
        _ctx.Lessons.Add(
            new Lesson
            {
                LessonId = firstLessonThirdCourseId,
                CourseId = _thirdCourseId,
                Title = "First Lesson Course 3",
                LessonContent = "{}",
                OrderIndex = 0,
            }
        );

        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.GetNextLessonAsync(lastLessonSecondCourseId);

        // Assert
        result.Should().NotBeNull();
        result.LessonId.Should().Be(firstLessonThirdCourseId);
        result.Title.Should().Be("First Lesson Course 3");
    }

    [Fact]
    public async Task GetNextLessonAsync_LastLessonOfLastCourse_ReturnsNull()
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
        var result = await _sut.GetNextLessonAsync(lastLessonId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetNextLessonAsync_InvalidLessonId_ReturnsNull()
    {
        // Arrange
        var invalidId = Guid.NewGuid().ToString();

        // Act
        var result = await _sut.GetNextLessonAsync(invalidId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetNextLessonAsync_SkipsLessonsInBetween_ReturnsNextByOrderIndex()
    {
        // Arrange
        var lesson1Id = Guid.NewGuid().ToString();
        var lesson3Id = Guid.NewGuid().ToString();

        _ctx.Lessons.Add(
            new Lesson
            {
                LessonId = lesson1Id,
                CourseId = _courseId,
                Title = "Lesson 1",
                LessonContent = "{}",
                OrderIndex = 0,
            }
        );
        _ctx.Lessons.Add(
            new Lesson
            {
                LessonId = lesson3Id,
                CourseId = _courseId,
                Title = "Lesson 3",
                LessonContent = "{}",
                OrderIndex = 2, // Gap at OrderIndex 1
            }
        );
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.GetNextLessonAsync(lesson1Id);

        // Assert
        result.Should().NotBeNull();
        result.LessonId.Should().Be(lesson3Id);
        result.OrderIndex.Should().Be(2);
    }

    #endregion

    #region IsLastLessonInCourseAsync

    [Fact]
    public async Task IsLastLessonInCourseAsync_LastLesson_ReturnsTrue()
    {
        // Arrange
        var lesson1Id = Guid.NewGuid().ToString();
        var lesson2Id = Guid.NewGuid().ToString();

        _ctx.Lessons.Add(
            new Lesson
            {
                LessonId = lesson1Id,
                CourseId = _courseId,
                Title = "Lesson 1",
                LessonContent = "{}",
                OrderIndex = 0,
            }
        );
        _ctx.Lessons.Add(
            new Lesson
            {
                LessonId = lesson2Id,
                CourseId = _courseId,
                Title = "Lesson 2",
                LessonContent = "{}",
                OrderIndex = 1,
            }
        );
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.IsLastLessonInCourseAsync(lesson2Id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task IsLastLessonInCourseAsync_NotLastLesson_ReturnsFalse()
    {
        // Arrange
        var lesson1Id = Guid.NewGuid().ToString();
        var lesson2Id = Guid.NewGuid().ToString();

        _ctx.Lessons.Add(
            new Lesson
            {
                LessonId = lesson1Id,
                CourseId = _courseId,
                Title = "Lesson 1",
                LessonContent = "{}",
                OrderIndex = 0,
            }
        );
        _ctx.Lessons.Add(
            new Lesson
            {
                LessonId = lesson2Id,
                CourseId = _courseId,
                Title = "Lesson 2",
                LessonContent = "{}",
                OrderIndex = 1,
            }
        );
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.IsLastLessonInCourseAsync(lesson1Id);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsLastLessonInCourseAsync_InvalidLessonId_ReturnsFalse()
    {
        // Arrange
        var invalidId = Guid.NewGuid().ToString();

        // Act
        var result = await _sut.IsLastLessonInCourseAsync(invalidId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task IsLastLessonInCourseAsync_OnlyLessonInCourse_ReturnsTrue()
    {
        // Arrange
        var lessonId = Guid.NewGuid().ToString();
        _ctx.Lessons.Add(
            new Lesson
            {
                LessonId = lessonId,
                CourseId = _secondCourseId,
                Title = "Only Lesson",
                LessonContent = "{}",
                OrderIndex = 0,
            }
        );
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.IsLastLessonInCourseAsync(lessonId);

        // Assert
        result.Should().BeTrue();
    }

    #endregion

    #region GetFirstLessonInCourseAsync

    [Fact]
    public async Task GetFirstLessonInCourseAsync_ValidCourse_ReturnsFirstLessonByOrderIndex()
    {
        // Arrange
        var lesson1Id = Guid.NewGuid().ToString();
        var lesson2Id = Guid.NewGuid().ToString();

        // Use _secondCourseId which doesn't have pre-existing lessons
        _ctx.Lessons.Add(
            new Lesson
            {
                LessonId = lesson2Id,
                CourseId = _secondCourseId,
                Title = "Second Lesson",
                LessonContent = "{}",
                OrderIndex = 1,
            }
        );
        _ctx.Lessons.Add(
            new Lesson
            {
                LessonId = lesson1Id,
                CourseId = _secondCourseId,
                Title = "First Lesson",
                LessonContent = "{}",
                OrderIndex = 0,
            }
        );
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.GetFirstLessonInCourseAsync(_secondCourseId);

        // Assert
        result.Should().NotBeNull();
        result.LessonId.Should().Be(lesson1Id);
        result.OrderIndex.Should().Be(0);
    }

    [Fact]
    public async Task GetFirstLessonInCourseAsync_InvalidCourseId_ReturnsNull()
    {
        // Arrange
        var invalidId = Guid.NewGuid().ToString();

        // Act
        var result = await _sut.GetFirstLessonInCourseAsync(invalidId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetFirstLessonInCourseAsync_EmptyCourse_ReturnsNull()
    {
        // Act
        var result = await _sut.GetFirstLessonInCourseAsync(_secondCourseId);

        // Assert
        result.Should().BeNull();
    }

    #endregion
}
