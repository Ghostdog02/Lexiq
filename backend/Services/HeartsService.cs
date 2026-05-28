using Backend.Api.Services.Clock;
using Backend.Database;
using Backend.Database.Entities.Users;

namespace Backend.Api.Services;

public class HeartsService(BackendDbContext context, IClock clock)
{
    private readonly BackendDbContext _context = context;
    private readonly IClock _clock = clock;

    public const int MaxHearts = 5;
    public const int RefillIntervalHours = 4;

    /// <summary>
    /// Applies the refill formula and saves. Returns the current heart count after refill.
    /// Formula: granted = floor(elapsedHours / 4) capped at (MaxHearts - hearts).
    /// LastHeartResetAt advances by granted * 4h so carry-over minutes persist.
    /// Timer is frozen when hearts == MaxHearts.
    /// </summary>
    public async Task<int> RefillAndGetHeartsAsync(User user)
    {
        if (user.Hearts < MaxHearts)
        {
            var elapsed = _clock.UtcNow - user.LastHeartResetAt;
            var granted = Math.Min(
                (int)Math.Floor(elapsed.TotalHours / RefillIntervalHours),
                MaxHearts - user.Hearts
            );

            if (granted > 0)
            {
                user.Hearts += granted;
                user.LastHeartResetAt = user.LastHeartResetAt.AddHours(granted * RefillIntervalHours);
                await _context.SaveChangesAsync();
            }
        }

        return user.Hearts;
    }

    /// <summary>
    /// Decrements hearts by wrongCount (clamped to 0). Resets LastHeartResetAt on the 5→4
    /// transition so surplus time at max hearts cannot grant an immediate refill.
    /// </summary>
    public void DecrementHearts(User user, int wrongCount)
    {
        if (wrongCount <= 0)
            return;

        var wasAtMax = user.Hearts == MaxHearts;
        user.Hearts = Math.Max(0, user.Hearts - wrongCount);

        if (wasAtMax && user.Hearts < MaxHearts)
            user.LastHeartResetAt = _clock.UtcNow;
    }
}
