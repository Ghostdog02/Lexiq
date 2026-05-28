namespace Backend.Api.Services.Clock;

public interface IClock
{
    DateTime UtcNow { get; }
}
