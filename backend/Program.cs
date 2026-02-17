using Backend.Api.Extensions;
using Backend.Database.Extensions;
using DotNetEnv;

namespace Backend.Api;

public class Program
{
    public static async Task Main(string[] args)
    {
        var secretPath = "/run/secrets/backend_env";
        if (File.Exists(secretPath))
        {
            Env.Load(secretPath);
        }
        else
        {
            Env.Load();
        }

        var builder = WebApplication.CreateBuilder(args);

        ConfigureServices(builder.Services);

        var app = builder.Build();
        app.Environment.EnsureUploadDirectoryStructure();

        ConfigureMiddleware(app);

        await InitializeDatabaseAsync(app.Services);

        app.Run();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddCorsPolicy();
        services.AddDatabaseContext();
        services.AddApplicationServices();
        services.AddControllersWithOptions();
        services.AddIdentityConfiguration();
        services.AddGoogleAuthentication();
        services.AddJwtAuthentication();
        services.AddSwaggerDocumentation();
        services.LimitFileUploads();
        services.AddHealthChecks();
    }

    private static void ConfigureMiddleware(WebApplication app)
    {
        app.UseRouting();
        app.UseCors("AllowAngular");
        app.UseStaticFiles();
        app.UseSwaggerWithUI();
        app.UseAuthentication();
        app.UseUserContext(); // Extract user entity from JWT
        app.UseAuthorization();
        app.MapHealthChecks("/health");
        app.MapControllers();
    }

    private static async Task InitializeDatabaseAsync(IServiceProvider serviceProvider)
    {
        await serviceProvider.MigrateDbAsync();
        await SeedData.InitializeAsync(serviceProvider);
    }
}
