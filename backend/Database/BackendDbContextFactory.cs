using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Backend.Database;

public class BackendDbContextFactory : IDesignTimeDbContextFactory<BackendDbContext>
{
    public BackendDbContext CreateDbContext(string[] args)
    {            
        var optionsBuilder = new DbContextOptionsBuilder<BackendDbContext>();

        return new BackendDbContext(optionsBuilder.Options);
    }
}