using Backend.Api.Services;
using Backend.Database;
using Backend.Database.Entities.Users;
using Backend.Tests.Builders;
using Backend.Tests.Helpers;
using Backend.Tests.Infrastructure;
using FluentAssertions;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Backend.Tests.Services;

/// <summary>
/// Integration tests for GoogleAuthService.LoginUser.
///
/// Three code paths exercised:
///   1. Returning Google user  — UserLoginInfo already exists → returns existing user, no role change
///   2. Email-match user       — Email matches but no Google login → adds login, skips role assignment
///   3. New user               — No match at all → creates user, assigns "Student" role, adds login
/// </summary>
public class LoginUserTests(DatabaseFixture fixture)
    : IClassFixture<DatabaseFixture>,
        IAsyncLifetime
{
    private readonly DatabaseFixture _fixture = fixture;
    private BackendDbContext _ctx = null!;
    private UserManager<User> _userManager = null!;
    private RoleManager<IdentityRole> _roleManager = null!;
    private GoogleAuthService _sut = null!;

    public async ValueTask InitializeAsync()
    {
        _ctx = _fixture.CreateDbContext();
        await DbSeeder.ClearLeaderboardDataAsync(_ctx, _fixture.SystemUserId);

        (_userManager, _roleManager) = BuildManagers(_ctx);
        await EnsureStudentRoleAsync(_roleManager);

        var avatarService = CreateAvatarService(_ctx);
        _sut = new GoogleAuthService(
            _userManager,
            NullLogger<GoogleAuthService>.Instance,
            avatarService
        );
    }

    public async ValueTask DisposeAsync()
    {
        _roleManager.Dispose();
        _userManager.Dispose();
        await _ctx.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task NewUser_ReturnsNonNullUser()
    {
        var payload = MakePayload("new-google-1", "newuser@example.com", "New User");

        var resultUser = await _sut.LoginUser(payload);

        AssertValidUser(resultUser, payload);
    }

    [Fact]
    public async Task NewUser_CreatesUserInDatabase()
    {
        var payload = MakePayload("new-google-2", "created@example.com", "Created User");

        var resultUser = await _sut.LoginUser(payload);
        AssertValidUser(resultUser, payload);

        var dbUser = await _userManager.FindByIdAsync(resultUser!.Id);

        AssertValidUser(dbUser, payload);
    }

    [Fact]
    public async Task NewUser_AssignsStudentRole()
    {
        var payload = MakePayload("new-google-3", "student@example.com", "Student User");

        var resultUser = await _sut.LoginUser(payload);
        AssertValidUser(resultUser, payload);

        var roles = await _userManager.GetRolesAsync(resultUser!);
        roles.Should().Contain("Student");
    }

    [Fact]
    public async Task NewUser_AddsGoogleLogin()
    {
        var payload = MakePayload("new-google-4", "login@example.com", "Login User");

        var resultUser = await _sut.LoginUser(payload);
        AssertValidUser(resultUser, payload);

        var logins = await _userManager.GetLoginsAsync(resultUser!);
        logins
            .Should()
            .Contain(l => l.LoginProvider == "Google" && l.ProviderKey == "new-google-4");
    }

    [Fact]
    public async Task ReturningGoogleUser_ReturnsExistingUser()
    {
        var existing = new UserBuilder()
            .WithUserName("returning")
            .WithEmail("returning@example.com")
            .Build();
        await _userManager.CreateAsync(existing);
        await _userManager.AddLoginAsync(
            existing,
            new UserLoginInfo("Google", "returning-sub", "Google")
        );

        var payload = MakePayload("returning-sub", "returning@example.com", "Returning User");

        var result = await _sut.LoginUser(payload);

        result.Should().NotBeNull();
        result.Id.Should().Be(existing.Id);
    }

    [Fact]
    public async Task ReturningGoogleUser_DoesNotCreateNewUser()
    {
        var existing = new UserBuilder()
            .WithUserName("no-duplicate")
            .WithEmail("no-duplicate@example.com")
            .Build();
        await _userManager.CreateAsync(existing);
        await _userManager.AddLoginAsync(
            existing,
            new UserLoginInfo("Google", "no-dup-sub", "Google")
        );

        var payload = MakePayload("no-dup-sub", "no-duplicate@example.com", "No Duplicate");

        await _sut.LoginUser(payload);

        _userManager.Users.Count(u => u.Email == "no-duplicate@example.com").Should().Be(1);
    }

    [Fact]
    public async Task EmailMatchUser_ReturnsExistingUser()
    {
        var existing = new UserBuilder()
            .WithUserName("email-match")
            .WithEmail("emailmatch@example.com")
            .Build();
        await _userManager.CreateAsync(existing);
        // No Google login — triggers email-match path

        var payload = MakePayload("email-match-sub", "emailmatch@example.com", "Email Match");

        var result = await _sut.LoginUser(payload);

        result.Should().NotBeNull();
        result!.Id.Should().Be(existing.Id);
    }

    [Fact]
    public async Task EmailMatchUser_AddsGoogleLogin()
    {
        var existing = new UserBuilder()
            .WithUserName("email-login-add")
            .WithEmail("emailloginadd@example.com")
            .Build();
        await _userManager.CreateAsync(existing);

        var payload = MakePayload(
            "email-login-sub",
            "emailloginadd@example.com",
            "Email Login Add"
        );

        await _sut.LoginUser(payload);

        var logins = await _userManager.GetLoginsAsync(existing);
        logins
            .Should()
            .Contain(l => l.LoginProvider == "Google" && l.ProviderKey == "email-login-sub");
    }

    [Fact]
    public async Task EmailMatchUser_DoesNotAssignRole()
    {
        var existing = new UserBuilder()
            .WithUserName("no-role-change")
            .WithEmail("norolechange@example.com")
            .Build();
        await _userManager.CreateAsync(existing);
        // Intentionally no role added — verifies email-match path doesn't assign Student

        var payload = MakePayload("no-role-sub", "norolechange@example.com", "No Role Change");

        await _sut.LoginUser(payload);

        var roles = await _userManager.GetRolesAsync(existing);
        roles.Should().BeEmpty();
    }

    [Fact]
    public async Task RoleAssignmentFails_ReturnsNull()
    {
        var role = await _roleManager.FindByNameAsync("Student");
        if (role != null)
            await _roleManager.DeleteAsync(role);

        var payload = MakePayload("fail-role-sub", "failrole@example.com", "Fail Role");

        Assert.Throws<InvalidOperationException>(async () => await _sut.LoginUser(payload));

        result.Should().BeNull();

        // Restore for any subsequent tests in the same run
        await EnsureStudentRoleAsync(_roleManager);
    }

    [Fact]
    public async Task NoPictureUrl_LoginSucceeds()
    {
        var payload = MakePayload("no-pic-sub", "nopic@example.com", "No Pic", pictureUrl: null);

        var result = await _sut.LoginUser(payload);

        result.Should().NotBeNull();
    }

    [Fact]
    public async Task AvatarDownloadFails_LoginSucceeds()
    {
        // Pointing at an invalid URL exercises the catch-and-continue path
        var payload = MakePayload(
            "bad-avatar-sub",
            "badavatar@example.com",
            "Bad Avatar",
            pictureUrl: "https://invalid.example.invalid/avatar.jpg"
        );

        var result = await _sut.LoginUser(payload);

        result.Should().NotBeNull();
    }

    private static GoogleJsonWebSignature.Payload MakePayload(
        string subject,
        string email,
        string name,
        string? pictureUrl = "https://example.com/avatar.jpg"
    ) =>
        new()
        {
            Subject = subject,
            Email = email,
            Name = name,
            Picture = pictureUrl,
        };

    private static (UserManager<User>, RoleManager<IdentityRole>) BuildManagers(
        BackendDbContext ctx
    )
    {
        var services = new ServiceCollection();
        services.AddLogging();
        // Provide the test-scoped DbContext as a singleton so Identity stores use it
        services.AddSingleton(ctx);
        services
            .AddIdentityCore<User>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<BackendDbContext>();

        var sp = services.BuildServiceProvider();
        return (
            sp.GetRequiredService<UserManager<User>>(),
            sp.GetRequiredService<RoleManager<IdentityRole>>()
        );
    }

    private static async Task EnsureStudentRoleAsync(RoleManager<IdentityRole> roleManager)
    {
        if (!await roleManager.RoleExistsAsync("Student"))
            await roleManager.CreateAsync(new IdentityRole("Student"));
    }

    private static AvatarService CreateAvatarService(BackendDbContext ctx)
    {
        var factory = new ServiceCollection()
            .AddHttpClient()
            .BuildServiceProvider()
            .GetRequiredService<IHttpClientFactory>();

        return new AvatarService(ctx, factory, NullLogger<AvatarService>.Instance);
    }

    private static void AssertValidUser(User? user, GoogleJsonWebSignature.Payload payload)
    {
        user.Should().NotBeNull();
        user.Email.Should().Be(payload.Email);
        user.UserName.Should().Be(CleanUsername(payload.Name));
        user.EmailConfirmed.Should().BeTrue();
    }

    /// <summary>
    /// Mirrors UserMapping.CleanUsername (private static there, so duplicated here).
    /// Strips characters that the mapping layer removes when building a username from a Google display name.
    /// </summary>
    private static string CleanUsername(string name)
    {
        char[] charsToRemove = ['-', ' ', '_', '*', '&'];
        return new string(name.Where(c => !charsToRemove.Contains(c)).ToArray());
    }
}
