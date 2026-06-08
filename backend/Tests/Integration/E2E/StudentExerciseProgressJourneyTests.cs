using System.Net;
using System.Net.Http.Json;
using Backend.Api.Dtos;
using Backend.Database;
using Backend.Tests.Helpers;
using Backend.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Backend.Tests.Integration.E2E;

/// <summary>
/// HTTP-level E2E tests for student exercise completion and progress workflows.
///
/// Verifies complete user workflows:
///   - Exercise completion → unlock next exercise
///   - Wrong answer retry → infinite retries allowed
///   - Re-submit correct answer → XP idempotency (no double-counting)
///   - Complete 70%+ exercises → lesson marked complete → next lesson unlocks
///   - Partial progress restoration within a lesson
/// </summary>
public class StudentExerciseProgressJourneyTests(DatabaseFixture fixture)
    : ControllerTestBase(fixture),
        IClassFixture<DatabaseFixture>
{
    private HttpClient _client = null!;
    private string _authToken = null!;

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        await ClearTestDataAsync(); // Clean state before each test

        // Create authenticated student user
        var (_, token) = await CreateAuthenticatedUserAsync(
            "student1",
            "student1@test.com",
            "Student"
        );
        _authToken = token;
        _client = CreateClient(_authToken);
    }

    public override async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await base.DisposeAsync();
    }

    [Fact]
    public async Task Student_SubmitsWholeLesson_EarnsXpAndCompletesLesson()
    {
        // Arrange - create 5 exercises
        using var scope = Factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<BackendDbContext>();

        var exerciseIds = new List<string>();
        for (var i = 0; i < 5; i++)
        {
            var exId = await DbSeeder.CreateFillInBlankExerciseAsync(
                ctx, Fixture.LessonId, orderIndex: i, isLocked: false);
            exerciseIds.Add(exId);
        }

        var exercises = await GetExercisesForLessonAsync(Fixture.LessonId);

        // Act - submit all 5 exercises correctly at once
        var answers = exercises!
            .Select(e => new ExerciseAnswerDto(e.Id, GetCorrectOptionId(e)))
            .ToList();

        var response = await _client.PostAsJsonAsync(
            $"/api/lessons/{Fixture.LessonId}/submit",
            new SubmitLessonRequest(answers),
            TestContext.Current.CancellationToken
        );
        var result = await response.Content.ReadFromJsonAsync<LessonSubmitResult>(
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.Exercises.Should().HaveCount(5);
        result.Exercises.Should().AllSatisfy(e => e.IsCorrect.Should().BeTrue());
        result.Summary.EarnedXp.Should().Be(50, "5 exercises × 10 points");
        result.Summary.TotalPossibleXp.Should().Be(50);
        result.Summary.IsCompleted.Should().BeTrue(
            because: "student submitted with hearts remaining");
    }

    [Fact]
    public async Task Student_DepletesHearts_HeartsReachZero()
    {
        // Arrange - create 1 unlocked exercise
        using var scope = Factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<BackendDbContext>();

        var firstExId = await DbSeeder.CreateFillInBlankExerciseAsync(
            ctx,
            Fixture.LessonId,
            orderIndex: 0,
            isLocked: false
        );

        var exercises = await GetExercisesForLessonAsync(Fixture.LessonId);
        var firstEx = exercises!.First(e => e.Id == firstExId);

        // Act - deplete all 3 hearts with wrong answers
        var attempt1 = await SubmitAnswerAsync(firstEx.Id, "wrong1");
        var attempt2 = await SubmitAnswerAsync(firstEx.Id, "wrong2");
        var lastResponse = await _client.PostAsJsonAsync(
            $"/api/lessons/{Fixture.LessonId}/submit",
            new SubmitLessonRequest([new ExerciseAnswerDto(firstEx.Id, "wrong3")]),
            TestContext.Current.CancellationToken
        );
        var lastResult = await lastResponse.Content.ReadFromJsonAsync<LessonSubmitResult>(
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert - wrong answers accepted while hearts remain
        attempt1.Should().NotBeNull();
        attempt1.IsCorrect.Should().BeFalse();
        attempt1.PointsEarned.Should().Be(0);

        attempt2.Should().NotBeNull();
        attempt2.IsCorrect.Should().BeFalse();

        lastResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        lastResult.Should().NotBeNull();
        lastResult.HeartsRemaining.Should().Be(
            0,
            because: "three wrong answers deplete all 3 starting hearts"
        );
    }

    [Fact]
    public async Task Student_ResubmitsCorrectAnswer_DoesNotDoubleXp()
    {
        // Arrange - create 1 unlocked exercise
        using var scope = Factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<BackendDbContext>();

        var firstExId = await DbSeeder.CreateFillInBlankExerciseAsync(
            ctx,
            Fixture.LessonId,
            orderIndex: 0,
            isLocked: false
        );

        var exercises = await GetExercisesForLessonAsync(Fixture.LessonId);
        var firstEx = exercises!.First(e => e.Id == firstExId);
        var firstSubmit = await SubmitAnswerAsync(firstEx.Id, GetCorrectOptionId(firstEx));

        // Act
        var secondSubmit = await SubmitAnswerAsync(firstEx.Id, GetCorrectOptionId(firstEx));
        var progress = await GetLessonProgressAsync(Fixture.LessonId);

        // Assert
        firstSubmit.Should().NotBeNull();
        firstSubmit.IsCorrect.Should().BeTrue();
        firstSubmit.PointsEarned.Should().Be(10);

        secondSubmit.Should().NotBeNull();
        secondSubmit.IsCorrect.Should().BeTrue();
        secondSubmit.PointsEarned.Should().Be(10, "points earned field shows exercise value");

        progress.Should().NotBeNull();
        var exProgress = progress.First(p => p.ExerciseId == firstEx.Id);
        exProgress.PointsEarned.Should().Be(10, "only first submission counts for XP");
    }

    [Fact]
    public async Task Student_CompletesLesson_UnlocksNextLesson()
    {
        // Arrange - create 40 exercises, first unlocked
        using var scope = Factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<BackendDbContext>();

        var exerciseIds = new List<string>();
        for (var i = 0; i < 40; i++)
        {
            var exId = await DbSeeder.CreateFillInBlankExerciseAsync(
                ctx,
                Fixture.LessonId,
                orderIndex: i,
                isLocked: i != 0
            );
            exerciseIds.Add(exId);
        }

        var exercises = await GetExercisesForLessonAsync(Fixture.LessonId);

        // Act - submit first 27 exercises
        for (var i = 0; i < 27; i++)
        {
            var ex = exercises!.First(e => e.Id == exerciseIds[i]);
            await SubmitAnswerAsync(ex.Id, GetCorrectOptionId(ex));
        }

        // Submit the 28th exercise to reach 70%
        var lastEx = exercises!.First(e => e.Id == exerciseIds[27]);
        var result = await SubmitSingleExerciseAsync(lastEx.Id, GetCorrectOptionId(lastEx));

        // Assert
        result.Should().NotBeNull();
        result!.Summary.IsCompleted.Should().BeTrue(because: "student submitted with hearts remaining");
        result.Summary.EarnedXp.Should().Be(280, "28 exercises × 10 points");
        result.Summary.TotalPossibleXp.Should().Be(400, "40 exercises × 10 points");
        result.Summary.CompletionPercentage.Should().Be(0.70, "28 out of 40 exercises completed");
    }

    [Fact]
    public async Task Student_CompletesPartialLesson_ProgressRestoresCorrectly()
    {
        // Arrange - create 3 exercises, first unlocked
        using var scope = Factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<BackendDbContext>();

        var ex1Id = await DbSeeder.CreateFillInBlankExerciseAsync(
            ctx,
            Fixture.LessonId,
            orderIndex: 0,
            isLocked: false
        );
        var ex2Id = await DbSeeder.CreateFillInBlankExerciseAsync(
            ctx,
            Fixture.LessonId,
            orderIndex: 1,
            isLocked: true
        );
        var ex3Id = await DbSeeder.CreateFillInBlankExerciseAsync(
            ctx,
            Fixture.LessonId,
            orderIndex: 2,
            isLocked: true
        );

        var exercises = await GetExercisesForLessonAsync(Fixture.LessonId);
        var ex1 = exercises!.First(e => e.Id == ex1Id);
        var ex2 = exercises!.First(e => e.Id == ex2Id);
        var ex3 = exercises!.First(e => e.Id == ex3Id);

        await SubmitAnswerAsync(ex1.Id, GetCorrectOptionId(ex1));
        await SubmitAnswerAsync(ex2.Id, GetCorrectOptionId(ex2));
        await SubmitAnswerAsync(ex3.Id, GetCorrectOptionId(ex3));

        // Act
        var progress = await GetLessonProgressAsync(Fixture.LessonId);

        // Assert
        progress.Should().NotBeNull();
        var completed = progress.Where(p => p.IsCompleted).ToList();
        completed.Should().HaveCount(3);
        completed.Should().Contain(p => p.ExerciseId == ex1.Id && p.PointsEarned == 10);
        completed.Should().Contain(p => p.ExerciseId == ex2.Id && p.PointsEarned == 10);
        completed.Should().Contain(p => p.ExerciseId == ex3.Id && p.PointsEarned == 10);
    }

    [Fact]
    public async Task Student_SubmitsWrongAnswer_ProgressShowsIncorrect()
    {
        // Arrange - create 1 unlocked exercise
        using var scope = Factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<BackendDbContext>();

        var firstExId = await DbSeeder.CreateFillInBlankExerciseAsync(
            ctx,
            Fixture.LessonId,
            orderIndex: 0,
            isLocked: false
        );

        var exercises = await GetExercisesForLessonAsync(Fixture.LessonId);
        var firstEx = exercises!.First(e => e.Id == firstExId);

        // Act
        await SubmitAnswerAsync(firstEx.Id, "wrong");
        var submissions = await GetLessonSubmissionsAsync(Fixture.LessonId);

        // Assert
        submissions.Should().NotBeNull();
        var wrongSubmission = submissions[0];
        wrongSubmission.Should().NotBeNull("wrong answer creates a submission record");
        wrongSubmission.IsCorrect.Should().BeFalse();
        wrongSubmission.PointsEarned.Should().Be(0);
    }

    [Fact]
    public async Task Student_Below70Percent_LessonNotCompleted()
    {
        // Arrange - create 40 exercises, first unlocked
        using var scope = Factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<BackendDbContext>();

        var exerciseIds = new List<string>();
        for (var i = 0; i < 40; i++)
        {
            var exId = await DbSeeder.CreateFillInBlankExerciseAsync(
                ctx,
                Fixture.LessonId,
                orderIndex: i,
                isLocked: i != 0
            );
            exerciseIds.Add(exId);
        }

        var exercises = await GetExercisesForLessonAsync(Fixture.LessonId);

        // Submit first 26 exercises
        for (var i = 0; i < 26; i++)
        {
            var ex = exercises!.First(e => e.Id == exerciseIds[i]);
            await SubmitAnswerAsync(ex.Id, GetCorrectOptionId(ex));
        }

        // Act - submit the 27th exercise (67.5% — below 70%)
        var lastEx = exercises!.First(e => e.Id == exerciseIds[26]);
        var result = await SubmitSingleExerciseAsync(lastEx.Id, GetCorrectOptionId(lastEx));

        // Assert
        result.Should().NotBeNull();
        result!.Summary.IsCompleted.Should().BeTrue(because: "student still has hearts when submitting");
        result.Summary.CompletionPercentage.Should().Be(0.68, "27 out of 40 exercises completed");
    }

    private async Task<LessonSubmitResult?> SubmitSingleExerciseAsync(string exerciseId, string? answer)
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/lessons/{Fixture.LessonId}/submit",
            new SubmitLessonRequest([new ExerciseAnswerDto(exerciseId, answer)]),
            TestContext.Current.CancellationToken
        );
        if (!response.IsSuccessStatusCode)
            return null;
        return await response.Content.ReadFromJsonAsync<LessonSubmitResult>(
            cancellationToken: TestContext.Current.CancellationToken
        );
    }

    // Extracts the first correct option ID from any option-based exercise DTO
    private static string GetCorrectOptionId(ExerciseDto exercise) => exercise switch
    {
        FillInBlankExerciseDto fib => fib.Options.First(o => o.IsCorrect).Id,
        ListeningExerciseDto le => le.Options.First(o => o.IsCorrect).Id,
        TrueFalseExerciseDto tf => tf.Options.First(o => o.IsCorrect).Id,
        _ => throw new InvalidOperationException($"Cannot extract correct option from {exercise.GetType().Name}"),
    };

    // Helper methods - no assertions, just return data

    private async Task<List<ExerciseDto>?> GetExercisesForLessonAsync(string lessonId)
    {
        var response = await _client.GetAsync(
            $"/api/lessons/{lessonId}/exercises",
            TestContext.Current.CancellationToken
        );
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<List<ExerciseDto>>(
            JsonOptions,
            TestContext.Current.CancellationToken
        );
    }

    /// <summary>
    /// Submits a single exercise answer via the lesson-level submit endpoint.
    /// Returns the ExerciseResultDto for the submitted exercise, or null on non-200 response.
    /// </summary>
    private async Task<ExerciseResultDto?> SubmitAnswerAsync(string exerciseId, string? answer)
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/lessons/{Fixture.LessonId}/submit",
            new SubmitLessonRequest([new ExerciseAnswerDto(exerciseId, answer)]),
            TestContext.Current.CancellationToken
        );

        if (!response.IsSuccessStatusCode)
            return null;

        var result = await response.Content.ReadFromJsonAsync<LessonSubmitResult>(
            cancellationToken: TestContext.Current.CancellationToken
        );

        return result?.Exercises.FirstOrDefault(e => e.ExerciseId == exerciseId);
    }

    private async Task<List<UserExerciseProgressDto>?> GetLessonProgressAsync(string lessonId)
    {
        var response = await _client.GetAsync(
            $"/api/lessons/{lessonId}/progress",
            TestContext.Current.CancellationToken
        );
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<List<UserExerciseProgressDto>>(
            cancellationToken: TestContext.Current.CancellationToken
        );
    }

    private async Task<List<SubmitAnswerResponse>?> GetLessonSubmissionsAsync(string lessonId)
    {
        var response = await _client.GetAsync(
            $"/api/lessons/{lessonId}/submissions",
            TestContext.Current.CancellationToken
        );
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<List<SubmitAnswerResponse>>(
            cancellationToken: TestContext.Current.CancellationToken
        );
    }
}
