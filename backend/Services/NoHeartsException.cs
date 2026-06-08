namespace Backend.Api.Services;

public class NoHeartsException()
    : InvalidOperationException("You need at least one heart to play. Hearts refill every 4 hours.");
