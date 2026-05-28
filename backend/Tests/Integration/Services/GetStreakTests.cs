using Backend.Api.Services;
using Backend.Database;
using Backend.Tests.Builders;
using Backend.Tests.Helpers;
using Backend.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Backend.Tests.Integration.Services;

/// <summary>
/// Integration tests for LeaderboardService.GetStreakAsync against a real SQL Server instance.
///
/// Streak rules:
/// - Counts consecutive UTC calendar days with at least one completed exercise.
/// - Grace period: if no activity today, yesterday still counts as the current streak.
/// - "Current streak" resets to 0 if the most recent activity is 2+ days ago.
/// </summary>
public class GetStreakTests(DatabaseFixture fixture)
    : IClassFixture<DatabaseFixture>,
        IAsyncLifetime
{
    private readonly DatabaseFixture _fixture = fixture;
    private BackendDbContext _ctx = null!;
    private LeaderboardService _service = null!;
    private string _userId = null!;
    private List<string> _exerciseIds = null!;

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

        // Create 30 generic exercises for streak tests (enough for any consecutive days test)
        _exerciseIds = new List<string>();
        for (var i = 0; i < 30; i++)
        {
            var id = await DbSeeder.CreateFillInBlankExerciseAsync(
                _ctx,
                _fixture.LessonId,
                orderIndex: i,
                isLocked: false
            );
            _exerciseIds.Add(id);
        }

        _service = new LeaderboardService(_ctx, CreateAvatarService(_ctx), new Backend.Api.Services.Clock.SystemClock());
    }

    public async ValueTask DisposeAsync()
    {
        await _ctx.DisposeAsync();
        GC.SuppressFinalize(this);
    }

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
        // Arrange - create 1 exercise
        var exerciseId = await DbSeeder.CreateFillInBlankExerciseAsync(
            _ctx,
            _fixture.LessonId,
            orderIndex: 0,
            isLocked: false
        );

        // IsCompleted = false rows are ignored by the streak query
        await DbSeeder.AddProgressAsync(
            _ctx,
            _userId,
            exerciseId,
            isCompleted: false,
            pointsEarned: 0,
            completedAt: DateTime.UtcNow
        );

        var (current, longest) = await _service.GetStreakAsync(_userId);

        current.Should().Be(0);
        longest.Should().Be(0);
    }

    [Fact]
    public async Task GetStreak_SingleCompletionToday_ReturnsBothOne()
    {
        // Arrange - create 1 exercise
        var exerciseId = await DbSeeder.CreateFillInBlankExerciseAsync(
            _ctx,
            _fixture.LessonId,
            orderIndex: 0,
            isLocked: false
        );

        await DbSeeder.AddProgressAsync(
            _ctx,
            _userId,
            exerciseId,
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
            _exerciseIds[0],
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
            _exerciseIds[0],
            isCompleted: true,
            pointsEarned: 10,
            completedAt: DateTime.UtcNow.Date.AddDays(-2)
        );

        var (current, longest) = await _service.GetStreakAsync(_userId);

        current.Should().Be(0);
        longest.Should().Be(1);
    }

    [Fact]
    public async Task GetStreak_3ConsecutiveDaysEndingToday_ReturnsBothThree()
    {
        await DbSeeder.AddConsecutiveDaysActivityAsync(
            _ctx,
            _userId,
            _exerciseIds,
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
            _exerciseIds,
            days: 3,
            startDaysAgo: 1
        );

        var (current, longest) = await _service.GetStreakAsync(_userId);

        current.Should().Be(3);
        longest.Should().Be(3);
    }

    [Fact]
    public async Task GetStreak_GapInMiddle_ReturnsCorrectCurrentAndLongest()
    {
        // Yesterday (current streak = 1), then gap, then 5+6 days ago (longest = 2)
        await DbSeeder.AddProgressAsync(
            _ctx,
            _userId,
            _exerciseIds[0],
            isCompleted: true,
            pointsEarned: 10,
            completedAt: DateTime.UtcNow.Date.AddDays(-1)
        );
        await DbSeeder.AddProgressAsync(
            _ctx,
            _userId,
            _exerciseIds[1],
            isCompleted: true,
            pointsEarned: 10,
            completedAt: DateTime.UtcNow.Date.AddDays(-5)
        );
        await DbSeeder.AddProgressAsync(
            _ctx,
            _userId,
            _exerciseIds[2],
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
            _exerciseIds[0],
            isCompleted: true,
            pointsEarned: 10,
            completedAt: DateTime.UtcNow.Date.AddHours(9)
        );
        await DbSeeder.AddProgressAsync(
            _ctx,
            _userId,
            _exerciseIds[1],
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
            _exerciseIds[0],
            isCompleted: true,
            pointsEarned: 10,
            completedAt: DateTime.UtcNow.Date.AddDays(-1)
        );

        await DbSeeder.AddConsecutiveDaysActivityAsync(
            _ctx,
            _userId,
            _exerciseIds.Skip(1).ToList().AsReadOnly(),
            days: 4,
            startDaysAgo: 10
        );

        var (current, longest) = await _service.GetStreakAsync(_userId);

        current.Should().Be(1);
        longest.Should().Be(4);
    }

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
            _exerciseIds,
            days: 5,
            startDaysAgo: 0
        );

        var (current, longest) = await _service.GetStreakAsync(_userId);

        current.Should().Be(0);
        longest.Should().Be(0);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Edge case tests (FakeClock to control "today")
    // ──────────────────────────────────────────────────────────────────────

    private LeaderboardService BuildServiceWithClock(FakeClock clock) =>
        new(_ctx, CreateAvatarService(_ctx), clock);

    [Fact]
    public async Task SameDayMultipleSolves_CurrentStreakIs1_NotHigher()
    {
        // Arrange — two completed rows on the same UTC date
        var clock = new FakeClock { UtcNow = new DateTime(2026, 6, 15, 12, 0, 0, DateTimeKind.Utc) };
        var service = BuildServiceWithClock(clock);

        await DbSeeder.AddProgressAsync(_ctx, _userId, _exerciseIds[0], true, 10, clock.UtcNow.AddHours(-3));
        await DbSeeder.AddProgressAsync(_ctx, _userId, _exerciseIds[1], true, 10, clock.UtcNow);

        // Act
        var (current, _) = await service.GetStreakAsync(_userId);

        // Assert
        current.Should().Be(1, because: "two completions on the same calendar day count as a single streak day");
    }

    [Fact]
    public async Task FullSkippedDay_CurrentStreakIsZero_LongestPreserved()
    {
        // Arrange — activity on days -5, -6, -7 (3-day run), nothing yesterday or today
        var clock = new FakeClock { UtcNow = new DateTime(2026, 6, 20, 10, 0, 0, DateTimeKind.Utc) };
        var service = BuildServiceWithClock(clock);

        for (var i = 0; i < 3; i++)
        {
            await DbSeeder.AddProgressAsync(
                _ctx, _userId, _exerciseIds[i], true, 10,
                clock.UtcNow.AddDays(-(5 + i))
            );
        }

        // Act
        var (current, longest) = await service.GetStreakAsync(_userId);

        // Assert
        current.Should().Be(0, because: "last activity was 5+ days ago — no current streak");
        longest.Should().Be(3, because: "longest run is the 3-day block ending on day -5");
    }

    [Fact]
    public async Task MonthBoundary_Jan31ToFeb1_CurrentStreak2()
    {
        // Arrange
        var clock = new FakeClock { UtcNow = new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc) };
        var service = BuildServiceWithClock(clock);

        await DbSeeder.AddProgressAsync(_ctx, _userId, _exerciseIds[0], true, 10,
            new DateTime(2026, 1, 31, 12, 0, 0, DateTimeKind.Utc));
        await DbSeeder.AddProgressAsync(_ctx, _userId, _exerciseIds[1], true, 10,
            new DateTime(2026, 2, 1, 12, 0, 0, DateTimeKind.Utc));

        // Act
        var (current, longest) = await service.GetStreakAsync(_userId);

        // Assert
        current.Should().Be(2, because: "Jan 31 and Feb 1 are consecutive UTC days");
    }

    [Fact]
    public async Task YearBoundary_Dec31ToJan1_CurrentStreak2()
    {
        // Arrange
        var clock = new FakeClock { UtcNow = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc) };
        var service = BuildServiceWithClock(clock);

        await DbSeeder.AddProgressAsync(_ctx, _userId, _exerciseIds[0], true, 10,
            new DateTime(2025, 12, 31, 12, 0, 0, DateTimeKind.Utc));
        await DbSeeder.AddProgressAsync(_ctx, _userId, _exerciseIds[1], true, 10,
            new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc));

        // Act
        var (current, longest) = await service.GetStreakAsync(_userId);

        // Assert
        current.Should().Be(2, because: "Dec 31 and Jan 1 are consecutive UTC calendar days");
    }

    [Fact]
    public async Task DstAffectedDates_UtcBased_StreakUnaffected()
    {
        // Arrange — EU DST transitions: last Sunday of March (2026-03-29 02:00 local → 03:00)
        // UTC timestamps span the DST boundary; UTC-date logic must be unaffected
        var clock = new FakeClock { UtcNow = new DateTime(2026, 3, 30, 0, 0, 0, DateTimeKind.Utc) };
        var service = BuildServiceWithClock(clock);

        await DbSeeder.AddProgressAsync(_ctx, _userId, _exerciseIds[0], true, 10,
            new DateTime(2026, 3, 29, 1, 30, 0, DateTimeKind.Utc)); // before DST switch
        await DbSeeder.AddProgressAsync(_ctx, _userId, _exerciseIds[1], true, 10,
            new DateTime(2026, 3, 29, 23, 0, 0, DateTimeKind.Utc)); // after DST switch, same UTC day
        await DbSeeder.AddProgressAsync(_ctx, _userId, _exerciseIds[2], true, 10,
            new DateTime(2026, 3, 30, 0, 0, 0, DateTimeKind.Utc));

        // Act
        var (current, longest) = await service.GetStreakAsync(_userId);

        // Assert
        current.Should().Be(2, because: "UTC-date grouping is immune to DST — both UTC-day 29 and 30 have activity");
    }

    [Fact]
    public async Task FiveDayRun_ThreeDayGap_OneDay_CurrentIs1_LongestIs5()
    {
        // Arrange
        var clock = new FakeClock { UtcNow = new DateTime(2026, 6, 25, 10, 0, 0, DateTimeKind.Utc) };
        var service = BuildServiceWithClock(clock);

        // 5-day run ending 10 days ago
        for (var i = 0; i < 5; i++)
        {
            await DbSeeder.AddProgressAsync(
                _ctx, _userId, _exerciseIds[i], true, 10,
                clock.UtcNow.AddDays(-(10 + i))
            );
        }

        // 3-day gap, then 1 day yesterday
        await DbSeeder.AddProgressAsync(_ctx, _userId, _exerciseIds[5], true, 10, clock.UtcNow.AddDays(-1));

        // Act
        var (current, longest) = await service.GetStreakAsync(_userId);

        // Assert
        current.Should().Be(1, because: "only yesterday has activity — that's the current streak");
        longest.Should().Be(5, because: "the 5-day block is the longest run");
    }

    [Fact]
    public async Task MultipleShortStreaksWithGaps_LongestEqualsMaxRunLength()
    {
        // Arrange — runs of 2, 3, 1 days with gaps between them
        var clock = new FakeClock { UtcNow = new DateTime(2026, 6, 30, 10, 0, 0, DateTimeKind.Utc) };
        var service = BuildServiceWithClock(clock);

        // Run of 2 (days -20, -19)
        await DbSeeder.AddProgressAsync(_ctx, _userId, _exerciseIds[0], true, 10, clock.UtcNow.AddDays(-20));
        await DbSeeder.AddProgressAsync(_ctx, _userId, _exerciseIds[1], true, 10, clock.UtcNow.AddDays(-19));

        // Gap day -18

        // Run of 3 (days -17, -16, -15)
        await DbSeeder.AddProgressAsync(_ctx, _userId, _exerciseIds[2], true, 10, clock.UtcNow.AddDays(-17));
        await DbSeeder.AddProgressAsync(_ctx, _userId, _exerciseIds[3], true, 10, clock.UtcNow.AddDays(-16));
        await DbSeeder.AddProgressAsync(_ctx, _userId, _exerciseIds[4], true, 10, clock.UtcNow.AddDays(-15));

        // Gap days -14 through -3

        // Run of 1 (day -2)
        await DbSeeder.AddProgressAsync(_ctx, _userId, _exerciseIds[5], true, 10, clock.UtcNow.AddDays(-2));

        // Act
        var (current, longest) = await service.GetStreakAsync(_userId);

        // Assert
        current.Should().Be(0, because: "last activity was 2 days ago — outside the grace period");
        longest.Should().Be(3, because: "the 3-day run is the maximum");
    }

    private static AvatarService CreateAvatarService(BackendDbContext ctx)
    {
        var factory = new ServiceCollection()
            .AddHttpClient()
            .BuildServiceProvider()
            .GetRequiredService<IHttpClientFactory>();

        return new AvatarService(ctx, factory, NullLogger<AvatarService>.Instance);
    }
}
