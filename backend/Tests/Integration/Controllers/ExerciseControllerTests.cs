using Backend.Database;
using Backend.Tests.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Backend.Tests.Integration.Controllers;

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
        scope.ServiceProvider.GetRequiredService<BackendDbContext>();

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
}
