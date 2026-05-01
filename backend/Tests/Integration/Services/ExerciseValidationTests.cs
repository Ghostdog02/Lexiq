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

namespace Backend.Tests.Services;

/// <summary>
/// Integration tests for ExerciseProgressService answer validation.
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
    private ExerciseProgressService _sut = null!;
    private string _testUserId = null!;

    public async ValueTask InitializeAsync()
    {
        _ctx = _fixture.CreateDbContext();
        await DbSeeder.ClearLeaderboardDataAsync(_ctx, _fixture.SystemUserId);

        // Build service dependencies
        _sut = BuildExerciseProgressService(_ctx);

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
    public async Task FillInBlank_CaseSensitiveTrue_RejectsWrongCase()
    {
        // Arrange
        var exerciseId = await CreateAndSaveFillInBlankAsync(
            correctAnswer: "Answer",
            caseSensitive: true,
            trimWhitespace: true
        );

        // Act
        var resultLower = await _sut.SubmitAnswerAsync(_testUserId, exerciseId, "answer");
        var resultCorrect = await _sut.SubmitAnswerAsync(_testUserId, exerciseId, "Answer");

        // Assert
        resultLower
            .IsCorrect.Should()
            .BeFalse(
                because: "case-sensitive validation rejects lowercase when correct answer is capitalized"
            );
        resultCorrect.IsCorrect.Should().BeTrue(because: "exact case match passes validation");
    }

    [Fact]
    public async Task FillInBlank_CaseSensitiveFalse_AcceptsAnyCase()
    {
        // Arrange
        var exerciseId = await CreateAndSaveFillInBlankAsync(
            correctAnswer: "Answer",
            caseSensitive: false,
            trimWhitespace: true
        );

        // Act
        var resultLower = await _sut.SubmitAnswerAsync(_testUserId, exerciseId, "answer");
        var resultUpper = await _sut.SubmitAnswerAsync(_testUserId, exerciseId, "ANSWER");
        var resultMixed = await _sut.SubmitAnswerAsync(_testUserId, exerciseId, "AnSwEr");

        // Assert
        resultLower.IsCorrect.Should().BeTrue();
        resultUpper.IsCorrect.Should().BeTrue();
        resultMixed.IsCorrect.Should().BeTrue();
    }

    [Fact]
    public async Task FillInBlank_TrimWhitespaceTrue_IgnoresLeadingTrailingSpaces()
    {
        // Arrange
        var exerciseId = await CreateAndSaveFillInBlankAsync(
            correctAnswer: "answer",
            caseSensitive: false,
            trimWhitespace: true
        );

        // Act
        var resultLeading = await _sut.SubmitAnswerAsync(_testUserId, exerciseId, "  answer");
        var resultTrailing = await _sut.SubmitAnswerAsync(_testUserId, exerciseId, "answer   ");
        var resultBoth = await _sut.SubmitAnswerAsync(_testUserId, exerciseId, "   answer   ");

        // Assert
        resultLeading.IsCorrect.Should().BeTrue(because: "TrimWhitespace removes leading spaces");
        resultTrailing.IsCorrect.Should().BeTrue(because: "TrimWhitespace removes trailing spaces");
        resultBoth.IsCorrect.Should().BeTrue(because: "TrimWhitespace removes both");
    }

    [Fact]
    public async Task FillInBlank_TrimWhitespaceFalse_RequiresExactWhitespace()
    {
        // Arrange
        var exerciseId = await CreateAndSaveFillInBlankAsync(
            correctAnswer: "answer",
            caseSensitive: false,
            trimWhitespace: false
        );

        // Act
        var resultWithSpace = await _sut.SubmitAnswerAsync(_testUserId, exerciseId, " answer");
        var resultExact = await _sut.SubmitAnswerAsync(_testUserId, exerciseId, "answer");

        // Assert
        resultWithSpace
            .IsCorrect.Should()
            .BeFalse(because: "TrimWhitespace=false requires exact whitespace match");
        resultExact.IsCorrect.Should().BeTrue();
    }

    [Fact]
    public async Task FillInBlank_AcceptedAnswers_ParsesCommaSeparatedList()
    {
        // Arrange
        var exerciseId = await CreateAndSaveFillInBlankAsync(
            correctAnswer: "hello",
            acceptedAnswers: "hi,hey,howdy",
            caseSensitive: false,
            trimWhitespace: true
        );

        // Act
        var resultHello = await _sut.SubmitAnswerAsync(_testUserId, exerciseId, "hello");
        var resultHi = await _sut.SubmitAnswerAsync(_testUserId, exerciseId, "hi");
        var resultHey = await _sut.SubmitAnswerAsync(_testUserId, exerciseId, "hey");
        var resultHowdy = await _sut.SubmitAnswerAsync(_testUserId, exerciseId, "howdy");
        var resultInvalid = await _sut.SubmitAnswerAsync(_testUserId, exerciseId, "greetings");

        // Assert
        resultHello.IsCorrect.Should().BeTrue(because: "correct answer always accepted");
        resultHi.IsCorrect.Should().BeTrue(because: "first alternative in AcceptedAnswers list");
        resultHey.IsCorrect.Should().BeTrue(because: "second alternative in AcceptedAnswers list");
        resultHowdy.IsCorrect.Should().BeTrue(because: "third alternative in AcceptedAnswers list");
        resultInvalid.IsCorrect.Should().BeFalse(because: "answer not in accepted list");
    }

    [Fact]
    public async Task FillInBlank_AcceptedAnswers_TrimsWhitespaceFromAlternatives()
    {
        // Arrange
        var exerciseId = await CreateAndSaveFillInBlankAsync(
            correctAnswer: "correct",
            acceptedAnswers: " alternative1 , alternative2 , alternative3 ",
            caseSensitive: false,
            trimWhitespace: true
        );

        // Act
        var resultAlt1 = await _sut.SubmitAnswerAsync(_testUserId, exerciseId, "alternative1");
        var resultAlt2 = await _sut.SubmitAnswerAsync(_testUserId, exerciseId, "alternative2");

        // Assert
        resultAlt1
            .IsCorrect.Should()
            .BeTrue(
                because: "AcceptedAnswers parsing trims whitespace from each comma-separated value"
            );
        resultAlt2
            .IsCorrect.Should()
            .BeTrue(because: "whitespace around alternatives is trimmed when TrimWhitespace=true");
    }

    [Fact]
    public async Task FillInBlank_AcceptedAnswers_RespectsCaseSensitivity()
    {
        // Arrange
        var exerciseId = await CreateAndSaveFillInBlankAsync(
            correctAnswer: "Answer",
            acceptedAnswers: "Alt1,Alt2",
            caseSensitive: true,
            trimWhitespace: true
        );

        // Act
        var resultCorrectCase = await _sut.SubmitAnswerAsync(_testUserId, exerciseId, "Alt1");
        var resultWrongCase = await _sut.SubmitAnswerAsync(_testUserId, exerciseId, "alt1");

        // Assert
        resultCorrectCase.IsCorrect.Should().BeTrue();
        resultWrongCase
            .IsCorrect.Should()
            .BeFalse(because: "case-sensitive validation applies to AcceptedAnswers as well");
    }

    [Fact]
    public async Task FillInBlank_EmptyAcceptedAnswers_OnlyCorrectAnswerAccepted()
    {
        // Arrange
        var exerciseId = await CreateAndSaveFillInBlankAsync(
            correctAnswer: "only",
            acceptedAnswers: "",
            caseSensitive: false,
            trimWhitespace: true
        );

        // Act
        var resultCorrect = await _sut.SubmitAnswerAsync(_testUserId, exerciseId, "only");
        var resultOther = await _sut.SubmitAnswerAsync(_testUserId, exerciseId, "other");

        // Assert
        resultCorrect.IsCorrect.Should().BeTrue();
        resultOther
            .IsCorrect.Should()
            .BeFalse(because: "empty AcceptedAnswers means only CorrectAnswer is valid");
    }

    // ── Listening Validation ────────────────────────────────────────────────

    [Fact]
    public async Task Listening_CaseSensitiveTrue_RejectsWrongCase()
    {
        // Arrange
        var exerciseId = await CreateAndSaveListeningAsync(
            audioUrl: "https://example.com/audio.mp3",
            correctAnswer: "Answer",
            acceptedAnswers: null,
            caseSensitive: true
        );

        // Act
        var resultWrong = await _sut.SubmitAnswerAsync(_testUserId, exerciseId, "answer");
        var resultCorrect = await _sut.SubmitAnswerAsync(_testUserId, exerciseId, "Answer");

        // Assert
        resultWrong
            .IsCorrect.Should()
            .BeFalse(because: "listening exercises respect CaseSensitive flag");
        resultCorrect.IsCorrect.Should().BeTrue();
    }

    [Fact]
    public async Task Listening_CaseSensitiveFalse_AcceptsAnyCase()
    {
        // Arrange
        var exerciseId = await CreateAndSaveListeningAsync(
            audioUrl: "https://example.com/audio.mp3",
            correctAnswer: "Answer",
            acceptedAnswers: null,
            caseSensitive: false
        );

        // Act
        var resultLower = await _sut.SubmitAnswerAsync(_testUserId, exerciseId, "answer");
        var resultUpper = await _sut.SubmitAnswerAsync(_testUserId, exerciseId, "ANSWER");

        // Assert
        resultLower.IsCorrect.Should().BeTrue();
        resultUpper.IsCorrect.Should().BeTrue();
    }

    [Fact]
    public async Task Listening_AlwaysTrimsWhitespace()
    {
        // Arrange
        var exerciseId = await CreateAndSaveListeningAsync(
            audioUrl: "https://example.com/audio.mp3",
            correctAnswer: "answer",
            acceptedAnswers: null,
            caseSensitive: false
        );

        // Act
        var result = await _sut.SubmitAnswerAsync(_testUserId, exerciseId, "  answer  ");

        // Assert
        result
            .IsCorrect.Should()
            .BeTrue(
                because: "listening validation always trims whitespace (trimWhitespace=true hardcoded)"
            );
    }

    [Fact]
    public async Task Listening_AcceptedAnswers_ParsesCommaSeparatedList()
    {
        // Arrange
        var exerciseId = await CreateAndSaveListeningAsync(
            audioUrl: "https://example.com/audio.mp3",
            correctAnswer: "correct",
            acceptedAnswers: "alt1,alt2,alt3",
            caseSensitive: false
        );

        // Act
        var resultCorrect = await _sut.SubmitAnswerAsync(_testUserId, exerciseId, "correct");
        var resultAlt1 = await _sut.SubmitAnswerAsync(_testUserId, exerciseId, "alt1");
        var resultAlt2 = await _sut.SubmitAnswerAsync(_testUserId, exerciseId, "alt2");
        var resultInvalid = await _sut.SubmitAnswerAsync(_testUserId, exerciseId, "wrong");

        // Assert
        resultCorrect.IsCorrect.Should().BeTrue();
        resultAlt1.IsCorrect.Should().BeTrue();
        resultAlt2.IsCorrect.Should().BeTrue();
        resultInvalid.IsCorrect.Should().BeFalse();
    }

    // ── Helper Methods ──────────────────────────────────────────────────────

    private static ExerciseProgressService BuildExerciseProgressService(BackendDbContext ctx)
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

        var exerciseService = new ExerciseService(ctx);
        var lessonService = new LessonService(ctx, exerciseService);
        var achievementService = new AchievementService(ctx);

        return new ExerciseProgressService(
            ctx,
            lessonService,
            exerciseService,
            userManager,
            achievementService
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

    private async Task<string> CreateAndSaveFillInBlankAsync(
        string correctAnswer,
        bool caseSensitive,
        bool trimWhitespace,
        string? acceptedAnswers = null
    )
    {
        var exerciseId = Guid.NewGuid().ToString();
        var options = new List<ExerciseOption>
        {
            new()
            {
                ExerciseId = exerciseId,
                OptionText = correctAnswer,
                IsCorrect = true,
                Explanation = "Correct answer",
            },
        };

        if (!string.IsNullOrEmpty(acceptedAnswers))
        {
            var alternatives = acceptedAnswers
                .Split(',')
                .Select(a => a.Trim())
                .Where(a => !string.IsNullOrEmpty(a));

            foreach (var alt in alternatives)
            {
                options.Add(
                    new ExerciseOption
                    {
                        ExerciseId = exerciseId,
                        OptionText = alt,
                        IsCorrect = true,
                        Explanation = "Accepted alternative",
                    }
                );
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
            IsLocked = false,
            Options = options,
        };

        _ctx.Exercises.Add(exercise);
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
        return exercise.ExerciseId;
    }


    private async Task<string> CreateAndSaveListeningAsync(
        string audioUrl,
        string correctAnswer,
        string? acceptedAnswers,
        bool caseSensitive
    )
    {
        var exerciseId = Guid.NewGuid().ToString();
        var options = new List<ExerciseOption>
        {
            new()
            {
                ExerciseId = exerciseId,
                OptionText = correctAnswer,
                IsCorrect = true,
                Explanation = "Correct answer",
            },
        };

        if (!string.IsNullOrEmpty(acceptedAnswers))
        {
            var alternatives = acceptedAnswers
                .Split(',')
                .Select(a => a.Trim())
                .Where(a => !string.IsNullOrEmpty(a));

            foreach (var alt in alternatives)
            {
                options.Add(
                    new ExerciseOption
                    {
                        ExerciseId = exerciseId,
                        OptionText = alt,
                        IsCorrect = true,
                        Explanation = "Accepted alternative",
                    }
                );
            }
        }

        var exercise = new ListeningExercise
        {
            ExerciseId = exerciseId,
            LessonId = _fixture.LessonId,
            Instructions = "Listen and type what you hear",
            AudioUrl = audioUrl,
            DifficultyLevel = DifficultyLevel.Beginner,
            Points = 10,
            IsLocked = false,
            Options = options,
        };

        _ctx.Exercises.Add(exercise);
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
        return exercise.ExerciseId;
    }

}
