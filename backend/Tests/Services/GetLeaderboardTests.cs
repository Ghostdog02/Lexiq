using Backend.Api.Dtos;
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
/// Integration tests for LeaderboardService.GetLeaderboardAsync against a real SQL Server instance.
///
/// Note on the system user: DatabaseFixture seeds a "_system_" user (0 XP) that persists
/// across all tests to own the content hierarchy. AllTime tests account for its presence
/// by looking up entries by specific user IDs rather than by position.
/// Weekly/Monthly tests are unaffected since the system user has no progress rows.
/// </summary>
public class GetLeaderboardTests(DatabaseFixture fixture)
    : IClassFixture<DatabaseFixture>,
        IAsyncLifetime
{
    private readonly DatabaseFixture _fixture = fixture;
    private Database.BackendDbContext _ctx = null!;
    private LeaderboardService _service = null!;

    public async ValueTask InitializeAsync()
    {
        _ctx = _fixture.CreateDbContext();
        await DbSeeder.ClearLeaderboardDataAsync(_ctx, _fixture.SystemUserId);
        _service = new LeaderboardService(_ctx, CreateAvatarService(_ctx));
    }

    public async ValueTask DisposeAsync()
    {
        await _ctx.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetLeaderboard_AllTime_OrdersByTotalPointsEarnedDescending()
    {
        var alice = Build("alice", "alice@t.com", 1000);
        var bob = Build("bob", "bob@t.com", 500);
        var charlie = Build("charlie", "charlie@t.com", 100);
        await DbSeeder.AddUsersAsync(_ctx, [alice, bob, charlie]);

        var response = await _service.GetLeaderboardAsync(TimeFrame.AllTime, null);

        var aliceRank = response.Entries.First(e => e.UserId == alice.Id).Rank;
        var bobRank = response.Entries.First(e => e.UserId == bob.Id).Rank;
        var charlieRank = response.Entries.First(e => e.UserId == charlie.Id).Rank;

        aliceRank.Should().BeLessThan(bobRank);
        bobRank.Should().BeLessThan(charlieRank);
    }

    [Fact]
    public async Task GetLeaderboard_AllTime_UsesCachedTotalPointsEarned_NotProgressSum()
    {
        // AllTime reads User.TotalPointsEarned, not the sum of UserExerciseProgress rows.
        // This verifies the caching contract: an old progress row (200 pts) doesn't
        // override the materialised aggregate (1000 pts) stored on the User entity.
        var user = Build("cached", "cached@t.com", totalPoints: 1000);
        await DbSeeder.AddUserAsync(_ctx, user);
        await DbSeeder.AddProgressAsync(
            _ctx,
            user.Id,
            _fixture.ExerciseIds[0],
            isCompleted: true,
            pointsEarned: 200,
            completedAt: DateTime.UtcNow.AddDays(-400)
        );

        var response = await _service.GetLeaderboardAsync(TimeFrame.AllTime, null);

        var entry = response.Entries.First(e => e.UserId == user.Id);
        entry.TotalXp.Should().Be(1000);
    }

    [Fact]
    public async Task GetLeaderboard_AllTime_NullCurrentUserId_CurrentUserEntryIsNull()
    {
        await DbSeeder.AddUserAsync(_ctx, Build("user1", "u1@t.com", 100));

        var response = await _service.GetLeaderboardAsync(TimeFrame.AllTime, null);

        response.CurrentUserEntry.Should().BeNull();
    }

    [Fact]
    public async Task GetLeaderboard_AllTime_CurrentUserInTop50_MarkedIsCurrentUser()
    {
        var user = Build("me", "me@t.com", 500);
        await DbSeeder.AddUserAsync(_ctx, user);

        var response = await _service.GetLeaderboardAsync(TimeFrame.AllTime, user.Id);

        var entry = response.Entries.First(e => e.UserId == user.Id);
        entry.IsCurrentUser.Should().BeTrue();
    }

    [Fact]
    public async Task GetLeaderboard_AllTime_CurrentUserInTop50_PresentInEntriesAndCurrentUserEntry()
    {
        var user = Build("me", "me@t.com", 500);
        await DbSeeder.AddUserAsync(_ctx, user);

        var response = await _service.GetLeaderboardAsync(TimeFrame.AllTime, user.Id);

        response.Entries.Should().Contain(e => e.UserId == user.Id);
        response.CurrentUserEntry.Should().NotBeNull();
        response.CurrentUserEntry.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task GetLeaderboard_AllTime_EntriesLimitedTo50()
    {
        // Insert 55 users with positive XP — top 50 capped; system user (0 XP) is excluded
        var users = Enumerable
            .Range(1, 55)
            .Select(i => Build($"user{i}", $"u{i}@t.com", i * 10))
            .ToList();
        await DbSeeder.AddUsersAsync(_ctx, users);

        var response = await _service.GetLeaderboardAsync(TimeFrame.AllTime, null);

        response.Entries.Count.Should().Be(50);
    }

    [Fact]
    public async Task GetLeaderboard_AllTime_CurrentUserOutsideTop50_FetchedSeparatelyWithCorrectRank()
    {
        // 50 users with XP 100–5000, plus a currentUser with XP 50 (rank 51)
        var topUsers = Enumerable
            .Range(1, 50)
            .Select(i => Build($"topuser{i}", $"top{i}@t.com", i * 100))
            .ToList();
        await DbSeeder.AddUsersAsync(_ctx, topUsers);

        var currentUser = Build("outside", "outside@t.com", 50);
        await DbSeeder.AddUserAsync(_ctx, currentUser);

        var response = await _service.GetLeaderboardAsync(TimeFrame.AllTime, currentUser.Id);

        response.Entries.Should().NotContain(e => e.UserId == currentUser.Id);
        response.CurrentUserEntry.Should().NotBeNull();
        response.CurrentUserEntry.Rank.Should().Be(51);
    }

    [Fact]
    public async Task GetLeaderboard_AllTime_LevelCalculatedFromTotalXp()
    {
        // 600 XP = Level 3 threshold (100 * 3 * 2)
        var user = Build("leveluser", "level@t.com", 600);
        await DbSeeder.AddUserAsync(_ctx, user);

        var response = await _service.GetLeaderboardAsync(TimeFrame.AllTime, null);

        var entry = response.Entries.First(e => e.UserId == user.Id);
        entry.Level.Should().Be(3);
    }

    [Fact]
    public async Task GetLeaderboard_AllTime_UserWithNullUserName_FallsBackToEmail()
    {
        var user = new UserBuilder()
            .WithNullUserName()
            .WithEmail("fallback@t.com")
            .WithTotalPoints(100)
            .Build();
        await DbSeeder.AddUserAsync(_ctx, user);

        var response = await _service.GetLeaderboardAsync(TimeFrame.AllTime, null);

        var entry = response.Entries.First(e => e.UserId == user.Id);
        entry.UserName.Should().Be("fallback@t.com");
    }

    [Fact]
    public async Task GetLeaderboard_AllTime_UserWithNullUserNameAndNullEmail_FallsBackToUnknown()
    {
        var user = new UserBuilder()
            .WithNullUserName()
            .WithNullEmail()
            .WithTotalPoints(100)
            .Build();
        await DbSeeder.AddUserAsync(_ctx, user);

        var response = await _service.GetLeaderboardAsync(TimeFrame.AllTime, null);

        var entry = response.Entries.First(e => e.UserId == user.Id);
        entry.UserName.Should().Be("Unknown");
    }

    [Fact]
    public async Task GetLeaderboard_Weekly_WithNoProgress_ReturnsEmptyEntries_NullCurrentUser()
    {
        // No progress rows → nobody qualifies for the weekly window
        var response = await _service.GetLeaderboardAsync(TimeFrame.Weekly, null);

        response.Entries.Should().BeEmpty();
        response.CurrentUserEntry.Should().BeNull();
    }

    [Fact]
    public async Task GetLeaderboard_Weekly_OnlyIncludesLast7DaysProgress()
    {
        var user = Build("weeklyuser", "weekly@t.com", 0);
        await DbSeeder.AddUserAsync(_ctx, user);

        // Old progress (10 days ago) — outside window
        await DbSeeder.AddProgressAsync(
            _ctx,
            user.Id,
            _fixture.ExerciseIds[0],
            isCompleted: true,
            pointsEarned: 500,
            completedAt: DateTime.UtcNow.AddDays(-10)
        );

        // Recent progress (3 days ago) — inside window
        await DbSeeder.AddProgressAsync(
            _ctx,
            user.Id,
            _fixture.ExerciseIds[1],
            isCompleted: true,
            pointsEarned: 100,
            completedAt: DateTime.UtcNow.AddDays(-3)
        );

        var response = await _service.GetLeaderboardAsync(TimeFrame.Weekly, null);

        var entry = response.Entries.First(e => e.UserId == user.Id);
        entry.TotalXp.Should().Be(100); // only the in-window row counts
    }

    [Fact]
    public async Task GetLeaderboard_Weekly_ExcludesUsersWithNoProgressInWindow()
    {
        // User has high cached XP but no recent activity
        var user = Build("inactiveuser", "inactive@t.com", 9999);
        await DbSeeder.AddUserAsync(_ctx, user);
        // No UserExerciseProgress rows in last 7 days

        var response = await _service.GetLeaderboardAsync(TimeFrame.Weekly, null);

        response.Entries.Should().NotContain(e => e.UserId == user.Id);
    }

    [Fact]
    public async Task GetLeaderboard_Weekly_SumsMultipleProgressRowsInWindow()
    {
        var user = Build("sumuser", "sum@t.com", 0);
        await DbSeeder.AddUserAsync(_ctx, user);

        await DbSeeder.AddProgressAsync(
            _ctx,
            user.Id,
            _fixture.ExerciseIds[0],
            isCompleted: true,
            pointsEarned: 100,
            completedAt: DateTime.UtcNow.AddDays(-1)
        );
        await DbSeeder.AddProgressAsync(
            _ctx,
            user.Id,
            _fixture.ExerciseIds[1],
            isCompleted: true,
            pointsEarned: 200,
            completedAt: DateTime.UtcNow.AddDays(-2)
        );
        await DbSeeder.AddProgressAsync(
            _ctx,
            user.Id,
            _fixture.ExerciseIds[2],
            isCompleted: true,
            pointsEarned: 50,
            completedAt: DateTime.UtcNow.AddDays(-3)
        );

        var response = await _service.GetLeaderboardAsync(TimeFrame.Weekly, null);

        var entry = response.Entries.First(e => e.UserId == user.Id);
        entry.TotalXp.Should().Be(350);
    }

    [Fact]
    public async Task GetLeaderboard_Weekly_OrdersByWindowXpDescending()
    {
        var alice = Build("alice_w", "alicew@t.com", 0);
        var bob = Build("bob_w", "bobw@t.com", 0);
        await DbSeeder.AddUsersAsync(_ctx, [alice, bob]);

        await DbSeeder.AddProgressAsync(
            _ctx,
            alice.Id,
            _fixture.ExerciseIds[0],
            isCompleted: true,
            pointsEarned: 300,
            completedAt: DateTime.UtcNow.AddDays(-1)
        );
        await DbSeeder.AddProgressAsync(
            _ctx,
            bob.Id,
            _fixture.ExerciseIds[0],
            isCompleted: true,
            pointsEarned: 100,
            completedAt: DateTime.UtcNow.AddDays(-1)
        );

        var response = await _service.GetLeaderboardAsync(TimeFrame.Weekly, null);

        var aliceRank = response.Entries.First(e => e.UserId == alice.Id).Rank;
        var bobRank = response.Entries.First(e => e.UserId == bob.Id).Rank;

        aliceRank.Should().BeLessThan(bobRank);
    }

    [Fact]
    public async Task GetLeaderboard_Weekly_CurrentUserOutsideTop50_CorrectRankCalculated()
    {
        // 50 users with XP 100–5000 in window; currentUser has 50 XP (rank 51)
        var topUsers = Enumerable
            .Range(1, 50)
            .Select(i => Build($"tw{i}", $"tw{i}@t.com", 0))
            .ToList();
        await DbSeeder.AddUsersAsync(_ctx, topUsers);

        for (var i = 0; i < topUsers.Count; i++)
            await DbSeeder.AddProgressAsync(
                _ctx,
                topUsers[i].Id,
                _fixture.ExerciseIds[0],
                isCompleted: true,
                pointsEarned: (i + 1) * 100,
                completedAt: DateTime.UtcNow.AddDays(-1)
            );

        var currentUser = Build("woutside", "woutside@t.com", 0);
        await DbSeeder.AddUserAsync(_ctx, currentUser);
        await DbSeeder.AddProgressAsync(
            _ctx,
            currentUser.Id,
            _fixture.ExerciseIds[1],
            isCompleted: true,
            pointsEarned: 50,
            completedAt: DateTime.UtcNow.AddDays(-1)
        );

        var response = await _service.GetLeaderboardAsync(TimeFrame.Weekly, currentUser.Id);

        response.Entries.Should().NotContain(e => e.UserId == currentUser.Id);
        response.CurrentUserEntry.Should().NotBeNull();
        response.CurrentUserEntry.Rank.Should().Be(51);
    }

    [Fact]
    public async Task GetLeaderboard_Monthly_OnlyIncludesLast30DaysProgress()
    {
        var user = Build("monthlyuser", "monthly@t.com", 0);
        await DbSeeder.AddUserAsync(_ctx, user);

        await DbSeeder.AddProgressAsync(
            _ctx,
            user.Id,
            _fixture.ExerciseIds[0],
            isCompleted: true,
            pointsEarned: 999,
            completedAt: DateTime.UtcNow.AddDays(-31)
        );

        await DbSeeder.AddProgressAsync(
            _ctx,
            user.Id,
            _fixture.ExerciseIds[1],
            isCompleted: true,
            pointsEarned: 200,
            completedAt: DateTime.UtcNow.AddDays(-15)
        );

        var response = await _service.GetLeaderboardAsync(TimeFrame.Monthly, null);

        var entry = response.Entries.First(e => e.UserId == user.Id);
        entry.TotalXp.Should().Be(200);
    }

    [Fact]
    public async Task GetLeaderboard_Monthly_ExcludesProgressOlderThan30Days()
    {
        var user = Build("olduser", "old@t.com", 5000);
        await DbSeeder.AddUserAsync(_ctx, user);
        await DbSeeder.AddProgressAsync(
            _ctx,
            user.Id,
            _fixture.ExerciseIds[0],
            isCompleted: true,
            pointsEarned: 5000,
            completedAt: DateTime.UtcNow.AddDays(-35)
        );

        var response = await _service.GetLeaderboardAsync(TimeFrame.Monthly, null);

        response.Entries.Should().NotContain(e => e.UserId == user.Id);
    }

    [Fact]
    public async Task GetLeaderboard_Weekly_NewEntryNotInPreviousPeriod_HasRankChangeZero()
    {
        // User only has current-week progress — not in previous period (days 8-14)
        var user = Build("newbie", "newbie@t.com", 0);
        await DbSeeder.AddUserAsync(_ctx, user);
        await DbSeeder.AddProgressAsync(
            _ctx,
            user.Id,
            _fixture.ExerciseIds[0],
            isCompleted: true,
            pointsEarned: 100,
            completedAt: DateTime.UtcNow.AddDays(-3)
        );

        var response = await _service.GetLeaderboardAsync(TimeFrame.Weekly, user.Id);

        var entry = response.Entries.First(e => e.UserId == user.Id);
        entry.Change.Should().Be(0);
    }

    [Fact]
    public async Task GetLeaderboard_Weekly_UserMovedUpFromRank3ToRank1_HasRankChangePlus2()
    {
        // Previous week (days 8-14): alice=100, bob=200, charlie=300 → ranks 3, 2, 1
        // Current week (days 1-7):   alice=300, bob=200, charlie=100 → ranks 1, 2, 3
        var alice = Build("alice_rc", "alicerc@t.com", 0);
        var bob = Build("bob_rc", "bobrc@t.com", 0);
        var charlie = Build("charlie_rc", "charlierc@t.com", 0);
        await DbSeeder.AddUsersAsync(_ctx, [alice, bob, charlie]);

        // Previous period
        await DbSeeder.AddProgressAsync(
            _ctx,
            alice.Id,
            _fixture.ExerciseIds[0],
            true,
            100,
            DateTime.UtcNow.AddDays(-10)
        );
        await DbSeeder.AddProgressAsync(
            _ctx,
            bob.Id,
            _fixture.ExerciseIds[0],
            true,
            200,
            DateTime.UtcNow.AddDays(-10)
        );
        await DbSeeder.AddProgressAsync(
            _ctx,
            charlie.Id,
            _fixture.ExerciseIds[0],
            true,
            300,
            DateTime.UtcNow.AddDays(-10)
        );

        // Current period
        await DbSeeder.AddProgressAsync(
            _ctx,
            alice.Id,
            _fixture.ExerciseIds[1],
            true,
            300,
            DateTime.UtcNow.AddDays(-3)
        );
        await DbSeeder.AddProgressAsync(
            _ctx,
            bob.Id,
            _fixture.ExerciseIds[1],
            true,
            200,
            DateTime.UtcNow.AddDays(-3)
        );
        await DbSeeder.AddProgressAsync(
            _ctx,
            charlie.Id,
            _fixture.ExerciseIds[1],
            true,
            100,
            DateTime.UtcNow.AddDays(-3)
        );

        var response = await _service.GetLeaderboardAsync(TimeFrame.Weekly, null);

        // Alice: previousRank=3, currentRank=1 → change = 3 - 1 = +2
        var aliceEntry = response.Entries.First(e => e.UserId == alice.Id);
        aliceEntry.Change.Should().Be(2);
    }

    [Fact]
    public async Task GetLeaderboard_Weekly_UserMovedDownFromRank1ToRank3_HasRankChangeMinus2()
    {
        // Same setup as above — charlie dropped from rank 1 to rank 3
        var alice = Build("alice_rd", "alicerd@t.com", 0);
        var bob = Build("bob_rd", "bobrd@t.com", 0);
        var charlie = Build("charlie_rd", "charlierd@t.com", 0);
        await DbSeeder.AddUsersAsync(_ctx, [alice, bob, charlie]);

        // Previous period — charlie was rank 1
        await DbSeeder.AddProgressAsync(
            _ctx,
            alice.Id,
            _fixture.ExerciseIds[0],
            true,
            100,
            DateTime.UtcNow.AddDays(-10)
        );
        await DbSeeder.AddProgressAsync(
            _ctx,
            bob.Id,
            _fixture.ExerciseIds[0],
            true,
            200,
            DateTime.UtcNow.AddDays(-10)
        );
        await DbSeeder.AddProgressAsync(
            _ctx,
            charlie.Id,
            _fixture.ExerciseIds[0],
            true,
            300,
            DateTime.UtcNow.AddDays(-10)
        );

        // Current period — charlie is now rank 3
        await DbSeeder.AddProgressAsync(
            _ctx,
            alice.Id,
            _fixture.ExerciseIds[1],
            true,
            300,
            DateTime.UtcNow.AddDays(-3)
        );
        await DbSeeder.AddProgressAsync(
            _ctx,
            bob.Id,
            _fixture.ExerciseIds[1],
            true,
            200,
            DateTime.UtcNow.AddDays(-3)
        );
        await DbSeeder.AddProgressAsync(
            _ctx,
            charlie.Id,
            _fixture.ExerciseIds[1],
            true,
            100,
            DateTime.UtcNow.AddDays(-3)
        );

        var response = await _service.GetLeaderboardAsync(TimeFrame.Weekly, null);

        // Charlie: previousRank=1, currentRank=3 → change = 1 - 3 = -2
        var charlieEntry = response.Entries.First(e => e.UserId == charlie.Id);
        charlieEntry.Change.Should().Be(-2);
    }

    [Fact]
    public async Task GetLeaderboard_UserWithAvatar_HasCorrectAvatarUrl()
    {
        var user = Build("avataruser", "avatar@t.com", 100);
        await DbSeeder.AddUserAsync(_ctx, user);
        await DbSeeder.AddAvatarAsync(_ctx, user.Id);

        var response = await _service.GetLeaderboardAsync(TimeFrame.AllTime, null);

        var entry = response.Entries.First(e => e.UserId == user.Id);
        entry.Avatar.Should().Be($"/api/user/{user.Id}/avatar");
    }

    [Fact]
    public async Task GetLeaderboard_UserWithoutAvatar_HasNullAvatarUrl()
    {
        var user = Build("noavataruser", "noavatar@t.com", 100);
        await DbSeeder.AddUserAsync(_ctx, user);
        // No UserAvatar row inserted

        var response = await _service.GetLeaderboardAsync(TimeFrame.AllTime, null);

        var entry = response.Entries.First(e => e.UserId == user.Id);
        entry.Avatar.Should().BeNull();
    }

    [Fact]
    public async Task GetLeaderboard_StreakIsIncludedInEachEntry()
    {
        var user = Build("streakentry", "streakentry@t.com", 100);
        await DbSeeder.AddUserAsync(_ctx, user);

        // 3-day consecutive streak ending today
        await DbSeeder.AddConsecutiveDaysActivityAsync(
            _ctx,
            user.Id,
            _fixture.ExerciseIds,
            days: 3,
            startDaysAgo: 0,
            pointsPerDay: 50
        );

        var response = await _service.GetLeaderboardAsync(TimeFrame.AllTime, null);

        var entry = response.Entries.First(e => e.UserId == user.Id);
        entry.CurrentStreak.Should().Be(3);
        entry.LongestStreak.Should().Be(3);
    }

    private static Database.Entities.Users.User Build(
        string userName,
        string email,
        int totalPoints
    ) =>
        new UserBuilder()
            .WithUserName(userName)
            .WithEmail(email)
            .WithTotalPoints(totalPoints)
            .Build();

    private static AvatarService CreateAvatarService(Database.BackendDbContext ctx)
    {
        var factory = new ServiceCollection()
            .AddHttpClient()
            .BuildServiceProvider()
            .GetRequiredService<IHttpClientFactory>();

        return new AvatarService(ctx, factory, NullLogger<AvatarService>.Instance);
    }
}
