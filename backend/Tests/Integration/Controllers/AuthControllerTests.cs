using System.Net.Http.Json;
using Backend.Database.Entities.Users;
using Backend.Tests.Builders;
using Backend.Tests.Infrastructure;
using FluentAssertions;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using Moq;
using Xunit;

namespace Backend.Tests.Controllers;

/// <summary>
/// HTTP-level integration tests for AuthController cookie behavior.
///
/// Verifies:
///   - POST /api/auth/google-login sets the AuthToken cookie
///   - The cookie is HttpOnly and expires in the configured number of hours
///   - The cookie value is a structurally valid JWT (3 base64url segments)
///   - POST /api/auth/logout sets an expired AuthToken cookie (clear pattern)
/// </summary>
public class AuthControllerTests(DatabaseFixture fixture)
    : ControllerTestBase(fixture),
        IClassFixture<DatabaseFixture>
{
    private HttpClient _client = null!;

    private static readonly User FakeUser = new UserBuilder()
        .WithUserName("cookietest")
        .WithEmail("cookietest@example.com")
        .Build();

    private static readonly GoogleJsonWebSignature.Payload FakePayload = new()
    {
        Subject = FakeUser.Id,
        Email = FakeUser.Email,
        Name = FakeUser.UserName,
    };

    public override async ValueTask InitializeAsync()
    {
        GoogleAuthMock
            .Setup(s => s.ValidateGoogleTokenAsync(It.IsAny<string>()))
            .ReturnsAsync(FakePayload);
        GoogleAuthMock
            .Setup(s => s.LoginUser(It.IsAny<GoogleJsonWebSignature.Payload>()))
            .ReturnsAsync(FakeUser);

        await base.InitializeAsync();

        _client = Factory.CreateClient(
            new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }
        );
    }

    public override async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await base.DisposeAsync();
    }

    [Fact]
    public async Task GoogleLogin_SetsAuthTokenCookie()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/google-login",
            new { idToken = "fake-token" },
            TestContext.Current.CancellationToken
        );

        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        cookies.Should().Contain(c => c.StartsWith("AuthToken="));
    }

    [Fact]
    public async Task GoogleLogin_CookieIsHttpOnly()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/google-login",
            new { idToken = "fake-token" },
            TestContext.Current.CancellationToken
        );

        GetAuthCookieHeader(response).Should().Contain("httponly");
    }

    [Fact]
    public async Task GoogleLogin_CookieExpiresInConfiguredHours()
    {
        var before = DateTime.UtcNow;

        var response = await _client.PostAsJsonAsync(
            "/api/auth/google-login",
            new { idToken = "fake-token" },
            TestContext.Current.CancellationToken
        );

        var expires = ParseCookieExpires(GetAuthCookieHeader(response));
        expires.Should().BeCloseTo(before.AddHours(24), precision: TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task GoogleLogin_CookieValueIsValidJwt()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/auth/google-login",
            new { idToken = "fake-token" },
            TestContext.Current.CancellationToken
        );

        var cookieValue = ParseCookieValue(GetAuthCookieHeader(response));
        cookieValue.Split('.').Should().HaveCount(3);
    }

    [Fact]
    public async Task Logout_SetsExpiredAuthTokenCookie()
    {
        var response = await _client.PostAsync(
            "/api/auth/logout",
            null,
            TestContext.Current.CancellationToken
        );

        var expires = ParseCookieExpires(GetAuthCookieHeader(response));
        expires.Should().BeBefore(DateTime.UtcNow);
    }

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
