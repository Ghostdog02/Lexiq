using System.Net;
using System.Net.Http.Json;
using Backend.Api.Dtos;
using Backend.Tests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.EndToEnd;

/// <summary>
/// HTTP-level E2E tests for progress persistence across user sessions.
///
/// Verifies:
///   - Submit 3/5 exercises → logout → login → 3 submissions restored
///   - Complete lesson → new session → all exercises still complete
///   - Submit wrong answer → new session → still shows wrong (not reset)
///   - Multiple sessions maintain consistent state (no XP duplication)
///   - Exercise unlock state persists across sessions
/// </summary>
public class StudentSessionPersistenceTests(DatabaseFixture fixture)
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
        // Arrange
        var exercises = await GetExercisesForLessonAsync(Fixture.ExerciseIds[0]);
        if (exercises.Count < 5)
            throw new InvalidOperationException("Fixture should seed at least 5 exercises");

        // Act
        await SubmitAnswerAsync(exercises[0].Id, "answer");
        await SubmitAnswerAsync(exercises[1].Id, "answer");
        await SubmitAnswerAsync(exercises[2].Id, "answer");

        _client.Dispose();
        _client = CreateClient(_authToken);

        var progress = await GetLessonProgressAsync(Fixture.ExerciseIds[0]);

        // Assert
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
        // Arrange
        var exercises = await GetExercisesForLessonAsync(Fixture.ExerciseIds[0]);
        if (exercises.Count != 20)
            throw new InvalidOperationException("Fixture should seed exactly 20 exercises");

        foreach (var exercise in exercises)
        {
            await SubmitAnswerAsync(exercise.Id, "answer");
        }

        // Act
        _client.Dispose();
        _client = CreateClient(_authToken);

        var progress = await GetLessonProgressAsync(Fixture.ExerciseIds[0]);

        // Assert
        var completedProgress = progress.Where(p => p.IsCompleted).ToList();
        completedProgress.Should().HaveCount(20, "all exercises should remain completed");

        var totalXp = completedProgress.Sum(p => p.PointsEarned);
        totalXp.Should().Be(200, "20 exercises × 10 points");
    }

    [Fact]
    public async Task Student_SubmitsWrongAnswer_ReturnsLater_StillShowsWrong()
    {
        // Arrange
        var exercises = await GetExercisesForLessonAsync(Fixture.ExerciseIds[0]);
        var firstExercise = exercises[0];

        var wrongSubmit = await SubmitAnswerAsync(firstExercise.Id, "wrong");

        // Act
        _client.Dispose();
        _client = CreateClient(_authToken);

        var submissions = await GetLessonSubmissionsAsync(Fixture.ExerciseIds[0]);

        // Assert
        wrongSubmit.IsCorrect.Should().BeFalse();
        wrongSubmit.CorrectAnswer.Should().NotBeNullOrEmpty("wrong answer reveals correct answer");

        var firstSubmission = submissions[0]; // Ordered by OrderIndex
        firstSubmission.IsCorrect.Should().BeFalse("wrong answer persists");
        firstSubmission.PointsEarned.Should().Be(0, "no points for wrong answer");
        firstSubmission.CorrectAnswer.Should().Be("answer", "correct answer still revealed");
    }

    [Fact]
    public async Task Student_PartialProgress_ThirdExerciseStillLocked()
    {
        // Arrange
        var exercises = await GetExercisesForLessonAsync(Fixture.ExerciseIds[0]);
        await SubmitAnswerAsync(exercises[0].Id, "answer");

        // Act
        _client.Dispose();
        _client = CreateClient(_authToken);

        var exercisesAfterReturn = await GetExercisesForLessonAsync(Fixture.ExerciseIds[0]);

        // Assert
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
        // Arrange
        var exercises = await GetExercisesForLessonAsync(Fixture.ExerciseIds[0]);
        await SubmitAnswerAsync(exercises[0].Id, "answer");

        // Act
        var progress1 = await GetLessonProgressAsync(Fixture.ExerciseIds[0]);
        var completed1 = progress1.Where(p => p.IsCompleted).ToList();

        _client.Dispose();
        _client = CreateClient(_authToken);

        var progress2 = await GetLessonProgressAsync(Fixture.ExerciseIds[0]);
        var completed2 = progress2.Where(p => p.IsCompleted).ToList();

        // Assert
        completed1.Should().HaveCount(1);
        completed2.Should().HaveCount(1, "progress consistent across sessions");
        completed2[0].ExerciseId.Should().Be(completed1[0].ExerciseId);
        completed2[0].PointsEarned.Should().Be(completed1[0].PointsEarned);
    }

    // Helper methods

    private async Task<List<ExerciseDto>> GetExercisesForLessonAsync(string firstExerciseId)
    {
        var exResponse = await _client.GetAsync(
            $"/api/exercises/{firstExerciseId}",
            TestContext.Current.CancellationToken
        );

        if (exResponse.StatusCode != HttpStatusCode.OK)
            throw new InvalidOperationException(
                $"Failed to fetch exercise {firstExerciseId}: {exResponse.StatusCode}"
            );

        var exDto = await exResponse.Content.ReadFromJsonAsync<ExerciseDto>(
            JsonOptions,
            TestContext.Current.CancellationToken
        );

        if (exDto == null)
            throw new InvalidOperationException("Exercise DTO was null");

        var lessonId = exDto.LessonId;

        var response = await _client.GetAsync(
            $"/api/exercises/lesson/{lessonId}",
            TestContext.Current.CancellationToken
        );

        if (response.StatusCode != HttpStatusCode.OK)
            throw new InvalidOperationException(
                $"Failed to fetch exercises for lesson {lessonId}: {response.StatusCode}"
            );

        var exercises =
            await response.Content.ReadFromJsonAsync<List<ExerciseDto>>(
                JsonOptions,
                TestContext.Current.CancellationToken
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

        if (response.StatusCode != HttpStatusCode.OK)
            throw new InvalidOperationException(
                $"Failed to submit answer for exercise {exerciseId}: {response.StatusCode}"
            );

        var result = await response.Content.ReadFromJsonAsync<ExerciseSubmitResult>(
            cancellationToken: TestContext.Current.CancellationToken
        );

        if (result == null)
            throw new InvalidOperationException("Submit result was null");

        return result;
    }

    private async Task<List<UserExerciseProgressDto>> GetLessonProgressAsync(string firstExerciseId)
    {
        var exResponse = await _client.GetAsync(
            $"/api/exercises/{firstExerciseId}",
            TestContext.Current.CancellationToken
        );

        if (exResponse.StatusCode != HttpStatusCode.OK)
            throw new InvalidOperationException(
                $"Failed to fetch exercise {firstExerciseId}: {exResponse.StatusCode}"
            );

        var exDto = await exResponse.Content.ReadFromJsonAsync<ExerciseDto>(
            JsonOptions,
            TestContext.Current.CancellationToken
        );

        if (exDto == null)
            throw new InvalidOperationException("Exercise DTO was null");

        var lessonId = exDto.LessonId;

        var response = await _client.GetAsync(
            $"/api/exercises/lesson/{lessonId}/progress",
            TestContext.Current.CancellationToken
        );

        if (response.StatusCode != HttpStatusCode.OK)
            throw new InvalidOperationException(
                $"Failed to fetch progress for lesson {lessonId}: {response.StatusCode}"
            );

        return await response.Content.ReadFromJsonAsync<List<UserExerciseProgressDto>>(
                cancellationToken: TestContext.Current.CancellationToken
            ) ?? [];
    }

    private async Task<List<SubmitAnswerResponse>> GetLessonSubmissionsAsync(string firstExerciseId)
    {
        var exResponse = await _client.GetAsync(
            $"/api/exercises/{firstExerciseId}",
            TestContext.Current.CancellationToken
        );

        if (exResponse.StatusCode != HttpStatusCode.OK)
            throw new InvalidOperationException(
                $"Failed to fetch exercise {firstExerciseId}: {exResponse.StatusCode}"
            );

        var exDto = await exResponse.Content.ReadFromJsonAsync<ExerciseDto>(
            JsonOptions,
            TestContext.Current.CancellationToken
        );

        if (exDto == null)
            throw new InvalidOperationException("Exercise DTO was null");

        var lessonId = exDto.LessonId;

        var response = await _client.GetAsync(
            $"/api/exercises/lesson/{lessonId}/submissions",
            TestContext.Current.CancellationToken
        );

        if (response.StatusCode != HttpStatusCode.OK)
            throw new InvalidOperationException(
                $"Failed to fetch submissions for lesson {lessonId}: {response.StatusCode}"
            );

        return await response.Content.ReadFromJsonAsync<List<SubmitAnswerResponse>>(
                cancellationToken: TestContext.Current.CancellationToken
            ) ?? [];
    }
}
