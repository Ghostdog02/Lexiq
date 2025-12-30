using Backend.Api.Extensions;
using Backend.Database;
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

        ConfigureMiddleware(app);

        await app.InitializeDatabaseAsync();

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
}
