using Backend.Api.Services;
using Backend.Database;
using Backend.Tests.Builders;
using Backend.Tests.Helpers;
using Backend.Tests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Services;

/// <summary>
/// Integration tests for UserXpService: XP aggregation, completed exercise counting, activity tracking.
/// </summary>
public class UserXpServiceTests(DatabaseFixture fixture)
    : IClassFixture<DatabaseFixture>,
        IAsyncLifetime
{
    private readonly DatabaseFixture _fixture = fixture;
    private BackendDbContext _ctx = null!;
    private UserXpService _sut = null!;
    private string _userId = null!;
    private List<string> _exerciseIds = null!;

    public async ValueTask InitializeAsync()
    {
        _ctx = _fixture.CreateDbContext();
        _sut = new UserXpService(_ctx);

        await DbSeeder.ClearLeaderboardDataAsync(_ctx, _fixture.SystemUserId);

        // Create test user
        var user = new UserBuilder().WithUserName("xptest").WithEmail("xp@test.com").Build();
        await DbSeeder.AddUserAsync(_ctx, user);
        _userId = user.Id;

        // Create test exercises
        _exerciseIds = [];
        for (var i = 0; i < 5; i++)
        {
            var exerciseId = await DbSeeder.CreateFillInBlankExerciseAsync(
                _ctx,
                _fixture.LessonId,
                orderIndex: i,
                isLocked: false
            );
            _exerciseIds.Add(exerciseId);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _ctx.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetUserXpAsync_ExistingUser_SumsXpCorrectly()
    {
        // Arrange
        await DbSeeder.AddProgressAsync(
            _ctx,
            _userId,
            _exerciseIds[0],
            isCompleted: true,
            pointsEarned: 10,
            completedAt: DateTime.UtcNow.AddDays(-2)
        );
        await DbSeeder.AddProgressAsync(
            _ctx,
            _userId,
            _exerciseIds[1],
            isCompleted: true,
            pointsEarned: 15,
            completedAt: DateTime.UtcNow.AddDays(-1)
        );
        await DbSeeder.AddProgressAsync(
            _ctx,
            _userId,
            _exerciseIds[2],
            isCompleted: true,
            pointsEarned: 20,
            completedAt: DateTime.UtcNow
        );

        // Act
        var result = await _sut.GetUserXpAsync(_userId);

        // Assert
        result.Should().NotBeNull();
        result!
            .TotalXp.Should()
            .Be(
                45,
                because: "total XP should sum all PointsEarned from UserExerciseProgress (10 + 15 + 20)"
            );
        result.UserId.Should().Be(_userId);
    }

    [Fact]
    public async Task GetUserXpAsync_NonExistentUser_ReturnsNull()
    {
        // Arrange
        var nonExistentUserId = Guid.NewGuid().ToString();

        // Act
        var result = await _sut.GetUserXpAsync(nonExistentUserId);

        // Assert
        result.Should().BeNull(because: "querying XP for a non-existent user should return null");
    }

    [Fact]
    public async Task GetUserXpAsync_CountsCompletedExercises()
    {
        // Arrange
        await DbSeeder.AddProgressAsync(
            _ctx,
            _userId,
            _exerciseIds[0],
            isCompleted: true,
            pointsEarned: 10,
            completedAt: DateTime.UtcNow.AddDays(-2)
        );
        await DbSeeder.AddProgressAsync(
            _ctx,
            _userId,
            _exerciseIds[1],
            isCompleted: true,
            pointsEarned: 10,
            completedAt: DateTime.UtcNow.AddDays(-1)
        );
        await DbSeeder.AddProgressAsync(
            _ctx,
            _userId,
            _exerciseIds[2],
            isCompleted: false,
            pointsEarned: 0,
            completedAt: null
        );

        // Act
        var result = await _sut.GetUserXpAsync(_userId);

        // Assert
        result.Should().NotBeNull();
        result!
            .CompletedExercises.Should()
            .Be(
                2,
                because: "only exercises with IsCompleted=true should be counted (excludes incomplete attempts)"
            );
    }

    [Fact]
    public async Task GetUserXpAsync_ReturnsLastActivityTimestamp()
    {
        // Arrange
        var oldestActivity = DateTime.UtcNow.AddDays(-5);
        var middleActivity = DateTime.UtcNow.AddDays(-2);
        var mostRecentActivity = DateTime.UtcNow.AddHours(-3);

        await DbSeeder.AddProgressAsync(
            _ctx,
            _userId,
            _exerciseIds[0],
            isCompleted: true,
            pointsEarned: 10,
            completedAt: oldestActivity
        );
        await DbSeeder.AddProgressAsync(
            _ctx,
            _userId,
            _exerciseIds[1],
            isCompleted: true,
            pointsEarned: 10,
            completedAt: middleActivity
        );
        await DbSeeder.AddProgressAsync(
            _ctx,
            _userId,
            _exerciseIds[2],
            isCompleted: true,
            pointsEarned: 10,
            completedAt: mostRecentActivity
        );

        // Act
        var result = await _sut.GetUserXpAsync(_userId);

        // Assert
        result.Should().NotBeNull();
        result!
            .LastActivityAt.Should()
            .BeCloseTo(
                mostRecentActivity,
                TimeSpan.FromSeconds(1),
                because: "LastActivityAt should return the most recent CompletedAt timestamp from all progress records"
            );
    }

    [Fact]
    public async Task GetUserXpAsync_NoProgress_ReturnsZero()
    {
        // Arrange - user exists but has no exercise progress

        // Act
        var result = await _sut.GetUserXpAsync(_userId);

        // Assert
        result.Should().NotBeNull();
        result!
            .TotalXp.Should()
            .Be(0, because: "users with no exercise progress should have zero total XP");
        result
            .CompletedExercises.Should()
            .Be(0, because: "users with no exercise progress should have zero completed exercises");
        result
            .LastActivityAt.Should()
            .BeNull(because: "users with no exercise progress should have null LastActivityAt");
    }
}
