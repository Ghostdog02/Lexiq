using Backend.Api.Services;
using Backend.Tests.Builders;
using Backend.Tests.Helpers;
using Backend.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Backend.Tests.Services;

/// <summary>
/// Integration tests for LeaderboardService.GetStreakAsync against a real SQL Server instance.
///
/// Streak rules:
/// - Counts consecutive UTC calendar days with at least one completed exercise.
/// - Grace period: if no activity today, yesterday still counts as the current streak.
/// - "Current streak" resets to 0 if the most recent activity is 2+ days ago.
/// </summary>
public class GetStreakTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private Database.BackendDbContext _ctx = null!;
    private LeaderboardService _service = null!;
    private string _userId = null!;

    public GetStreakTests(DatabaseFixture fixture) => _fixture = fixture;

    // xUnit creates a new instance per test, so InitializeAsync/DisposeAsync run per test.
    public async ValueTask InitializeAsync()
    {
        _ctx = _fixture.CreateDbContext();
        await DbSeeder.ClearLeaderboardDataAsync(_ctx, _fixture.SystemUserId);

        var user = new UserBuilder()
            .WithUserName("streakuser")
            .WithEmail("streak@test.com")
            .Build();
        await DbSeeder.AddUserAsync(_ctx, user);
        _userId = user.Id;

        _service = new LeaderboardService(_ctx, CreateAvatarService(_ctx));
    }

    public async ValueTask DisposeAsync() => await _ctx.DisposeAsync();

    // ── No activity ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStreak_NoProgress_ReturnsBothZero()
    {
        var (current, longest) = await _service.GetStreakAsync(_userId);

        current.Should().Be(0);
        longest.Should().Be(0);
    }

    [Fact]
    public async Task GetStreak_OnlyUncompletedProgress_ReturnsBothZero()
    {
        // IsCompleted = false rows are ignored by the streak query
        await DbSeeder.AddProgressAsync(
            _ctx,
            _userId,
            _fixture.ExerciseIds[0],
            isCompleted: false,
            pointsEarned: 0,
            completedAt: DateTime.UtcNow
        );

        var (current, longest) = await _service.GetStreakAsync(_userId);

        current.Should().Be(0);
        longest.Should().Be(0);
    }

    // ── Single day ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStreak_SingleCompletionToday_ReturnsBothOne()
    {
        await DbSeeder.AddProgressAsync(
            _ctx,
            _userId,
            _fixture.ExerciseIds[0],
            isCompleted: true,
            pointsEarned: 10,
            completedAt: DateTime.UtcNow
        );

        var (current, longest) = await _service.GetStreakAsync(_userId);

        current.Should().Be(1);
        longest.Should().Be(1);
    }

    [Fact]
    public async Task GetStreak_SingleCompletionYesterday_ReturnsCurrentStreakOne()
    {
        // Grace period: activity yesterday counts as an active current streak
        await DbSeeder.AddProgressAsync(
            _ctx,
            _userId,
            _fixture.ExerciseIds[0],
            isCompleted: true,
            pointsEarned: 10,
            completedAt: DateTime.UtcNow.Date.AddDays(-1)
        );

        var (current, longest) = await _service.GetStreakAsync(_userId);

        current.Should().Be(1);
        longest.Should().Be(1);
    }

    [Fact]
    public async Task GetStreak_SingleCompletion2DaysAgo_ReturnsCurrentStreakZero_LongestOne()
    {
        // 2 days ago is outside the grace period — current streak is broken
        await DbSeeder.AddProgressAsync(
            _ctx,
            _userId,
            _fixture.ExerciseIds[0],
            isCompleted: true,
            pointsEarned: 10,
            completedAt: DateTime.UtcNow.Date.AddDays(-2)
        );

        var (current, longest) = await _service.GetStreakAsync(_userId);

        current.Should().Be(0);
        longest.Should().Be(1);
    }

    // ── Consecutive days ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetStreak_3ConsecutiveDaysEndingToday_ReturnsBothThree()
    {
        await DbSeeder.AddConsecutiveDaysActivityAsync(
            _ctx,
            _userId,
            _fixture.ExerciseIds,
            days: 3,
            startDaysAgo: 0
        );

        var (current, longest) = await _service.GetStreakAsync(_userId);

        current.Should().Be(3);
        longest.Should().Be(3);
    }

    [Fact]
    public async Task GetStreak_3ConsecutiveDaysEndingYesterday_ReturnsBothThree()
    {
        // Grace period extends to the whole consecutive run ending yesterday
        await DbSeeder.AddConsecutiveDaysActivityAsync(
            _ctx,
            _userId,
            _fixture.ExerciseIds,
            days: 3,
            startDaysAgo: 1
        );

        var (current, longest) = await _service.GetStreakAsync(_userId);

        current.Should().Be(3);
        longest.Should().Be(3);
    }

    // ── Gaps and broken streaks ───────────────────────────────────────────────

    [Fact]
    public async Task GetStreak_GapInMiddle_ReturnsCorrectCurrentAndLongest()
    {
        // Yesterday (current streak = 1), then gap, then 5+6 days ago (longest = 2)
        await DbSeeder.AddProgressAsync(
            _ctx,
            _userId,
            _fixture.ExerciseIds[0],
            isCompleted: true,
            pointsEarned: 10,
            completedAt: DateTime.UtcNow.Date.AddDays(-1)
        );
        await DbSeeder.AddProgressAsync(
            _ctx,
            _userId,
            _fixture.ExerciseIds[1],
            isCompleted: true,
            pointsEarned: 10,
            completedAt: DateTime.UtcNow.Date.AddDays(-5)
        );
        await DbSeeder.AddProgressAsync(
            _ctx,
            _userId,
            _fixture.ExerciseIds[2],
            isCompleted: true,
            pointsEarned: 10,
            completedAt: DateTime.UtcNow.Date.AddDays(-6)
        );

        var (current, longest) = await _service.GetStreakAsync(_userId);

        current.Should().Be(1);
        longest.Should().Be(2);
    }

    [Fact]
    public async Task GetStreak_MultipleCompletionsSameDay_CountsAsOneDay()
    {
        // Two progress rows on the same UTC date — distinct date collapsing in query
        await DbSeeder.AddProgressAsync(
            _ctx,
            _userId,
            _fixture.ExerciseIds[0],
            isCompleted: true,
            pointsEarned: 10,
            completedAt: DateTime.UtcNow.Date.AddHours(9)
        );
        await DbSeeder.AddProgressAsync(
            _ctx,
            _userId,
            _fixture.ExerciseIds[1],
            isCompleted: true,
            pointsEarned: 10,
            completedAt: DateTime.UtcNow.Date.AddHours(20)
        );

        var (current, longest) = await _service.GetStreakAsync(_userId);

        current.Should().Be(1);
        longest.Should().Be(1);
    }

    [Fact]
    public async Task GetStreak_LongestStreakInPastWithBrokenCurrentStreak_ReturnsBothCorrectly()
    {
        // Yesterday = 1-day current streak
        // 10–13 days ago = 4-day past streak (longest)
        await DbSeeder.AddProgressAsync(
            _ctx,
            _userId,
            _fixture.ExerciseIds[0],
            isCompleted: true,
            pointsEarned: 10,
            completedAt: DateTime.UtcNow.Date.AddDays(-1)
        );

        await DbSeeder.AddConsecutiveDaysActivityAsync(
            _ctx,
            _userId,
            _fixture.ExerciseIds.Skip(1).ToList().AsReadOnly(),
            days: 4,
            startDaysAgo: 10
        );

        var (current, longest) = await _service.GetStreakAsync(_userId);

        current.Should().Be(1);
        longest.Should().Be(4);
    }

    // ── Isolation ────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetStreak_ForDifferentUsers_DoesNotCrossContaminate()
    {
        var otherUser = new UserBuilder()
            .WithUserName("otherstreakuser")
            .WithEmail("other@test.com")
            .Build();
        await DbSeeder.AddUserAsync(_ctx, otherUser);

        // Only the other user has activity
        await DbSeeder.AddConsecutiveDaysActivityAsync(
            _ctx,
            otherUser.Id,
            _fixture.ExerciseIds,
            days: 5,
            startDaysAgo: 0
        );

        var (current, longest) = await _service.GetStreakAsync(_userId);

        current.Should().Be(0);
        longest.Should().Be(0);
    }

    // ── Helper ───────────────────────────────────────────────────────────────

    private static AvatarService CreateAvatarService(Backend.Database.BackendDbContext ctx)
    {
        var factory = new ServiceCollection()
            .AddHttpClient()
            .BuildServiceProvider()
            .GetRequiredService<System.Net.Http.IHttpClientFactory>();

        return new AvatarService(ctx, factory, NullLogger<AvatarService>.Instance);
    }
}
