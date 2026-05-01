using Backend.Api.Services;
using Backend.Database.Entities.Users;
using Backend.Tests.Builders;
using Backend.Tests.Helpers;
using Backend.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend.Tests.Services;

/// <summary>
/// Integration tests for AchievementService against a real SQL Server instance.
///
/// Tests cover:
/// - XP threshold-based achievement unlocking
/// - Idempotency (duplicate triggers don't grant achievements twice)
/// - Multiple achievement unlocking in a single call
/// - Edge cases (XP at threshold boundary, zero XP, no qualifying achievements)
/// - GetUserAchievementsAsync merging of definitions with unlock status
/// </summary>
public class AchievementServiceTests(DatabaseFixture fixture)
    : IClassFixture<DatabaseFixture>,
        IAsyncLifetime
{
    private readonly DatabaseFixture _fixture = fixture;
    private Database.BackendDbContext _ctx = null!;
    private AchievementService _service = null!;
    private string _userId = null!;
    private List<Achievement> _achievements = null!;

    public async ValueTask InitializeAsync()
    {
        _ctx = _fixture.CreateDbContext();
        await DbSeeder.ClearLeaderboardDataAsync(_ctx, _fixture.SystemUserId);

        // Clear achievements from previous tests
        await _ctx.UserAchievements.ExecuteDeleteAsync(TestContext.Current.CancellationToken);
        await _ctx.Achievements.ExecuteDeleteAsync(TestContext.Current.CancellationToken);

        var user = new UserBuilder()
            .WithUserName("achievementuser")
            .WithEmail("achievement@test.com")
            .WithTotalPoints(0)
            .Build();
        await DbSeeder.AddUserAsync(_ctx, user);
        _userId = user.Id;

        // Seed achievements with known XP thresholds for deterministic testing
        _achievements = new List<Achievement>
        {
            new()
            {
                AchievementId = Guid.NewGuid().ToString(),
                AchievementName = "First Steps",
                Description = "Earned 100 XP",
                XpRequired = 100,
                Icon = "🌱",
                OrderIndex = 0,
            },
            new()
            {
                AchievementId = Guid.NewGuid().ToString(),
                AchievementName = "Getting Started",
                Description = "Reached 500 XP",
                XpRequired = 500,
                Icon = "🚀",
                OrderIndex = 1,
            },
            new()
            {
                AchievementId = Guid.NewGuid().ToString(),
                AchievementName = "Dedicated Learner",
                Description = "Accumulated 1,000 XP",
                XpRequired = 1000,
                Icon = "📚",
                OrderIndex = 2,
            },
            new()
            {
                AchievementId = Guid.NewGuid().ToString(),
                AchievementName = "Rising Star",
                Description = "Reached 2,500 XP",
                XpRequired = 2500,
                Icon = "⭐",
                OrderIndex = 3,
            },
        };

        await _ctx.Achievements.AddRangeAsync(_achievements);
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        _service = new AchievementService(_ctx);
    }

    public async ValueTask DisposeAsync()
    {
        await _ctx.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task CheckAndUnlockAchievements_ZeroXp_DoesNotUnlockAny()
    {
        // Arrange
        var totalXp = 0;

        // Act
        await _service.CheckAndUnlockAchievementsAsync(_userId, totalXp);

        // Assert
        var unlocked = await _ctx
            .UserAchievements.Where(ua => ua.UserId == _userId)
            .ToListAsync(TestContext.Current.CancellationToken);

        unlocked.Should().BeEmpty(because: "no achievements qualify at 0 XP");
    }

    [Fact]
    public async Task CheckAndUnlockAchievements_BelowLowestThreshold_DoesNotUnlockAny()
    {
        // Arrange
        var totalXp = 99;

        // Act
        await _service.CheckAndUnlockAchievementsAsync(_userId, totalXp);

        // Assert
        var unlocked = await _ctx
            .UserAchievements.Where(ua => ua.UserId == _userId)
            .ToListAsync(TestContext.Current.CancellationToken);

        unlocked.Should().BeEmpty(because: "99 XP is below the 100 XP threshold");
    }

    [Fact]
    public async Task CheckAndUnlockAchievements_ExactlyAtThreshold_UnlocksThatAchievement()
    {
        // Arrange
        var totalXp = 100;

        // Act
        await _service.CheckAndUnlockAchievementsAsync(_userId, totalXp);

        // Assert
        var unlocked = await _ctx
            .UserAchievements.Where(ua => ua.UserId == _userId)
            .ToListAsync(TestContext.Current.CancellationToken);

        unlocked
            .Should()
            .ContainSingle(because: "100 XP exactly matches the first achievement threshold");
        unlocked[0]
            .AchievementId.Should()
            .Be(_achievements[0].AchievementId, because: "First Steps requires 100 XP");
        unlocked[0]
            .UnlockedAt.Should()
            .BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task CheckAndUnlockAchievements_AboveThreshold_UnlocksAllQualifying()
    {
        // Arrange
        var totalXp = 1200;

        // Act
        await _service.CheckAndUnlockAchievementsAsync(_userId, totalXp);

        // Assert
        var unlocked = await _ctx
            .UserAchievements.Where(ua => ua.UserId == _userId)
            .Select(ua => ua.AchievementId)
            .ToListAsync(TestContext.Current.CancellationToken);

        unlocked
            .Should()
            .HaveCount(3, because: "1200 XP qualifies for achievements at 100, 500, and 1000 XP");
        unlocked
            .Should()
            .Contain(
                _achievements[0].AchievementId,
                because: "First Steps (100 XP) is unlocked when user has 1200 XP"
            );
        unlocked
            .Should()
            .Contain(
                _achievements[1].AchievementId,
                because: "Getting Started (500 XP) is unlocked when user has 1200 XP"
            );
        unlocked
            .Should()
            .Contain(
                _achievements[2].AchievementId,
                because: "Dedicated Learner (1000 XP) is unlocked when user has 1200 XP"
            );
        unlocked
            .Should()
            .NotContain(
                _achievements[3].AchievementId,
                because: "Rising Star requires 2500 XP, user only has 1200 XP"
            );
    }

    [Fact]
    public async Task CheckAndUnlockAchievements_CalledTwiceWithSameXp_IsIdempotent()
    {
        // Arrange
        var totalXp = 500;

        // Act
        await _service.CheckAndUnlockAchievementsAsync(_userId, totalXp);
        await _service.CheckAndUnlockAchievementsAsync(_userId, totalXp);

        // Assert
        var unlocked = await _ctx
            .UserAchievements.Where(ua => ua.UserId == _userId)
            .ToListAsync(TestContext.Current.CancellationToken);

        unlocked
            .Should()
            .HaveCount(
                2,
                because: "duplicate trigger should not create additional UserAchievement records (prevents XP farming)"
            );
        unlocked
            .Select(u => u.AchievementId)
            .Should()
            .BeEquivalentTo(new[] { _achievements[0].AchievementId, _achievements[1].AchievementId });
    }

    [Fact]
    public async Task CheckAndUnlockAchievements_IncrementalXpIncrease_OnlyUnlocksNewAchievements()
    {
        // Arrange — user already has First Steps (100 XP)
        await _service.CheckAndUnlockAchievementsAsync(_userId, 100);

        // Act — user gains more XP and crosses 500 XP threshold
        await _service.CheckAndUnlockAchievementsAsync(_userId, 600);

        // Assert
        var unlocked = await _ctx
            .UserAchievements.Where(ua => ua.UserId == _userId)
            .Select(ua => ua.AchievementId)
            .ToListAsync(TestContext.Current.CancellationToken);

        unlocked
            .Should()
            .HaveCount(2, because: "user now qualifies for both 100 XP and 500 XP achievements");
        unlocked
            .Should()
            .Contain(_achievements[0].AchievementId, because: "First Steps was already unlocked at 100 XP");
        unlocked
            .Should()
            .Contain(
                _achievements[1].AchievementId,
                because: "Getting Started unlocks at 500 XP, user now has 600 XP"
            );
    }

    [Fact]
    public async Task CheckAndUnlockAchievements_HighXp_UnlocksAllAchievements()
    {
        // Arrange
        var totalXp = 10000;

        // Act
        await _service.CheckAndUnlockAchievementsAsync(_userId, totalXp);

        // Assert
        var unlocked = await _ctx
            .UserAchievements.Where(ua => ua.UserId == _userId)
            .ToListAsync(TestContext.Current.CancellationToken);

        unlocked.Should().HaveCount(4, because: "10000 XP exceeds all achievement thresholds");
        unlocked
            .Select(u => u.AchievementId)
            .Should()
            .BeEquivalentTo(_achievements.Select(a => a.AchievementId));
    }

    [Fact]
    public async Task CheckAndUnlockAchievements_MultipleUsers_DoesNotCrossContaminate()
    {
        // Arrange
        var otherUser = new UserBuilder()
            .WithUserName("otheruser")
            .WithEmail("other@test.com")
            .Build();
        await DbSeeder.AddUserAsync(_ctx, otherUser);

        // Act — unlock achievements for other user only
        await _service.CheckAndUnlockAchievementsAsync(otherUser.Id, 1000);

        // Assert — verify _userId has no achievements
        var unlocked = await _ctx
            .UserAchievements.Where(ua => ua.UserId == _userId)
            .ToListAsync(TestContext.Current.CancellationToken);

        unlocked.Should().BeEmpty(because: "achievements are user-scoped");
    }

    [Fact]
    public async Task GetUserAchievements_NoUnlocks_ReturnsAllDefinitionsWithIsUnlockedFalse()
    {
        // Act
        var result = await _service.GetUserAchievementsAsync(_userId);

        // Assert
        result
            .Should()
            .HaveCount(_achievements.Count, because: "returns all achievement definitions");
        result
            .Should()
            .AllSatisfy(a =>
            {
                a.IsUnlocked.Should().BeFalse(because: "user has not unlocked any achievements");
                a.UnlockedAt.Should().BeNull();
            });
        result
            .Should()
            .BeInAscendingOrder(
                a => a.XpRequired,
                because: "ordered by OrderIndex which matches XP progression"
            );
    }

    [Fact]
    public async Task GetUserAchievements_SomeUnlocked_MergesUnlockStatusCorrectly()
    {
        // Arrange — unlock first 2 achievements
        await _service.CheckAndUnlockAchievementsAsync(_userId, 500);

        // Act
        var result = await _service.GetUserAchievementsAsync(_userId);

        // Assert
        result
            .Should()
            .HaveCount(_achievements.Count, because: "returns all achievement definitions");

        var firstSteps = result.First(a => a.XpRequired == 100);
        firstSteps
            .IsUnlocked.Should()
            .BeTrue(because: "user has 500 XP, unlocked 100 XP achievement");
        firstSteps.UnlockedAt.Should().NotBeNull();

        var gettingStarted = result.First(a => a.XpRequired == 500);
        gettingStarted
            .IsUnlocked.Should()
            .BeTrue(because: "user has 500 XP, unlocked 500 XP achievement");
        gettingStarted.UnlockedAt.Should().NotBeNull();

        var dedicated = result.First(a => a.XpRequired == 1000);
        dedicated.IsUnlocked.Should().BeFalse(because: "user only has 500 XP, not 1000 XP");
        dedicated.UnlockedAt.Should().BeNull();

        var risingStar = result.First(a => a.XpRequired == 2500);
        risingStar.IsUnlocked.Should().BeFalse(because: "user only has 500 XP, not 2500 XP");
        risingStar.UnlockedAt.Should().BeNull();
    }

    [Fact]
    public async Task GetUserAchievements_AllUnlocked_ReturnsAllWithIsUnlockedTrue()
    {
        // Arrange
        await _service.CheckAndUnlockAchievementsAsync(_userId, 10000);

        // Act
        var result = await _service.GetUserAchievementsAsync(_userId);

        // Assert
        result.Should().HaveCount(_achievements.Count);
        result
            .Should()
            .AllSatisfy(a =>
            {
                a.IsUnlocked.Should()
                    .BeTrue(because: "user has 10000 XP, unlocked all achievements");
                a.UnlockedAt.Should().NotBeNull();
            });
    }

    [Fact]
    public async Task GetUserAchievements_OrderedByOrderIndex_ReturnsInCorrectSequence()
    {
        // Act
        var result = await _service.GetUserAchievementsAsync(_userId);

        // Assert
        result.Should().HaveCount(_achievements.Count);
        result
            .Should()
            .BeInAscendingOrder(a => a.XpRequired, because: "OrderIndex matches XP progression");
        result[0].Name.Should().Be("First Steps");
        result[1].Name.Should().Be("Getting Started");
        result[2].Name.Should().Be("Dedicated Learner");
        result[3].Name.Should().Be("Rising Star");
    }

    [Fact]
    public async Task GetUserAchievements_NonExistentUser_ReturnsAllDefinitionsUnlocked()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid().ToString();

        // Act
        var result = await _service.GetUserAchievementsAsync(nonExistentUserId);

        // Assert
        result
            .Should()
            .HaveCount(
                _achievements.Count,
                because: "returns all definitions even for non-existent users"
            );
        result
            .Should()
            .AllSatisfy(a =>
            {
                a.IsUnlocked.Should().BeFalse(because: "user does not exist, no unlocks possible");
                a.UnlockedAt.Should().BeNull();
            });
    }

    [Fact]
    public async Task CheckAndUnlockAchievements_UnlockedAtTimestamp_IsPersisted()
    {
        // Arrange
        var beforeUnlock = DateTime.UtcNow;

        // Act
        await _service.CheckAndUnlockAchievementsAsync(_userId, 100);

        // Assert
        var unlocked = await _ctx
            .UserAchievements.Where(ua => ua.UserId == _userId)
            .FirstAsync(TestContext.Current.CancellationToken);

        var afterUnlock = DateTime.UtcNow;
        unlocked
            .UnlockedAt.Should()
            .BeAfter(beforeUnlock.AddSeconds(-1))
            .And.BeBefore(
                afterUnlock.AddSeconds(1),
                because: "UnlockedAt timestamp is set to UtcNow"
            );
    }
}
