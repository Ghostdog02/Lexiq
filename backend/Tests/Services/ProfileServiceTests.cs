using Backend.Api.Services;
using Backend.Database.Entities.Users;
using Backend.Tests.Builders;
using Backend.Tests.Helpers;
using Backend.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Backend.Tests.Services;

/// <summary>
/// Integration tests for ProfileService against a real SQL Server instance.
///
/// ProfileService aggregates data from multiple sources:
/// - User table (ID, UserName, RegistrationDate, TotalPointsEarned)
/// - StreakService (CurrentStreak, LongestStreak)
/// - LeaderboardService (Level calculation)
/// - AvatarService (HasAvatar check, avatar URL construction)
/// - AchievementService (User achievements with unlock status)
///
/// Tests verify:
/// - Multi-service aggregation into a single UserProfileDto
/// - Null user handling (returns null)
/// - Avatar URL construction when avatar exists
/// - Null avatar URL when no avatar exists
/// - Achievement integration
/// - Streak integration
/// - Level calculation
/// </summary>
public class ProfileServiceTests(DatabaseFixture fixture)
    : IClassFixture<DatabaseFixture>,
        IAsyncLifetime
{
    private readonly DatabaseFixture _fixture = fixture;
    private Database.BackendDbContext _ctx = null!;
    private ProfileService _service = null!;
    private string _userId = null!;
    private List<string> _exerciseIds = null!;

    public async ValueTask InitializeAsync()
    {
        _ctx = _fixture.CreateDbContext();
        await DbSeeder.ClearLeaderboardDataAsync(_ctx, _fixture.SystemUserId);

        // Clear achievements from previous tests
        await _ctx.UserAchievements.ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await _ctx.Achievements.ExecuteDeleteAsync(TestContext.Current.CancellationToken);

        var user = new UserBuilder()
            .WithUserName("profileuser")
            .WithEmail("profile@test.com")
            .WithTotalPoints(1500)
            .Build();
        await DbSeeder.AddUserAsync(_ctx, user);
        _userId = user.Id;

        // Create exercises for streak/achievement tests
        _exerciseIds = new List<string>();
        for (var i = 0; i < 10; i++)
        {
            var id = await DbSeeder.CreateFillInBlankExerciseAsync(
                _ctx,
                _fixture.LessonId,
                orderIndex: i,
                isLocked: false
            );
            _exerciseIds.Add(id);
        }

        // Seed achievements with known thresholds
        var achievements = new List<Achievement>
        {
            new()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "First Steps",
                Description = "Earned 100 XP",
                XpRequired = 100,
                Icon = "🌱",
                OrderIndex = 0,
            },
            new()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Getting Started",
                Description = "Reached 500 XP",
                XpRequired = 500,
                Icon = "🚀",
                OrderIndex = 1,
            },
            new()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Dedicated Learner",
                Description = "Accumulated 1,000 XP",
                XpRequired = 1000,
                Icon = "📚",
                OrderIndex = 2,
            },
            new()
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Rising Star",
                Description = "Reached 2,500 XP",
                XpRequired = 2500,
                Icon = "⭐",
                OrderIndex = 3,
            },
        };

        await _ctx.Achievements.AddRangeAsync(achievements);
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Wire up services
        var avatarService = CreateAvatarService(_ctx);
        var streakService = new StreakService(_ctx);
        var achievementService = new AchievementService(_ctx);

        _service = new ProfileService(_ctx, streakService, achievementService, avatarService);
    }

    public async ValueTask DisposeAsync()
    {
        await _ctx.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetUserProfile_NonExistentUser_ReturnsNull()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid().ToString();

        // Act
        var profile = await _service.GetUserProfileAsync(nonExistentUserId);

        // Assert
        profile.Should().BeNull(because: "user does not exist in the database");
    }

    [Fact]
    public async Task GetUserProfile_ExistingUserWithNoActivity_ReturnsBasicProfile()
    {
        // Act
        var profile = await _service.GetUserProfileAsync(_userId);

        // Assert
        profile.Should().NotBeNull();
        profile!.UserId.Should().Be(_userId);
        profile.UserName.Should().Be("profileuser");
        profile.TotalXp.Should().Be(1500, because: "user was seeded with 1500 XP");
        profile.CurrentStreak.Should().Be(0, because: "no exercise progress exists");
        profile.LongestStreak.Should().Be(0, because: "no exercise progress exists");
        profile.Level.Should().Be(LeaderboardService.CalculateLevel(1500), because: "level is calculated from total XP");
        profile.AvatarUrl.Should().BeNull(because: "no avatar exists for this user");
        profile.Achievements.Should().NotBeEmpty(because: "achievement definitions are always returned");
    }

    [Fact]
    public async Task GetUserProfile_WithStreak_ReturnsStreakData()
    {
        // Arrange — add 3 consecutive days of activity ending yesterday
        await DbSeeder.AddConsecutiveDaysActivityAsync(
            _ctx,
            _userId,
            _exerciseIds,
            days: 3,
            startDaysAgo: 1
        );

        // Act
        var profile = await _service.GetUserProfileAsync(_userId);

        // Assert
        profile.Should().NotBeNull();
        profile!.CurrentStreak
            .Should()
            .Be(3, because: "grace period extends streak to consecutive 3-day run ending yesterday");
        profile.LongestStreak
            .Should()
            .Be(3, because: "longest streak is the only streak (3 days)");
    }

    [Fact]
    public async Task GetUserProfile_WithLongestStreakInPast_ReturnsCorrectStreaks()
    {
        // Arrange
        // Yesterday = 1-day current streak
        await DbSeeder.AddProgressAsync(
            _ctx,
            _userId,
            _exerciseIds[0],
            isCompleted: true,
            pointsEarned: 10,
            completedAt: DateTime.UtcNow.Date.AddDays(-1)
        );

        // 10-14 days ago = 5-day past streak (longest)
        await DbSeeder.AddConsecutiveDaysActivityAsync(
            _ctx,
            _userId,
            _exerciseIds.Skip(1).Take(5).ToList().AsReadOnly(),
            days: 5,
            startDaysAgo: 10
        );

        // Act
        var profile = await _service.GetUserProfileAsync(_userId);

        // Assert
        profile.Should().NotBeNull();
        profile!.CurrentStreak
            .Should()
            .Be(1, because: "yesterday's activity counts as 1-day current streak (grace period)");
        profile.LongestStreak
            .Should()
            .Be(5, because: "longest streak is the 5-day run from 10-14 days ago");
    }

    [Fact]
    public async Task GetUserProfile_WithAvatar_ReturnsAvatarUrl()
    {
        // Arrange — add avatar for user
        await DbSeeder.AddAvatarAsync(_ctx, _userId);

        // Act
        var profile = await _service.GetUserProfileAsync(_userId);

        // Assert
        profile.Should().NotBeNull();
        profile!
            .AvatarUrl.Should()
            .Be(
                $"/api/user/{_userId}/avatar",
                because: "avatar URL is constructed from user ID when avatar exists"
            );
    }

    [Fact]
    public async Task GetUserProfile_WithoutAvatar_ReturnsNullAvatarUrl()
    {
        // Act
        var profile = await _service.GetUserProfileAsync(_userId);

        // Assert
        profile.Should().NotBeNull();
        profile!.AvatarUrl.Should().BeNull(because: "no avatar exists for this user");
    }

    [Fact]
    public async Task GetUserProfile_WithAchievements_ReturnsUnlockedAchievements()
    {
        // Arrange — trigger achievement unlocking via CheckAndUnlockAchievementsAsync
        var achievementService = new AchievementService(_ctx);
        await achievementService.CheckAndUnlockAchievementsAsync(_userId, 1500);

        // Act
        var profile = await _service.GetUserProfileAsync(_userId);

        // Assert
        profile.Should().NotBeNull();
        profile!.Achievements.Should().HaveCount(4, because: "returns all achievement definitions");

        var unlockedAchievements = profile.Achievements.Where(a => a.IsUnlocked).ToList();
        unlockedAchievements
            .Should()
            .HaveCount(
                3,
                because: "1500 XP unlocks achievements at 100, 500, and 1000 XP thresholds"
            );

        var firstSteps = profile.Achievements.First(a => a.XpRequired == 100);
        firstSteps.IsUnlocked.Should().BeTrue();
        firstSteps.UnlockedAt.Should().NotBeNull();

        var gettingStarted = profile.Achievements.First(a => a.XpRequired == 500);
        gettingStarted.IsUnlocked.Should().BeTrue();
        gettingStarted.UnlockedAt.Should().NotBeNull();

        var dedicated = profile.Achievements.First(a => a.XpRequired == 1000);
        dedicated.IsUnlocked.Should().BeTrue();
        dedicated.UnlockedAt.Should().NotBeNull();

        var risingStar = profile.Achievements.First(a => a.XpRequired == 2500);
        risingStar.IsUnlocked.Should().BeFalse(because: "user only has 1500 XP, not 2500 XP");
        risingStar.UnlockedAt.Should().BeNull();
    }

    [Fact]
    public async Task GetUserProfile_LevelCalculation_MatchesLeaderboardServiceFormula()
    {
        // Act
        var profile = await _service.GetUserProfileAsync(_userId);

        // Assert
        profile.Should().NotBeNull();
        profile!
            .Level.Should()
            .Be(
                LeaderboardService.CalculateLevel(1500),
                because: "ProfileService delegates level calculation to LeaderboardService.CalculateLevel"
            );
    }

    [Fact]
    public async Task GetUserProfile_JoinDate_ReturnsRegistrationDate()
    {
        // Arrange — retrieve user's registration date
        var user = await _ctx
            .Users.Where(u => u.Id == _userId)
            .FirstAsync(TestContext.Current.CancellationToken);
        var expectedJoinDate = user.RegistrationDate;

        // Act
        var profile = await _service.GetUserProfileAsync(_userId);

        // Assert
        profile.Should().NotBeNull();
        profile!
            .JoinDate.Should()
            .BeCloseTo(
                expectedJoinDate,
                precision: TimeSpan.FromSeconds(1),
                because: "JoinDate maps to User.RegistrationDate"
            );
    }

    [Fact]
    public async Task GetUserProfile_CompleteAggregation_CombinesAllServices()
    {
        // Arrange — set up complete scenario with all data
        await DbSeeder.AddAvatarAsync(_ctx, _userId);
        await DbSeeder.AddConsecutiveDaysActivityAsync(
            _ctx,
            _userId,
            _exerciseIds,
            days: 7,
            startDaysAgo: 0
        );

        var achievementService = new AchievementService(_ctx);
        await achievementService.CheckAndUnlockAchievementsAsync(_userId, 1500);

        // Act
        var profile = await _service.GetUserProfileAsync(_userId);

        // Assert — verify all fields are populated correctly
        profile.Should().NotBeNull();
        profile!.UserId.Should().Be(_userId);
        profile.UserName.Should().Be("profileuser");
        profile.TotalXp.Should().Be(1500);
        profile.Level.Should().Be(LeaderboardService.CalculateLevel(1500));
        profile.CurrentStreak.Should().Be(7, because: "7 consecutive days ending today");
        profile.LongestStreak.Should().Be(7, because: "only one streak exists (7 days)");
        profile.AvatarUrl.Should().Be($"/api/user/{_userId}/avatar");
        profile.Achievements.Should().HaveCount(4);
        profile.Achievements.Count(a => a.IsUnlocked).Should().Be(3, because: "1500 XP unlocks 3 achievements");
    }

    [Fact]
    public async Task GetUserProfile_UserWithNullUserName_FallsBackCorrectly()
    {
        // Arrange — create user with no UserName (null)
        var nullUsernameUser = new UserBuilder()
            .WithNullUserName()
            .WithEmail("nousername@test.com")
            .WithTotalPoints(200)
            .Build();
        await DbSeeder.AddUserAsync(_ctx, nullUsernameUser);

        // Act
        var profile = await _service.GetUserProfileAsync(nullUsernameUser.Id);

        // Assert
        profile.Should().NotBeNull();
        profile!
            .UserName.Should()
            .Be("Unknown", because: "UserName ?? \"Unknown\" fallback when UserName is null");
    }

    [Fact]
    public async Task GetUserProfile_MultipleUsers_ReturnsIsolatedProfileData()
    {
        // Arrange — create second user with different data
        var otherUser = new UserBuilder()
            .WithUserName("otheruser")
            .WithEmail("other@test.com")
            .WithTotalPoints(5000)
            .Build();
        await DbSeeder.AddUserAsync(_ctx, otherUser);

        await DbSeeder.AddAvatarAsync(_ctx, otherUser.Id);
        await DbSeeder.AddConsecutiveDaysActivityAsync(
            _ctx,
            otherUser.Id,
            _exerciseIds,
            days: 10,
            startDaysAgo: 0
        );

        var achievementService = new AchievementService(_ctx);
        await achievementService.CheckAndUnlockAchievementsAsync(otherUser.Id, 5000);

        // Act — fetch both profiles
        var profile1 = await _service.GetUserProfileAsync(_userId);
        var profile2 = await _service.GetUserProfileAsync(otherUser.Id);

        // Assert — verify data does not cross-contaminate
        profile1.Should().NotBeNull();
        profile1!.UserId.Should().Be(_userId);
        profile1.UserName.Should().Be("profileuser");
        profile1.TotalXp.Should().Be(1500);
        profile1.CurrentStreak.Should().Be(0, because: "first user has no activity");
        profile1.AvatarUrl.Should().BeNull(because: "first user has no avatar");

        profile2.Should().NotBeNull();
        profile2!.UserId.Should().Be(otherUser.Id);
        profile2.UserName.Should().Be("otheruser");
        profile2.TotalXp.Should().Be(5000);
        profile2.CurrentStreak.Should().Be(10, because: "second user has 10-day streak");
        profile2.AvatarUrl.Should().Be($"/api/user/{otherUser.Id}/avatar");
    }

    [Fact]
    public async Task GetUserProfile_AchievementsOrderedByOrderIndex_ReturnsInCorrectSequence()
    {
        // Act
        var profile = await _service.GetUserProfileAsync(_userId);

        // Assert
        profile.Should().NotBeNull();
        profile!.Achievements.Should().BeInAscendingOrder(a => a.XpRequired);
        profile.Achievements[0].Name.Should().Be("First Steps");
        profile.Achievements[1].Name.Should().Be("Getting Started");
        profile.Achievements[2].Name.Should().Be("Dedicated Learner");
        profile.Achievements[3].Name.Should().Be("Rising Star");
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
