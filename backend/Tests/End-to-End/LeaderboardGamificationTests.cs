using System.Net;
using System.Net.Http.Json;
using Backend.Api.Dtos;
using Backend.Database;
using Backend.Tests.Builders;
using Backend.Tests.Helpers;
using Backend.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Backend.Tests.EndToEnd;

/// <summary>
/// HTTP-level E2E tests for gamification features.
///
/// Verifies:
///   - Student solves exercises → leaderboard rank increases
///   - 3-day streak → streak displayed correctly
///   - Inactive 2 days → streak resets to 0
///   - Student uploads avatar → avatar shows on leaderboard
/// </summary>
public class LeaderboardGamificationTests(DatabaseFixture fixture)
    : ControllerTestBase(fixture),
        IClassFixture<DatabaseFixture>
{
    private HttpClient _client1 = null!;
    private HttpClient _client2 = null!;
    private string _student1Token = null!;
    private string _student2Token = null!;
    private string _student1Id = null!;
    private string _student2Id = null!;

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        await ClearTestDataAsync(); // Clean state before each test

        // Create two competing students
        var (student1Id, student1Token) = await CreateAuthenticatedUserAsync(
            "student1",
            "student1@test.com",
            "Student"
        );
        var (student2Id, student2Token) = await CreateAuthenticatedUserAsync(
            "student2",
            "student2@test.com",
            "Student"
        );

        _student1Id = student1Id;
        _student2Id = student2Id;
        _student1Token = student1Token;
        _student2Token = student2Token;

        _client1 = CreateClient(_student1Token);
        _client2 = CreateClient(_student2Token);
    }

    public override async ValueTask DisposeAsync()
    {
        _client1.Dispose();
        _client2.Dispose();
        await base.DisposeAsync();
    }

    [Fact]
    public async Task Student_EarnsXp_AppearsOnLeaderboard()
    {
        // Arrange: Student1 has no XP initially
        var initialLeaderboard = await GetLeaderboardAsync(_client1, TimeFrame.AllTime);
        var student1Initial = initialLeaderboard.Entries.FirstOrDefault(e =>
            e.UserId == _student1Id
        );

        // Student1 not on leaderboard yet (or has 0 XP)
        if (student1Initial != null)
            student1Initial.TotalXp.Should().Be(0);

        // Act: Student1 completes 3 exercises (30 XP)
        for (var i = 0; i < 3; i++)
        {
            var exerciseId = Fixture.ExerciseIds[i];
            await SubmitAnswerAsync(_client1, exerciseId, "answer");
        }

        // Assert: Student1 appears on leaderboard with 30 XP
        var updatedLeaderboard = await GetLeaderboardAsync(_client1, TimeFrame.AllTime);
        var student1Entry = updatedLeaderboard.Entries.FirstOrDefault(e => e.UserId == _student1Id);

        student1Entry.Should().NotBeNull("student1 should be on leaderboard");
        student1Entry!.TotalXp.Should().Be(30);
        student1Entry.Level.Should().BeGreaterThan(0, "level calculated from XP");
    }

    [Fact]
    public async Task Student_Builds3DayStreak_StreakDisplayedCorrectly()
    {
        // Arrange: Seed 3 consecutive days of activity for student1
        using var scope = Factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<BackendDbContext>();

        await DbSeeder.AddConsecutiveDaysActivityAsync(
            ctx,
            _student1Id,
            Fixture.ExerciseIds,
            days: 3,
            startDaysAgo: 0,
            pointsPerDay: 10
        );

        // Act: Fetch leaderboard
        var leaderboard = await GetLeaderboardAsync(_client1, TimeFrame.AllTime);
        var student1Entry = leaderboard.Entries.FirstOrDefault(e => e.UserId == _student1Id);

        // Assert: Current streak is 3
        student1Entry.Should().NotBeNull();
        student1Entry!.CurrentStreak.Should().Be(3);
        student1Entry.TotalXp.Should().Be(30, "3 days × 10 points");
    }

    [Fact]
    public async Task Student_Inactive2Days_StreakResets()
    {
        // Arrange: Student1 had a 3-day streak, but it ended 2 days ago
        using var scope = Factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<BackendDbContext>();

        // Seed streak that ended 3 days ago (gap of 2 days from today)
        await DbSeeder.AddConsecutiveDaysActivityAsync(
            ctx,
            _student1Id,
            Fixture.ExerciseIds,
            days: 3,
            startDaysAgo: 3, // Last activity was 3 days ago
            pointsPerDay: 10
        );

        // Act: Fetch leaderboard
        var leaderboard = await GetLeaderboardAsync(_client1, TimeFrame.AllTime);
        var student1Entry = leaderboard.Entries.FirstOrDefault(e => e.UserId == _student1Id);

        // Assert: Current streak is 0 due to 2-day gap
        student1Entry.Should().NotBeNull();
        student1Entry!.CurrentStreak.Should().Be(0, "streak resets after missing a day");
        student1Entry.LongestStreak.Should().Be(3, "longest streak preserved");
    }

    [Fact]
    public async Task Student_UploadsAvatar_ShowsOnLeaderboard()
    {
        // Arrange: Student1 uploads an avatar
        using var scope = Factory.Services.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<BackendDbContext>();

        await DbSeeder.AddAvatarAsync(ctx, _student1Id);

        // Also give student1 some XP so they appear on leaderboard
        await DbSeeder.AddProgressAsync(
            ctx,
            _student1Id,
            Fixture.ExerciseIds[0],
            isCompleted: true,
            pointsEarned: 10,
            completedAt: DateTime.UtcNow
        );

        // Act: Fetch leaderboard
        var leaderboard = await GetLeaderboardAsync(_client1, TimeFrame.AllTime);
        var student1Entry = leaderboard.Entries.FirstOrDefault(e => e.UserId == _student1Id);

        // Assert: Avatar URL is present
        student1Entry.Should().NotBeNull();
        student1Entry!.Avatar.Should().NotBeNullOrEmpty("avatar should be present");
        student1Entry.Avatar.Should().Contain("/api/user/");
        student1Entry.Avatar.Should().Contain("/avatar");
    }

    [Fact]
    public async Task TwoStudents_CompeteForRank_OrderedByXp()
    {
        // Arrange: Student1 earns 50 XP, Student2 earns 30 XP
        for (var i = 0; i < 5; i++)
            await SubmitAnswerAsync(_client1, Fixture.ExerciseIds[i], "answer");

        for (var i = 0; i < 3; i++)
            await SubmitAnswerAsync(_client2, Fixture.ExerciseIds[i + 5], "answer");

        // Act: Fetch leaderboard
        var leaderboard = await GetLeaderboardAsync(_client1, TimeFrame.AllTime);

        var student1Entry = leaderboard.Entries.FirstOrDefault(e => e.UserId == _student1Id);
        var student2Entry = leaderboard.Entries.FirstOrDefault(e => e.UserId == _student2Id);

        // Assert: Student1 ranks higher than Student2
        student1Entry.Should().NotBeNull();
        student2Entry.Should().NotBeNull();

        student1Entry!.TotalXp.Should().Be(50);
        student2Entry!.TotalXp.Should().Be(30);

        student1Entry
            .Rank.Should()
            .BeLessThan(student2Entry.Rank, "higher XP means lower rank number");
    }

    // Helper methods

    private async Task<LeaderboardResponse> GetLeaderboardAsync(
        HttpClient client,
        TimeFrame timeFrame
    )
    {
        var response = await client.GetAsync(
            $"/api/leaderboard?timeFrame={timeFrame}",
            TestContext.Current.CancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var leaderboard = await response.Content.ReadFromJsonAsync<LeaderboardResponse>(
            cancellationToken: TestContext.Current.CancellationToken
        );

        leaderboard.Should().NotBeNull();
        return leaderboard!;
    }

    private async Task SubmitAnswerAsync(HttpClient client, string exerciseId, string answer)
    {
        var response = await client.PostAsJsonAsync(
            $"/api/exercises/{exerciseId}/submit",
            new SubmitAnswerRequest(answer),
            TestContext.Current.CancellationToken
        );

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
