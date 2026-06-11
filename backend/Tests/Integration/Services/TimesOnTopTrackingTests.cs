using Backend.Api.Dtos;
using Backend.Api.Services;
using Backend.Database;
using Backend.Tests.Builders;
using Backend.Tests.Helpers;
using Backend.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Backend.Tests.Integration.Services;

/// <summary>
/// Integration tests for the TimesOnTop increment logic in LeaderboardService.
///
/// Rules:
/// - Incremented at most once per UTC day for the AllTime rank-1 user
/// - Tie at rank 1 broken by lowest UserId (lexicographic)
/// - Only triggered when timeFrame == AllTime
/// </summary>
public class TimesOnTopTrackingTests(DatabaseFixture fixture)
    : IClassFixture<DatabaseFixture>,
        IAsyncLifetime
{
    private readonly DatabaseFixture _fixture = fixture;
    private BackendDbContext _ctx = null!;
    private FakeClock _clock = null!;
    private LeaderboardService _sut = null!;

    public async ValueTask InitializeAsync()
    {
        _ctx = _fixture.CreateDbContext();
        await DbSeeder.ClearLeaderboardDataAsync(_ctx, _fixture.SystemUserId);
        _clock = new FakeClock();
        _sut = new LeaderboardService(_ctx, CreateAvatarService(), _clock, new MemoryCache(new MemoryCacheOptions()));
    }

    public async ValueTask DisposeAsync()
    {
        await _ctx.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    // Simulates a new day: advances the fake clock and recreates the service
    // with a fresh cache so stale leaderboard data doesn't persist across day boundaries.
    private void AdvanceDay(int days = 1)
    {
        _clock.Advance(TimeSpan.FromDays(days));
        _sut = new LeaderboardService(_ctx, CreateAvatarService(), _clock, new MemoryCache(new MemoryCacheOptions()));
    }

    private AvatarService CreateAvatarService()
    {
        var factory = new ServiceCollection()
            .AddHttpClient()
            .BuildServiceProvider()
            .GetRequiredService<IHttpClientFactory>();
        return new AvatarService(_ctx, factory, NullLogger<AvatarService>.Instance);
    }

    private async Task<Backend.Database.Entities.Users.User> AddUserWithXpAsync(
        string username, int xp, string? fixedId = null
    )
    {
        var user = new UserBuilder()
            .WithUserName(username)
            .WithEmail($"{username}@test.com")
            .Build();
        if (fixedId != null)
            user.Id = fixedId;
        user.TotalPointsEarned = xp;
        await DbSeeder.AddUserAsync(_ctx, user);
        return user;
    }

    [Fact]
    public async Task NewUser_TimesOnTopIsZero_LastTimesOnTopAtIsNull()
    {
        // Arrange
        var user = await AddUserWithXpAsync("newuser", 100);

        // Assert
        user.TimesOnTop.Should().Be(0);
        user.LastTimesOnTopAt.Should().BeNull();
    }

    [Fact]
    public async Task UserBecomesRank1_TimesOnTopIncrements()
    {
        // Arrange
        var user = await AddUserWithXpAsync("top", 1000);

        // Act
        await _sut.GetLeaderboardAsync(TimeFrame.AllTime, user.Id);
        await _ctx.Entry(user).ReloadAsync(TestContext.Current.CancellationToken);

        // Assert
        user.TimesOnTop.Should().Be(1, because: "first time at rank 1 for this UTC day");
        user.LastTimesOnTopAt.Should().NotBeNull();
    }

    [Fact]
    public async Task SameDayTwoCalls_IdempotentIncrement()
    {
        // Arrange
        var user = await AddUserWithXpAsync("idem", 1000);

        // Act — two calls on the same UTC day
        await _sut.GetLeaderboardAsync(TimeFrame.AllTime, user.Id);
        await _sut.GetLeaderboardAsync(TimeFrame.AllTime, user.Id);
        await _ctx.Entry(user).ReloadAsync(TestContext.Current.CancellationToken);

        // Assert
        user.TimesOnTop.Should().Be(1, because: "increment is idempotent within one UTC day");
    }

    [Fact]
    public async Task NextDay_StillRank1_IncrementsAgain()
    {
        // Arrange
        var user = await AddUserWithXpAsync("twodays", 1000);
        await _sut.GetLeaderboardAsync(TimeFrame.AllTime, user.Id);

        // Advance to the next day
        AdvanceDay();

        // Act
        await _sut.GetLeaderboardAsync(TimeFrame.AllTime, user.Id);
        await _ctx.Entry(user).ReloadAsync(TestContext.Current.CancellationToken);

        // Assert
        user.TimesOnTop.Should().Be(2, because: "a new UTC day means a new increment");
    }

    [Fact]
    public async Task UserDropsToRank2_NoIncrement()
    {
        // Arrange
        var user1 = await AddUserWithXpAsync("rank1", 1000);
        var user2 = await AddUserWithXpAsync("rank2", 500);

        await _sut.GetLeaderboardAsync(TimeFrame.AllTime, user1.Id);

        // Now user2 surpasses user1
        user2.TotalPointsEarned = 2000;
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        AdvanceDay();

        // Act — leaderboard with user2 at rank 1
        await _sut.GetLeaderboardAsync(TimeFrame.AllTime, user2.Id);
        await _ctx.Entry(user1).ReloadAsync(TestContext.Current.CancellationToken);
        await _ctx.Entry(user2).ReloadAsync(TestContext.Current.CancellationToken);

        // Assert
        user1.TimesOnTop.Should().Be(1, because: "user1 was rank 1 on day 1 only");
        user2.TimesOnTop.Should().Be(1, because: "user2 becomes rank 1 on day 2");
    }

    [Fact]
    public async Task TieAtRank1_LowestUserIdGetsCredit()
    {
        // Arrange — use known IDs to control lexicographic order
        var lowId = "aaaa-0001-0001-0001";
        var highId = "zzzz-9999-9999-9999";

        var userLow = await AddUserWithXpAsync("lowid", 1000, lowId);
        var userHigh = await AddUserWithXpAsync("highid", 1000, highId);

        // Act
        await _sut.GetLeaderboardAsync(TimeFrame.AllTime, null);
        await _ctx.Entry(userLow).ReloadAsync(TestContext.Current.CancellationToken);
        await _ctx.Entry(userHigh).ReloadAsync(TestContext.Current.CancellationToken);

        // Assert
        userLow.TimesOnTop.Should().Be(1, because: "lowest UserId wins the tie");
        userHigh.TimesOnTop.Should().Be(0, because: "higher UserId does not get the increment on a tie");
    }

    [Fact]
    public async Task WeeklyTimeFrame_DoesNotIncrementTimesOnTop()
    {
        // Arrange
        var user = await AddUserWithXpAsync("weeklyuser", 500);
        // Seed some weekly XP via progress rows
        for (var i = 0; i < 3; i++)
        {
            var exId = await DbSeeder.CreateFillInBlankExerciseAsync(
                _ctx, _fixture.LessonId, orderIndex: i + 100
            );
            await DbSeeder.AddProgressAsync(_ctx, user.Id, exId, true, 100, _clock.UtcNow.AddDays(-1));
        }

        // Act
        await _sut.GetLeaderboardAsync(TimeFrame.Weekly, user.Id);
        await _ctx.Entry(user).ReloadAsync(TestContext.Current.CancellationToken);

        // Assert
        user.TimesOnTop.Should().Be(0, because: "TimesOnTop is only incremented for AllTime leaderboard");
    }

    [Fact]
    public async Task UserReturnsToRank1_AfterGap_IncrementOnce()
    {
        // Arrange
        var user = await AddUserWithXpAsync("comeback", 1000);

        // Day 1 — at rank 1
        await _sut.GetLeaderboardAsync(TimeFrame.AllTime, user.Id);

        // Day 2 — competitor surpasses
        AdvanceDay();
        var rival = await AddUserWithXpAsync("rival", 2000);
        await _sut.GetLeaderboardAsync(TimeFrame.AllTime, user.Id);

        // Day 3 — user regains rank 1
        AdvanceDay();
        rival.TotalPointsEarned = 500;
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
        await _sut.GetLeaderboardAsync(TimeFrame.AllTime, user.Id);

        await _ctx.Entry(user).ReloadAsync(TestContext.Current.CancellationToken);

        // Assert
        user.TimesOnTop.Should().Be(2, because: "rank-1 on day 1 and day 3 but not day 2");
    }

    [Fact]
    public async Task TwoUsersAlternate_EachGetsCorrectCount()
    {
        // Arrange
        var a = await AddUserWithXpAsync("alternateA", 1000);
        var b = await AddUserWithXpAsync("alternateB", 500);

        // Days 1, 3, 5 — A at rank 1. Days 2, 4 — B at rank 1.
        for (var day = 1; day <= 5; day++)
        {
            if (day % 2 == 0)
            {
                a.TotalPointsEarned = 500;
                b.TotalPointsEarned = 1000;
            }
            else
            {
                a.TotalPointsEarned = 1000;
                b.TotalPointsEarned = 500;
            }
            await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
            await _sut.GetLeaderboardAsync(TimeFrame.AllTime, null);
            AdvanceDay();
        }

        await _ctx.Entry(a).ReloadAsync(TestContext.Current.CancellationToken);
        await _ctx.Entry(b).ReloadAsync(TestContext.Current.CancellationToken);

        // Assert
        a.TimesOnTop.Should().Be(3, because: "A is rank 1 on days 1, 3, 5");
        b.TimesOnTop.Should().Be(2, because: "B is rank 1 on days 2 and 4");
    }
}
