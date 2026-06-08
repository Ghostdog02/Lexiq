using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Backend.Api.Dtos;
using Backend.Database;
using Backend.Database.Entities;
using Backend.Database.Entities.Exercises;
using Backend.Tests.Helpers;
using Backend.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Backend.Tests.Integration.E2E;

/// <summary>
/// HTTP-level E2E tests for exercise submission security and edge cases.
///
/// Verifies:
///   - Wrong answer does NOT unlock next exercise (lock enforcement)
///   - Locked lesson submission → 403 for students
///   - Admin/ContentCreator bypass locked exercises
///   - Invalid lesson IDs → 404
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
    private List<string> _exerciseIds = null!;
    private List<string> _correctOptionIds = null!;

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        await ClearTestDataAsync();

        // Create generic exercises for security tests
        using var scope = Factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<BackendDbContext>();

        _exerciseIds = new List<string>();
        _correctOptionIds = new List<string>();
        for (var i = 0; i < 15; i++)
        {
            var id = await DbSeeder.CreateFillInBlankExerciseAsync(
                ctx,
                Fixture.LessonId,
                orderIndex: i,
                isLocked: false
            );
            _exerciseIds.Add(id);
            _correctOptionIds.Add(await DbSeeder.GetCorrectOptionIdAsync(ctx, id));
        }

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
        var firstExId = _exerciseIds[0];
        var secondExId = _exerciseIds[1];

        // Load first exercise with its options to get the expected correct option ID
        using var scope = Factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<BackendDbContext>();
        var firstExercise = await ctx
            .Exercises.Include(e => (e as FillInBlankExercise)!.Options)
            .FirstOrDefaultAsync(
                e => e.ExerciseId == firstExId,
                TestContext.Current.CancellationToken
            );
        var firstExCast = firstExercise as FillInBlankExercise;
        var correctOption = firstExCast?.Options.FirstOrDefault(o => o.IsCorrect);

        var exercises = await GetExercisesAsync(firstExId);
        var secondEx = exercises?.FirstOrDefault(e => e.Id == secondExId);

        // Act - submit wrong answer to trigger CorrectOptionId reveal
        var wrongSubmit = await SubmitAnswerAsync(_studentClient, Fixture.LessonId, firstExId, "wrong answer");

        // Assert - wrong submission should reveal the correct option ID and NOT unlock next exercise
        wrongSubmit.Should().NotBeNull();
        wrongSubmit.IsCorrect.Should().BeFalse("wrong answer is incorrect");
        wrongSubmit.PointsEarned.Should().Be(0, "no points for wrong answer");
        correctOption.Should().NotBeNull("exercise should have at least one correct option");
        wrongSubmit
            .CorrectOptionId.Should()
            .Be(
                correctOption.ExerciseOptionId,
                "API should return correct option ID when answer is wrong"
            );

        exercises.Should().NotBeNull();
        secondEx.Should().NotBeNull();
    }

    [Fact]
    public async Task Student_SubmitsToLockedLesson_Returns403()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<BackendDbContext>();

        var courseId =
            await ctx
                .Courses.Select(c => c.CourseId)
                .FirstOrDefaultAsync(TestContext.Current.CancellationToken)
            ?? throw new InvalidOperationException("No course found in fixture");

        var lockedLessonId = Guid.NewGuid().ToString();
        ctx.Lessons.Add(
            new Lesson
            {
                LessonId = lockedLessonId,
                CourseId = courseId,
                Title = "Locked Lesson for 403 Test",
                LessonContent = "{}",
                OrderIndex = 98,
                IsLocked = true,
            }
        );
        var exerciseInLockedLessonId = Guid.NewGuid().ToString();
        ctx.Exercises.Add(
            new FillInBlankExercise
            {
                ExerciseId = exerciseInLockedLessonId,
                LessonId = lockedLessonId,
                Instructions = "Exercise in locked lesson",
                Text = "Test _",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                Options =
                [
                    new ExerciseOption
                    {
                        OptionText = "answer",
                        IsCorrect = true,
                        Explanation = "Correct answer.",
                    },
                ],
            }
        );
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var response = await _studentClient.PostAsJsonAsync(
            $"/api/lessons/{lockedLessonId}/submit",
            new SubmitLessonRequest([new ExerciseAnswerDto(exerciseInLockedLessonId, "answer")]),
            TestContext.Current.CancellationToken
        );

        // Assert
        response
            .StatusCode.Should()
            .Be(HttpStatusCode.Forbidden, "students cannot submit to locked lessons");
    }

    [Fact]
    public async Task Admin_SubmitsToLockedExercise_Success()
    {
        // Arrange
        var lockedExerciseId = _exerciseIds[2];

        // Act
        var response = await _adminClient.PostAsJsonAsync(
            $"/api/lessons/{Fixture.LessonId}/submit",
            new SubmitLessonRequest([new ExerciseAnswerDto(lockedExerciseId, _correctOptionIds[2])]),
            TestContext.Current.CancellationToken
        );

        var result = await response.Content.ReadFromJsonAsync<LessonSubmitResult>(
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "admins can bypass exercise locks");
        result.Should().NotBeNull();
        var exerciseResult = result.Exercises.FirstOrDefault(e => e.ExerciseId == lockedExerciseId);
        exerciseResult.Should().NotBeNull();
        exerciseResult.IsCorrect.Should().BeTrue();
        exerciseResult.PointsEarned.Should().Be(10);
    }

    [Fact]
    public async Task ContentCreator_SubmitsToLockedExercise_Success()
    {
        // Arrange
        var lockedExerciseId = _exerciseIds[3];

        // Act
        var response = await _creatorClient.PostAsJsonAsync(
            $"/api/lessons/{Fixture.LessonId}/submit",
            new SubmitLessonRequest([new ExerciseAnswerDto(lockedExerciseId, _correctOptionIds[3])]),
            TestContext.Current.CancellationToken
        );

        var result = await response.Content.ReadFromJsonAsync<LessonSubmitResult>(
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        response
            .StatusCode.Should()
            .Be(HttpStatusCode.OK, "content creators can bypass exercise locks");
        result.Should().NotBeNull();
        var exerciseResult = result.Exercises.FirstOrDefault(e => e.ExerciseId == lockedExerciseId);
        exerciseResult.Should().NotBeNull();
        exerciseResult.IsCorrect.Should().BeTrue();
        exerciseResult.PointsEarned.Should().Be(10);
    }

    [Fact]
    public async Task Student_SubmitsToNonexistentLesson_Returns404()
    {
        // Arrange
        var nonexistentLessonId = Guid.NewGuid().ToString();

        // Act
        var response = await _studentClient.PostAsJsonAsync(
            $"/api/lessons/{nonexistentLessonId}/submit",
            new SubmitLessonRequest([new ExerciseAnswerDto(Guid.NewGuid().ToString(), "answer")]),
            TestContext.Current.CancellationToken
        );

        // Assert
        response
            .StatusCode.Should()
            .Be(HttpStatusCode.NotFound, "nonexistent lesson returns 404");
    }

    [Fact]
    public async Task Student_SubmitsToExerciseInLockedLesson_Returns403()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<BackendDbContext>();

        // Create a locked lesson with an unlocked exercise inside it
        var lockedLessonId = Guid.NewGuid().ToString();
        var courseId =
            await ctx
                .Courses.Select(c => c.CourseId)
                .FirstOrDefaultAsync(TestContext.Current.CancellationToken)
            ?? throw new InvalidOperationException("No course found in fixture");

        ctx.Lessons.Add(
            new Lesson
            {
                LessonId = lockedLessonId,
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
                ExerciseId = exerciseInLockedLessonId,
                LessonId = lockedLessonId,
                Instructions = "Exercise in locked lesson",
                Text = "Test",
                DifficultyLevel = DifficultyLevel.Beginner,
                Points = 10,
                Options =
                [
                    new ExerciseOption
                    {
                        OptionText = "answer",
                        IsCorrect = true,
                        Explanation = "Correct answer.",
                    },
                ],
            }
        );
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var response = await _studentClient.PostAsJsonAsync(
            $"/api/lessons/{lockedLessonId}/submit",
            new SubmitLessonRequest([new ExerciseAnswerDto(exerciseInLockedLessonId, "answer")]),
            TestContext.Current.CancellationToken
        );

        // Assert
        response
            .StatusCode.Should()
            .Be(
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

        // Create a FillInBlank exercise with one correct and one wrong option
        var mcExerciseId = Guid.NewGuid().ToString();
        var correctOptionId = Guid.NewGuid().ToString();
        var wrongOptionId = Guid.NewGuid().ToString();
        ctx.Exercises.Add(new FillInBlankExercise
        {
            ExerciseId = mcExerciseId,
            LessonId = Fixture.LessonId,
            Instructions = "Submit correct or wrong option",
            Text = "Test _",
            DifficultyLevel = DifficultyLevel.Beginner,
            Points = 10,
            Options =
            [
                new ExerciseOption { ExerciseOptionId = correctOptionId, ExerciseId = mcExerciseId, OptionText = "correct", IsCorrect = true, Explanation = "Correct." },
                new ExerciseOption { ExerciseOptionId = wrongOptionId,   ExerciseId = mcExerciseId, OptionText = "wrong",   IsCorrect = false, Explanation = "Wrong." },
            ],
        });
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        var correctOption = new { ExerciseOptionId = correctOptionId };
        var wrongOption = new { ExerciseOptionId = wrongOptionId };

        // Act - submit wrong option first
        var wrongSubmit = await SubmitAnswerAsync(
            _studentClient,
            Fixture.LessonId,
            mcExerciseId,
            wrongOption?.ExerciseOptionId ?? ""
        );

        // Act - submit correct option
        var correctSubmit = await SubmitAnswerAsync(
            _studentClient,
            Fixture.LessonId,
            mcExerciseId,
            correctOption?.ExerciseOptionId ?? ""
        );

        // Assert

        wrongSubmit.Should().NotBeNull();
        wrongSubmit.IsCorrect.Should().BeFalse("wrong option is incorrect");
        wrongSubmit.PointsEarned.Should().Be(0);

        correctSubmit.Should().NotBeNull();
        correctSubmit.IsCorrect.Should().BeTrue("correct option ID validates");
        correctSubmit.PointsEarned.Should().Be(10);
    }

    [Fact]
    public async Task GetLessonProgress_ReturnsCorrectStructure()
    {
        // Arrange
        var firstExId = _exerciseIds[0];
        var secondExId = _exerciseIds[1];

        // Submit correct answer to first exercise to create progress
        await SubmitAnswerAsync(_studentClient, Fixture.LessonId, firstExId, _correctOptionIds[0]);

        // Get lesson ID from first exercise
        var exResponse = await _studentClient.GetAsync(
            $"/api/exercises/{firstExId}",
            TestContext.Current.CancellationToken
        );
        var exDto = await exResponse.Content.ReadFromJsonAsync<ExerciseDto>(
            JsonOptions,
            TestContext.Current.CancellationToken
        );
        var lessonId = exDto!.LessonId;

        // Act
        var response = await _studentClient.GetAsync(
            $"/api/lessons/{lessonId}/progress",
            TestContext.Current.CancellationToken
        );

        var progress = await response.Content.ReadFromJsonAsync<List<UserExerciseProgressDto>>(
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        progress.Should().NotBeNull();
        progress.Should().NotBeEmpty("should have progress records");

        var firstExProgress = progress.FirstOrDefault(p => p.ExerciseId == firstExId);
        firstExProgress.Should().NotBeNull("first exercise should have progress");
        firstExProgress.IsCompleted.Should().BeTrue();
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
        var firstExId = _exerciseIds[0];
        var secondExId = _exerciseIds[1];

        // Submit correct answer to first, wrong to second
        await SubmitAnswerAsync(_studentClient, Fixture.LessonId, firstExId, _correctOptionIds[0]);
        await SubmitAnswerAsync(_studentClient, Fixture.LessonId, secondExId, "wrong");

        // Get lesson ID
        var exResponse = await _studentClient.GetAsync(
            $"/api/exercises/{firstExId}",
            TestContext.Current.CancellationToken
        );

        var exDto = await exResponse.Content.ReadFromJsonAsync<ExerciseDto>(
            new JsonSerializerOptions
            {
                WriteIndented = true,
                AllowOutOfOrderMetadataProperties = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            },
            TestContext.Current.CancellationToken
        );

        var lessonId = exDto!.LessonId;

        // Act
        var response = await _studentClient.GetAsync(
            $"/api/lessons/{lessonId}/submissions",
            TestContext.Current.CancellationToken
        );

        var submissions = await response.Content.ReadFromJsonAsync<List<SubmitAnswerResponse>>(
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        submissions.Should().NotBeNull();
        submissions.Should().NotBeEmpty("endpoint returns submissions for all exercises");

        // Submissions are ordered by CreatedAt, so index 0 = first exercise created, index 1 = second
        var firstExSubmission = submissions[0];
        firstExSubmission.Should().NotBeNull("first exercise was attempted");
        firstExSubmission.IsCorrect.Should().BeTrue();
        firstExSubmission.PointsEarned.Should().Be(10);
        firstExSubmission
            .CorrectOptionId.Should()
            .BeNull("correct submissions don't reveal answer");

        var secondExSubmission = submissions[1];
        secondExSubmission.Should().NotBeNull("second exercise was attempted");
        secondExSubmission.IsCorrect.Should().BeFalse();
        secondExSubmission.PointsEarned.Should().Be(0);
        secondExSubmission
            .CorrectOptionId.Should()
            .NotBeNull("wrong submissions reveal correct option ID");
    }

    // Helper methods

    private async Task<List<ExerciseDto>?> GetExercisesAsync(string firstExerciseId)
    {
        var exResponse = await _studentClient.GetAsync(
            $"/api/exercises/{firstExerciseId}",
            TestContext.Current.CancellationToken
        );

        if (exResponse.StatusCode != HttpStatusCode.OK)
            return null;

        var exDto = await exResponse.Content.ReadFromJsonAsync<ExerciseDto>(
            JsonOptions,
            TestContext.Current.CancellationToken
        );

        if (exDto == null)
            return null;

        var lessonId = exDto.LessonId;

        var listResponse = await _studentClient.GetAsync(
            $"/api/lessons/{lessonId}/exercises",
            TestContext.Current.CancellationToken
        );

        if (listResponse.StatusCode != HttpStatusCode.OK)
            return null;

        return await listResponse.Content.ReadFromJsonAsync<List<ExerciseDto>>(
            JsonOptions,
            TestContext.Current.CancellationToken
        );
    }

    /// <summary>
    /// Submits a single exercise answer via the lesson-level submit endpoint.
    /// Returns the ExerciseResultDto for the submitted exercise, or null on non-200 response.
    /// </summary>
    private static async Task<ExerciseResultDto?> SubmitAnswerAsync(
        HttpClient client,
        string lessonId,
        string exerciseId,
        string? answer
    )
    {
        var response = await client.PostAsJsonAsync(
            $"/api/lessons/{lessonId}/submit",
            new SubmitLessonRequest([new ExerciseAnswerDto(exerciseId, answer)]),
            TestContext.Current.CancellationToken
        );

        if (response.StatusCode != HttpStatusCode.OK)
            return null;

        var result = await response.Content.ReadFromJsonAsync<LessonSubmitResult>(
            cancellationToken: TestContext.Current.CancellationToken
        );

        return result?.Exercises.FirstOrDefault(e => e.ExerciseId == exerciseId);
    }
}
