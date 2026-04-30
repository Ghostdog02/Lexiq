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
/// Tests for lesson queries: listing lessons and fetching detailed lesson data with includes.
/// </summary>
public class LessonQueryTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private BackendDbContext _ctx = null!;
    private LessonService _sut = null!;

    private string _courseId = null!;
    private string _secondCourseId = null!;
    private string _languageId = null!;

    public LessonQueryTests(DatabaseFixture fixture) => _fixture = fixture;

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
                Description = "Second test course for lesson query tests.",
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

    #region GetLessonsByCourseAsync

    [Fact]
    public async Task GetLessonsByCourseAsync_ValidCourse_ReturnsLessonsOrderedByOrderIndex()
    {
        // Arrange
        var lesson1Id = Guid.NewGuid().ToString();
        var lesson2Id = Guid.NewGuid().ToString();
        var lesson3Id = Guid.NewGuid().ToString();

        // Use _secondCourseId which doesn't have pre-existing lessons
        _ctx.Lessons.AddRange(
            new Lesson
            {
                LessonId =lesson2Id,
                CourseId = _secondCourseId,
                Title = "Lesson 2",
                LessonContent = "{}",
                OrderIndex = 1,
            },
            new Lesson
            {
                LessonId =lesson3Id,
                CourseId = _secondCourseId,
                Title = "Lesson 3",
                LessonContent = "{}",
                OrderIndex = 2,
            },
            new Lesson
            {
                LessonId =lesson1Id,
                CourseId = _secondCourseId,
                Title = "Lesson 1",
                LessonContent = "{}",
                OrderIndex = 0,
            }
        );
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.GetLessonsByCourseAsync(_secondCourseId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
        result![0].LessonId.Should().Be(lesson1Id);
        result[1].LessonId.Should().Be(lesson2Id);
        result[2].LessonId.Should().Be(lesson3Id);
    }

    [Fact]
    public async Task GetLessonsByCourseAsync_InvalidCourseId_ReturnsNull()
    {
        // Arrange
        var invalidId = Guid.NewGuid().ToString();

        // Act
        var result = await _sut.GetLessonsByCourseAsync(invalidId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetLessonsByCourseAsync_EmptyCourse_ReturnsEmptyList()
    {
        // Act
        var result = await _sut.GetLessonsByCourseAsync(_secondCourseId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    #endregion

    #region GetLessonWithDetailsAsync

    [Fact]
    public async Task GetLessonWithDetailsAsync_ValidLesson_IncludesAllRelatedEntities()
    {
        // Arrange
        var lessonId = Guid.NewGuid().ToString();
        var exerciseId = Guid.NewGuid().ToString();
        var optionId = Guid.NewGuid().ToString();

        _ctx.Lessons.Add(
            new Lesson
            {
                LessonId =lessonId,
                CourseId = _courseId,
                Title = "Test Lesson",
                LessonContent = "{}",
                OrderIndex = 0,
            }
        );

        var exercise = new FillInBlankExercise
        {
            ExerciseId = exerciseId,
            LessonId = lessonId,
            Instructions = "Fill in the blank",
            Text = "The answer is ___",
            DifficultyLevel = DifficultyLevel.Beginner,
            Points = 10,
            IsLocked = false,
            Options =
            [
                new ExerciseOption
                {
                    ExerciseId = exerciseId,
                    OptionText = "Option A",
                    IsCorrect = true,
                    Explanation = "Correct answer.",
                },
            ],
        };

        _ctx.Exercises.Add(exercise);
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.GetLessonWithDetailsAsync(lessonId);

        // Assert
        result.Should().NotBeNull();
        result.LessonId.Should().Be(lessonId);
        result.Course.Should().NotBeNull();
        result.Course.CourseId.Should().Be(_courseId);
        result.Course.Language.Should().NotBeNull();
        result.Course.Language.LanguageName.Should().Be("Italian");

        result.Exercises.Should().HaveCount(1);
        var fibExercise = result.Exercises[0] as FillInBlankExercise;
        fibExercise.Should().NotBeNull();
        fibExercise.Options.Should().HaveCount(1);
        fibExercise.Options[0].OptionText.Should().Be("Option A");
    }

    [Fact]
    public async Task GetLessonWithDetailsAsync_InvalidLessonId_ReturnsNull()
    {
        // Arrange
        var invalidId = Guid.NewGuid().ToString();

        // Act
        var result = await _sut.GetLessonWithDetailsAsync(invalidId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetLessonWithDetailsAsync_LessonWithNoExercises_IncludesEmptyExercisesList()
    {
        // Arrange
        var lessonId = Guid.NewGuid().ToString();
        _ctx.Lessons.Add(
            new Lesson
            {
                LessonId =lessonId,
                CourseId = _courseId,
                Title = "Empty Lesson",
                LessonContent = "{}",
                OrderIndex = 0,
            }
        );
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.GetLessonWithDetailsAsync(lessonId);

        // Assert
        result.Should().NotBeNull();
        result.Exercises.Should().NotBeNull();
        result.Exercises.Should().BeEmpty();
    }

    [Fact]
    public async Task GetLessonWithDetailsAsync_MultipleExerciseTypes_LoadsAllTypes()
    {
        // Arrange
        var lessonId = Guid.NewGuid().ToString();
        var fillInBlankId = Guid.NewGuid().ToString();
        var listeningId = Guid.NewGuid().ToString();
        var translationId = Guid.NewGuid().ToString();

        _ctx.Lessons.Add(
            new Lesson
            {
                LessonId =lessonId,
                CourseId = _courseId,
                Title = "Mixed Exercise Lesson",
                LessonContent = "{}",
                OrderIndex = 0,
            }
        );

        _ctx.Exercises.AddRange(
            new FillInBlankExercise
            {
                ExerciseId = fillInBlankId,
                LessonId = lessonId,
                Instructions = "FillInBlank Exercise",
                Text = "Fill ___",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                IsLocked = false,
                Options =
                [
                    new ExerciseOption
                    {
                        ExerciseId = fillInBlankId,
                        OptionText = "blank",
                        IsCorrect = true,
                        Explanation = "Correct answer.",
                    },
                ],
            },
            new ListeningExercise
            {
                ExerciseId = listeningId,
                LessonId = lessonId,
                Instructions = "Listening Exercise",
                AudioUrl = "https://example.com/audio.mp3",
                MaxReplays = 3,
                DifficultyLevel = DifficultyLevel.Intermediate,
                Points = 15,
                IsLocked = false,
                Options =
                [
                    new ExerciseOption
                    {
                        ExerciseId = listeningId,
                        OptionText = "listen",
                        IsCorrect = true,
                        Explanation = "Correct answer.",
                    },
                ],
            }
        );
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.GetLessonWithDetailsAsync(lessonId);

        // Assert
        result.Should().NotBeNull();
        result.Exercises.Should().HaveCount(2);

        var fillInBlank = result.Exercises[0] as FillInBlankExercise;
        fillInBlank.Should().NotBeNull();
        fillInBlank.Instructions.Should().Be("FillInBlank Exercise");

        var listening = result.Exercises[1] as ListeningExercise;
        listening.Should().NotBeNull();
        listening.Instructions.Should().Be("Listening Exercise");
    }

    #endregion
}
