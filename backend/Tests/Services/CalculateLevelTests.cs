using Backend.Api.Services;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Services;

/// <summary>
/// Pure unit tests for LeaderboardService.CalculateLevel.
/// No database or fixture required — static method, no side effects.
///
/// Formula: level = floor((1 + sqrt(1 + totalXp / 25)) / 2)
/// Thresholds (50 * n * (n-1)): Level 2 = 100, Level 3 = 300, Level 4 = 600, Level 5 = 1000
/// </summary>
public class CalculateLevelTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(-100, 1)]
    public void CalculateLevel_ZeroOrNegativeXp_ReturnsLevel1(int xp, int expected)
    {
        LeaderboardService.CalculateLevel(xp).Should().Be(expected);
    }

    [Fact]
    public void CalculateLevel_At99Xp_ReturnsLevel1()
    {
        LeaderboardService.CalculateLevel(99).Should().Be(1);
    }

    [Fact]
    public void CalculateLevel_At100Xp_ReturnsLevel2()
    {
        // 100 is the exact Level 2 threshold (50 * 2 * 1).
        // Validates that floating-point precision in sqrt doesn't push it below 2.
        LeaderboardService.CalculateLevel(100).Should().Be(2);
    }

    [Fact]
    public void CalculateLevel_At299Xp_ReturnsLevel2()
    {
        LeaderboardService.CalculateLevel(299).Should().Be(2);
    }

    [Fact]
    public void CalculateLevel_At300Xp_ReturnsLevel3()
    {
        LeaderboardService.CalculateLevel(300).Should().Be(3);
    }

    [Fact]
    public void CalculateLevel_At599Xp_ReturnsLevel3()
    {
        LeaderboardService.CalculateLevel(599).Should().Be(3);
    }

    [Fact]
    public void CalculateLevel_At600Xp_ReturnsLevel4()
    {
        LeaderboardService.CalculateLevel(600).Should().Be(4);
    }

    [Fact]
    public void CalculateLevel_At1000Xp_ReturnsLevel5()
    {
        LeaderboardService.CalculateLevel(1000).Should().Be(5);
    }

    [Theory]
    [InlineData(100, 2)]
    [InlineData(300, 3)]
    [InlineData(600, 4)]
    [InlineData(1000, 5)]
    [InlineData(1500, 6)]
    [InlineData(2100, 7)]
    public void CalculateLevel_AtThresholdBoundaries_ReturnsCorrectLevel(int xp, int expected)
    {
        // threshold(n) = 50 * n * (n - 1) — each value is the minimum XP for that level
        LeaderboardService.CalculateLevel(xp).Should().Be(expected);
    }

    [Fact]
    public void CalculateLevel_WithLargeXp_DoesNotOverflow()
    {
        // sqrt(1 + 1_000_000 / 25) = sqrt(40001) ≈ 200 — no overflow risk, but worth asserting
        var level = LeaderboardService.CalculateLevel(1_000_000);

        level.Should().BeGreaterThan(10);
    }
}
