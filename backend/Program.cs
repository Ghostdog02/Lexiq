using Backend.Api.Extensions;
using Backend.Database.Extensions;
using DotNetEnv;

namespace Backend.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        Env.Load();

        var builder = WebApplication.CreateBuilder(args);

        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            serverOptions.ListenLocalhost(5000); // HTTP
            serverOptions.ListenLocalhost(
                5001,
                listenOptions =>
                {
                    listenOptions.UseHttps();
                }
            );
        });

        ConfigureServices(builder.Services);

        var app = builder.Build();

        ConfigureMiddleware(app);

        await InitializeDatabaseAsync(app.Services);

        app.Run();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddHsts(options =>
        {
            options.Preload = true;
            options.IncludeSubDomains = true;
        });
        services.AddHttpsRedirection(options =>
        {
            options.RedirectStatusCode = 307;
            options.HttpsPort = 5001;
        });
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
        app.UseHttpsRedirection();
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
