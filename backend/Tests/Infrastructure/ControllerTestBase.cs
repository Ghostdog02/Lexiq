using Backend.Api;
using Backend.Api.Services;
using Backend.Database;
using Backend.Tests.Infrastructure;
using Microsoft.AspNetCore.Identity;
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

        // Ensure wwwroot/uploads structure exists for static file middleware
        // (only created during test runs, never committed to git)
        EnsureUploadDirectoriesExist();

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

                // Register JwtService for token generation in tests
                services.AddSingleton<IJwtService, JwtService>();

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
        // NOTE: We intentionally do NOT clear environment variables here.
        // Clearing them causes race conditions when xUnit runs test classes in parallel:
        //   - Test class A sets JWT_SECRET
        //   - Test class B sets JWT_SECRET (parallel execution)
        //   - Test class A finishes and clears JWT_SECRET
        //   - Test class B tries to create factory → JWT_SECRET missing!
        // The test values are harmless to leave set for the entire test process lifetime.
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

    /// <summary>
    /// Clears all test user data (users, progress, avatars) except the system user.
    /// Call this at the start of InitializeAsync in E2E tests to ensure clean state.
    /// </summary>
    protected async Task ClearTestDataAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<BackendDbContext>();
        await Helpers.DbSeeder.ClearLeaderboardDataAsync(ctx, Fixture.SystemUserId);
    }

    /// <summary>
    /// Creates a user via GoogleAuthService mock and returns user ID and JWT token.
    /// The token can be used with CreateClient() to authenticate requests.
    /// </summary>
    protected async Task<(string UserId, string Token)> CreateAuthenticatedUserAsync(
        string userName,
        string email,
        params string[] roles
    )
    {
        var userId = Guid.NewGuid().ToString();
        var user = new Backend.Database.Entities.Users.User
        {
            Id = userId,
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            RegistrationDate = DateTime.UtcNow,
            LastLoginDate = DateTime.UtcNow,
            TotalPointsEarned = 0,
        };

        // Insert user into database using the fixture's context
        using var scope = Factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<BackendDbContext>();
        ctx.Users.Add(user);
        await ctx.SaveChangesAsync();

        // Add roles if specified
        if (roles.Length > 0)
        {
            var userManager = scope.ServiceProvider.GetRequiredService<
                UserManager<Backend.Database.Entities.Users.User>
            >();
            foreach (var role in roles)
            {
                var roleManager = scope.ServiceProvider.GetRequiredService<
                    RoleManager<IdentityRole>
                >();
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole(role));

                await userManager.AddToRoleAsync(user, role);
            }
        }

        // Generate JWT token
        var jwtService = scope.ServiceProvider.GetRequiredService<IJwtService>();
        var token = jwtService.GenerateToken(user, roles);

        return (userId, token);
    }

    /// <summary>
    /// Creates the wwwroot/uploads directory structure required by static file middleware.
    /// Path is relative to the backend project root (where Program.cs lives).
    /// Only runs during tests — directories are gitignored and not committed.
    /// </summary>
    private static void EnsureUploadDirectoriesExist()
    {
        // Navigate from Tests/ to backend/ (parent directory)
        var backendRoot = Path.Combine(
            Directory.GetCurrentDirectory(),
            "..",
            "..",
            ".."
        );

        var uploadPaths = new[] { "audio", "documents", "images", "videos", "files" };
        foreach (var subdir in uploadPaths)
        {
            var fullPath = Path.Combine(backendRoot, "wwwroot", "uploads", subdir);
            Directory.CreateDirectory(fullPath);
        }
    }
}
