using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using DotNetEnv;

namespace Backend.Database
{
    public class BackendDbContextFactory : IDesignTimeDbContextFactory<BackendDbContext>
    {
        public BackendDbContext CreateDbContext(string[] args)
        {
            Env.Load();
            var optionsBuilder = new DbContextOptionsBuilder<BackendDbContext>();

            var dbServer = Environment.GetEnvironmentVariable("DB_SERVER") ?? "localhost";
            var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "lexiq";
            var dbUser = Environment.GetEnvironmentVariable("DB_USER_ID") ?? "sa";
            var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

            var connectionString =
                $"Server={dbServer};Database={dbName};User Id={dbUser};Password={dbPassword};Encrypt=True;TrustServerCertificate=True;Connection Timeout=30;";

            optionsBuilder.UseSqlServer(connectionString);

            return new BackendDbContext(optionsBuilder.Options);
        }
    }
}