using Backend.Api;
using Backend.Api.Services;
using Backend.Database;
using Backend.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Xunit;

namespace Backend.Tests.Infrastructure;

/// <summary>
/// Base class for WebApplicationFactory-backed controller integration tests.
///
/// Handles the boilerplate shared across all controller test classes:
///   - Required environment variables (JWT_SECRET, DATA_PROTECTION_KEYS_PATH, etc.)
///   - Replacing the real DbContext with the Testcontainers instance
///   - Providing a pre-built Mock&lt;IGoogleAuthService&gt; — configure setups on
///     <see cref="GoogleAuthMock"/> before calling base.InitializeAsync()
///   - Factory and env var teardown in DisposeAsync
///
/// Usage:
/// <code>
/// public class MyTests(DatabaseFixture fixture)
///     : ControllerTestBase(fixture), IClassFixture&lt;DatabaseFixture&gt;
/// {
///     public override async ValueTask InitializeAsync()
///     {
///         GoogleAuthMock.Setup(...).ReturnsAsync(...);
///         await base.InitializeAsync();
///         // additional per-test setup
///     }
/// }
/// </code>
/// </summary>
public abstract class ControllerTestBase(DatabaseFixture fixture) : IAsyncLifetime
{
    protected const string JwtSecret = "test-super-secret-key-minimum-32-chars-long!";

    protected readonly DatabaseFixture Fixture = fixture;
    protected WebApplicationFactory<Program> Factory = null!;

    /// <summary>
    /// Pre-built mock for IGoogleAuthService. Configure setups before calling
    /// base.InitializeAsync() — the mock object is registered into the DI container
    /// during factory construction.
    /// </summary>
    protected readonly Mock<IGoogleAuthService> GoogleAuthMock = new();

    public virtual async ValueTask InitializeAsync()
    {
        Environment.SetEnvironmentVariable("JWT_SECRET", JwtSecret);
        Environment.SetEnvironmentVariable("JWT_EXPIRATION_HOURS", "24");
        Environment.SetEnvironmentVariable("DATA_PROTECTION_KEYS_PATH", Path.GetTempPath());
        Environment.SetEnvironmentVariable("GOOGLE_CLIENT_ID", "test-client-id");
        Environment.SetEnvironmentVariable("GOOGLE_CLIENT_SECRET", "test-client-secret");

        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureServices(services =>
            {
                var dbDescriptor = services.Single(d =>
                    d.ServiceType == typeof(DbContextOptions<BackendDbContext>)
                );
                services.Remove(dbDescriptor);
                services.AddDbContext<BackendDbContext>(opts =>
                    opts.UseSqlServer(Fixture.ConnectionString)
                        .ConfigureWarnings(w =>
                            w.Ignore(RelationalEventId.PendingModelChangesWarning)
                        )
                );

                services.RemoveAll<IGoogleAuthService>();
                services.AddSingleton(GoogleAuthMock.Object);

                ConfigureTestServices(services);
            })
        );

        await Task.CompletedTask;
    }

    /// <summary>
    /// Override to register additional service overrides into the test factory.
    /// Called after the DbContext and IGoogleAuthService replacements are applied.
    /// </summary>
    protected virtual void ConfigureTestServices(IServiceCollection services) { }

    public virtual async ValueTask DisposeAsync()
    {
        await Factory.DisposeAsync();
        Environment.SetEnvironmentVariable("JWT_SECRET", null);
        Environment.SetEnvironmentVariable("JWT_EXPIRATION_HOURS", null);
        Environment.SetEnvironmentVariable("DATA_PROTECTION_KEYS_PATH", null);
        Environment.SetEnvironmentVariable("GOOGLE_CLIENT_ID", null);
        Environment.SetEnvironmentVariable("GOOGLE_CLIENT_SECRET", null);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Creates an HttpClient that does not follow redirects and does not auto-manage
    /// cookies. When <paramref name="authToken"/> is provided it is injected as the
    /// AuthToken cookie, enabling JWT-authenticated requests without going through login.
    /// </summary>
    protected HttpClient CreateClient(string? authToken = null)
    {
        var client = Factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = false,
            }
        );
        if (authToken != null)
            client.DefaultRequestHeaders.Add("Cookie", $"AuthToken={authToken}");
        return client;
    }
}
