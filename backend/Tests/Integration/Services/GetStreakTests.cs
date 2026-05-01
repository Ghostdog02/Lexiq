using Backend.Api.Services;
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
    private Database.BackendDbContext _ctx = null!;
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

        _service = new LeaderboardService(_ctx, CreateAvatarService(_ctx));
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

    private static AvatarService CreateAvatarService(Database.BackendDbContext ctx)
    {
        var factory = new ServiceCollection()
            .AddHttpClient()
            .BuildServiceProvider()
            .GetRequiredService<IHttpClientFactory>();

        return new AvatarService(ctx, factory, NullLogger<AvatarService>.Instance);
    }
}
