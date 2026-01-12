using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Database.Extensions;

public static class DataExtensions
{
    public static async Task MigrateDbAsync(this IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BackendDbContext>();

        var pendingMigrations = dbContext.Database.GetPendingMigrations();
        if (pendingMigrations.Any())
        {
            await dbContext.Database.MigrateAsync();
        }
    }
}
