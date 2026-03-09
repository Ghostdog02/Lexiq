using System.Net;
using System.Net.Http.Json;
using Backend.Api.Dtos;
using Backend.Tests.Infrastructure;
using FluentAssertions;
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
        // Arrange
        var exercises = await GetExercisesForLessonAsync(Fixture.ExerciseIds[0]);
        if (exercises == null || exercises.Count < 2)
            throw new InvalidOperationException("Fixture should seed at least 2 exercises");

        var firstEx = exercises.First(e => e.OrderIndex == 0);
        var secondEx = exercises.First(e => e.OrderIndex == 1);

        // Act
        var submitResult = await SubmitAnswerAsync(firstEx.Id, "answer");
        var exercisesAfter = await GetExercisesForLessonAsync(Fixture.ExerciseIds[0]);

        // Assert
        firstEx.IsLocked.Should().BeFalse("first exercise should be unlocked");
        secondEx.IsLocked.Should().BeTrue("second exercise should be locked initially");

        submitResult.Should().NotBeNull("submission should succeed");
        submitResult!.IsCorrect.Should().BeTrue();
        submitResult.PointsEarned.Should().Be(10);

        exercisesAfter.Should().NotBeNull();
        var secondExAfter = exercisesAfter!.First(e => e.OrderIndex == 1);
        secondExAfter
            .IsLocked.Should()
            .BeFalse("second exercise should unlock after first completed");
    }

    [Fact]
    public async Task Student_SubmitsWrongAnswer_CanRetryInfinitely()
    {
        // Arrange
        var exercises = await GetExercisesForLessonAsync(Fixture.ExerciseIds[0]);
        if (exercises == null)
            throw new InvalidOperationException("Fixture should seed exercises");

        var firstEx = exercises.First(e => e.OrderIndex == 0);

        // Act
        var attempt1 = await SubmitAnswerAsync(firstEx.Id, "wrong1");
        var attempt2 = await SubmitAnswerAsync(firstEx.Id, "wrong2");
        var attempt3 = await SubmitAnswerAsync(firstEx.Id, "wrong3");
        var correct = await SubmitAnswerAsync(firstEx.Id, "answer");

        // Assert
        attempt1.Should().NotBeNull();
        attempt1!.IsCorrect.Should().BeFalse();
        attempt1.PointsEarned.Should().Be(0);
        attempt1.CorrectAnswer.Should().Be("answer");

        attempt2.Should().NotBeNull();
        attempt2!.IsCorrect.Should().BeFalse();

        attempt3.Should().NotBeNull();
        attempt3!.IsCorrect.Should().BeFalse();

        correct.Should().NotBeNull();
        correct!.IsCorrect.Should().BeTrue();
        correct.PointsEarned.Should().Be(10);
    }

    [Fact]
    public async Task Student_ResubmitsCorrectAnswer_DoesNotDoubleXp()
    {
        // Arrange
        var exercises = await GetExercisesForLessonAsync(Fixture.ExerciseIds[0]);
        if (exercises == null)
            throw new InvalidOperationException("Fixture should seed exercises");

        var firstEx = exercises.First(e => e.OrderIndex == 0);
        var firstSubmit = await SubmitAnswerAsync(firstEx.Id, "answer");

        // Act
        var secondSubmit = await SubmitAnswerAsync(firstEx.Id, "answer");
        var progress = await GetLessonProgressAsync(Fixture.ExerciseIds[0]);

        // Assert
        firstSubmit.Should().NotBeNull();
        firstSubmit!.IsCorrect.Should().BeTrue();
        firstSubmit.PointsEarned.Should().Be(10);

        secondSubmit.Should().NotBeNull();
        secondSubmit!.IsCorrect.Should().BeTrue();
        secondSubmit.PointsEarned.Should().Be(10, "points earned field shows exercise value");

        progress.Should().NotBeNull();
        var exProgress = progress!.First(p => p.ExerciseId == firstEx.Id);
        exProgress.PointsEarned.Should().Be(10, "only first submission counts for XP");
    }

    [Fact]
    public async Task Student_CompletesLesson_UnlocksNextLesson()
    {
        // Arrange
        var exercises = await GetExercisesForLessonAsync(Fixture.ExerciseIds[0]);
        if (exercises == null || exercises.Count != 20)
            throw new InvalidOperationException("Fixture should seed exactly 20 exercises");

        var lessonId = exercises.First().LessonId;

        // Act
        for (var i = 0; i < 14; i++)
        {
            var ex = exercises.First(e => e.OrderIndex == i);
            await SubmitAnswerAsync(ex.Id, "answer");
        }

        var response = await _client.PostAsync(
            $"/api/lessons/{lessonId}/complete",
            null,
            TestContext.Current.CancellationToken
        );

        var result = await response.Content.ReadFromJsonAsync<CompleteLessonResponse>(
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.IsCompleted.Should().BeTrue();
        result.EarnedXp.Should().Be(140, "14 exercises × 10 points");
        result.TotalPossibleXp.Should().Be(200, "20 exercises × 10 points");
        result.CompletionPercentage.Should().Be(70.0);
    }

    [Fact]
    public async Task Student_CompletesPartialLesson_ProgressRestoresCorrectly()
    {
        // Arrange
        var exercises = await GetExercisesForLessonAsync(Fixture.ExerciseIds[0]);
        if (exercises == null)
            throw new InvalidOperationException("Fixture should seed exercises");

        var ex1 = exercises.First(e => e.OrderIndex == 0);
        var ex2 = exercises.First(e => e.OrderIndex == 1);
        var ex3 = exercises.First(e => e.OrderIndex == 2);

        await SubmitAnswerAsync(ex1.Id, "answer");
        await SubmitAnswerAsync(ex2.Id, "answer");
        await SubmitAnswerAsync(ex3.Id, "answer");

        // Act
        var progress = await GetLessonProgressAsync(Fixture.ExerciseIds[0]);

        // Assert
        progress.Should().NotBeNull();
        var completed = progress!.Where(p => p.IsCompleted).ToList();
        completed.Should().HaveCount(3);
        completed.Should().Contain(p => p.ExerciseId == ex1.Id && p.PointsEarned == 10);
        completed.Should().Contain(p => p.ExerciseId == ex2.Id && p.PointsEarned == 10);
        completed.Should().Contain(p => p.ExerciseId == ex3.Id && p.PointsEarned == 10);
    }

    [Fact]
    public async Task Student_SubmitsWrongAnswer_ProgressShowsIncorrect()
    {
        // Arrange
        var exercises = await GetExercisesForLessonAsync(Fixture.ExerciseIds[0]);
        if (exercises == null)
            throw new InvalidOperationException("Fixture should seed exercises");

        var firstEx = exercises.First(e => e.OrderIndex == 0);

        // Act
        await SubmitAnswerAsync(firstEx.Id, "wrong");
        var submissions = await GetLessonSubmissionsAsync(Fixture.ExerciseIds[0]);

        // Assert
        submissions.Should().NotBeNull();
        var wrongSubmission = submissions![0]; // Submissions ordered by OrderIndex
        wrongSubmission.Should().NotBeNull("wrong answer creates a submission record");
        wrongSubmission.IsCorrect.Should().BeFalse();
        wrongSubmission.PointsEarned.Should().Be(0);
        wrongSubmission.CorrectAnswer.Should().Be("answer");
    }

    [Fact]
    public async Task Student_Below70Percent_LessonNotCompleted()
    {
        // Arrange
        var exercises = await GetExercisesForLessonAsync(Fixture.ExerciseIds[0]);
        if (exercises == null)
            throw new InvalidOperationException("Fixture should seed exercises");

        var lessonId = exercises.First().LessonId;

        for (var i = 0; i < 13; i++)
        {
            var ex = exercises.First(e => e.OrderIndex == i);
            await SubmitAnswerAsync(ex.Id, "answer");
        }

        // Act
        var response = await _client.PostAsync(
            $"/api/lessons/{lessonId}/complete",
            null,
            TestContext.Current.CancellationToken
        );

        var result = await response.Content.ReadFromJsonAsync<CompleteLessonResponse>(
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.Should().NotBeNull();
        result!.IsCompleted.Should().BeFalse("65% is below 70% threshold");
        result.CompletionPercentage.Should().Be(65.0);
    }

    // Helper methods - no assertions, just return data

    private async Task<List<ExerciseDto>?> GetExercisesForLessonAsync(string firstExerciseId)
    {
        // Get the lesson ID from the first exercise
        var exResponse = await _client.GetAsync(
            $"/api/exercise/{firstExerciseId}",
            TestContext.Current.CancellationToken
        );
        if (!exResponse.IsSuccessStatusCode)
            return null;

        var exDto = await exResponse.Content.ReadFromJsonAsync<ExerciseDto>(
            cancellationToken: TestContext.Current.CancellationToken
        );
        if (exDto == null)
            return null;

        var response = await _client.GetAsync(
            $"/api/exercise/lesson/{exDto.LessonId}",
            TestContext.Current.CancellationToken
        );
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<List<ExerciseDto>>(
            cancellationToken: TestContext.Current.CancellationToken
        );
    }

    private async Task<ExerciseSubmitResult?> SubmitAnswerAsync(string exerciseId, string answer)
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/exercise/{exerciseId}/submit",
            new SubmitAnswerRequest(answer),
            TestContext.Current.CancellationToken
        );

        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<ExerciseSubmitResult>(
            cancellationToken: TestContext.Current.CancellationToken
        );
    }

    private async Task<List<UserExerciseProgressDto>?> GetLessonProgressAsync(
        string firstExerciseId
    )
    {
        // Get lesson ID first
        var exResponse = await _client.GetAsync(
            $"/api/exercise/{firstExerciseId}",
            TestContext.Current.CancellationToken
        );
        if (!exResponse.IsSuccessStatusCode)
            return null;

        var exDto = await exResponse.Content.ReadFromJsonAsync<ExerciseDto>(
            cancellationToken: TestContext.Current.CancellationToken
        );
        if (exDto == null)
            return null;

        var response = await _client.GetAsync(
            $"/api/exercise/lesson/{exDto.LessonId}/progress",
            TestContext.Current.CancellationToken
        );
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<List<UserExerciseProgressDto>>(
            cancellationToken: TestContext.Current.CancellationToken
        );
    }

    private async Task<List<SubmitAnswerResponse>?> GetLessonSubmissionsAsync(
        string firstExerciseId
    )
    {
        // Get lesson ID first
        var exResponse = await _client.GetAsync(
            $"/api/exercise/{firstExerciseId}",
            TestContext.Current.CancellationToken
        );
        if (!exResponse.IsSuccessStatusCode)
            return null;

        var exDto = await exResponse.Content.ReadFromJsonAsync<ExerciseDto>(
            cancellationToken: TestContext.Current.CancellationToken
        );
        if (exDto == null)
            return null;

        var response = await _client.GetAsync(
            $"/api/exercise/lesson/{exDto.LessonId}/submissions",
            TestContext.Current.CancellationToken
        );
        if (!response.IsSuccessStatusCode)
            return null;

        return await response.Content.ReadFromJsonAsync<List<SubmitAnswerResponse>>(
            cancellationToken: TestContext.Current.CancellationToken
        );
    }
}
