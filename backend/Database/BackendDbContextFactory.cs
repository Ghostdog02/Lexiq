using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Backend.Database;

public class BackendDbContextFactory : IDesignTimeDbContextFactory<BackendDbContext>
{
    public BackendDbContext CreateDbContext(string[] args)
    {
        // Load environment variables from .env file
        var envPath = "/run/secrets/backend_env";
        if (File.Exists(envPath))
        {
            Env.Load(envPath);
        }

        else
        {
            Env.Load();
        }

        var optionsBuilder = new DbContextOptionsBuilder<BackendDbContext>();

        var connectionString = BuildConnectionString();

        optionsBuilder.UseSqlServer(connectionString);

        return new BackendDbContext(optionsBuilder.Options);
    }

    private static string BuildConnectionString()
    {
        var dbServer = Environment.GetEnvironmentVariable("DB_SERVER");
        var dbName = Environment.GetEnvironmentVariable("DB_NAME");
        var dbUser = Environment.GetEnvironmentVariable("DB_USER_ID");
        var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        return environment switch
        {
            "development" =>
                $"Server={dbServer};Database={dbName};User Id={dbUser};Password={dbPassword};"
                    + $"Encrypt=False;TrustServerCertificate=True;Connection Timeout=30",

            "production" =>
                $"Server={dbServer};Database={dbName};User Id={dbUser};Password={dbPassword};"
                    + $"Encrypt=True;TrustServerCertificate=True;Connection Timeout=30",

            _ => $"Server={dbServer};Database={dbName};User Id={dbUser};Password={dbPassword};"
                + $"Encrypt=False;TrustServerCertificate=True;Connection Timeout=30",
        };
    }
}
