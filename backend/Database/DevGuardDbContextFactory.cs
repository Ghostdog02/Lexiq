using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace DevGuard.Database
{
    public class DevGuardDbContextFactory : IDesignTimeDbContextFactory<DevGuardDbContext>
    {
        public DevGuardDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<DevGuardDbContext>();

            var basePath = Path.GetFullPath(
                Path.Combine(Directory.GetCurrentDirectory(), "..", "DevGuard.Api")
            );

            var configuration = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .Build();

            var connectionString = configuration.GetConnectionString("DevGuardConnection");
            optionsBuilder.UseSqlServer(connectionString);

            return new DevGuardDbContext(optionsBuilder.Options);
        }
    }
}
