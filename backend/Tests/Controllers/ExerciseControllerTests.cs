using System.Net;
using System.Net.Http.Json;
using Backend.Api.Dtos;
using Backend.Database;
using Backend.Database.Entities.Exercises;
using Backend.Tests.Helpers;
using Backend.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Backend.Tests.Controllers;

/// <summary>
/// Integration tests for ExerciseController endpoints.
/// Covers: GET {id}, POST, PUT {id}, DELETE {id}, POST {exerciseId}/submit, GET {id}/correct-answer
/// </summary>
public class ExerciseControllerTests(DatabaseFixture fixture)
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

        // Create test exercises
        using var scope = Factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<BackendDbContext>();

        _exerciseIds =
        [
            await DbSeeder.CreateFillInBlankExerciseAsync(
                ctx,
                Fixture.LessonId,
                orderIndex: 0,
                isLocked: false,
                points: 10
            ),
            await DbSeeder.CreateMultipleChoiceExerciseAsync(
                ctx,
                Fixture.LessonId,
                orderIndex: 1,
                isLocked: false,
                points: 15
            ),
            await DbSeeder.CreateListeningExerciseAsync(
                ctx,
                Fixture.LessonId,
                orderIndex: 2,
                isLocked: false,
                points: 20
            ),
            await DbSeeder.CreateTranslationExerciseAsync(
                ctx,
                Fixture.LessonId,
                orderIndex: 3,
                isLocked: false,
                points: 25
            ),
        ];

        GC.SuppressFinalize(this);
    }

    public override async ValueTask DisposeAsync()
    {
        _studentClient.Dispose();
        _adminClient.Dispose();
        _creatorClient.Dispose();
        await base.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    #region GET /api/exercises/{id}

    [Fact]
    public async Task GetExercise_UnlockedExercise_ReturnsExerciseDto()
    {
        // Arrange
        var exerciseId = _exerciseIds[0]; // Unlocked FillInBlank

        // Act
        var response = await _studentClient.GetAsync(
            $"/api/exercises/{exerciseId}",
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ExerciseDto>(
            JsonOptions,
            TestContext.Current.CancellationToken
        );
        result.Should().NotBeNull();
        result!.Id.Should().Be(exerciseId);
        result.Should().BeOfType<FillInBlankExerciseDto>();
    }

    [Fact]
    public async Task GetExercise_LockedExercise_Student_Returns403()
    {
        // Arrange - Create locked exercise
        using var scope = Factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<BackendDbContext>();
        var lockedId = await DbSeeder.CreateFillInBlankExerciseAsync(
            ctx,
            Fixture.LessonId,
            orderIndex: 99,
            isLocked: true
        );

        // Act
        var response = await _studentClient.GetAsync(
            $"/api/exercises/{lockedId}",
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode
            .Should()
            .Be(
                HttpStatusCode.Forbidden,
                because: "students cannot access locked exercises - sequential progression enforcement"
            );
    }

    [Fact]
    public async Task GetExercise_LockedExercise_Admin_ReturnsExercise()
    {
        // Arrange - Create locked exercise
        using var scope = Factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<BackendDbContext>();
        var lockedId = await DbSeeder.CreateFillInBlankExerciseAsync(
            ctx,
            Fixture.LessonId,
            orderIndex: 99,
            isLocked: true
        );

        // Act
        var response = await _adminClient.GetAsync(
            $"/api/exercises/{lockedId}",
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK, because: "admins can bypass lock checks");
    }

    [Fact]
    public async Task GetExercise_NonexistentExercise_Returns404()
    {
        // Arrange
        var nonexistentId = Guid.NewGuid().ToString();

        // Act
        var response = await _studentClient.GetAsync(
            $"/api/exercises/{nonexistentId}",
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetExercise_Unauthenticated_Returns401()
    {
        // Arrange
        var unauthenticatedClient = CreateClient(authToken: null);
        var exerciseId = _exerciseIds[0];

        // Act
        var response = await unauthenticatedClient.GetAsync(
            $"/api/exercises/{exerciseId}",
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        unauthenticatedClient.Dispose();
    }

    #endregion

    #region POST /api/exercises

    [Fact]
    public async Task CreateExercise_Admin_ValidDto_CreatesAndReturns201()
    {
        // Arrange
        CreateExerciseDto exerciseDto = new CreateFillInBlankExerciseDto(
            LessonId: Fixture.LessonId,
            Title: "New Fill-in-Blank",
            Instructions: "Complete the sentence",
            EstimatedDurationMinutes: 5,
            DifficultyLevel: DifficultyLevel.Intermediate,
            Points: 20,
            OrderIndex: null,
            Explanation: "Test explanation",
            Text: "The capital of Italy is ____.",
            CorrectAnswer: "Rome",
            AcceptedAnswers: "Roma",
            CaseSensitive: false,
            TrimWhitespace: true
        );

        // Act
        var response = await _adminClient.PostAsJsonAsync(
            "/api/exercises",
            exerciseDto,
            JsonOptions,
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var result = await response.Content.ReadFromJsonAsync<ExerciseDto>(
            JsonOptions,
            TestContext.Current.CancellationToken
        );
        result.Should().NotBeNull();
        result!.Title.Should().Be("New Fill-in-Blank");
        result.Should().BeOfType<FillInBlankExerciseDto>();
    }

    [Fact]
    public async Task CreateExercise_ContentCreator_ValidDto_CreatesExercise()
    {
        // Arrange
        CreateExerciseDto exerciseDto = new CreateFillInBlankExerciseDto(
            LessonId: Fixture.LessonId,
            Title: "Creator Exercise",
            Instructions: null,
            EstimatedDurationMinutes: null,
            DifficultyLevel: DifficultyLevel.Beginner,
            Points: 10,
            OrderIndex: null,
            Explanation: null,
            Text: "Test text",
            CorrectAnswer: "answer",
            AcceptedAnswers: null,
            CaseSensitive: false,
            TrimWhitespace: true
        );

        // Act
        var response = await _creatorClient.PostAsJsonAsync(
            "/api/exercises",
            exerciseDto,
            JsonOptions,
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode
            .Should()
            .Be(
                HttpStatusCode.Created,
                because: "ContentCreator role can create exercises per [Authorize(Roles = 'Admin,ContentCreator')]"
            );
    }

    [Fact]
    public async Task CreateExercise_Student_Returns403()
    {
        // Arrange
        CreateExerciseDto exerciseDto = new CreateFillInBlankExerciseDto(
            LessonId: Fixture.LessonId,
            Title: "Student Exercise",
            Instructions: null,
            EstimatedDurationMinutes: null,
            DifficultyLevel: DifficultyLevel.Beginner,
            Points: 10,
            OrderIndex: null,
            Explanation: null,
            Text: "Test",
            CorrectAnswer: "answer",
            AcceptedAnswers: null,
            CaseSensitive: false,
            TrimWhitespace: true
        );

        // Act
        var response = await _studentClient.PostAsJsonAsync(
            "/api/exercises",
            exerciseDto,
            JsonOptions,
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode
            .Should()
            .Be(
                HttpStatusCode.Forbidden,
                because: "Student role is not in the [Authorize(Roles = 'Admin,ContentCreator')] list"
            );
    }

    #endregion

    #region PUT /api/exercises/{id}

    [Fact]
    public async Task UpdateExercise_Admin_ValidDto_UpdatesAndReturns200()
    {
        // Arrange
        var exerciseId = _exerciseIds[0]; // FillInBlank
        var updateDto = new UpdateExerciseDto(
            Title: "Updated Title",
            Instructions: "Updated instructions",
            EstimatedDurationMinutes: 10,
            DifficultyLevel: DifficultyLevel.Advanced,
            Points: 30,
            OrderIndex: null,
            Explanation: "Updated explanation"
        );

        // Act
        var response = await _adminClient.PutAsJsonAsync(
            $"/api/exercises/{exerciseId}",
            updateDto,
            JsonOptions,
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ExerciseDto>(
            JsonOptions,
            TestContext.Current.CancellationToken
        );
        result.Should().NotBeNull();
        result!.Title.Should().Be("Updated Title");
        result.Points.Should().Be(30);
    }

    [Fact]
    public async Task UpdateExercise_ContentCreator_CanUpdate()
    {
        // Arrange
        var exerciseId = _exerciseIds[1]; // MultipleChoice
        var updateDto = new UpdateExerciseDto(
            Title: "Creator Updated",
            Instructions: null,
            EstimatedDurationMinutes: null,
            DifficultyLevel: null,
            Points: null,
            OrderIndex: null,
            Explanation: null
        );

        // Act
        var response = await _creatorClient.PutAsJsonAsync(
            $"/api/exercises/{exerciseId}",
            updateDto,
            JsonOptions,
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task UpdateExercise_Student_Returns403()
    {
        // Arrange
        var exerciseId = _exerciseIds[0];
        var updateDto = new UpdateExerciseDto(
            Title: "Student Update Attempt",
            Instructions: null,
            EstimatedDurationMinutes: null,
            DifficultyLevel: null,
            Points: null,
            OrderIndex: null,
            Explanation: null
        );

        // Act
        var response = await _studentClient.PutAsJsonAsync(
            $"/api/exercises/{exerciseId}",
            updateDto,
            JsonOptions,
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateExercise_NonexistentExercise_Returns404()
    {
        // Arrange
        var nonexistentId = Guid.NewGuid().ToString();
        var updateDto = new UpdateExerciseDto(
            Title: "Won't work",
            Instructions: null,
            EstimatedDurationMinutes: null,
            DifficultyLevel: null,
            Points: null,
            OrderIndex: null,
            Explanation: null
        );

        // Act
        var response = await _adminClient.PutAsJsonAsync(
            $"/api/exercises/{nonexistentId}",
            updateDto,
            JsonOptions,
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region DELETE /api/exercises/{id}

    [Fact]
    public async Task DeleteExercise_Admin_ExistingExercise_Returns204()
    {
        // Arrange - Create dedicated exercise for deletion
        using var scope = Factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<BackendDbContext>();
        var toDeleteId = await DbSeeder.CreateFillInBlankExerciseAsync(
            ctx,
            Fixture.LessonId,
            orderIndex: 98
        );

        // Act
        var response = await _adminClient.DeleteAsync(
            $"/api/exercises/{toDeleteId}",
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deletion
        var getResponse = await _adminClient.GetAsync(
            $"/api/exercises/{toDeleteId}",
            TestContext.Current.CancellationToken
        );
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteExercise_ContentCreator_CanDelete()
    {
        // Arrange
        using var scope = Factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<BackendDbContext>();
        var toDeleteId = await DbSeeder.CreateFillInBlankExerciseAsync(
            ctx,
            Fixture.LessonId,
            orderIndex: 97
        );

        // Act
        var response = await _creatorClient.DeleteAsync(
            $"/api/exercises/{toDeleteId}",
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteExercise_Student_Returns403()
    {
        // Arrange
        var exerciseId = _exerciseIds[0];

        // Act
        var response = await _studentClient.DeleteAsync(
            $"/api/exercises/{exerciseId}",
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteExercise_NonexistentExercise_Returns404()
    {
        // Arrange
        var nonexistentId = Guid.NewGuid().ToString();

        // Act
        var response = await _adminClient.DeleteAsync(
            $"/api/exercises/{nonexistentId}",
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region GET /api/exercises/{id}/correct-answer

    [Fact]
    public async Task GetCorrectAnswer_FillInBlank_ReturnsCorrectAnswer()
    {
        // Arrange
        var exerciseId = _exerciseIds[0]; // FillInBlankExercise

        // Act
        var response = await _studentClient.GetAsync(
            $"/api/exercises/{exerciseId}/correct-answer",
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CorrectAnswerDto>(
            cancellationToken: TestContext.Current.CancellationToken
        );
        result.Should().NotBeNull();
        result!.CorrectAnswer
            .Should()
            .Be("answer", because: "FillInBlankExercise seed data sets CorrectAnswer to 'answer'");
    }

    [Fact]
    public async Task GetCorrectAnswer_MultipleChoice_ReturnsCorrectOptionText()
    {
        // Arrange
        var exerciseId = _exerciseIds[1]; // MultipleChoiceExercise

        // Act
        var response = await _studentClient.GetAsync(
            $"/api/exercises/{exerciseId}/correct-answer",
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CorrectAnswerDto>(
            cancellationToken: TestContext.Current.CancellationToken
        );
        result.Should().NotBeNull();
        result!.CorrectAnswer
            .Should()
            .Be(
                "answer",
                because: "MultipleChoice seed data sets option at index 1 (IsCorrect=true) with OptionText='answer'"
            );
    }

    [Fact]
    public async Task GetCorrectAnswer_Listening_ReturnsCorrectAnswer()
    {
        // Arrange
        var exerciseId = _exerciseIds[2]; // ListeningExercise

        // Act
        var response = await _studentClient.GetAsync(
            $"/api/exercises/{exerciseId}/correct-answer",
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CorrectAnswerDto>(
            cancellationToken: TestContext.Current.CancellationToken
        );
        result.Should().NotBeNull();
        result!.CorrectAnswer
            .Should()
            .Be("answer", because: "ListeningExercise seed data sets CorrectAnswer to 'answer'");
    }

    [Fact]
    public async Task GetCorrectAnswer_Translation_ReturnsTargetText()
    {
        // Arrange
        var exerciseId = _exerciseIds[3]; // TranslationExercise

        // Act
        var response = await _studentClient.GetAsync(
            $"/api/exercises/{exerciseId}/correct-answer",
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CorrectAnswerDto>(
            cancellationToken: TestContext.Current.CancellationToken
        );
        result.Should().NotBeNull();
        result!.CorrectAnswer
            .Should()
            .Be("answer", because: "TranslationExercise seed data sets TargetText to 'answer'");
    }

    [Fact]
    public async Task GetCorrectAnswer_NonexistentExercise_Returns404()
    {
        // Arrange
        var nonexistentId = Guid.NewGuid().ToString();

        // Act
        var response = await _studentClient.GetAsync(
            $"/api/exercises/{nonexistentId}/correct-answer",
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode
            .Should()
            .Be(
                HttpStatusCode.NotFound,
                because: "endpoint should return 404 for exercises that don't exist in the database"
            );
    }

    [Fact]
    public async Task GetCorrectAnswer_RequiresAuthentication()
    {
        // Arrange
        var unauthenticatedClient = CreateClient(authToken: null);
        var exerciseId = _exerciseIds[0];

        // Act
        var response = await unauthenticatedClient.GetAsync(
            $"/api/exercises/{exerciseId}/correct-answer",
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode
            .Should()
            .Be(
                HttpStatusCode.Unauthorized,
                because: "ExerciseController has [Authorize] at class level - all endpoints require authentication"
            );
        unauthenticatedClient.Dispose();
    }

    [Fact]
    public async Task GetCorrectAnswer_Student_CanAccess()
    {
        // Arrange
        var exerciseId = _exerciseIds[0];

        // Act
        var response = await _studentClient.GetAsync(
            $"/api/exercises/{exerciseId}/correct-answer",
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode
            .Should()
            .Be(
                HttpStatusCode.OK,
                because: "endpoint has no role restriction - any authenticated user (including Students) can access it"
            );
    }

    [Fact]
    public async Task GetCorrectAnswer_Admin_CanAccess()
    {
        // Arrange
        var exerciseId = _exerciseIds[0];

        // Act
        var response = await _adminClient.GetAsync(
            $"/api/exercises/{exerciseId}/correct-answer",
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode
            .Should()
            .Be(HttpStatusCode.OK, because: "Admins can access all authenticated endpoints");
    }

    [Fact]
    public async Task GetCorrectAnswer_MultipleChoiceWithNoCorrectOption_ReturnsNull()
    {
        // Arrange - Create a broken MultipleChoice exercise with no correct option
        using var scope = Factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<BackendDbContext>();

        var brokenExercise = new MultipleChoiceExercise
        {
            Id = Guid.NewGuid().ToString(),
            LessonId = Fixture.LessonId,
            Title = "Broken MC Exercise",
            Points = 10,
            OrderIndex = 99,
            DifficultyLevel = DifficultyLevel.Beginner,
            IsLocked = false,
            Options =
            [
                new ExerciseOption
                {
                    Id = Guid.NewGuid().ToString(),
                    OptionText = "Wrong 1",
                    IsCorrect = false,
                    OrderIndex = 0,
                },
                new ExerciseOption
                {
                    Id = Guid.NewGuid().ToString(),
                    OptionText = "Wrong 2",
                    IsCorrect = false,
                    OrderIndex = 1,
                },
            ],
        };

        ctx.Exercises.Add(brokenExercise);
        await ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var response = await _studentClient.GetAsync(
            $"/api/exercises/{brokenExercise.Id}/correct-answer",
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<CorrectAnswerDto>(
            cancellationToken: TestContext.Current.CancellationToken
        );
        result.Should().NotBeNull();
        result!.CorrectAnswer
            .Should()
            .BeNull(
                because: "when no option has IsCorrect=true, FirstOrDefault returns null and switch returns null"
            );
    }

    #endregion
}
