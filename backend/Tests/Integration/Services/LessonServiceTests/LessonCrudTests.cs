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
/// Tests for lesson CRUD operations: Create, Update, and Delete.
/// </summary>
public class LessonCrudTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private BackendDbContext _ctx = null!;
    private LessonService _sut = null!;

    private string _courseId = null!;
    private string _secondCourseId = null!;
    private string _languageId = null!;

    public LessonCrudTests(DatabaseFixture fixture) => _fixture = fixture;

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
                Description = "Second test course for lesson CRUD tests.",
                CreatedById = _fixture.SystemUserId,
                OrderIndex = 1,
            }
        );

        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _ctx.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    #region CreateLessonAsync

    [Fact]
    public async Task CreateLessonAsync_WithAutoOrderIndex_CreatesLessonWithCorrectIndex()
    {
        // Arrange
        var existingLessonId = Guid.NewGuid().ToString();
        _ctx.Lessons.Add(
            new Lesson
            {
                LessonId = existingLessonId,
                CourseId = _courseId,
                Title = "Existing Lesson",
                LessonContent = "{}",
                OrderIndex = 0,
            }
        );
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var dto = new CreateLessonDto(
            CourseId: _courseId,
            Title: "New Lesson",
            Description: "Test description",
            EstimatedDurationMinutes: 30,
            OrderIndex: null, // Auto-calculate
            Content: "{\"blocks\":[]}"
        );

        // Act
        var result = await _sut.CreateLessonAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("New Lesson");
        result.OrderIndex.Should().Be(1, "should be next after existing lesson");
        result.IsLocked.Should().BeTrue("new lessons default to locked");
        result.LessonContent.Should().Be("{\"blocks\":[]}");
    }

    [Fact]
    public async Task CreateLessonAsync_WithSpecifiedOrderIndex_UsesProvidedIndex()
    {
        // Arrange
        var dto = new CreateLessonDto(
            CourseId: _courseId,
            Title: "Custom Order Lesson",
            Description: null,
            EstimatedDurationMinutes: null,
            OrderIndex: 5,
            Content: "{}"
        );

        // Act
        var result = await _sut.CreateLessonAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.OrderIndex.Should().Be(5);
    }

    [Fact]
    public async Task CreateLessonAsync_WithExercises_CreatesLessonAndExercises()
    {
        // Arrange
        var exercises = new List<CreateExerciseDto>
        {
            new CreateFillInBlankExerciseDto(
                LessonId: null,
                Instructions: "Exercise 1",
                DifficultyLevel: DifficultyLevel.Beginner,
                Points: 10,
                Explanation: null,
                Text: "Fill in: The cat is ___",
                Options:
                [
                    new CreateExerciseOptionDto(
                        "meowing",
                        IsCorrect: true,
                        Explanation: "Correct answer."
                    ),
                    new CreateExerciseOptionDto(
                        "sleeping",
                        IsCorrect: false,
                        Explanation: "Wrong answer."
                    ),
                ]
            ),
            new CreateFillInBlankExerciseDto(
                LessonId: null,
                Instructions: "Exercise 2",
                DifficultyLevel: DifficultyLevel.Intermediate,
                Points: 15,
                Explanation: null,
                Text: "Fill in: The dog is ___",
                Options:
                [
                    new CreateExerciseOptionDto(
                        "barking",
                        IsCorrect: true,
                        Explanation: "Correct answer."
                    ),
                    new CreateExerciseOptionDto(
                        "quiet",
                        IsCorrect: false,
                        Explanation: "Wrong answer."
                    ),
                ]
            ),
        };

        var dto = new CreateLessonDto(
            CourseId: _courseId,
            Title: "Lesson with Exercises",
            Description: "Test",
            EstimatedDurationMinutes: 45,
            OrderIndex: null,
            Content: "{}",
            Exercises: exercises
        );

        // Act
        var result = await _sut.CreateLessonAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Lesson with Exercises");

        var createdExercises = await _ctx
            .Exercises.Where(e => e.LessonId == result.LessonId)
            .OrderBy(e => e.CreatedAt)
            .ToListAsync(cancellationToken: TestContext.Current.CancellationToken);

        createdExercises.Should().HaveCount(2);
        createdExercises[0].IsLocked.Should().BeFalse("first exercise should be unlocked");
        createdExercises[1].IsLocked.Should().BeTrue("second exercise should be locked");
        createdExercises[0].Instructions.Should().Be("Exercise 1");
        createdExercises[1].Instructions.Should().Be("Exercise 2");
    }

    [Fact]
    public async Task CreateLessonAsync_InvalidCourseId_ThrowsArgumentException()
    {
        // Arrange
        var invalidCourseId = Guid.NewGuid().ToString();
        var dto = new CreateLessonDto(
            CourseId: invalidCourseId,
            Title: "Test Lesson",
            Description: null,
            EstimatedDurationMinutes: null,
            OrderIndex: null,
            Content: "{}"
        );

        // Act
        var act = async () => await _sut.CreateLessonAsync(dto);

        // Assert
        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage($"Course with ID '{invalidCourseId}' not found.");
    }

    [Fact]
    public async Task CreateLessonAsync_EmptyCourse_StartsAtOrderIndexZero()
    {
        // Arrange
        var dto = new CreateLessonDto(
            CourseId: _secondCourseId,
            Title: "First Lesson",
            Description: null,
            EstimatedDurationMinutes: null,
            OrderIndex: null,
            Content: "{}"
        );

        // Act
        var result = await _sut.CreateLessonAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.OrderIndex.Should().Be(0);
    }

    #endregion

    #region UpdateLessonAsync

    [Fact]
    public async Task UpdateLessonAsync_AllProperties_UpdatesSuccessfully()
    {
        // Arrange
        var lessonId = Guid.NewGuid().ToString();
        _ctx.Lessons.Add(
            new Lesson
            {
                LessonId = lessonId,
                CourseId = _courseId,
                Title = "Original Title",
                EstimatedDurationMinutes = 30,
                OrderIndex = 0,
                LessonContent = "{}",
            }
        );
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var dto = new UpdateLessonDto(
            CourseId: _secondCourseId,
            Title: "Updated Title",
            Description: "Updated Description",
            EstimatedDurationMinutes: 45,
            OrderIndex: 5,
            LessonContent: "{\"updated\":true}"
        );

        // Act
        var result = await _sut.UpdateLessonAsync(lessonId, dto);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Updated Title");
        result.EstimatedDurationMinutes.Should().Be(45);
        result.OrderIndex.Should().Be(5);
        result.CourseId.Should().Be(_secondCourseId);
        result.LessonContent.Should().Be("{\"updated\":true}");
    }

    [Fact]
    public async Task UpdateLessonAsync_PartialUpdate_UpdatesOnlyProvidedFields()
    {
        // Arrange
        var lessonId = Guid.NewGuid().ToString();
        _ctx.Lessons.Add(
            new Lesson
            {
                LessonId = lessonId,
                CourseId = _courseId,
                Title = "Original Title",
                EstimatedDurationMinutes = 30,
                OrderIndex = 0,
                LessonContent = "{}",
            }
        );
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var dto = new UpdateLessonDto(
            CourseId: null,
            Title: "Updated Title",
            Description: null,
            EstimatedDurationMinutes: null,
            OrderIndex: null,
            LessonContent: null
        );

        // Act
        var result = await _sut.UpdateLessonAsync(lessonId, dto);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Updated Title");
        result.EstimatedDurationMinutes.Should().Be(30, "should not change");
        result.OrderIndex.Should().Be(0, "should not change");
        result.CourseId.Should().Be(_courseId, "should not change");
    }

    [Fact]
    public async Task UpdateLessonAsync_InvalidLessonId_ReturnsNull()
    {
        // Arrange
        var invalidId = Guid.NewGuid().ToString();
        var dto = new UpdateLessonDto(
            CourseId: null,
            Title: "Updated",
            Description: null,
            EstimatedDurationMinutes: null,
            OrderIndex: null,
            LessonContent: null
        );

        // Act
        var result = await _sut.UpdateLessonAsync(invalidId, dto);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateLessonAsync_InvalidCourseId_ThrowsArgumentException()
    {
        // Arrange
        var lessonId = Guid.NewGuid().ToString();
        _ctx.Lessons.Add(
            new Lesson
            {
                LessonId = lessonId,
                CourseId = _courseId,
                Title = "Test",
                LessonContent = "{}",
                OrderIndex = 0,
            }
        );
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var invalidCourseId = Guid.NewGuid().ToString();
        var dto = new UpdateLessonDto(
            CourseId: invalidCourseId,
            Title: null,
            Description: null,
            EstimatedDurationMinutes: null,
            OrderIndex: null,
            LessonContent: null
        );

        // Act
        var act = async () => await _sut.UpdateLessonAsync(lessonId, dto);

        // Assert
        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage($"Course with ID '{invalidCourseId}' not found.");
    }

    #endregion

    #region DeleteLessonAsync

    [Fact]
    public async Task DeleteLessonAsync_ValidLesson_DeletesAndReturnsTrue()
    {
        // Arrange
        var lessonId = Guid.NewGuid().ToString();
        _ctx.Lessons.Add(
            new Lesson
            {
                LessonId = lessonId,
                CourseId = _courseId,
                Title = "To Delete",
                LessonContent = "{}",
                OrderIndex = 0,
            }
        );
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.DeleteLessonAsync(lessonId);

        // Assert
        result.Should().BeTrue();

        var deleted = await _ctx.Lessons.FindAsync(
            new object[] { lessonId },
            TestContext.Current.CancellationToken
        );
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteLessonAsync_InvalidLessonId_ReturnsFalse()
    {
        // Arrange
        var invalidId = Guid.NewGuid().ToString();

        // Act
        var result = await _sut.DeleteLessonAsync(invalidId);

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
