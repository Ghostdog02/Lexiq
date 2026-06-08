using Backend.Api.Services;
using Backend.Database;
using Backend.Tests.Builders;
using Backend.Tests.Helpers;
using Backend.Tests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Integration.Services;

/// <summary>
/// Integration tests for HeartsService.RefillAndGetHeartsAsync against a real SQL Server instance.
///
/// Refill formula: granted = floor(elapsedHours / 4) capped at (MaxHearts - hearts).
/// LastHeartResetAt advances by granted * 4 h so carry-over minutes persist.
/// Timer is frozen at max (5) hearts; any decrement from 5 → 4 resets LastHeartResetAt.
/// </summary>
public class HeartsRefillTests(DatabaseFixture fixture)
    : IClassFixture<DatabaseFixture>,
        IAsyncLifetime
{
    private readonly DatabaseFixture _fixture = fixture;
    private BackendDbContext _ctx = null!;
    private FakeClock _clock = null!;
    private HeartsService _sut = null!;

    public async ValueTask InitializeAsync()
    {
        _ctx = _fixture.CreateDbContext();
        await DbSeeder.ClearLeaderboardDataAsync(_ctx, _fixture.SystemUserId);
        _clock = new FakeClock();
        _sut = new HeartsService(_ctx, _clock);
        await Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await _ctx.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    private async Task<Backend.Database.Entities.Users.User> CreateUserWithHeartsAsync(
        int hearts,
        DateTime? lastHeartResetAt = null
    )
    {
        var user = new UserBuilder()
            .WithUserName($"user-{Guid.NewGuid():N}")
            .WithEmail($"{Guid.NewGuid():N}@test.com")
            .Build();
        user.Hearts = hearts;
        user.LastHeartResetAt = lastHeartResetAt ?? _clock.UtcNow;
        await DbSeeder.AddUserAsync(_ctx, user);
        return user;
    }

    [Fact]
    public async Task NewUser_Has5Hearts_TimerSetAtRegistration()
    {
        // Arrange
        var user = await CreateUserWithHeartsAsync(HeartsService.MaxHearts);

        // Act
        var hearts = await _sut.RefillAndGetHeartsAsync(user);

        // Assert
        hearts.Should().Be(5, because: "new users start at max hearts");
        user.Hearts.Should().Be(5);
    }

    [Fact]
    public async Task HeartsAtMax_AnyElapsedTime_NoRefill()
    {
        // Arrange
        var resetAt = _clock.UtcNow.AddHours(-8);
        var user = await CreateUserWithHeartsAsync(5, resetAt);

        // Act
        _clock.Advance(TimeSpan.FromHours(8));
        var hearts = await _sut.RefillAndGetHeartsAsync(user);

        // Assert
        hearts.Should().Be(5, because: "timer is frozen at max hearts — elapsed time does not trigger refill");
        user.LastHeartResetAt.Should().Be(resetAt, because: "frozen timer must not advance");
    }

    [Fact]
    public async Task Hearts3_LessThan4HoursElapsed_NoRefill()
    {
        // Arrange
        var user = await CreateUserWithHeartsAsync(3);
        _clock.Advance(TimeSpan.FromHours(3).Add(TimeSpan.FromMinutes(59)));

        // Act
        var hearts = await _sut.RefillAndGetHeartsAsync(user);

        // Assert
        hearts.Should().Be(3, because: "3h 59m < 4h interval — no refill yet");
    }

    [Fact]
    public async Task Hearts3_4Hours1MinElapsed_RefillsTo4AndAdvancesTimer()
    {
        // Arrange
        var start = _clock.UtcNow;
        var user = await CreateUserWithHeartsAsync(3, start);
        _clock.Advance(TimeSpan.FromHours(4).Add(TimeSpan.FromMinutes(1)));

        // Act
        var hearts = await _sut.RefillAndGetHeartsAsync(user);

        // Assert
        hearts.Should().Be(4, because: "floor(4.017h / 4) = 1 heart granted");
        user.LastHeartResetAt.Should().Be(
            start.AddHours(4),
            because: "timer advances by granted * 4h so the 1-min carry-over persists into the next cycle"
        );
    }

    [Fact]
    public async Task Hearts3_8HoursElapsed_RefillsTo5AndAdvancesTimerBy8h()
    {
        // Arrange
        var start = _clock.UtcNow;
        var user = await CreateUserWithHeartsAsync(3, start);
        _clock.Advance(TimeSpan.FromHours(8));

        // Act
        var hearts = await _sut.RefillAndGetHeartsAsync(user);

        // Assert
        hearts.Should().Be(5, because: "floor(8 / 4) = 2 hearts granted, 3+2 = 5 = max");
        user.LastHeartResetAt.Should().Be(start.AddHours(8));
    }

    [Fact]
    public async Task Hearts0_12HoursElapsed_Refills3Hearts()
    {
        // Arrange
        var start = _clock.UtcNow;
        var user = await CreateUserWithHeartsAsync(0, start);
        _clock.Advance(TimeSpan.FromHours(12));

        // Act
        var hearts = await _sut.RefillAndGetHeartsAsync(user);

        // Assert
        hearts.Should().Be(3, because: "floor(12 / 4) = 3 hearts granted from 0");
        user.LastHeartResetAt.Should().Be(start.AddHours(12));
    }

    [Fact]
    public async Task Hearts0_28HoursElapsed_CapsAt5AndAdvancesTimerBy20h()
    {
        // Arrange
        var start = _clock.UtcNow;
        var user = await CreateUserWithHeartsAsync(0, start);
        _clock.Advance(TimeSpan.FromHours(28));

        // Act
        var hearts = await _sut.RefillAndGetHeartsAsync(user);

        // Assert
        hearts.Should().Be(5, because: "floor(28 / 4) = 7, capped at MaxHearts = 5");
        user.LastHeartResetAt.Should().Be(
            start.AddHours(20),
            because: "timer advances by 5 (granted) * 4h = 20h, not 28h"
        );
    }

    [Fact]
    public async Task Hearts4_4HoursElapsed_CapRespectedOnly1Granted()
    {
        // Arrange
        var start = _clock.UtcNow;
        var user = await CreateUserWithHeartsAsync(4, start);
        _clock.Advance(TimeSpan.FromHours(4));

        // Act
        var hearts = await _sut.RefillAndGetHeartsAsync(user);

        // Assert
        hearts.Should().Be(5, because: "only 1 heart needed to reach max even though formula allows 1");
        user.LastHeartResetAt.Should().Be(start.AddHours(4));
    }

    [Fact]
    public async Task NoRefillEligible_TimerNotBumped()
    {
        // Arrange
        var start = _clock.UtcNow;
        var user = await CreateUserWithHeartsAsync(3, start);
        // No time advances

        // Act
        var hearts = await _sut.RefillAndGetHeartsAsync(user);

        // Assert
        hearts.Should().Be(3);
        user.LastHeartResetAt.Should().Be(start, because: "timer must not move when no refill occurs");
    }

    [Fact]
    public async Task Hearts2_TwoConsecutiveRefills_4ThenMax()
    {
        // Arrange
        var start = _clock.UtcNow;
        var user = await CreateUserWithHeartsAsync(2, start);

        // First refill: 4h elapsed
        _clock.Advance(TimeSpan.FromHours(4));
        var after4h = await _sut.RefillAndGetHeartsAsync(user);

        // Second refill: another 4h elapsed
        _clock.Advance(TimeSpan.FromHours(4));
        var after8h = await _sut.RefillAndGetHeartsAsync(user);

        // Assert
        after4h.Should().Be(3, because: "2 + floor(4/4) = 3");
        after8h.Should().Be(4, because: "3 + floor(4/4) = 4 on the second call");
    }

    [Fact]
    public async Task NoSurplusAtMax_AfterDecrement_TimerResetToDecrement_NoImmediateRefill()
    {
        // Arrange — start at 5 hearts, advance 8h with no loss (timer frozen)
        var decrement = _clock.UtcNow.AddHours(8);
        _clock.UtcNow = decrement;
        var user = await CreateUserWithHeartsAsync(5, _clock.UtcNow.AddHours(-8));

        // Simulate a wrong answer: 5 → 4, timer resets to moment of loss
        _sut.DecrementHearts(user, 1);
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        user.Hearts.Should().Be(4);
        user.LastHeartResetAt.Should().Be(decrement, because: "timer resets at moment of 5→4 loss");

        // 3h 59m later — no refill yet (surplus from 8h at max is gone)
        _clock.Advance(TimeSpan.FromHours(3).Add(TimeSpan.FromMinutes(59)));
        var hearts = await _sut.RefillAndGetHeartsAsync(user);
        hearts.Should().Be(4, because: "only 3h59m since loss — 4h interval not reached");

        // Another 2 min (total 4h01m since loss) → refill
        _clock.Advance(TimeSpan.FromMinutes(2));
        hearts = await _sut.RefillAndGetHeartsAsync(user);
        hearts.Should().Be(5, because: "4h01m since loss completes one refill cycle");
    }
}
