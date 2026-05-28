using Backend.Api.Services.Clock;

namespace Backend.Tests.Infrastructure;

public class FakeClock : IClock
{
    public DateTime UtcNow { get; set; } = DateTime.UtcNow;

    public void Advance(TimeSpan by) => UtcNow += by;
    public void SetDate(DateOnly date) => UtcNow = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
}
