using System.Net;
using System.Net.Http.Json;
using Backend.Api.Dtos;
using Backend.Database;
using Backend.Database.Entities;
using Backend.Database.Entities.Exercises;
using Backend.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Backend.Tests.EndToEnd;

/// <summary>
/// HTTP-level E2E tests for exercise submission security and edge cases.
///
/// Verifies:
///   - Wrong answer does NOT unlock next exercise (lock enforcement)
///   - Locked exercise submission → 403 for students
///   - Admin/ContentCreator bypass locked exercises
///   - Invalid exercise IDs → 404
///   - Exercises in locked lessons → 403
///   - MultipleChoice answer validation (option IDs)
///   - Progress endpoint response shape
///   - Submissions endpoint response shape
/// </summary>
public class ExerciseSubmissionSecurityTests(DatabaseFixture fixture)
    : ControllerTestBase(fixture),
        IClassFixture<DatabaseFixture>
{
    private HttpClient _studentClient = null!;
    private HttpClient _adminClient = null!;
    private HttpClient _creatorClient = null!;
    private string _studentToken = null!;
    private string _adminToken = null!;
    private string _creatorToken = null!;

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        await ClearTestDataAsync();

        // Create users with different roles
        var (_, studentToken) = await CreateAuthenticatedUserAsync(
            "student",
            "student@test.com",
            "Student"
        );
        var (_, adminToken) = await CreateAuthenticatedUserAsync(
            "admin",
            "admin@test.com",
            "Admin"
        );
        var (_, creatorToken) = await CreateAuthenticatedUserAsync(
            "creator",
            "creator@test.com",
            "ContentCreator"
        );

        _studentToken = studentToken;
        _adminToken = adminToken;
        _creatorToken = creatorToken;

        _studentClient = CreateClient(_studentToken);
        _adminClient = CreateClient(_adminToken);
        _creatorClient = CreateClient(_creatorToken);
    }

    public override async ValueTask DisposeAsync()
    {
        _studentClient.Dispose();
        _adminClient.Dispose();
        _creatorClient.Dispose();
        await base.DisposeAsync();
    }

    [Fact]
    public async Task Student_SubmitsWrongAnswer_DoesNotUnlockNextExercise()
    {
        // Arrange
        var firstExId = Fixture.ExerciseIds[0];
        var secondExId = Fixture.ExerciseIds[1];

        var exercisesBefore = await GetExercisesAsync(firstExId);
        if (exercisesBefore == null || exercisesBefore.Count < 2)
            throw new InvalidOperationException("Fixture should seed at least 2 exercises");

        var secondExBefore = exercisesBefore.First(e => e.Id == secondExId);

        // Act
        var wrongSubmit = await SubmitAnswerAsync(_studentClient, firstExId, "wrong answer");

        var exercisesAfter = await GetExercisesAsync(firstExId);

        // Assert
        secondExBefore.IsLocked.Should().BeTrue("second exercise starts locked");
        wrongSubmit.Should().NotBeNull();
        wrongSubmit!.IsCorrect.Should().BeFalse("wrong answer is incorrect");
        wrongSubmit.PointsEarned.Should().Be(0, "no points for wrong answer");
        wrongSubmit.CorrectAnswer.Should().Be("answer", "correct answer revealed after wrong submission");

        exercisesAfter.Should().NotBeNull();
        var secondExAfter = exercisesAfter!.First(e => e.Id == secondExId);
        secondExAfter
            .IsLocked.Should()
            .BeTrue("second exercise should remain locked after wrong answer");
    }

    [Fact]
    public async Task Student_SubmitsToLockedExercise_Returns403()
    {
        // Arrange
        var lockedExerciseId = Fixture.ExerciseIds[2]; // Locked by fixture (OrderIndex > 0)

        // Act
        var response = await _studentClient.PostAsJsonAsync(
            $"/api/exercises/{lockedExerciseId}/submit",
            new SubmitAnswerRequest("answer"),
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, "students cannot submit to locked exercises");
    }

    [Fact]
    public async Task Admin_SubmitsToLockedExercise_Success()
    {
        // Arrange
        var lockedExerciseId = Fixture.ExerciseIds[2];

        // Act
        var response = await _adminClient.PostAsJsonAsync(
            $"/api/exercises/{lockedExerciseId}/submit",
            new SubmitAnswerRequest("answer"),
            TestContext.Current.CancellationToken
        );

        var result = await response.Content.ReadFromJsonAsync<ExerciseSubmitResult>(
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "admins can bypass exercise locks");
        result.Should().NotBeNull();
        result!.IsCorrect.Should().BeTrue();
        result.PointsEarned.Should().Be(10);
    }

    [Fact]
    public async Task ContentCreator_SubmitsToLockedExercise_Success()
    {
        // Arrange
        var lockedExerciseId = Fixture.ExerciseIds[3];

        // Act
        var response = await _creatorClient.PostAsJsonAsync(
            $"/api/exercises/{lockedExerciseId}/submit",
            new SubmitAnswerRequest("answer"),
            TestContext.Current.CancellationToken
        );

        var result = await response.Content.ReadFromJsonAsync<ExerciseSubmitResult>(
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "content creators can bypass exercise locks");
        result.Should().NotBeNull();
        result!.IsCorrect.Should().BeTrue();
        result.PointsEarned.Should().Be(10);
    }

    [Fact]
    public async Task Student_SubmitsToNonexistentExercise_Returns404()
    {
        // Arrange
        var nonexistentId = Guid.NewGuid().ToString();

        // Act
        var response = await _studentClient.PostAsJsonAsync(
            $"/api/exercises/{nonexistentId}/submit",
            new SubmitAnswerRequest("answer"),
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound, "nonexistent exercise returns 404");
    }

    [Fact]
    public async Task Student_SubmitsToExerciseInLockedLesson_Returns403()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<BackendDbContext>();

        // Create a locked lesson with an unlocked exercise inside it
        var lockedLessonId = Guid.NewGuid().ToString();
        var courseId = await ctx.Courses
            .Select(c => c.Id)
            .FirstOrDefaultAsync(TestContext.Current.CancellationToken)
            ?? throw new InvalidOperationException("No course found in fixture");

        ctx.Lessons.Add(
            new Lesson
            {
                Id = lockedLessonId,
                CourseId = courseId,
                Title = "Locked Lesson",
                LessonContent = "{}",
                OrderIndex = 99,
                IsLocked = true, // Lesson is locked
            }
        );
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var exerciseInLockedLessonId = Guid.NewGuid().ToString();
        ctx.Exercises.Add(
            new FillInBlankExercise
            {
                Id = exerciseInLockedLessonId,
                LessonId = lockedLessonId,
                Title = "Exercise in locked lesson",
                Text = "Test",
                CorrectAnswer = "answer",
                CaseSensitive = false,
                TrimWhitespace = true,
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                OrderIndex = 0,
                IsLocked = false, // Exercise itself is unlocked
            }
        );
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var response = await _studentClient.PostAsJsonAsync(
            $"/api/exercises/{exerciseInLockedLessonId}/submit",
            new SubmitAnswerRequest("answer"),
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(
            HttpStatusCode.Forbidden,
            "students cannot submit to exercises in locked lessons, even if exercise itself is unlocked"
        );
    }

    [Fact]
    public async Task Student_SubmitsCorrectMultipleChoiceAnswer_Success()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<BackendDbContext>();

        // Get a MultipleChoice exercise from fixture (ExerciseIds[10-19])
        var mcExerciseId = Fixture.ExerciseIds[10];

        // Unlock it manually for test (bypass admin-only unlock endpoint)
        var mcExercise = await ctx.Exercises.FindAsync(
            [mcExerciseId],
            TestContext.Current.CancellationToken
        );
        if (mcExercise == null)
            throw new InvalidOperationException($"MC exercise {mcExerciseId} not found");
        mcExercise.IsLocked = false;
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Get the correct option ID
        var mcExerciseWithOptions = await ctx.Exercises
            .Include(e => (e as MultipleChoiceExercise)!.Options)
            .FirstOrDefaultAsync(
                e => e.Id == mcExerciseId,
                TestContext.Current.CancellationToken
            );

        var mcCast = mcExerciseWithOptions as MultipleChoiceExercise;
        if (mcCast == null || mcCast.Options.Count == 0)
            throw new InvalidOperationException("MC exercise should have options");

        var correctOption = mcCast.Options.First(o => o.IsCorrect);
        var wrongOption = mcCast.Options.First(o => !o.IsCorrect);

        // Act - submit wrong option first
        var wrongSubmit = await SubmitAnswerAsync(_studentClient, mcExerciseId, wrongOption.Id);

        // Act - submit correct option
        var correctSubmit = await SubmitAnswerAsync(_studentClient, mcExerciseId, correctOption.Id);

        // Assert
        wrongSubmit.Should().NotBeNull();
        wrongSubmit!.IsCorrect.Should().BeFalse("wrong option is incorrect");
        wrongSubmit.PointsEarned.Should().Be(0);

        correctSubmit.Should().NotBeNull();
        correctSubmit!.IsCorrect.Should().BeTrue("correct option ID validates");
        correctSubmit.PointsEarned.Should().Be(10);
    }

    [Fact]
    public async Task GetLessonProgress_ReturnsCorrectStructure()
    {
        // Arrange
        var firstExId = Fixture.ExerciseIds[0];
        var secondExId = Fixture.ExerciseIds[1];

        // Submit correct answer to first exercise to create progress
        await SubmitAnswerAsync(_studentClient, firstExId, "answer");

        // Get lesson ID from first exercise
        var exResponse = await _studentClient.GetAsync(
            $"/api/exercises/{firstExId}",
            TestContext.Current.CancellationToken
        );
        var exDto = await exResponse.Content.ReadFromJsonAsync<ExerciseDto>(
            cancellationToken: TestContext.Current.CancellationToken
        );
        var lessonId = exDto!.LessonId;

        // Act
        var response = await _studentClient.GetAsync(
            $"/api/exercises/lesson/{lessonId}/progress",
            TestContext.Current.CancellationToken
        );

        var progress = await response.Content.ReadFromJsonAsync<List<UserExerciseProgressDto>>(
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        progress.Should().NotBeNull();
        progress!.Should().NotBeEmpty("should have progress records");

        var firstExProgress = progress.FirstOrDefault(p => p.ExerciseId == firstExId);
        firstExProgress.Should().NotBeNull("first exercise should have progress");
        firstExProgress!.IsCompleted.Should().BeTrue();
        firstExProgress.PointsEarned.Should().Be(10);
        firstExProgress.CompletedAt.Should().NotBeNull("completed exercises have timestamp");

        // Second exercise not attempted
        var secondExProgress = progress.FirstOrDefault(p => p.ExerciseId == secondExId);
        if (secondExProgress != null)
        {
            secondExProgress.IsCompleted.Should().BeFalse();
            secondExProgress.PointsEarned.Should().Be(0);
        }
    }

    [Fact]
    public async Task GetLessonSubmissions_ReturnsAllExercisesWithSubmissionState()
    {
        // Arrange
        var firstExId = Fixture.ExerciseIds[0];
        var secondExId = Fixture.ExerciseIds[1];

        // Submit correct answer to first, wrong to second (which unlocks it)
        await SubmitAnswerAsync(_studentClient, firstExId, "answer");
        await SubmitAnswerAsync(_studentClient, secondExId, "wrong");

        // Get lesson ID
        var exResponse = await _studentClient.GetAsync(
            $"/api/exercises/{firstExId}",
            TestContext.Current.CancellationToken
        );
        var exDto = await exResponse.Content.ReadFromJsonAsync<ExerciseDto>(
            cancellationToken: TestContext.Current.CancellationToken
        );
        var lessonId = exDto!.LessonId;

        // Act
        var response = await _studentClient.GetAsync(
            $"/api/exercises/lesson/{lessonId}/submissions",
            TestContext.Current.CancellationToken
        );

        var submissions = await response.Content.ReadFromJsonAsync<List<SubmitAnswerResponse>>(
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        submissions.Should().NotBeNull();
        submissions!.Should().NotBeEmpty("endpoint returns submissions for all exercises");

        // Submissions are ordered by OrderIndex, so index 0 = first exercise, index 1 = second
        var firstExSubmission = submissions[0];
        firstExSubmission.Should().NotBeNull("first exercise was attempted");
        firstExSubmission.IsCorrect.Should().BeTrue();
        firstExSubmission.PointsEarned.Should().Be(10);
        firstExSubmission.CorrectAnswer.Should().BeNull("correct submissions don't reveal answer");

        var secondExSubmission = submissions[1];
        secondExSubmission.Should().NotBeNull("second exercise was attempted");
        secondExSubmission.IsCorrect.Should().BeFalse();
        secondExSubmission.PointsEarned.Should().Be(0);
        secondExSubmission.CorrectAnswer.Should().Be("answer", "wrong submissions reveal correct answer");
    }

    // Helper methods

    private async Task<List<ExerciseDto>?> GetExercisesAsync(string firstExerciseId)
    {
        var exResponse = await _studentClient.GetAsync(
            $"/api/exercises/{firstExerciseId}",
            TestContext.Current.CancellationToken
        );

        if (exResponse.StatusCode != HttpStatusCode.OK)
            throw new InvalidOperationException(
                $"Failed to fetch exercise {firstExerciseId}: {exResponse.StatusCode}"
            );

        var exDto = await exResponse.Content.ReadFromJsonAsync<ExerciseDto>(
            cancellationToken: TestContext.Current.CancellationToken
        );

        if (exDto == null)
            throw new InvalidOperationException("Exercise DTO was null");

        var lessonId = exDto.LessonId;

        var listResponse = await _studentClient.GetAsync(
            $"/api/exercises/lesson/{lessonId}",
            TestContext.Current.CancellationToken
        );

        if (listResponse.StatusCode != HttpStatusCode.OK)
            throw new InvalidOperationException(
                $"Failed to fetch exercises for lesson {lessonId}: {listResponse.StatusCode}"
            );

        return await listResponse.Content.ReadFromJsonAsync<List<ExerciseDto>>(
            cancellationToken: TestContext.Current.CancellationToken
        );
    }

    private async Task<ExerciseSubmitResult?> SubmitAnswerAsync(
        HttpClient client,
        string exerciseId,
        string answer
    )
    {
        var response = await client.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/submit",
            new SubmitAnswerRequest(answer),
            TestContext.Current.CancellationToken
        );

        if (response.StatusCode != HttpStatusCode.OK)
            return null;

        return await response.Content.ReadFromJsonAsync<ExerciseSubmitResult>(
            cancellationToken: TestContext.Current.CancellationToken
        );
    }
}
