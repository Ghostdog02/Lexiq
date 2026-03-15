using System.Net;
using System.Net.Http.Json;
using Backend.Api.Dtos;
using Backend.Database.Entities.Exercises;
using Backend.Tests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.EndToEnd;

/// <summary>
/// HTTP-level E2E tests for admin and content creator CRUD workflows.
///
/// Verifies:
///   - Admin creates course/lesson/exercises → students can access after unlock
///   - ContentCreator creates content → locked by default → students blocked
///   - Admin manually unlocks lesson → students gain immediate access
///   - Admin updates lesson → students see changes
///   - Admin deletes lesson → students lose access (404)
///   - Admin adds exercises to existing lesson → students see new exercises
///   - Student role cannot create courses (403)
/// </summary>
public class AdminContentManagementJourneyTests(DatabaseFixture fixture)
    : ControllerTestBase(fixture),
        IClassFixture<DatabaseFixture>
{
    private HttpClient _adminClient = null!;
    private HttpClient _studentClient = null!;
    private string _adminToken = null!;
    private string _studentToken = null!;

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        await ClearTestDataAsync(); // Clean state before each test

        // Create authenticated users
        var (_, adminToken) = await CreateAuthenticatedUserAsync(
            "admin",
            "admin@test.com",
            "Admin"
        );
        var (_, studentToken) = await CreateAuthenticatedUserAsync(
            "student",
            "student@test.com",
            "Student"
        );

        _adminToken = adminToken;
        _studentToken = studentToken;

        _adminClient = CreateClient(_adminToken);
        _studentClient = CreateClient(_studentToken);
    }

    public override async ValueTask DisposeAsync()
    {
        _adminClient.Dispose();
        _studentClient.Dispose();
        await base.DisposeAsync();
    }

    [Fact]
    public async Task Admin_CreatesLesson_StudentCanAccessAndSolve()
    {
        // Arrange
        var courseId = await GetExistingCourseIdAsync();

        var createLessonDto = new CreateLessonDto(
            CourseId: courseId,
            Title: "Admin Test Lesson",
            Description: "Created by admin",
            EstimatedDurationMinutes: 30,
            OrderIndex: 99, // High index to avoid conflicts
            Content: "{}",
            Exercises:
            [
                new CreateFillInBlankExerciseDto(
                    LessonId: null,
                    Title: "Test Exercise 1",
                    Instructions: "Fill in",
                    EstimatedDurationMinutes: 5,
                    DifficultyLevel: DifficultyLevel.Beginner,
                    Points: 10,
                    OrderIndex: 0,
                    Explanation: "Good job",
                    Text: "Test _",
                    CorrectAnswer: "answer",
                    AcceptedAnswers: null,
                    CaseSensitive: false,
                    TrimWhitespace: true
                ),
            ]
        );

        // Act
        var createResponse = await _adminClient.PostAsJsonAsync(
            "/api/lessonss",
            createLessonDto,
            TestContext.Current.CancellationToken
        );

        var createdLesson = await createResponse.Content.ReadFromJsonAsync<LessonDto>(
            cancellationToken: TestContext.Current.CancellationToken
        );

        var unlockResponse = await _adminClient.PostAsync(
            $"/api/lessonss/{createdLesson!.LessonId}/unlock",
            null,
            TestContext.Current.CancellationToken
        );

        var studentFetchResponse = await _studentClient.GetAsync(
            $"/api/lessonss/{createdLesson.LessonId}",
            TestContext.Current.CancellationToken
        );

        var studentLesson = await studentFetchResponse.Content.ReadFromJsonAsync<LessonDto>(
            cancellationToken: TestContext.Current.CancellationToken
        );

        var exerciseId = studentLesson!.Exercises[0].Id;
        var submitResponse = await _studentClient.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/submit",
            new SubmitAnswerRequest("answer"),
            TestContext.Current.CancellationToken
        );

        var submitResult = await submitResponse.Content.ReadFromJsonAsync<ExerciseSubmitResult>(
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        courseId.Should().NotBeNull();
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        createdLesson.Should().NotBeNull();
        createdLesson!.Title.Should().Be("Admin Test Lesson");
        createdLesson.IsLocked.Should().BeTrue("new lessons are locked by default");

        unlockResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        studentFetchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        studentLesson.Should().NotBeNull();
        studentLesson!.IsLocked.Should().BeFalse("admin unlocked it");
        studentLesson.Exercises.Should().HaveCount(1);

        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        submitResult.Should().NotBeNull();
        submitResult!.IsCorrect.Should().BeTrue();
        submitResult.PointsEarned.Should().Be(10);
    }

    [Fact]
    public async Task Admin_UpdatesLesson_StudentSeesChanges()
    {
        // Arrange
        var courseId = await GetExistingCourseIdAsync();
        var createLessonDto = new CreateLessonDto(
            CourseId: courseId,
            Title: "Original Title",
            Description: "Original description",
            EstimatedDurationMinutes: 30,
            OrderIndex: 100,
            Content: "{}",
            Exercises: null
        );

        var createResponse = await _adminClient.PostAsJsonAsync(
            "/api/lessonss",
            createLessonDto,
            TestContext.Current.CancellationToken
        );
        var createdLesson = await createResponse.Content.ReadFromJsonAsync<LessonDto>(
            cancellationToken: TestContext.Current.CancellationToken
        );

        await _adminClient.PostAsync(
            $"/api/lessonss/{createdLesson!.LessonId}/unlock",
            null,
            TestContext.Current.CancellationToken
        );

        // Act
        var updateDto = new UpdateLessonDto(
            CourseId: null,
            Title: "Updated Title",
            Description: "Updated description",
            EstimatedDurationMinutes: 45,
            OrderIndex: null,
            LessonContent: "{\"updated\": true}"
        );

        var updateResponse = await _adminClient.PutAsJsonAsync(
            $"/api/lessonss/{createdLesson.LessonId}",
            updateDto,
            TestContext.Current.CancellationToken
        );

        var studentFetchResponse = await _studentClient.GetAsync(
            $"/api/lessonss/{createdLesson.LessonId}",
            TestContext.Current.CancellationToken
        );

        var updatedLesson = await studentFetchResponse.Content.ReadFromJsonAsync<LessonDto>(
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        courseId.Should().NotBeNull();
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        updatedLesson.Should().NotBeNull();
        updatedLesson!.Title.Should().Be("Updated Title");
        updatedLesson.Description.Should().Be("Updated description");
        updatedLesson.EstimatedDurationMinutes.Should().Be(45);
    }

    [Fact]
    public async Task Admin_DeletesLesson_StudentCannotAccess()
    {
        // Arrange
        var courseId = await GetExistingCourseIdAsync();
        var createLessonDto = new CreateLessonDto(
            CourseId: courseId,
            Title: "To Be Deleted",
            Description: null,
            EstimatedDurationMinutes: 30,
            OrderIndex: 101,
            Content: "{}",
            Exercises: null
        );

        var createResponse = await _adminClient.PostAsJsonAsync(
            "/api/lessonss",
            createLessonDto,
            TestContext.Current.CancellationToken
        );
        var createdLesson = await createResponse.Content.ReadFromJsonAsync<LessonDto>(
            cancellationToken: TestContext.Current.CancellationToken
        );

        await _adminClient.PostAsync(
            $"/api/lessonss/{createdLesson!.LessonId}/unlock",
            null,
            TestContext.Current.CancellationToken
        );

        var initialFetch = await _studentClient.GetAsync(
            $"/api/lessonss/{createdLesson.LessonId}",
            TestContext.Current.CancellationToken
        );

        // Act
        var deleteResponse = await _adminClient.DeleteAsync(
            $"/api/lessonss/{createdLesson.LessonId}",
            TestContext.Current.CancellationToken
        );

        var studentFetchAfterDelete = await _studentClient.GetAsync(
            $"/api/lessonss/{createdLesson.LessonId}",
            TestContext.Current.CancellationToken
        );

        // Assert
        courseId.Should().NotBeNull();
        initialFetch.StatusCode.Should().Be(HttpStatusCode.OK);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        studentFetchAfterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ContentCreator_CreatesLesson_LockedByDefault()
    {
        // Arrange
        var (_, creatorToken) = await CreateAuthenticatedUserAsync(
            "creator",
            "creator@test.com",
            "ContentCreator"
        );
        var creatorClient = CreateClient(creatorToken);

        var courseId = await GetExistingCourseIdAsync();

        var createLessonDto = new CreateLessonDto(
            CourseId: courseId,
            Title: "Creator Lesson",
            Description: "Created by ContentCreator",
            EstimatedDurationMinutes: 30,
            OrderIndex: 102,
            Content: "{}",
            Exercises: null
        );

        // Act
        var createResponse = await creatorClient.PostAsJsonAsync(
            "/api/lessonss",
            createLessonDto,
            TestContext.Current.CancellationToken
        );

        var createdLesson = await createResponse.Content.ReadFromJsonAsync<LessonDto>(
            cancellationToken: TestContext.Current.CancellationToken
        );

        var studentFetchResponse = await _studentClient.GetAsync(
            $"/api/lessonss/{createdLesson!.LessonId}",
            TestContext.Current.CancellationToken
        );

        // Assert
        courseId.Should().NotBeNull();
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        createdLesson.Should().NotBeNull();
        createdLesson!.IsLocked.Should().BeTrue("new lessons are locked by default");

        if (studentFetchResponse.StatusCode == HttpStatusCode.OK)
        {
            var studentLesson = await studentFetchResponse.Content.ReadFromJsonAsync<LessonDto>(
                cancellationToken: TestContext.Current.CancellationToken
            );
            studentLesson!.IsLocked.Should().BeTrue();
        }
        else
        {
            studentFetchResponse
                .StatusCode.Should()
                .Be(HttpStatusCode.Forbidden, "student cannot access locked lesson");
        }

        creatorClient.Dispose();
    }

    [Fact]
    public async Task Admin_AddsExerciseToExistingLesson_StudentSeesNewExercise()
    {
        // Arrange
        var courseId = await GetExistingCourseIdAsync();
        var createLessonDto = new CreateLessonDto(
            CourseId: courseId,
            Title: "Expandable Lesson",
            Description: null,
            EstimatedDurationMinutes: 30,
            OrderIndex: 103,
            Content: "{}",
            Exercises:
            [
                new CreateFillInBlankExerciseDto(
                    LessonId: null,
                    Title: "Exercise 1",
                    Instructions: null,
                    EstimatedDurationMinutes: 5,
                    DifficultyLevel: DifficultyLevel.Beginner,
                    Points: 10,
                    OrderIndex: 0,
                    Explanation: null,
                    Text: "First",
                    CorrectAnswer: "one",
                    AcceptedAnswers: null,
                    CaseSensitive: false,
                    TrimWhitespace: true
                ),
            ]
        );

        var createResponse = await _adminClient.PostAsJsonAsync(
            "/api/lessonss",
            createLessonDto,
            TestContext.Current.CancellationToken
        );
        var createdLesson = await createResponse.Content.ReadFromJsonAsync<LessonDto>(
            cancellationToken: TestContext.Current.CancellationToken
        );

        await _adminClient.PostAsync(
            $"/api/lessonss/{createdLesson!.LessonId}/unlock",
            null,
            TestContext.Current.CancellationToken
        );

        // Act
        var addExerciseDto = new CreateFillInBlankExerciseDto(
            LessonId: createdLesson.LessonId,
            Title: "Exercise 2",
            Instructions: null,
            EstimatedDurationMinutes: 5,
            DifficultyLevel: DifficultyLevel.Beginner,
            Points: 10,
            OrderIndex: 1,
            Explanation: null,
            Text: "Second",
            CorrectAnswer: "two",
            AcceptedAnswers: null,
            CaseSensitive: false,
            TrimWhitespace: true
        );

        var addExerciseResponse = await _adminClient.PostAsJsonAsync(
            "/api/exercises",
            addExerciseDto,
            TestContext.Current.CancellationToken
        );

        var studentFetchResponse = await _studentClient.GetAsync(
            $"/api/lessonss/{createdLesson.LessonId}",
            TestContext.Current.CancellationToken
        );

        var lessonWithExercises = await studentFetchResponse.Content.ReadFromJsonAsync<LessonDto>(
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert
        courseId.Should().NotBeNull();
        addExerciseResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        lessonWithExercises.Should().NotBeNull();
        lessonWithExercises!.Exercises.Should().HaveCount(2);
        lessonWithExercises.Exercises.Should().Contain(e => e.Title == "Exercise 1");
        lessonWithExercises.Exercises.Should().Contain(e => e.Title == "Exercise 2");
    }

    [Fact]
    public async Task Student_CannotCreateCourse_Returns403()
    {
        // Arrange
        var createCourseDto = new CreateCourseDto(
            LanguageName: "Italian",
            Title: "Student Course",
            Description: "Should fail",
            EstimatedDurationHours: 10,
            OrderIndex: 0
        );

        // Act
        var response = await _studentClient.PostAsJsonAsync(
            "/api/courses",
            createCourseDto,
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // Helper methods

    private async Task<string?> GetExistingCourseIdAsync()
    {
        var response = await _adminClient.GetAsync(
            "/api/courses",
            TestContext.Current.CancellationToken
        );

        if (response.StatusCode != HttpStatusCode.OK)
            return null;

        var courses =
            await response.Content.ReadFromJsonAsync<List<CourseDto>>(
                cancellationToken: TestContext.Current.CancellationToken
            ) ?? [];

        if (courses.Count == 0)
            return null;

        return courses.First().CourseId;
    }
}
