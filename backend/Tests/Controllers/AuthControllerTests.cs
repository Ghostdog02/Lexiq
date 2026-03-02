using System.Net.Http.Json;
using Backend.Api;
using Backend.Api.Services;
using Backend.Database;
using Backend.Database.Entities.Users;
using Backend.Tests.Builders;
using Backend.Tests.Infrastructure;
using FluentAssertions;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace Backend.Tests.Controllers;

/// <summary>
/// HTTP-level integration tests for AuthController cookie behavior.
/// Uses WebApplicationFactory with a Moq'd IGoogleAuthService to bypass Google
/// token validation and a shared Testcontainers SQL Server for Identity stores.
///
/// Verifies:
///   - POST /api/auth/google-login sets the AuthToken cookie
///   - The cookie is HttpOnly and expires in the configured number of hours
///   - The cookie value is a structurally valid JWT (3 base64url segments)
///   - POST /api/auth/logout sets an expired AuthToken cookie (clear pattern)
/// </summary>
public class AuthControllerTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    // The fake user returned by the mocked IGoogleAuthService.
    // Does not need to exist in the DB — GetRolesAsync returns [] for unknown users.
    private static readonly User FakeUser = new UserBuilder()
        .WithUserName("cookietest")
        .WithEmail("cookietest@example.com")
        .Build();

    private static readonly GoogleJsonWebSignature.Payload FakePayload =
        new()
        {
            Subject = FakeUser.Id,
            Email = FakeUser.Email,
            Name = FakeUser.UserName,
        };

    public AuthControllerTests(DatabaseFixture fixture) => _fixture = fixture;

    public async ValueTask InitializeAsync()
    {
        // JwtService reads env vars in its constructor (called per scoped request).
        // DATA_PROTECTION_KEYS_PATH redirects key persistence away from /app/.
        Environment.SetEnvironmentVariable(
            "JWT_SECRET",
            "test-super-secret-key-minimum-32-chars-long!"
        );
        Environment.SetEnvironmentVariable("JWT_EXPIRATION_HOURS", "24");
        Environment.SetEnvironmentVariable("DATA_PROTECTION_KEYS_PATH", Path.GetTempPath());

        var googleAuthMock = new Mock<IGoogleAuthService>();
        googleAuthMock
            .Setup(s => s.ValidateGoogleTokenAsync(It.IsAny<string>()))
            .ReturnsAsync(FakePayload);
        googleAuthMock
            .Setup(s => s.LoginUser(It.IsAny<GoogleJsonWebSignature.Payload>()))
            .ReturnsAsync(FakeUser);

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                // Replace real DbContext with Testcontainers instance (already migrated)
                var dbDescriptor = services.Single(
                    d => d.ServiceType == typeof(DbContextOptions<BackendDbContext>)
                );
                services.Remove(dbDescriptor);
                services.AddDbContext<BackendDbContext>(opts =>
                    opts.UseSqlServer(_fixture.ConnectionString)
                        .ConfigureWarnings(w =>
                            w.Ignore(RelationalEventId.PendingModelChangesWarning)
                        )
                );

                // Replace GoogleAuthService with Moq — bypasses Google token validation
                services.RemoveAll<IGoogleAuthService>();
                services.AddSingleton(googleAuthMock.Object);
            })
        );

        // Don't follow redirects so we can inspect raw Set-Cookie headers
        _client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }
        );

        await Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        Environment.SetEnvironmentVariable("JWT_SECRET", null);
        Environment.SetEnvironmentVariable("JWT_EXPIRATION_HOURS", null);
        Environment.SetEnvironmentVariable("DATA_PROTECTION_KEYS_PATH", null);
    }

    // ── Cookie presence ──────────────────────────────────────────────────────

    [Fact]
    public async Task GoogleLogin_SetsAuthTokenCookie()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/google-login",
            new { idToken = "fake-token" },
            TestContext.Current.CancellationToken
        );

        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        cookies!.Should().Contain(c => c.StartsWith("AuthToken="));
    }

    [Fact]
    public async Task GoogleLogin_CookieIsHttpOnly()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/google-login",
            new { idToken = "fake-token" },
            TestContext.Current.CancellationToken
        );

        GetAuthCookieHeader(response).Should().Contain("HttpOnly");
    }

    // ── Cookie expiry ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GoogleLogin_CookieExpiresInConfiguredHours()
    {
        // Cookie Expires is set to DateTime.UtcNow.AddHours(JwtService.ExpirationHours)
        var before = DateTime.UtcNow;

        var response = await _client.PostAsJsonAsync(
            "/api/auth/google-login",
            new { idToken = "fake-token" },
            TestContext.Current.CancellationToken
        );

        var expires = ParseCookieExpires(GetAuthCookieHeader(response));
        expires.Should().BeCloseTo(before.AddHours(24), precision: TimeSpan.FromMinutes(1));
    }

    // ── Cookie value is a valid JWT ───────────────────────────────────────────

    [Fact]
    public async Task GoogleLogin_CookieValueIsValidJwt()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/google-login",
            new { idToken = "fake-token" },
            TestContext.Current.CancellationToken
        );

        var cookieValue = ParseCookieValue(GetAuthCookieHeader(response));
        // A JWT has exactly three base64url segments separated by dots
        cookieValue.Split('.').Should().HaveCount(3);
    }

    // ── Logout clears the cookie ──────────────────────────────────────────────

    [Fact]
    public async Task Logout_SetsExpiredAuthTokenCookie()
    {
        // AuthController.Logout calls SetAuthCookie("", DateTime.UtcNow.AddDays(-1))
        var response = await _client.PostAsync(
            "/api/auth/logout",
            null,
            TestContext.Current.CancellationToken
        );

        var expires = ParseCookieExpires(GetAuthCookieHeader(response));
        expires.Should().BeBefore(DateTime.UtcNow);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static string GetAuthCookieHeader(HttpResponseMessage response)
    {
        response.Headers.TryGetValues("Set-Cookie", out var values);
        return values!.First(c => c.StartsWith("AuthToken="));
    }

    private static string ParseCookieValue(string setCookieHeader) =>
        setCookieHeader.Split(';')[0]["AuthToken=".Length..];

    private static DateTime ParseCookieExpires(string setCookieHeader)
    {
        var expiresPart = setCookieHeader
            .Split(';')
            .Select(p => p.Trim())
            .First(p => p.StartsWith("expires=", StringComparison.OrdinalIgnoreCase));

        return DateTime.Parse(
            expiresPart["expires=".Length..],
            null,
            System.Globalization.DateTimeStyles.RoundtripKind
        );
    }
}
