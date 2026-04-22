using System.Net;
using System.Net.Http.Json;
using Backend.Database.Entities.Users;
using Backend.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Backend.Tests.Controllers;

/// <summary>
/// Integration tests for RoleManagementController endpoints.
/// Covers: GET role by email
/// </summary>
public class RoleManagementControllerTests(DatabaseFixture fixture)
    : ControllerTestBase(fixture),
        IClassFixture<DatabaseFixture>
{
    private HttpClient _adminClient = null!;
    private HttpClient _studentClient = null!;
    private string _adminToken = null!;
    private string _studentToken = null!;
    private string _testUserEmail = null!;

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        await ClearTestDataAsync();

        // Create admin and student users
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

        // Create a test user with ContentCreator role
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Ensure ContentCreator role exists
        if (!await roleManager.RoleExistsAsync("ContentCreator"))
        {
            await roleManager.CreateAsync(new IdentityRole("ContentCreator"));
        }

        var testUser = new User
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "creator",
            Email = "creator@test.com",
            NormalizedUserName = "CREATOR",
            NormalizedEmail = "CREATOR@TEST.COM",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            RegistrationDate = DateTime.UtcNow,
        };
        await userManager.CreateAsync(testUser);
        await userManager.AddToRoleAsync(testUser, "ContentCreator");
        _testUserEmail = testUser.Email!;

        GC.SuppressFinalize(this);
    }

    public override async ValueTask DisposeAsync()
    {
        _adminClient.Dispose();
        _studentClient.Dispose();
        await base.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    #region GET /api/roleManagement/{email}

    [Fact]
    public async Task GetRoleByEmail_Admin_ExistingUser_ReturnsRole()
    {
        // Act
        var response = await _adminClient.GetAsync(
            $"/api/roleManagement/{_testUserEmail}",
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var role = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        // Response is JSON string, so it will be quoted
        role.Should().Contain("ContentCreator");
    }

    [Fact]
    public async Task GetRoleByEmail_Admin_AdminUser_ReturnsAdminRole()
    {
        // Act
        var response = await _adminClient.GetAsync(
            "/api/roleManagement/admin@test.com",
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var role = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        role.Should().Contain("Admin");
    }

    [Fact]
    public async Task GetRoleByEmail_Admin_StudentUser_ReturnsStudentRole()
    {
        // Act
        var response = await _adminClient.GetAsync(
            "/api/roleManagement/student@test.com",
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var role = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        role.Should().Contain("Student");
    }

    [Fact]
    public async Task GetRoleByEmail_Admin_NonexistentEmail_Returns404()
    {
        // Act
        var response = await _adminClient.GetAsync(
            "/api/roleManagement/nonexistent@test.com",
            TestContext.Current.CancellationToken
        );

        // Assert
        response
            .StatusCode.Should()
            .Be(HttpStatusCode.NotFound, because: "user with that email does not exist");
    }

    [Fact]
    public async Task GetRoleByEmail_Admin_UserWithNoRole_Returns404()
    {
        // Arrange - Create a user without any role
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        var userWithoutRole = new User
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "norole",
            Email = "norole@test.com",
            NormalizedUserName = "NOROLE",
            NormalizedEmail = "NOROLE@TEST.COM",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            RegistrationDate = DateTime.UtcNow,
        };
        await userManager.CreateAsync(userWithoutRole);

        // Act
        var response = await _adminClient.GetAsync(
            $"/api/roleManagement/{userWithoutRole.Email}",
            TestContext.Current.CancellationToken
        );

        // Assert
        response
            .StatusCode.Should()
            .Be(
                HttpStatusCode.NotFound,
                because: "controller returns 404 when user exists but has no role assigned"
            );
    }

    [Fact]
    public async Task GetRoleByEmail_Student_Returns403()
    {
        // Act
        var response = await _studentClient.GetAsync(
            $"/api/roleManagement/{_testUserEmail}",
            TestContext.Current.CancellationToken
        );

        // Assert
        response
            .StatusCode.Should()
            .Be(
                HttpStatusCode.Forbidden,
                because: "RoleManagementController has [Authorize(Roles = 'Admin')] - Students cannot access"
            );
    }

    [Fact]
    public async Task GetRoleByEmail_Unauthenticated_Returns401()
    {
        // Arrange
        var unauthenticatedClient = CreateClient(authToken: null);

        // Act
        var response = await unauthenticatedClient.GetAsync(
            $"/api/roleManagement/{_testUserEmail}",
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        unauthenticatedClient.Dispose();
    }

    #endregion
}
