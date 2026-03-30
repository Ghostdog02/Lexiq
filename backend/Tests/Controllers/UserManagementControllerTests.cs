using System.Net;
using System.Net.Http.Json;
using Backend.Api.Dtos;
using Backend.Database;
using Backend.Database.Entities.Users;
using Backend.Tests.Helpers;
using Backend.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Backend.Tests.Controllers;

/// <summary>
/// Integration tests for UserManagementController endpoints.
/// Covers: GET all, GET by id, GET by email, POST assignRole, PUT update, PUT updateLoginDate, DELETE
/// </summary>
public class UserManagementControllerTests(DatabaseFixture fixture)
    : ControllerTestBase(fixture),
        IClassFixture<DatabaseFixture>
{
    private HttpClient _adminClient = null!;
    private HttpClient _studentClient = null!;
    private string _adminToken = null!;
    private string _studentToken = null!;
    private string _testUserId = null!;

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        await ClearTestDataAsync();

        // Create admin and student users
        var (adminId, adminToken) = await CreateAuthenticatedUserAsync(
            "admin",
            "admin@test.com",
            "Admin"
        );
        var (studentId, studentToken) = await CreateAuthenticatedUserAsync(
            "student",
            "student@test.com",
            "Student"
        );

        _adminToken = adminToken;
        _studentToken = studentToken;
        _adminClient = CreateClient(_adminToken);
        _studentClient = CreateClient(_studentToken);

        // Create a test user for manipulation
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var testUser = new User
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "testuser",
            Email = "testuser@test.com",
            NormalizedUserName = "TESTUSER",
            NormalizedEmail = "TESTUSER@TEST.COM",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            RegistrationDate = DateTime.UtcNow,
        };
        await userManager.CreateAsync(testUser);
        await userManager.AddToRoleAsync(testUser, "Student");
        _testUserId = testUser.Id;

        GC.SuppressFinalize(this);
    }

    public override async ValueTask DisposeAsync()
    {
        _adminClient.Dispose();
        _studentClient.Dispose();
        await base.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    #region GET /api/userManagement

    [Fact]
    public async Task GetAll_Admin_ReturnsAllUsers()
    {
        // Act
        var response = await _adminClient.GetAsync(
            "/api/userManagement",
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var users = await response.Content.ReadFromJsonAsync<List<UserDetailsDto>>(
            cancellationToken: TestContext.Current.CancellationToken
        );
        users.Should().NotBeNull();
        users!
            .Should()
            .HaveCountGreaterThanOrEqualTo(
                3,
                because: "At least the admin, student, and test user should be returned"
            );
    }

    [Fact]
    public async Task GetAll_Student_Returns403()
    {
        // Act
        var response = await _studentClient.GetAsync(
            "/api/userManagement",
            TestContext.Current.CancellationToken
        );

        // Assert
        response
            .StatusCode.Should()
            .Be(
                HttpStatusCode.Forbidden,
                because: "UserManagementController has [Authorize(Roles = 'Admin')] - Students cannot access"
            );
    }

    [Fact]
    public async Task GetAll_Unauthenticated_Returns401()
    {
        // Arrange
        var unauthenticatedClient = CreateClient(authToken: null);

        // Act
        var response = await unauthenticatedClient.GetAsync(
            "/api/userManagement",
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        unauthenticatedClient.Dispose();
    }

    #endregion

    #region GET /api/userManagement/{id}

    [Fact]
    public async Task GetById_Admin_ExistingUser_ReturnsUser()
    {
        // Act
        var response = await _adminClient.GetAsync(
            $"/api/userManagement/{_testUserId}",
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var user = await response.Content.ReadFromJsonAsync<UserDetailsDto>(
            cancellationToken: TestContext.Current.CancellationToken
        );
        user.Should().NotBeNull();
        user!.Email.Should().Be("testuser@test.com");
        user.FullName.Should().Be("testuser");
    }

    [Fact]
    public async Task GetById_Admin_NonexistentUser_Returns404()
    {
        // Arrange
        var nonexistentId = Guid.NewGuid().ToString();

        // Act
        var response = await _adminClient.GetAsync(
            $"/api/userManagement/{nonexistentId}",
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetById_Student_Returns403()
    {
        // Act
        var response = await _studentClient.GetAsync(
            $"/api/userManagement/{_testUserId}",
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region GET /api/userManagement/email/{email}

    [Fact]
    public async Task GetByEmail_Admin_ExistingUser_ReturnsUser()
    {
        // Act
        var response = await _adminClient.GetAsync(
            "/api/userManagement/email/testuser@test.com",
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var user = await response.Content.ReadFromJsonAsync<UserDetailsDto>(
            cancellationToken: TestContext.Current.CancellationToken
        );
        user.Should().NotBeNull();
        user!.Email.Should().Be("testuser@test.com");
    }

    [Fact]
    public async Task GetByEmail_Admin_NonexistentEmail_Returns404()
    {
        // Act
        var response = await _adminClient.GetAsync(
            "/api/userManagement/email/nonexistent@test.com",
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetByEmail_Student_Returns403()
    {
        // Act
        var response = await _studentClient.GetAsync(
            "/api/userManagement/email/testuser@test.com",
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region POST /api/userManagement/assignRole

    [Fact]
    public async Task AssignRole_Admin_ValidUser_AssignsRole()
    {
        // Arrange - Create a user without any role
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        // Ensure ContentCreator role exists
        if (!await roleManager.RoleExistsAsync("ContentCreator"))
        {
            await roleManager.CreateAsync(new IdentityRole("ContentCreator"));
        }

        var newUser = new User
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
        await userManager.CreateAsync(newUser);

        var assignRoleDto = new UserRoleDto(newUser.Id, "ContentCreator");

        // Act
        var response = await _adminClient.PostAsJsonAsync(
            "/api/userManagement/assignRole",
            assignRoleDto,
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify role was assigned
        var userWithRole = await userManager.FindByIdAsync(newUser.Id);
        userWithRole.Should().NotBeNull("user should exist after role assignment");
        var roles = await userManager.GetRolesAsync(userWithRole!);
        roles.Should().Contain("ContentCreator");
    }

    [Fact]
    public async Task AssignRole_Admin_UserAlreadyHasRole_ReturnsNoContent()
    {
        // Arrange - testuser already has Student role
        var assignRoleDto = new UserRoleDto(_testUserId, "Admin");

        // Act
        var response = await _adminClient.PostAsJsonAsync(
            "/api/userManagement/assignRole",
            assignRoleDto,
            TestContext.Current.CancellationToken
        );

        // Assert
        response
            .StatusCode.Should()
            .Be(
                HttpStatusCode.NoContent,
                because: "controller returns NoContent if user already has any role (skips assignment)"
            );
    }

    [Fact]
    public async Task AssignRole_Admin_NonexistentUser_Returns404()
    {
        // Arrange
        var nonexistentId = Guid.NewGuid().ToString();
        var assignRoleDto = new UserRoleDto(nonexistentId, "Admin");

        // Act
        var response = await _adminClient.PostAsJsonAsync(
            "/api/userManagement/assignRole",
            assignRoleDto,
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AssignRole_Student_Returns403()
    {
        // Arrange
        var assignRoleDto = new UserRoleDto(_testUserId, "Admin");

        // Act
        var response = await _studentClient.PostAsJsonAsync(
            "/api/userManagement/assignRole",
            assignRoleDto,
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region PUT /api/userManagement/{id}

    [Fact]
    public async Task Update_Admin_ValidDto_UpdatesUser()
    {
        // Arrange
        var updateDto = new UserManagementUpdateDto(_testUserId, "Updated User", "testuser@test.com", "555-1234", DateTime.UtcNow);

        // Act
        var response = await _adminClient.PutAsJsonAsync(
            $"/api/userManagement/{_testUserId}",
            updateDto,
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify update
        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var updatedUser = await userManager.FindByIdAsync(_testUserId);
        updatedUser.Should().NotBeNull();
        updatedUser!.UserName.Should().Be("Updated User");
        updatedUser.Email.Should().Be("testuser@test.com");
        updatedUser.PhoneNumber.Should().Be("555-1234");
    }

    [Fact]
    public async Task Update_Admin_NonexistentUser_Returns404()
    {
        // Arrange
        var nonexistentId = Guid.NewGuid().ToString();
        var updateDto = new UserManagementUpdateDto(
            nonexistentId,
            "New Name",
            "newuser@test.com",
            "555-0000",
            DateTime.UtcNow
        );

        // Act
        var response = await _adminClient.PutAsJsonAsync(
            $"/api/userManagement/{nonexistentId}",
            updateDto,
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_Student_Returns403()
    {
        // Arrange
        var updateDto = new UserManagementUpdateDto(
            _testUserId,
            "Hacker Name",
            "hacker@test.com",
            "666-6666",
            DateTime.UtcNow
        );

        // Act
        var response = await _studentClient.PutAsJsonAsync(
            $"/api/userManagement/{_testUserId}",
            updateDto,
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region PUT /api/userManagement/updateLoginDate/{id}

    [Fact]
    public async Task UpdateLastLoginDate_Admin_ValidUser_UpdatesDate()
    {
        // Arrange
        var originalDate = DateTime.UtcNow.AddDays(-7);

        using (var scope = Factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var user = await userManager.FindByIdAsync(_testUserId);
            user.Should().NotBeNull("test user should exist in database");
            user!.LastLoginDate = originalDate;
            await userManager.UpdateAsync(user);
        }

        // Act
        var response = await _adminClient.PutAsync(
            $"/api/userManagement/updateLoginDate/{_testUserId}",
            null,
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify date was updated
        using var verifyScope = Factory.Services.CreateScope();
        var verifyUserManager = verifyScope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var updatedUser = await verifyUserManager.FindByIdAsync(_testUserId);
        updatedUser.Should().NotBeNull();
        updatedUser!
            .LastLoginDate.Should()
            .BeAfter(originalDate, because: "UpdateLastLoginDate sets LastLoginDate to UtcNow");
    }

    [Fact]
    public async Task UpdateLastLoginDate_Admin_NonexistentUser_Returns404()
    {
        // Arrange
        var nonexistentId = Guid.NewGuid().ToString();

        // Act
        var response = await _adminClient.PutAsync(
            $"/api/userManagement/updateLoginDate/{nonexistentId}",
            null,
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateLastLoginDate_Student_Returns403()
    {
        // Act
        var response = await _studentClient.PutAsync(
            $"/api/userManagement/updateLoginDate/{_testUserId}",
            null,
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion

    #region DELETE /api/userManagement/{id}

    [Fact]
    public async Task Delete_Admin_ExistingUser_DeletesUser()
    {
        // Arrange - Create a user specifically for deletion
        string userIdToDelete;
        {
            using var scope = Factory.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var userToDelete = new User
            {
                Id = Guid.NewGuid().ToString(),
                UserName = "todelete",
                Email = "todelete@test.com",
                NormalizedUserName = "TODELETE",
                NormalizedEmail = "TODELETE@TEST.COM",
                EmailConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString(),
                RegistrationDate = DateTime.UtcNow,
            };
            await userManager.CreateAsync(userToDelete);
            userIdToDelete = userToDelete.Id;
        }

        // Act
        var response = await _adminClient.DeleteAsync(
            $"/api/userManagement/{userIdToDelete}",
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify deletion with a fresh scope
        using var verifyScope = Factory.Services.CreateScope();
        var verifyUserManager = verifyScope
            .ServiceProvider
            .GetRequiredService<UserManager<User>>();
        var deletedUser = await verifyUserManager.FindByIdAsync(userIdToDelete);
        deletedUser.Should().BeNull();
    }

    [Fact]
    public async Task Delete_Admin_NonexistentUser_Returns404()
    {
        // Arrange
        var nonexistentId = Guid.NewGuid().ToString();

        // Act
        var response = await _adminClient.DeleteAsync(
            $"/api/userManagement/{nonexistentId}",
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_Student_Returns403()
    {
        // Act
        var response = await _studentClient.DeleteAsync(
            $"/api/userManagement/{_testUserId}",
            TestContext.Current.CancellationToken
        );

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    #endregion
}
