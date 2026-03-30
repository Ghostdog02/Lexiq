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
/// Tests for lesson queries: listing lessons and fetching detailed lesson data with includes.
/// </summary>
public class LessonQueryTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly DatabaseFixture _fixture = fixture;
    private BackendDbContext _ctx = null!;
    private LessonService _sut = null!;

    private string _courseId = null!;
    private string _secondCourseId = null!;
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
                Id = lesson2Id,
                CourseId = _secondCourseId,
                Title = "Lesson 2",
                LessonContent = "{}",
                OrderIndex = 1,
            },
            new Lesson
            {
                Id = lesson3Id,
                CourseId = _secondCourseId,
                Title = "Lesson 3",
                LessonContent = "{}",
                OrderIndex = 2,
            },
            new Lesson
            {
                Id = lesson1Id,
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
        result![0].Id.Should().Be(lesson1Id);
        result[1].Id.Should().Be(lesson2Id);
        result[2].Id.Should().Be(lesson3Id);
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
                Id = lessonId,
                CourseId = _courseId,
                Title = "Test Lesson",
                LessonContent = "{}",
                OrderIndex = 0,
            }
        );

        var exercise = new MultipleChoiceExercise
        {
            Id = exerciseId,
            LessonId = lessonId,
            Title = "MC Exercise",
            Instructions = "What is the answer?",
            DifficultyLevel = DifficultyLevel.Beginner,
            Points = 10,
            OrderIndex = 0,
            Options = new List<ExerciseOption>
            {
                new()
                {
                    Id = optionId,
                    ExerciseId = exerciseId,
                    OptionText = "Option A",
                    IsCorrect = true,
                },
            },
        };

        _ctx.Exercises.Add(exercise);
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.GetLessonWithDetailsAsync(lessonId);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(lessonId);
        result.Course.Should().NotBeNull();
        result.Course.Id.Should().Be(_courseId);
        result.Course.Language.Should().NotBeNull();
        result.Course.Language.Name.Should().Be("Italian");

        result.Exercises.Should().HaveCount(1);
        var mcExercise = result.Exercises[0] as MultipleChoiceExercise;
        mcExercise.Should().NotBeNull();
        mcExercise.Options.Should().HaveCount(1);
        mcExercise.Options[0].OptionText.Should().Be("Option A");
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
                Id = lessonId,
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
                Id = lessonId,
                CourseId = _courseId,
                Title = "Mixed Exercise Lesson",
                LessonContent = "{}",
                OrderIndex = 0,
            }
        );

        _ctx.Exercises.AddRange(
            new FillInBlankExercise
            {
                Id = fillInBlankId,
                LessonId = lessonId,
                Title = "FillInBlank Exercise",
                Text = "Fill ___",
                CorrectAnswer = "blank",
                CaseSensitive = false,
                TrimWhitespace = true,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                OrderIndex = 0,
            },
            new ListeningExercise
            {
                Id = listeningId,
                LessonId = lessonId,
                Title = "Listening Exercise",
                AudioUrl = "https://example.com/audio.mp3",
                CorrectAnswer = "listen",
                CaseSensitive = false,
                MaxReplays = 3,
                DifficultyLevel = DifficultyLevel.Intermediate,
                Points = 15,
                OrderIndex = 1,
            },
            new TranslationExercise
            {
                Id = translationId,
                LessonId = lessonId,
                Title = "Translation Exercise",
                SourceText = "Hello",
                TargetText = "Ciao",
                SourceLanguageCode = "en",
                TargetLanguageCode = "it",
                MatchingThreshold = 0.85,
                DifficultyLevel = DifficultyLevel.Advanced,
                Points = 20,
                OrderIndex = 2,
            }
        );
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.GetLessonWithDetailsAsync(lessonId);

        // Assert
        result.Should().NotBeNull();
        result.Exercises.Should().HaveCount(3);

        var fillInBlank = result.Exercises[0] as FillInBlankExercise;
        fillInBlank.Should().NotBeNull();
        fillInBlank.Title.Should().Be("FillInBlank Exercise");

        var listening = result.Exercises[1] as ListeningExercise;
        listening.Should().NotBeNull();
        listening.Title.Should().Be("Listening Exercise");

        var translation = result.Exercises[2] as TranslationExercise;
        translation.Should().NotBeNull();
        translation.Title.Should().Be("Translation Exercise");
    }

    #endregion
}
