using Backend.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Backend.Database.Extensions;

public static class DatabaseExtensions
{
    public static async Task MigrateDbAsync(this IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILogger<BackendDbContext>>();

        const int maxRetries = 10;
        const int baseDelayMs = 3000;

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<BackendDbContext>();

                logger.LogInformation(
                    "Attempting to apply database migrations... (Attempt {Attempt}/{MaxRetries})",
                    attempt,
                    maxRetries
                );

                var pending = (await dbContext.Database.GetPendingMigrationsAsync()).ToList();

                if (pending.Count > 0)
                {
                    logger.LogInformation("Applying {Count} pending migrations", pending.Count);
                    await dbContext.Database.MigrateAsync();
                    logger.LogInformation("Database migrations applied successfully");
                }

                else
                {
                    logger.LogInformation("No pending migrations found");
                }

                return;
            }
                
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                logger.LogWarning(
                    ex,
                    "Failed to apply migrations (Attempt {Attempt}/{MaxRetries})",
                    attempt,
                    maxRetries
                );

                if (attempt >= maxRetries)
                {
                    logger.LogError(
                        ex,
                        "Failed to apply database migrations after {MaxRetries} attempts",
                        maxRetries
                    );
                    throw;
                }

                var delay = baseDelayMs * (1 << (attempt - 1)); // 3s, 6s, 12s, 24s...
                logger.LogInformation("Retrying in {Delay}ms...", delay);
                await Task.Delay(delay);
            }
        }
    }
}
