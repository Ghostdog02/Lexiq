using System.Net;
using System.Net.Http.Json;
using Backend.Api.Dtos;
using Backend.Tests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.EndToEnd;

/// <summary>
/// HTTP-level E2E tests for progress persistence and restoration.
///
/// Verifies:
///   - Submit 3/5 exercises → leave → return → 3 submissions restored
///   - Complete lesson → return → all exercises show as complete
///   - Submit wrong answer → return → still shows wrong (not reset)
///   - Multiple sessions maintain consistent state
/// </summary>
public class ProgressRestorationTests(DatabaseFixture fixture)
    : ControllerTestBase(fixture),
        IClassFixture<DatabaseFixture>
{
    private HttpClient _client = null!;
    private string _authToken = null!;
    private string _userId = null!;

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        await ClearTestDataAsync(); // Clean state before each test

        // Create authenticated student user
        var (userId, token) = await CreateAuthenticatedUserAsync(
            "student",
            "student@test.com",
            "Student"
        );

        _userId = userId;
        _authToken = token;
        _client = CreateClient(_authToken);
    }

    public override async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await base.DisposeAsync();
    }

    [Fact]
    public async Task Student_CompletesPartial_ReturnsLater_ProgressRestored()
    {
        // Arrange: Get exercises
        var exercises = await GetExercisesForLessonAsync(Fixture.ExerciseIds[0]);
        exercises.Should().HaveCountGreaterThanOrEqualTo(5);

        // Act Session 1: Complete first 3 exercises
        await SubmitAnswerAsync(exercises[0].Id, "answer");
        await SubmitAnswerAsync(exercises[1].Id, "answer");
        await SubmitAnswerAsync(exercises[2].Id, "answer");

        // "Leave" (dispose and recreate client simulating new session)
        _client.Dispose();
        _client = CreateClient(_authToken);

        // Act Session 2: Fetch progress
        var progress = await GetLessonProgressAsync(Fixture.ExerciseIds[0]);

        // Assert: First 3 exercises completed, rest not attempted
        var completedProgress = progress.Where(p => p.IsCompleted).ToList();
        completedProgress.Should().HaveCount(3, "exactly 3 exercises were completed");

        completedProgress.Should().Contain(p => p.ExerciseId == exercises[0].Id);
        completedProgress.Should().Contain(p => p.ExerciseId == exercises[1].Id);
        completedProgress.Should().Contain(p => p.ExerciseId == exercises[2].Id);

        completedProgress
            .All(p => p.PointsEarned == 10)
            .Should()
            .BeTrue("each completed exercise earns 10 points");
    }

    [Fact]
    public async Task Student_CompletesLesson_ReturnsLater_AllExercisesStillComplete()
    {
        // Arrange: Complete all 20 exercises
        var exercises = await GetExercisesForLessonAsync(Fixture.ExerciseIds[0]);
        exercises.Should().HaveCount(20);

        foreach (var exercise in exercises)
        {
            await SubmitAnswerAsync(exercise.Id, "answer");
        }

        // Act: "Leave" and return
        _client.Dispose();
        _client = CreateClient(_authToken);

        // Fetch progress again
        var progress = await GetLessonProgressAsync(Fixture.ExerciseIds[0]);

        // Assert: All 20 exercises still completed
        var completedProgress = progress.Where(p => p.IsCompleted).ToList();
        completedProgress.Should().HaveCount(20, "all exercises should remain completed");

        var totalXp = completedProgress.Sum(p => p.PointsEarned);
        totalXp.Should().Be(200, "20 exercises × 10 points");
    }

    [Fact]
    public async Task Student_SubmitsWrongAnswer_ReturnsLater_StillShowsWrong()
    {
        // Arrange: Submit wrong answer to first exercise
        var exercises = await GetExercisesForLessonAsync(Fixture.ExerciseIds[0]);
        var firstExercise = exercises[0];

        var wrongSubmit = await SubmitAnswerAsync(firstExercise.Id, "wrong");
        wrongSubmit.IsCorrect.Should().BeFalse();
        wrongSubmit.CorrectAnswer.Should().NotBeNullOrEmpty("wrong answer reveals correct answer");

        // Act: "Leave" and return
        _client.Dispose();
        _client = CreateClient(_authToken);

        // Fetch submissions
        var submissions = await GetLessonSubmissionsAsync(Fixture.ExerciseIds[0]);

        // Assert: First exercise submission shows as incorrect
        var firstSubmission = submissions[0]; // Ordered by OrderIndex
        firstSubmission.IsCorrect.Should().BeFalse("wrong answer persists");
        firstSubmission.PointsEarned.Should().Be(0, "no points for wrong answer");
        firstSubmission.CorrectAnswer.Should().Be("answer", "correct answer still revealed");
    }

    [Fact]
    public async Task Student_PartialProgress_ThirdExerciseStillLocked()
    {
        // Arrange: Complete only first exercise
        var exercises = await GetExercisesForLessonAsync(Fixture.ExerciseIds[0]);
        await SubmitAnswerAsync(exercises[0].Id, "answer");

        // Act: "Leave" and return - fetch exercises again
        _client.Dispose();
        _client = CreateClient(_authToken);

        var exercisesAfterReturn = await GetExercisesForLessonAsync(Fixture.ExerciseIds[0]);

        // Assert: First and second unlocked, third locked
        exercisesAfterReturn[0].IsLocked.Should().BeFalse("first exercise unlocked");
        exercisesAfterReturn[0].UserProgress.Should().NotBeNull("has progress");
        exercisesAfterReturn[0].UserProgress!.IsCompleted.Should().BeTrue();

        exercisesAfterReturn[1].IsLocked.Should().BeFalse("second unlocked after first completed");

        if (exercisesAfterReturn.Count > 2)
        {
            exercisesAfterReturn[2].IsLocked.Should().BeTrue("third exercise still locked");
        }
    }

    [Fact]
    public async Task MultipleSessionsConsistentState_XpDoesNotDuplicate()
    {
        // Arrange: Complete first exercise in session 1
        var exercises = await GetExercisesForLessonAsync(Fixture.ExerciseIds[0]);
        await SubmitAnswerAsync(exercises[0].Id, "answer");

        // Act Session 1: Get progress
        var progress1 = await GetLessonProgressAsync(Fixture.ExerciseIds[0]);
        var completed1 = progress1.Where(p => p.IsCompleted).ToList();
        completed1.Should().HaveCount(1);

        // "Leave" and return - fetch progress again
        _client.Dispose();
        _client = CreateClient(_authToken);

        var progress2 = await GetLessonProgressAsync(Fixture.ExerciseIds[0]);
        var completed2 = progress2.Where(p => p.IsCompleted).ToList();

        // Assert: Same progress across sessions
        completed2.Should().HaveCount(1, "progress consistent across sessions");
        completed2[0].ExerciseId.Should().Be(completed1[0].ExerciseId);
        completed2[0].PointsEarned.Should().Be(completed1[0].PointsEarned);
    }

    // Helper methods

    private async Task<List<ExerciseDto>> GetExercisesForLessonAsync(string firstExerciseId)
    {
        // Get the lesson ID from the first exercise
        var exResponse = await _client.GetAsync(
            $"/api/exercises/{firstExerciseId}",
            TestContext.Current.CancellationToken
        );
        exResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var exDto = await exResponse.Content.ReadFromJsonAsync<ExerciseDto>(
            cancellationToken: TestContext.Current.CancellationToken
        );

        var lessonId = exDto!.LessonId;

        var response = await _client.GetAsync(
            $"/api/exercises/lesson/{lessonId}",
            TestContext.Current.CancellationToken
        );
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var exercises =
            await response.Content.ReadFromJsonAsync<List<ExerciseDto>>(
                cancellationToken: TestContext.Current.CancellationToken
            ) ?? [];
        return exercises;
    }

    private async Task<ExerciseSubmitResult> SubmitAnswerAsync(string exerciseId, string answer)
    {
        var response = await _client.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/submit",
            new SubmitAnswerRequest(answer),
            TestContext.Current.CancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<ExerciseSubmitResult>(
            cancellationToken: TestContext.Current.CancellationToken
        );
        result.Should().NotBeNull();
        return result!;
    }

    private async Task<List<UserExerciseProgressDto>> GetLessonProgressAsync(string firstExerciseId)
    {
        // Get lesson ID first
        var exResponse = await _client.GetAsync(
            $"/api/exercises/{firstExerciseId}",
            TestContext.Current.CancellationToken
        );
        var exDto = await exResponse.Content.ReadFromJsonAsync<ExerciseDto>(
            cancellationToken: TestContext.Current.CancellationToken
        );
        var lessonId = exDto!.LessonId;

        var response = await _client.GetAsync(
            $"/api/exercises/lesson/{lessonId}/progress",
            TestContext.Current.CancellationToken
        );
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return await response.Content.ReadFromJsonAsync<List<UserExerciseProgressDto>>(
                cancellationToken: TestContext.Current.CancellationToken
            ) ?? [];
    }

    private async Task<List<SubmitAnswerResponse>> GetLessonSubmissionsAsync(string firstExerciseId)
    {
        // Get lesson ID first
        var exResponse = await _client.GetAsync(
            $"/api/exercises/{firstExerciseId}",
            TestContext.Current.CancellationToken
        );
        var exDto = await exResponse.Content.ReadFromJsonAsync<ExerciseDto>(
            cancellationToken: TestContext.Current.CancellationToken
        );
        var lessonId = exDto!.LessonId;

        var response = await _client.GetAsync(
            $"/api/exercises/lesson/{lessonId}/submissions",
            TestContext.Current.CancellationToken
        );
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        return await response.Content.ReadFromJsonAsync<List<SubmitAnswerResponse>>(
                cancellationToken: TestContext.Current.CancellationToken
            ) ?? [];
    }
}
