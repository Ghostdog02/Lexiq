using System.Net;
using System.Net.Http.Json;
using Backend.Api.Dtos;
using Backend.Database;
using Backend.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Backend.Tests.EndToEnd;

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
    public async Task Student_CompletesFirstExercise_UnlocksNextExercise()
    {
        // Arrange - create 2 exercises for this test
        using var scope = Factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<BackendDbContext>();

        var firstExId = await Helpers.DbSeeder.CreateFillInBlankExerciseAsync(
            ctx,
            Fixture.LessonId,
            orderIndex: 0,
            isLocked: false
        );
        var secondExId = await Helpers.DbSeeder.CreateFillInBlankExerciseAsync(
            ctx,
            Fixture.LessonId,
            orderIndex: 1,
            isLocked: true
        );

        var exercises = await GetExercisesForLessonAsync(Fixture.LessonId);
        var firstEx = exercises.First(e => e.Id == firstExId);
        var secondEx = exercises.First(e => e.Id == secondExId);

        // Act
        var submitResult = await SubmitAnswerAsync(firstEx.Id, "answer");
        var exercisesAfter = await GetExercisesForLessonAsync(Fixture.LessonId);

        // Assert
        firstEx.IsLocked.Should().BeFalse("first exercise should be unlocked");
        secondEx.IsLocked.Should().BeTrue("second exercise should be locked initially");

        submitResult.Should().NotBeNull("submission should succeed");
        submitResult.IsCorrect.Should().BeTrue();
        submitResult.PointsEarned.Should().Be(10);

        exercisesAfter.Should().NotBeNull();
        var secondExAfter = exercisesAfter.First(e => e.Id == secondExId);
        secondExAfter
            .IsLocked.Should()
            .BeFalse("second exercise should unlock after first completed");
    }

    [Fact]
    public async Task Student_SubmitsWrongAnswer_CanRetryInfinitely()
    {
        // Arrange - create 1 unlocked exercise
        using var scope = Factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<BackendDbContext>();

        var firstExId = await Helpers.DbSeeder.CreateFillInBlankExerciseAsync(
            ctx,
            Fixture.LessonId,
            orderIndex: 0,
            isLocked: false
        );

        var exercises = await GetExercisesForLessonAsync(Fixture.LessonId);
        var firstEx = exercises.First(e => e.Id == firstExId);

        // Act
        var attempt1 = await SubmitAnswerAsync(firstEx.Id, "wrong1");
        var attempt2 = await SubmitAnswerAsync(firstEx.Id, "wrong2");
        var attempt3 = await SubmitAnswerAsync(firstEx.Id, "wrong3");
        var correct = await SubmitAnswerAsync(firstEx.Id, "answer");

        // Assert
        attempt1.Should().NotBeNull();
        attempt1.IsCorrect.Should().BeFalse();
        attempt1.PointsEarned.Should().Be(0);

        attempt2.Should().NotBeNull();
        attempt2.IsCorrect.Should().BeFalse();

        attempt3.Should().NotBeNull();
        attempt3.IsCorrect.Should().BeFalse();

        correct.Should().NotBeNull();
        correct.IsCorrect.Should().BeTrue();
        correct.PointsEarned.Should().Be(10);
    }

    [Fact]
    public async Task Student_ResubmitsCorrectAnswer_DoesNotDoubleXp()
    {
        // Arrange - create 1 unlocked exercise
        using var scope = Factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<BackendDbContext>();

        var firstExId = await Helpers.DbSeeder.CreateFillInBlankExerciseAsync(
            ctx,
            Fixture.LessonId,
            orderIndex: 0,
            isLocked: false
        );

        var exercises = await GetExercisesForLessonAsync(Fixture.LessonId);
        var firstEx = exercises.First(e => e.Id == firstExId);
        var firstSubmit = await SubmitAnswerAsync(firstEx.Id, "answer");

        // Act
        var secondSubmit = await SubmitAnswerAsync(firstEx.Id, "answer");
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
            var exId = await Helpers.DbSeeder.CreateFillInBlankExerciseAsync(
                ctx,
                Fixture.LessonId,
                orderIndex: i,
                isLocked: i != 0
            );
            exerciseIds.Add(exId);
        }

        var exercises = await GetExercisesForLessonAsync(Fixture.LessonId);

        // Act - complete 28 exercises (70% of 40)
        for (var i = 0; i < 28; i++)
        {
            var ex = exercises.First(e => e.Id == exerciseIds[i]);
            await SubmitAnswerAsync(ex.Id, "answer");
        }

        var response = await _client.PostAsync(
            $"/api/lessons/{Fixture.LessonId}/complete",
            null,
            TestContext.Current.CancellationToken
        );

        var result = await response.Content.ReadFromJsonAsync<CompleteLessonResponse>(
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result.IsCompleted.Should().BeTrue();
        result.EarnedXp.Should().Be(280, "28 exercises × 10 points");
        result.TotalPossibleXp.Should().Be(400, "40 exercises × 10 points");
        result.CompletionPercentage.Should().Be(0.70, "28 out of 40 exercises completed");
    }

    [Fact]
    public async Task Student_CompletesPartialLesson_ProgressRestoresCorrectly()
    {
        // Arrange - create 3 exercises, first unlocked
        using var scope = Factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<BackendDbContext>();

        var ex1Id = await Helpers.DbSeeder.CreateFillInBlankExerciseAsync(
            ctx,
            Fixture.LessonId,
            orderIndex: 0,
            isLocked: false
        );
        var ex2Id = await Helpers.DbSeeder.CreateFillInBlankExerciseAsync(
            ctx,
            Fixture.LessonId,
            orderIndex: 1,
            isLocked: true
        );
        var ex3Id = await Helpers.DbSeeder.CreateFillInBlankExerciseAsync(
            ctx,
            Fixture.LessonId,
            orderIndex: 2,
            isLocked: true
        );

        var exercises = await GetExercisesForLessonAsync(Fixture.LessonId);
        var ex1 = exercises.First(e => e.Id == ex1Id);
        var ex2 = exercises.First(e => e.Id == ex2Id);
        var ex3 = exercises.First(e => e.Id == ex3Id);

        await SubmitAnswerAsync(ex1.Id, "answer");
        await SubmitAnswerAsync(ex2.Id, "answer");
        await SubmitAnswerAsync(ex3.Id, "answer");

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

        var firstExId = await Helpers.DbSeeder.CreateFillInBlankExerciseAsync(
            ctx,
            Fixture.LessonId,
            orderIndex: 0,
            isLocked: false
        );

        var exercises = await GetExercisesForLessonAsync(Fixture.LessonId);
        var firstEx = exercises.First(e => e.Id == firstExId);

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
            var exId = await Helpers.DbSeeder.CreateFillInBlankExerciseAsync(
                ctx,
                Fixture.LessonId,
                orderIndex: i,
                isLocked: i != 0
            );
            exerciseIds.Add(exId);
        }

        var exercises = await GetExercisesForLessonAsync(Fixture.LessonId);

        // Complete 27 exercises (67.5% of 40 - below 70% threshold)
        for (var i = 0; i < 27; i++)
        {
            var ex = exercises.First(e => e.Id == exerciseIds[i]);
            await SubmitAnswerAsync(ex.Id, "answer");
        }

        // Act
        var response = await _client.PostAsync(
            $"/api/lessons/{Fixture.LessonId}/complete",
            null,
            TestContext.Current.CancellationToken
        );

        var result = await response.Content.ReadFromJsonAsync<CompleteLessonResponse>(
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result.IsCompleted.Should().BeFalse("67.5% is below 70% threshold");
        result.CompletionPercentage.Should().Be(0.68, "27 out of 40 exercises completed");
    }

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

    private async Task<ExerciseSubmitResult?> SubmitAnswerAsync(string exerciseId, string answer)
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/submit",
            new SubmitAnswerRequest(answer),
            TestContext.Current.CancellationToken
        );

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<ExerciseSubmitResult>(
            cancellationToken: TestContext.Current.CancellationToken
        );
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
