using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Backend.Database;

public class BackendDbContextFactory : IDesignTimeDbContextFactory<BackendDbContext>
{
    public BackendDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BackendDbContext>();

        var connectionString = BuildConnectionString();

        optionsBuilder.UseSqlServer(connectionString);

        return new BackendDbContext(optionsBuilder.Options);
    }

    private static string BuildConnectionString()
    {
        var dbServer = Environment.GetEnvironmentVariable("DB_SERVER") ?? "localhost";
        var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "lexiq";
        var dbUser = Environment.GetEnvironmentVariable("DB_USER_ID") ?? "sa";
        var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

        return $"Server={dbServer};Database={dbName};User Id={dbUser};Password={dbPassword};"
            + $"Encrypt=True;TrustServerCertificate=True;Connection Timeout=30";
    }
}
