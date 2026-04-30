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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Backend.Tests.Services;

/// <summary>
/// Integration tests for ExerciseProgressService answer validation.
///
/// Verifies:
///   - FillInBlank: case sensitivity, whitespace trimming, AcceptedAnswers parsing
///   - Translation: Levenshtein distance, MatchingThreshold, fuzzy matching
///   - Listening: AcceptedAnswers, case sensitivity
///   - MultipleChoice: option ID validation
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

    // ── Translation Validation ──────────────────────────────────────────────

    [Fact]
    public async Task Translation_ExactMatch_Returns100PercentSimilarity()
    {
        // Arrange
        var exerciseId = await CreateAndSaveTranslationAsync(
            sourceText: "Hello",
            targetText: "Ciao",
            matchingThreshold: 0.8
        );

        // Act
        var result = await _sut.SubmitAnswerAsync(_testUserId, exerciseId, "Ciao");

        // Assert
        result
            .IsCorrect.Should()
            .BeTrue(because: "exact match meets any threshold (100% similarity)");
    }

    [Fact]
    public async Task Translation_OneCharDifference_PassesLowThreshold()
    {
        // Arrange
        var exerciseId = await CreateAndSaveTranslationAsync(
            sourceText: "Hello",
            targetText: "buongiorno",
            matchingThreshold: 0.7
        );

        // Act - "buongirno" has 1 char deletion (missing 'o'), 9/10 chars = 90% similarity
        var result = await _sut.SubmitAnswerAsync(_testUserId, exerciseId, "buongirno");

        // Assert
        result
            .IsCorrect.Should()
            .BeTrue(
                because: "90% similarity exceeds 70% threshold (1 char difference in 10-char word)"
            );
    }

    [Fact]
    public async Task Translation_BelowThreshold_Fails()
    {
        // Arrange
        var exerciseId = await CreateAndSaveTranslationAsync(
            sourceText: "Good morning",
            targetText: "buongiorno",
            matchingThreshold: 0.9
        );

        // Act - "buon" is 4 chars, "buongiorno" is 10 chars
        // Similarity = (10 - 6) / 10 = 0.4 = 40%
        var result = await _sut.SubmitAnswerAsync(_testUserId, exerciseId, "buon");

        // Assert
        result.IsCorrect.Should().BeFalse(because: "40% similarity is below 90% threshold");
        result
            .CorrectAnswer.Should()
            .Be("buongiorno", because: "wrong answers reveal the correct answer");
    }

    [Fact]
    public async Task Translation_Threshold80_AcceptsCloseMatch()
    {
        // Arrange
        var exerciseId = await CreateAndSaveTranslationAsync(
            sourceText: "Thank you",
            targetText: "grazie",
            matchingThreshold: 0.8
        );

        // Act - "grazi" has 1 char missing, 5/6 = 83.3% similarity
        var result = await _sut.SubmitAnswerAsync(_testUserId, exerciseId, "grazi");

        // Assert
        result.IsCorrect.Should().BeTrue(because: "83.3% similarity exceeds 80% threshold");
    }

    [Fact]
    public async Task Translation_Threshold80_RejectsDistantMatch()
    {
        // Arrange
        var exerciseId = await CreateAndSaveTranslationAsync(
            sourceText: "Thank you",
            targetText: "grazie",
            matchingThreshold: 0.8
        );

        // Act - "graz" has 2 chars missing, 4/6 = 66.7% similarity
        var result = await _sut.SubmitAnswerAsync(_testUserId, exerciseId, "graz");

        // Assert
        result.IsCorrect.Should().BeFalse(because: "66.7% similarity is below 80% threshold");
    }

    [Fact]
    public async Task Translation_CaseInsensitive_IgnoresCase()
    {
        // Arrange
        var exerciseId = await CreateAndSaveTranslationAsync(
            sourceText: "Hello",
            targetText: "Ciao",
            matchingThreshold: 0.8
        );

        // Act
        var resultLower = await _sut.SubmitAnswerAsync(_testUserId, exerciseId, "ciao");
        var resultUpper = await _sut.SubmitAnswerAsync(_testUserId, exerciseId, "CIAO");

        // Assert
        resultLower
            .IsCorrect.Should()
            .BeTrue(because: "translation validation is case-insensitive");
        resultUpper
            .IsCorrect.Should()
            .BeTrue(because: "translation validation is case-insensitive");
    }

    [Fact]
    public async Task Translation_EmptyStrings_ReturnsTrue()
    {
        // Arrange
        var exerciseId = await CreateAndSaveTranslationAsync(
            sourceText: "",
            targetText: "",
            matchingThreshold: 0.8
        );

        // Act
        var result = await _sut.SubmitAnswerAsync(_testUserId, exerciseId, "");

        // Assert
        result
            .IsCorrect.Should()
            .BeTrue(
                because: "two empty strings have 100% similarity (edge case for empty translations)"
            );
    }

    [Fact]
    public async Task Translation_MultipleSubstitutions_CalculatesCorrectSimilarity()
    {
        // Arrange
        var exerciseId = await CreateAndSaveTranslationAsync(
            sourceText: "How are you?",
            targetText: "come stai",
            matchingThreshold: 0.7
        );

        // Act - "come stay" vs "come stai": 1 substitution (y→i)
        // Similarity = (9 - 1) / 9 = 88.9%
        var result = await _sut.SubmitAnswerAsync(_testUserId, exerciseId, "come stay");

        // Assert
        result.IsCorrect.Should().BeTrue(because: "88.9% similarity exceeds 70% threshold");
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

    // ── MultipleChoice Validation ───────────────────────────────────────────

    [Fact]
    public async Task MultipleChoice_CorrectOptionId_ReturnsTrue()
    {
        // Arrange
        var (exerciseId, correctOptionId, _) = await CreateAndSaveMultipleChoiceAsync();

        // Act
        var result = await _sut.SubmitAnswerAsync(_testUserId, exerciseId, correctOptionId);

        // Assert
        result
            .IsCorrect.Should()
            .BeTrue(because: "submitting the correct option ID validates successfully");
        result.PointsEarned.Should().Be(10);
    }

    [Fact]
    public async Task MultipleChoice_WrongOptionId_ReturnsFalse()
    {
        // Arrange
        var (exerciseId, _, wrongOptionId) = await CreateAndSaveMultipleChoiceAsync();

        // Act
        var result = await _sut.SubmitAnswerAsync(_testUserId, exerciseId, wrongOptionId);

        // Assert
        result
            .IsCorrect.Should()
            .BeFalse(because: "submitting a wrong option ID fails validation");
        result.PointsEarned.Should().Be(0);
        result.CorrectAnswer.Should().NotBeNullOrEmpty("wrong answers reveal correct answer");
    }

    [Fact]
    public async Task MultipleChoice_InvalidOptionId_ReturnsFalse()
    {
        // Arrange
        var (exerciseId, _, _) = await CreateAndSaveMultipleChoiceAsync();
        var invalidId = Guid.NewGuid().ToString();

        // Act
        var result = await _sut.SubmitAnswerAsync(_testUserId, exerciseId, invalidId);

        // Assert
        result
            .IsCorrect.Should()
            .BeFalse(
                because: "option ID not in the exercise's Options collection fails validation"
            );
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
            new object[] { lessonId },
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
        var exercise = new FillInBlankExercise
        {
            Id = Guid.NewGuid().ToString(),
            LessonId = _fixture.LessonId,
            Title = "Test FillInBlank",
            Text = "Fill in the blank: ___",
            CorrectAnswer = correctAnswer,
            AcceptedAnswers = acceptedAnswers,
            CaseSensitive = caseSensitive,
            TrimWhitespace = trimWhitespace,
            DifficultyLevel = DifficultyLevel.Beginner,
            Points = 10,
            OrderIndex = 0,
            IsLocked = false,
        };

        _ctx.Exercises.Add(exercise);
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
        return exercise.ExerciseId;
    }

    private async Task<string> CreateAndSaveTranslationAsync(
        string sourceText,
        string targetText,
        double matchingThreshold
    )
    {
        var exercise = new TranslationExercise
        {
            Id = Guid.NewGuid().ToString(),
            LessonId = _fixture.LessonId,
            Title = "Test Translation",
            SourceText = sourceText,
            TargetText = targetText,
            SourceLanguageCode = "en",
            TargetLanguageCode = "it",
            MatchingThreshold = matchingThreshold,
            DifficultyLevel = DifficultyLevel.Beginner,
            Points = 10,
            OrderIndex = 0,
            IsLocked = false,
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
        var exercise = new ListeningExercise
        {
            Id = Guid.NewGuid().ToString(),
            LessonId = _fixture.LessonId,
            Title = "Test Listening",
            AudioUrl = audioUrl,
            CorrectAnswer = correctAnswer,
            AcceptedAnswers = acceptedAnswers,
            CaseSensitive = caseSensitive,
            DifficultyLevel = DifficultyLevel.Beginner,
            Points = 10,
            OrderIndex = 0,
            IsLocked = false,
        };

        _ctx.Exercises.Add(exercise);
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
        return exercise.ExerciseId;
    }

    private async Task<(
        string exerciseId,
        string correctId,
        string wrongId
    )> CreateAndSaveMultipleChoiceAsync()
    {
        var exercise = new MultipleChoiceExercise
        {
            Id = Guid.NewGuid().ToString(),
            LessonId = _fixture.LessonId,
            Title = "Which is correct?",
            DifficultyLevel = DifficultyLevel.Beginner,
            Points = 10,
            OrderIndex = 0,
            IsLocked = false,
            Options = new List<ExerciseOption>(),
        };

        var correctOption = new ExerciseOption
        {
            Id = Guid.NewGuid().ToString(),
            ExerciseId = exercise.ExerciseId,
            OptionText = "Correct option",
            IsCorrect = true,
            OrderIndex = 0,
        };

        var wrongOption1 = new ExerciseOption
        {
            Id = Guid.NewGuid().ToString(),
            ExerciseId = exercise.ExerciseId,
            OptionText = "Wrong option 1",
            IsCorrect = false,
            OrderIndex = 1,
        };

        var wrongOption2 = new ExerciseOption
        {
            Id = Guid.NewGuid().ToString(),
            ExerciseId = exercise.ExerciseId,
            OptionText = "Wrong option 2",
            IsCorrect = false,
            OrderIndex = 2,
        };

        exercise.Options.Add(correctOption);
        exercise.Options.Add(wrongOption1);
        exercise.Options.Add(wrongOption2);

        _ctx.Exercises.Add(exercise);
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        return (exercise.ExerciseId, correctOption.Id, wrongOption1.Id);
    }
}
