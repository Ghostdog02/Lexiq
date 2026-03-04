using System.Net;
using System.Net.Http.Json;
using Backend.Api.Dtos;
using Backend.Database.Entities.Exercises;
using Backend.Tests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.EndToEnd;

/// <summary>
/// HTTP-level E2E tests for content creator and admin workflows.
///
/// Verifies:
///   - Admin creates course → lesson → exercises → student can access
///   - ContentCreator creates content → student interacts
///   - Admin manual unlock → student sees unlock
///   - Admin updates lesson → student sees changes
///   - Admin deletes lesson → student loses access
/// </summary>
public class ContentCreatorJourneyTests(DatabaseFixture fixture)
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
        // Arrange: Admin creates a new lesson with exercises in the existing course
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

        // Act: Admin creates lesson
        var createResponse = await _adminClient.PostAsJsonAsync(
            "/api/lessons",
            createLessonDto,
            TestContext.Current.CancellationToken
        );

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdLesson = await createResponse.Content.ReadFromJsonAsync<LessonDto>(
            cancellationToken: TestContext.Current.CancellationToken
        );

        createdLesson.Should().NotBeNull();
        createdLesson!.Title.Should().Be("Admin Test Lesson");
        createdLesson.IsLocked.Should().BeTrue("new lessons are locked by default");

        // Admin manually unlocks the lesson
        var unlockResponse = await _adminClient.PostAsync(
            $"/api/lessons/{createdLesson.LessonId}/unlock",
            null,
            TestContext.Current.CancellationToken
        );
        unlockResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Student fetches the lesson
        var studentFetchResponse = await _studentClient.GetAsync(
            $"/api/lessons/{createdLesson.LessonId}",
            TestContext.Current.CancellationToken
        );

        studentFetchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var studentLesson = await studentFetchResponse.Content.ReadFromJsonAsync<LessonDto>(
            cancellationToken: TestContext.Current.CancellationToken
        );

        studentLesson.Should().NotBeNull();
        studentLesson!.IsLocked.Should().BeFalse("admin unlocked it");
        studentLesson.Exercises.Should().HaveCount(1);

        // Student completes the exercise
        var exerciseId = studentLesson.Exercises[0].Id;
        var submitResponse = await _studentClient.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/submit",
            new SubmitAnswerRequest("answer"),
            TestContext.Current.CancellationToken
        );

        submitResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var submitResult = await submitResponse.Content.ReadFromJsonAsync<ExerciseSubmitResult>(
            cancellationToken: TestContext.Current.CancellationToken
        );

        submitResult.Should().NotBeNull();
        submitResult!.IsCorrect.Should().BeTrue();
        submitResult.PointsEarned.Should().Be(10);
    }

    [Fact]
    public async Task Admin_UpdatesLesson_StudentSeesChanges()
    {
        // Arrange: Admin creates a lesson
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
            "/api/lessons",
            createLessonDto,
            TestContext.Current.CancellationToken
        );
        var createdLesson = await createResponse.Content.ReadFromJsonAsync<LessonDto>(
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Unlock so student can see it
        await _adminClient.PostAsync(
            $"/api/lessons/{createdLesson!.LessonId}/unlock",
            null,
            TestContext.Current.CancellationToken
        );

        // Act: Admin updates the lesson
        var updateDto = new UpdateLessonDto(
            CourseId: null,
            Title: "Updated Title",
            Description: "Updated description",
            EstimatedDurationMinutes: 45,
            OrderIndex: null,
            LessonContent: "{\"updated\": true}"
        );

        var updateResponse = await _adminClient.PutAsJsonAsync(
            $"/api/lessons/{createdLesson.LessonId}",
            updateDto,
            TestContext.Current.CancellationToken
        );

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Assert: Student sees updated content
        var studentFetchResponse = await _studentClient.GetAsync(
            $"/api/lessons/{createdLesson.LessonId}",
            TestContext.Current.CancellationToken
        );

        var updatedLesson = await studentFetchResponse.Content.ReadFromJsonAsync<LessonDto>(
            cancellationToken: TestContext.Current.CancellationToken
        );

        updatedLesson.Should().NotBeNull();
        updatedLesson!.Title.Should().Be("Updated Title");
        updatedLesson.Description.Should().Be("Updated description");
        updatedLesson.EstimatedDurationMinutes.Should().Be(45);
    }

    [Fact]
    public async Task Admin_DeletesLesson_StudentCannotAccess()
    {
        // Arrange: Admin creates and unlocks a lesson
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
            "/api/lessons",
            createLessonDto,
            TestContext.Current.CancellationToken
        );
        var createdLesson = await createResponse.Content.ReadFromJsonAsync<LessonDto>(
            cancellationToken: TestContext.Current.CancellationToken
        );

        await _adminClient.PostAsync(
            $"/api/lessons/{createdLesson!.LessonId}/unlock",
            null,
            TestContext.Current.CancellationToken
        );

        // Student can initially access
        var initialFetch = await _studentClient.GetAsync(
            $"/api/lessons/{createdLesson.LessonId}",
            TestContext.Current.CancellationToken
        );
        initialFetch.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act: Admin deletes the lesson
        var deleteResponse = await _adminClient.DeleteAsync(
            $"/api/lessons/{createdLesson.LessonId}",
            TestContext.Current.CancellationToken
        );

        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Assert: Student cannot access deleted lesson
        var studentFetchAfterDelete = await _studentClient.GetAsync(
            $"/api/lessons/{createdLesson.LessonId}",
            TestContext.Current.CancellationToken
        );

        studentFetchAfterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ContentCreator_CreatesLesson_LockedByDefault()
    {
        // Arrange: Create a ContentCreator user
        var (_, creatorToken) = await CreateAuthenticatedUserAsync(
            "creator",
            "creator@test.com",
            "ContentCreator"
        );
        var creatorClient = CreateClient(creatorToken);

        var courseId = await GetExistingCourseIdAsync();

        // Act: ContentCreator creates a lesson
        var createLessonDto = new CreateLessonDto(
            CourseId: courseId,
            Title: "Creator Lesson",
            Description: "Created by ContentCreator",
            EstimatedDurationMinutes: 30,
            OrderIndex: 102,
            Content: "{}",
            Exercises: null
        );

        var createResponse = await creatorClient.PostAsJsonAsync(
            "/api/lessons",
            createLessonDto,
            TestContext.Current.CancellationToken
        );

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdLesson = await createResponse.Content.ReadFromJsonAsync<LessonDto>(
            cancellationToken: TestContext.Current.CancellationToken
        );

        // Assert: Lesson is locked, student cannot access exercises
        createdLesson.Should().NotBeNull();
        createdLesson!.IsLocked.Should().BeTrue("new lessons are locked by default");

        // Student tries to access - should see 403 or empty exercises
        var studentFetchResponse = await _studentClient.GetAsync(
            $"/api/lessons/{createdLesson.LessonId}",
            TestContext.Current.CancellationToken
        );

        // Locked lessons might return 403 or 200 with locked=true depending on policy
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
        // Arrange: Admin creates a lesson with one exercise
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
            "/api/lessons",
            createLessonDto,
            TestContext.Current.CancellationToken
        );
        var createdLesson = await createResponse.Content.ReadFromJsonAsync<LessonDto>(
            cancellationToken: TestContext.Current.CancellationToken
        );

        await _adminClient.PostAsync(
            $"/api/lessons/{createdLesson!.LessonId}/unlock",
            null,
            TestContext.Current.CancellationToken
        );

        // Act: Admin adds a second exercise to the lesson
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

        addExerciseResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Assert: Student sees both exercises
        var studentFetchResponse = await _studentClient.GetAsync(
            $"/api/lessons/{createdLesson.LessonId}",
            TestContext.Current.CancellationToken
        );

        var lessonWithExercises = await studentFetchResponse.Content.ReadFromJsonAsync<LessonDto>(
            cancellationToken: TestContext.Current.CancellationToken
        );

        lessonWithExercises.Should().NotBeNull();
        lessonWithExercises!.Exercises.Should().HaveCount(2);
        lessonWithExercises.Exercises.Should().Contain(e => e.Title == "Exercise 1");
        lessonWithExercises.Exercises.Should().Contain(e => e.Title == "Exercise 2");
    }

    [Fact]
    public async Task Student_CannotCreateCourse_Returns403()
    {
        // Arrange: Student tries to create a course
        var createCourseDto = new CreateCourseDto(
            LanguageName: "Italian",
            Title: "Student Course",
            Description: "Should fail",
            EstimatedDurationHours: 10,
            OrderIndex: 0
        );

        // Act: Student POSTs to /api/courses
        var response = await _studentClient.PostAsJsonAsync(
            "/api/courses",
            createCourseDto,
            TestContext.Current.CancellationToken
        );

        // Assert: 403 Forbidden (role-based authorization)
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // Helper methods

    private async Task<string> GetExistingCourseIdAsync()
    {
        // Fetch courses to get the seeded course ID
        var response = await _adminClient.GetAsync(
            "/api/courses",
            TestContext.Current.CancellationToken
        );
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var courses =
            await response.Content.ReadFromJsonAsync<List<CourseDto>>(
                cancellationToken: TestContext.Current.CancellationToken
            ) ?? [];

        courses.Should().NotBeEmpty("fixture seeds at least one course");
        return courses.First().CourseId;
    }
}
