using Backend.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Backend.Api.Extensions;

public static class DatabaseExtensions
{
    public static async Task MigrateDbAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BackendDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<BackendDbContextFactory>>();

        var retryCount = 0;
        const int maxRetries = 10;
        const int delayMilliseconds = 3000;

        while (retryCount < maxRetries)
        {
            try
            {
                logger.LogInformation(
                    "Attempting to apply database migrations... (Attempt {Attempt}/{MaxRetries})",
                    retryCount + 1,
                    maxRetries
                );

                var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();

                if (pendingMigrations.Any())
                {
                    logger.LogInformation(
                        "Applying {Count} pending migrations",
                        pendingMigrations.Count()
                    );
                    await dbContext.Database.MigrateAsync();
                    logger.LogInformation("Database migrations applied successfully");
                }
                
                else
                {
                    logger.LogInformation("No pending migrations found");
                }

                return; // Success, exit the method
            }
            catch (Exception ex)
            {
                retryCount++;
                logger.LogWarning(
                    ex,
                    "Failed to apply migrations (Attempt {Attempt}/{MaxRetries})",
                    retryCount,
                    maxRetries
                );

                if (retryCount >= maxRetries)
                {
                    logger.LogError(
                        ex,
                        "Failed to apply database migrations after {MaxRetries} attempts",
                        maxRetries
                    );
                    throw;
                }

                logger.LogInformation("Retrying in {Delay}ms...", delayMilliseconds);
                await Task.Delay(delayMilliseconds);
            }
        }
    }
}
