using Backend.Api.Extensions;
using Backend.Database.Extensions;
using DotNetEnv;

namespace Backend.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        Env.Load("/run/secrets/backend_env");

        var builder = WebApplication.CreateBuilder(args);

        ConfigureServices(builder.Services);

        var app = builder.Build();

        app.WaitForDatabaseCreation();

        ConfigureMiddleware(app);

        await InitializeDatabaseAsync(app.Services);

        app.Run();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddCorsPolicy();
        services.AddDatabaseContext();
        services.AddApplicationServices();
        services.AddCookieAuthentication();
        services.AddControllersWithOptions();
        services.AddIdentityConfiguration();
        services.AddGoogleAuthentication();
        services.AddSwaggerDocumentation();
    }

    private static void ConfigureMiddleware(WebApplication app)
    {
        app.UseCors();
        app.UseSecurityHeaders();
        app.UseSwaggerWithUI();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
    }

    private static async Task InitializeDatabaseAsync(IServiceProvider services)
    {
        await services.MigrateDbAsync();
        await SeedData.InitializeAsync(services);
    }
}
