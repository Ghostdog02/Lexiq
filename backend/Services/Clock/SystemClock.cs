namespace Backend.Api.Services.Clock;

public class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
