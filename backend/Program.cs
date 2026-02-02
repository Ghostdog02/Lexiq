using System.Net;
using Backend.Api.Extensions;
using Backend.Database.Extensions;
using DotNetEnv;
using LettuceEncrypt;

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

        var useHttps = Environment.GetEnvironmentVariable("USE_HTTPS")?.ToLower() == "true";

        if (useHttps)
        {
            builder.WebHost.UseKestrel(kestrel =>
            {
                var appServices = kestrel.ApplicationServices;

                // HTTP on port 80 (required for ACME HTTP-01 challenge)
                kestrel.Listen(IPAddress.Any, 80);

                // HTTPS on port 443 with Let's Encrypt
                kestrel.Listen(IPAddress.Any, 443, listenOptions =>
                {
                    listenOptions.UseHttps(httpsOptions =>
                    {
                        httpsOptions.UseLettuceEncrypt(appServices);
                    });
                });
            });
        }

        ConfigureServices(builder.Services, useHttps);

        var app = builder.Build();

        app.Environment.EnsureUploadDirectoryStructure();

        ConfigureMiddleware(app, useHttps);

        await InitializeDatabaseAsync(app.Services);

        app.Run();
    }

    private static void ConfigureServices(IServiceCollection services, bool useHttps)
    {
        if (useHttps)
        {
            var certPath = Environment.GetEnvironmentVariable("CERT_STORAGE_PATH") ?? "/app/certs";
            var certPassword = Environment.GetEnvironmentVariable("CERT_PASSWORD") ?? "lexiq-cert-password";

            services.AddLettuceEncrypt()
                .PersistDataToDirectory(new DirectoryInfo(certPath), certPassword);
        }

        services.AddCorsPolicy();
        services.AddDatabaseContext();
        services.AddApplicationServices();
        services.AddCookieAuthentication();
        services.AddControllersWithOptions();
        services.AddIdentityConfiguration();
        services.AddGoogleAuthentication();
        services.AddSwaggerDocumentation();
        services.LimitFileUploads();
        services.AddHealthChecks();
    }

    private static void ConfigureMiddleware(WebApplication app, bool useHttps)
    {
        app.UseRouting();

        if (useHttps)
        {
            app.UseHttpsRedirection();
        }
        else
        {
            app.ConfigureHttpPort();
        }

        app.UseCors("AllowAngular");
        app.UseStaticFiles();
        app.UseSwaggerWithUI();
        app.UseAuthentication();
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
