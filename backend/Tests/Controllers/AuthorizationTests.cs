using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using Backend.Database;
using Backend.Database.Entities.Users;
using Backend.Tests.Builders;
using Backend.Tests.Helpers;
using Backend.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Backend.Tests.Controllers;

/// <summary>
/// HTTP-level integration tests for authorization policy enforcement across all controllers.
///
/// Eight test categories:
///   1. Unauthenticated → 401 on every [Authorize]-guarded endpoint
///   2. Student role → 403 on Admin-only and Admin/ContentCreator endpoints
///   3. ContentCreator role → 403 on Admin-only endpoints
///   4. ContentCreator role → not rejected on [Authorize(Roles = "Admin,ContentCreator")] endpoints
///   5. Admin role → not rejected (not 401/403) on all role-restricted endpoints
///   6. Public endpoints → not blocked (not 401/403) without credentials
///   7. Missing auth on UserManagementController and RoleManagementController (security documentation)
///   8. Expired JWT → 401 on every [Authorize]-guarded endpoint
/// </summary>
public class AuthorizationTests(DatabaseFixture fixture)
    : ControllerTestBase(fixture),
        IClassFixture<DatabaseFixture>
{
    private BackendDbContext _ctx = null!;
    private User _adminUser = null!;

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();

        _ctx = Fixture.CreateDbContext();
        await DbSeeder.ClearLeaderboardDataAsync(_ctx, Fixture.SystemUserId);

        _adminUser = new UserBuilder()
            .WithUserName("adminuser")
            .WithEmail("admin@authtest.com")
            .Build();

        await DbSeeder.AddUserAsync(_ctx, _adminUser);
    }

    public override async ValueTask DisposeAsync()
    {
        await _ctx.DisposeAsync();
        await base.DisposeAsync();
    }

    // ── 1. Unauthenticated → 401 ────────────────────────────────────────────

    [Theory]
    [InlineData("GET", "/api/course")]
    [InlineData("GET", "/api/course/test-id")]
    [InlineData("POST", "/api/course")]
    [InlineData("PUT", "/api/course/test-id")]
    [InlineData("DELETE", "/api/course/test-id")]
    [InlineData("GET", "/api/lesson/course/test-id")]
    [InlineData("GET", "/api/lesson/test-id")]
    [InlineData("POST", "/api/lesson")]
    [InlineData("PUT", "/api/lesson/test-id")]
    [InlineData("DELETE", "/api/lesson/test-id")]
    [InlineData("POST", "/api/lesson/test-id/complete")]
    [InlineData("POST", "/api/lesson/test-id/unlock")]
    [InlineData("GET", "/api/exercise/lesson/test-id")]
    [InlineData("POST", "/api/exercises")]
    [InlineData("PUT", "/api/exercises/test-id")]
    [InlineData("DELETE", "/api/exercises/test-id")]
    [InlineData("POST", "/api/exercises/test-id/submit")]
    [InlineData("GET", "/api/userlanguage")]
    [InlineData("GET", "/api/user/xp")]
    [InlineData("GET", "/api/auth/is-admin")]
    public async Task Unauthenticated_ProtectedEndpoint_Returns401(string method, string path)
    {
        using var client = CreateClient();
        var response = await SendAsync(client, method, path);
        response
            .StatusCode.Should()
            .Be(HttpStatusCode.Unauthorized, because: $"{method} {path} requires authentication");
    }

    /// <summary>
    /// PUT /api/user/avatar uses [Consumes("multipart/form-data")], which is a resource
    /// filter that runs before authorization. Sending any other content type returns 415
    /// before [Authorize] is evaluated — so this endpoint requires its own test with the
    /// correct content type to reach the auth check.
    /// </summary>
    [Fact]
    public async Task Unauthenticated_AvatarUpload_Returns401()
    {
        using var client = CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Put, "/api/user/avatar")
        {
            Content = new MultipartFormDataContent(),
        };
        var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        response
            .StatusCode.Should()
            .Be(
                HttpStatusCode.Unauthorized,
                because: "PUT /api/user/avatar requires authentication"
            );
    }

    // ── 2. Student → 403 on role-restricted endpoints ───────────────────────

    [Theory]
    [InlineData("POST", "/api/language")]
    [InlineData("PUT", "/api/language/test-id")]
    [InlineData("DELETE", "/api/language/test-id")]
    [InlineData("POST", "/api/course")]
    [InlineData("PUT", "/api/course/test-id")]
    [InlineData("DELETE", "/api/course/test-id")]
    [InlineData("POST", "/api/lesson/test-id/unlock")]
    [InlineData("POST", "/api/lesson")]
    [InlineData("PUT", "/api/lesson/test-id")]
    [InlineData("DELETE", "/api/lesson/test-id")]
    [InlineData("POST", "/api/exercise")]
    [InlineData("PUT", "/api/exercise/test-id")]
    [InlineData("DELETE", "/api/exercise/test-id")]
    public async Task Student_RoleRestrictedEndpoint_Returns403(string method, string path)
    {
        var token = MintToken("student-id", "student@test.com", "Student");
        using var client = CreateClient(token);
        var response = await SendAsync(client, method, path);
        response
            .StatusCode.Should()
            .Be(
                HttpStatusCode.Forbidden,
                because: $"Student role is not authorized for {method} {path}"
            );
    }

    // ── 3. ContentCreator → 403 on Admin-only endpoints ─────────────────────

    [Theory]
    [InlineData("POST", "/api/language")]
    [InlineData("PUT", "/api/language/test-id")]
    [InlineData("DELETE", "/api/language/test-id")]
    [InlineData("DELETE", "/api/course/test-id")]
    [InlineData("POST", "/api/lesson/test-id/unlock")]
    public async Task ContentCreator_AdminOnlyEndpoint_Returns403(string method, string path)
    {
        var token = MintToken("cc-id", "cc@test.com", "ContentCreator");
        using var client = CreateClient(token);
        var response = await SendAsync(client, method, path);
        response
            .StatusCode.Should()
            .Be(
                HttpStatusCode.Forbidden,
                because: $"ContentCreator is not authorized for Admin-only {method} {path}"
            );
    }

    // ── 4. ContentCreator → not rejected on Admin,ContentCreator endpoints ───

    [Theory]
    [InlineData("POST", "/api/course")]
    [InlineData("PUT", "/api/course/test-id")]
    [InlineData("POST", "/api/lesson")]
    [InlineData("PUT", "/api/lesson/test-id")]
    [InlineData("DELETE", "/api/lesson/test-id")]
    [InlineData("POST", "/api/exercise")]
    [InlineData("PUT", "/api/exercise/test-id")]
    [InlineData("DELETE", "/api/exercise/test-id")]
    public async Task ContentCreator_AdminContentCreatorEndpoint_IsNotRejectedByAuthPolicy(
        string method,
        string path
    )
    {
        var token = MintToken("cc-id", "cc@test.com", "ContentCreator");
        using var client = CreateClient(token);
        var response = await SendAsync(client, method, path);
        response
            .StatusCode.Should()
            .NotBe(
                HttpStatusCode.Unauthorized,
                because: $"ContentCreator token should pass [Authorize] for {method} {path}"
            );
        response
            .StatusCode.Should()
            .NotBe(
                HttpStatusCode.Forbidden,
                because: $"ContentCreator role satisfies [Authorize(Roles = \"Admin,ContentCreator\")] for {method} {path}"
            );
    }

    // ── 5. Admin → not rejected by auth policy ──────────────────────────────

    [Theory]
    [InlineData("POST", "/api/language")]
    [InlineData("PUT", "/api/language/test-id")]
    [InlineData("DELETE", "/api/language/test-id")]
    [InlineData("POST", "/api/course")]
    [InlineData("PUT", "/api/course/test-id")]
    [InlineData("DELETE", "/api/course/test-id")]
    [InlineData("POST", "/api/lesson/test-id/unlock")]
    [InlineData("POST", "/api/lesson")]
    [InlineData("PUT", "/api/lesson/test-id")]
    [InlineData("DELETE", "/api/lesson/test-id")]
    [InlineData("POST", "/api/exercise")]
    [InlineData("PUT", "/api/exercise/test-id")]
    [InlineData("DELETE", "/api/exercise/test-id")]
    public async Task Admin_RoleRestrictedEndpoint_IsNotRejectedByAuthPolicy(
        string method,
        string path
    )
    {
        var token = MintToken(_adminUser.Id, _adminUser.Email!, "Admin");
        using var client = CreateClient(token);
        var response = await SendAsync(client, method, path);
        response
            .StatusCode.Should()
            .NotBe(
                HttpStatusCode.Unauthorized,
                because: $"Admin token should pass [Authorize] for {method} {path}"
            );
        response
            .StatusCode.Should()
            .NotBe(
                HttpStatusCode.Forbidden,
                because: $"Admin role satisfies all role requirements for {method} {path}"
            );
    }

    // ── 6. Public endpoints are not blocked ─────────────────────────────────

    [Theory]
    [InlineData("GET", "/api/language")]
    [InlineData("GET", "/api/language/test-id")]
    [InlineData("GET", "/api/leaderboard")]
    [InlineData("GET", "/api/user/test-id/xp")]
    [InlineData("GET", "/api/user/test-id/avatar")]
    [InlineData("GET", "/api/auth/auth-status")]
    [InlineData("POST", "/api/auth/logout")]
    public async Task Unauthenticated_PublicEndpoint_IsNotBlocked(string method, string path)
    {
        using var client = CreateClient();
        var response = await SendAsync(client, method, path);
        response
            .StatusCode.Should()
            .NotBe(
                HttpStatusCode.Unauthorized,
                because: $"{method} {path} is public and must not require authentication"
            );
        response
            .StatusCode.Should()
            .NotBe(
                HttpStatusCode.Forbidden,
                because: $"{method} {path} is public and must not require a specific role"
            );
    }

    // ── 7. Missing auth on management controllers (security documentation) ──

    /// <summary>
    /// UserManagementController carries no [Authorize] attribute — all user CRUD
    /// operations are reachable by anonymous callers. These endpoints should be
    /// secured with [Authorize(Roles = "Admin")] before production.
    /// </summary>
    [Fact]
    public async Task UserManagementController_HasNoAuthAttribute_AccessibleWithoutToken()
    {
        using var client = CreateClient();
        var response = await client.GetAsync(
            "/api/usermanagement",
            TestContext.Current.CancellationToken
        );
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    /// <summary>
    /// RoleManagementController carries no [Authorize] attribute — role lookup by
    /// email is reachable by anonymous callers. Should be secured before production.
    /// </summary>
    [Fact]
    public async Task RoleManagementController_HasNoAuthAttribute_AccessibleWithoutToken()
    {
        using var client = CreateClient();
        var response = await client.GetAsync(
            "/api/rolemanagement/nonexistent@example.com",
            TestContext.Current.CancellationToken
        );
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    // ── 8. Expired JWT → 401 ────────────────────────────────────────────────

    [Theory]
    [InlineData("GET", "/api/course")]
    [InlineData("GET", "/api/course/test-id")]
    [InlineData("POST", "/api/course")]
    [InlineData("PUT", "/api/course/test-id")]
    [InlineData("GET", "/api/lesson/test-id")]
    [InlineData("POST", "/api/lesson")]
    [InlineData("PUT", "/api/lesson/test-id")]
    [InlineData("GET", "/api/exercise/lesson/test-id")]
    [InlineData("POST", "/api/exercise")]
    [InlineData("PUT", "/api/exercise/test-id")]
    [InlineData("POST", "/api/exercise/test-id/submit")]
    [InlineData("GET", "/api/user/xp")]
    [InlineData("GET", "/api/auth/is-admin")]
    public async Task ExpiredJwt_ProtectedEndpoint_Returns401(string method, string path)
    {
        var token = MintExpiredToken("some-user-id", "user@test.com", "Student");
        using var client = CreateClient(token);
        var response = await SendAsync(client, method, path);
        response
            .StatusCode.Should()
            .Be(
                HttpStatusCode.Unauthorized,
                because: $"An expired JWT must not grant access to {method} {path}"
            );
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Mints a JWT signed with the same secret, issuer, and audience as the application.
    /// Roles are embedded as ClaimTypes.Role claims — matching how JwtService produces tokens.
    /// </summary>
    private static string MintToken(string userId, string email, params string[] roles)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Email, email),
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(
            issuer: "lexiq-api",
            audience: "lexiq-frontend",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Mints a JWT that expired one hour ago — safely outside ASP.NET Core's
    /// default 5-minute clock skew, so the middleware will always reject it.
    /// </summary>
    private static string MintExpiredToken(string userId, string email, params string[] roles)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Email, email),
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

        var token = new JwtSecurityToken(
            issuer: "lexiq-api",
            audience: "lexiq-frontend",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(-1),
            signingCredentials: creds
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static async Task<HttpResponseMessage> SendAsync(
        HttpClient client,
        string method,
        string path
    )
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method is "POST" or "PUT" or "PATCH")
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        return await client.SendAsync(request, TestContext.Current.CancellationToken);
    }
}
