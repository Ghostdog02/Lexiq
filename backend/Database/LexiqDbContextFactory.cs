using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace DevGuard.Database
{
    public class LexiqDbContextFactory : IDesignTimeDbContextFactory<LexiqDbContext>
    {
        public LexiqDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<LexiqDbContext>();

            var basePath = Path.GetFullPath(
                Path.Combine(Directory.GetCurrentDirectory(), "..", "DevGuard.Api")
            );

            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var connectionString = configuration.GetConnectionString("DevGuardConnection");
            optionsBuilder.UseSqlServer(connectionString);

            return new LexiqDbContext(optionsBuilder.Options);
        }
    }
}
