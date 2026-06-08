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
    /// Grants at most 1 heart per call: if at least one full 4-hour interval has elapsed,
    /// increments by 1 and advances LastHeartResetAt by 4h to preserve carry-over minutes.
    /// Timer is frozen when hearts == MaxHearts.
    /// </summary>
    public async Task<int> RefillAndGetHeartsAsync(User user)
    {
        if (user.Hearts < MaxHearts)
        {
            var elapsed = _clock.UtcNow - user.LastHeartResetAt;

            if (elapsed >= TimeSpan.FromHours(RefillIntervalHours))
            {
                user.Hearts = Math.Min(user.Hearts + 1, MaxHearts);
                user.LastHeartResetAt = user.LastHeartResetAt.AddHours(RefillIntervalHours);
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
