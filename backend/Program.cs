using Backend.Database.ExtensionClasses;
using Backend.Extensions;
using DotNetEnv;

namespace Backend.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        Env.Load();

        var builder = WebApplication.CreateBuilder(args);

        ConfigureServices(builder.Services);

        var app = builder.Build();

        ConfigureMiddleware(app);

        await InitializeDatabaseAsync(app.Services);

        app.Run();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddHttpsRedirection(options => options.HttpsPort = 5000);
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
        if (app.Environment.IsDevelopment())
        {
            app.UseSwaggerWithUI();
        }

        app.UseHttpsRedirection();
        app.UseSecurityHeaders();
        app.UseCors();
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
