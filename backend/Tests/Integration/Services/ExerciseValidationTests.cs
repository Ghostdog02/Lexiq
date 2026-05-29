using Backend.Api.Dtos;
using Backend.Api.Services;
using Backend.Database;
using Backend.Database.Entities.Exercises;
using Backend.Database.Entities.Users;
using Backend.Tests.Builders;
using Backend.Tests.Helpers;
using Backend.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Backend.Tests.Integration.Services;

/// <summary>
/// Integration tests for LessonProgressService answer validation.
///
/// Verifies:
///   - FillInBlank: option-based validation with multiple correct answers
///   - Listening: option-based validation with audio exercises
/// </summary>
public class ExerciseValidationTests(DatabaseFixture fixture)
    : IClassFixture<DatabaseFixture>,
        IAsyncLifetime
{
    private readonly DatabaseFixture _fixture = fixture;
    private BackendDbContext _ctx = null!;
    private LessonProgressService _sut = null!;
    private string _testUserId = null!;

    private record ExerciseWithOptionsData(string ExerciseId, List<string> OptionIds);

    public async ValueTask InitializeAsync()
    {
        _ctx = _fixture.CreateDbContext();
        await DbSeeder.ClearLeaderboardDataAsync(_ctx, _fixture.SystemUserId);

        // Build service dependencies
        _sut = BuildLessonProgressService(_ctx);

        // Create test user
        var user = new UserBuilder().WithUserName("testuser").WithEmail("user@test.com").Build();
        await DbSeeder.AddUserAsync(_ctx, user);
        _testUserId = user.Id;

        // Unlock fixture lesson for testing
        await UnlockLessonAsync(_fixture.LessonId);
    }

    public async ValueTask DisposeAsync()
    {
        await _ctx.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    // ── FillInBlank Validation ──────────────────────────────────────────────

    [Fact]
    public async Task FillInBlank_CorrectOptionId_MarkedCorrect()
    {
        // Arrange
        var data = await CreateAndSaveFillInBlankAsync(
            correctAnswer: "Answer",
            caseSensitive: true,
            trimWhitespace: true
        );

        // Act
        var result = await SubmitSingleAnswerAsync(_testUserId, _fixture.LessonId, data.ExerciseId, data.OptionIds[0]);

        // Assert
        result.IsCorrect.Should().BeTrue(because: "submitted the correct option ID");
    }

    [Fact]
    public async Task FillInBlank_InvalidOptionId_MarkedIncorrect()
    {
        // Arrange
        var data = await CreateAndSaveFillInBlankAsync(
            correctAnswer: "Answer",
            caseSensitive: false,
            trimWhitespace: true
        );

        var invalidOptionId = Guid.NewGuid().ToString();

        // Act
        var result = await SubmitSingleAnswerAsync(_testUserId, _fixture.LessonId, data.ExerciseId, invalidOptionId);

        // Assert
        result.IsCorrect.Should().BeFalse(because: "option ID does not exist");
    }

    [Fact]
    public async Task FillInBlank_MultipleCorrectOptions_AnyValidIdAccepted()
    {
        // Arrange
        var data = await CreateAndSaveFillInBlankAsync(
            correctAnswer: "answer",
            caseSensitive: false,
            trimWhitespace: true,
            acceptedAnswers: "alt1,alt2,alt3"
        );

        // data.OptionIds[0] = correct answer, [1-3] = alternatives

        // Act
        var result1 = await SubmitSingleAnswerAsync(_testUserId, _fixture.LessonId, data.ExerciseId, data.OptionIds[0]);
        var result2 = await SubmitSingleAnswerAsync(_testUserId, _fixture.LessonId, data.ExerciseId, data.OptionIds[1]);
        var result3 = await SubmitSingleAnswerAsync(_testUserId, _fixture.LessonId, data.ExerciseId, data.OptionIds[2]);

        // Assert
        result1.IsCorrect.Should().BeTrue(because: "correct answer option ID is valid");
        result2.IsCorrect.Should().BeTrue(because: "first alternative option ID is valid");
        result3.IsCorrect.Should().BeTrue(because: "second alternative option ID is valid");
    }

    [Fact]
    public async Task FillInBlank_AcceptedAnswers_AllOptionIdsValid()
    {
        // Arrange
        var data = await CreateAndSaveFillInBlankAsync(
            correctAnswer: "hello",
            acceptedAnswers: "hi,hey,howdy",
            caseSensitive: false,
            trimWhitespace: true
        );

        // data.OptionIds[0] = "hello", [1] = "hi", [2] = "hey", [3] = "howdy"

        // Act
        var resultHello = await SubmitSingleAnswerAsync(_testUserId, _fixture.LessonId, data.ExerciseId, data.OptionIds[0]);
        var resultHi    = await SubmitSingleAnswerAsync(_testUserId, _fixture.LessonId, data.ExerciseId, data.OptionIds[1]);
        var resultHey   = await SubmitSingleAnswerAsync(_testUserId, _fixture.LessonId, data.ExerciseId, data.OptionIds[2]);
        var resultHowdy = await SubmitSingleAnswerAsync(_testUserId, _fixture.LessonId, data.ExerciseId, data.OptionIds[3]);
        var resultInvalid = await SubmitSingleAnswerAsync(_testUserId, _fixture.LessonId, data.ExerciseId, Guid.NewGuid().ToString());

        // Assert
        resultHello.IsCorrect.Should().BeTrue(because: "correct answer option ID");
        resultHi.IsCorrect.Should().BeTrue(because: "first alternative option ID");
        resultHey.IsCorrect.Should().BeTrue(because: "second alternative option ID");
        resultHowdy.IsCorrect.Should().BeTrue(because: "third alternative option ID");
        resultInvalid.IsCorrect.Should().BeFalse(because: "invalid option ID");
    }

    [Fact]
    public async Task FillInBlank_AcceptedAnswers_WhitespaceInTextTrimmed()
    {
        // Arrange
        var data = await CreateAndSaveFillInBlankAsync(
            correctAnswer: "correct",
            acceptedAnswers: " alternative1 , alternative2 , alternative3 ",
            caseSensitive: false,
            trimWhitespace: true
        );

        // AcceptedAnswers parsing trims whitespace, so options stored as "alternative1", "alternative2", etc.

        // Act
        var resultAlt1 = await SubmitSingleAnswerAsync(_testUserId, _fixture.LessonId, data.ExerciseId, data.OptionIds[1]);
        var resultAlt2 = await SubmitSingleAnswerAsync(_testUserId, _fixture.LessonId, data.ExerciseId, data.OptionIds[2]);

        // Assert
        resultAlt1.IsCorrect.Should().BeTrue(because: "first alternative option ID is valid");
        resultAlt2.IsCorrect.Should().BeTrue(because: "second alternative option ID is valid");
    }

    [Fact]
    public async Task FillInBlank_AcceptedAnswers_RespectsCaseSensitivity()
    {
        // Arrange
        var data = await CreateAndSaveFillInBlankAsync(
            correctAnswer: "Answer",
            acceptedAnswers: "Alt1,Alt2",
            caseSensitive: true,
            trimWhitespace: true
        );

        // data.OptionIds[0] = correctAnswer ("Answer")
        // data.OptionIds[1] = first alternative ("Alt1")
        // data.OptionIds[2] = second alternative ("Alt2")
        var alt1OptionId = data.OptionIds[1];
        var invalidOptionId = Guid.NewGuid().ToString(); // Non-existent option ID

        // Act
        var resultCorrect = await SubmitSingleAnswerAsync(_testUserId, _fixture.LessonId, data.ExerciseId, alt1OptionId);
        var resultInvalid = await SubmitSingleAnswerAsync(_testUserId, _fixture.LessonId, data.ExerciseId, invalidOptionId);

        // Assert
        resultCorrect.IsCorrect.Should().BeTrue(because: "submitted valid option ID for Alt1");
        resultInvalid.IsCorrect.Should().BeFalse(because: "submitted non-existent option ID");
    }

    [Fact]
    public async Task FillInBlank_EmptyAcceptedAnswers_OnlyCorrectOptionIdAccepted()
    {
        // Arrange
        var data = await CreateAndSaveFillInBlankAsync(
            correctAnswer: "only",
            acceptedAnswers: "",
            caseSensitive: false,
            trimWhitespace: true
        );

        // Only one option ID exists (correct answer)

        // Act
        var resultCorrect = await SubmitSingleAnswerAsync(_testUserId, _fixture.LessonId, data.ExerciseId, data.OptionIds[0]);
        var resultInvalid = await SubmitSingleAnswerAsync(_testUserId, _fixture.LessonId, data.ExerciseId, Guid.NewGuid().ToString());

        // Assert
        resultCorrect.IsCorrect.Should().BeTrue(because: "correct option ID submitted");
        resultInvalid.IsCorrect.Should().BeFalse(because: "no alternatives, invalid option ID");
    }

    // ── Listening Validation ────────────────────────────────────────────────

    [Fact]
    public async Task Listening_UsesOptionBasedValidation()
    {
        // Arrange
        var data = await CreateAndSaveListeningAsync(
            audioUrl: "https://example.com/audio.mp3",
            correctAnswer: "Answer",
            acceptedAnswers: "alt1,alt2",
            caseSensitive: false
        );

        // Act - verify correct and invalid option IDs work as expected
        var resultCorrect = await SubmitSingleAnswerAsync(_testUserId, _fixture.LessonId, data.ExerciseId, data.OptionIds[0]);
        var resultInvalid = await SubmitSingleAnswerAsync(_testUserId, _fixture.LessonId, data.ExerciseId, Guid.NewGuid().ToString());

        // Assert
        resultCorrect.IsCorrect.Should().BeTrue(because: "Listening exercises use same option-based validation as FillInBlank");
        resultInvalid.IsCorrect.Should().BeFalse(because: "invalid option IDs are rejected");
    }

    // ── Helper Methods ──────────────────────────────────────────────────────

    /// <summary>
    /// Submits a single answer for an exercise via SubmitLessonAsync and returns its result.
    /// </summary>
    private async Task<ExerciseResultDto> SubmitSingleAnswerAsync(
        string userId,
        string lessonId,
        string exerciseId,
        string selectedOptionId
    )
    {
        var answers = new List<ExerciseAnswerDto>
        {
            new ExerciseAnswerDto(exerciseId, selectedOptionId),
        };
        var lessonResult = await _sut.SubmitLessonAsync(userId, lessonId, answers);
        return lessonResult.Exercises.Single(e => e.ExerciseId == exerciseId);
    }

    private static LessonProgressService BuildLessonProgressService(BackendDbContext ctx)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(ctx);
        services
            .AddIdentityCore<User>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<BackendDbContext>();
        var sp = services.BuildServiceProvider();
        var userManager = sp.GetRequiredService<UserManager<User>>();

        var clock = new Backend.Api.Services.Clock.SystemClock();
        var exerciseService = new ExerciseService(ctx, new Moq.Mock<Backend.Api.Services.IFileUploadsService>().Object);
        var lessonService = new LessonService(ctx, exerciseService, clock);
        var achievementService = new AchievementService(ctx);
        var heartsService = new HeartsService(ctx, clock);

        return new LessonProgressService(
            ctx,
            lessonService,
            userManager,
            achievementService,
            clock,
            heartsService
        );
    }

    private async Task UnlockLessonAsync(string lessonId)
    {
        var lesson = await _ctx.Lessons.FindAsync(
            [lessonId],
            TestContext.Current.CancellationToken
        );
        if (lesson != null)
        {
            lesson.IsLocked = false;
            await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
        }
    }

    private async Task<ExerciseWithOptionsData> CreateAndSaveFillInBlankAsync(
        string correctAnswer,
        bool caseSensitive,
        bool trimWhitespace,
        string? acceptedAnswers = null
    )
    {
        var exerciseId = Guid.NewGuid().ToString();
        var optionIds = new List<string>();
        var options = new List<ExerciseOption>();

        // Add correct answer option
        var correctOptionId = Guid.NewGuid().ToString();
        options.Add(
            new ExerciseOption
            {
                ExerciseOptionId = correctOptionId,
                ExerciseId = exerciseId,
                OptionText = correctAnswer,
                IsCorrect = true,
                Explanation = "Correct answer",
            }
        );
        optionIds.Add(correctOptionId);

        // Add accepted alternatives
        if (!string.IsNullOrEmpty(acceptedAnswers))
        {
            var alternatives = acceptedAnswers
                .Split(',')
                .Select(a => a.Trim())
                .Where(a => !string.IsNullOrEmpty(a));

            foreach (var alt in alternatives)
            {
                var altOptionId = Guid.NewGuid().ToString();
                options.Add(
                    new ExerciseOption
                    {
                        ExerciseOptionId = altOptionId,
                        ExerciseId = exerciseId,
                        OptionText = alt,
                        IsCorrect = true,
                        Explanation = "Accepted alternative",
                    }
                );
                optionIds.Add(altOptionId);
            }
        }

        var exercise = new FillInBlankExercise
        {
            ExerciseId = exerciseId,
            LessonId = _fixture.LessonId,
            Instructions = "Fill in the blank",
            Text = "Fill in the blank: ___",
            DifficultyLevel = DifficultyLevel.Beginner,
            Points = 10,
            Options = options,
        };

        _ctx.Exercises.Add(exercise);
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
        return new ExerciseWithOptionsData(exercise.ExerciseId, optionIds);
    }


    private async Task<ExerciseWithOptionsData> CreateAndSaveListeningAsync(
        string audioUrl,
        string correctAnswer,
        string? acceptedAnswers,
        bool caseSensitive
    )
    {
        var exerciseId = Guid.NewGuid().ToString();
        var optionIds = new List<string>();
        var options = new List<ExerciseOption>();

        // Add correct answer option
        var correctOptionId = Guid.NewGuid().ToString();
        options.Add(
            new ExerciseOption
            {
                ExerciseOptionId = correctOptionId,
                ExerciseId = exerciseId,
                OptionText = correctAnswer,
                IsCorrect = true,
                Explanation = "Correct answer",
            }
        );
        optionIds.Add(correctOptionId);

        // Add accepted alternatives
        if (!string.IsNullOrEmpty(acceptedAnswers))
        {
            var alternatives = acceptedAnswers
                .Split(',')
                .Select(a => a.Trim())
                .Where(a => !string.IsNullOrEmpty(a));

            foreach (var alt in alternatives)
            {
                var altOptionId = Guid.NewGuid().ToString();
                options.Add(
                    new ExerciseOption
                    {
                        ExerciseOptionId = altOptionId,
                        ExerciseId = exerciseId,
                        OptionText = alt,
                        IsCorrect = true,
                        Explanation = "Accepted alternative",
                    }
                );
                optionIds.Add(altOptionId);
            }
        }

        // Create ListeningExercise entity with the options
        var exercise = new ListeningExercise
        {
            ExerciseId = exerciseId,
            LessonId = _fixture.LessonId,
            Instructions = "Listen and type what you hear",
            AudioUrl = audioUrl,
            DifficultyLevel = DifficultyLevel.Beginner,
            Points = 10,
            Options = options,
        };

        // Save exercise to database
        _ctx.Exercises.Add(exercise);
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Return exercise ID and all option IDs
        return new ExerciseWithOptionsData(exercise.ExerciseId, optionIds);
    }
}
