using Backend.Api.Services;
using FluentAssertions;
using Xunit;

namespace Backend.Tests.Services;

/// <summary>
/// Pure unit tests for LeaderboardService.CalculateLevel.
/// No database or fixture required — static method, no side effects.
///
/// Formula: level = floor((1 + sqrt(1 + totalXp / 25)) / 2)
/// Thresholds (100 * n * (n-1)): Level 2 = 200, Level 3 = 600, Level 4 = 1200, Level 5 = 2000
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
    public void CalculateLevel_At199Xp_ReturnsLevel1()
    {
        LeaderboardService.CalculateLevel(199).Should().Be(1);
    }

    [Fact]
    public void CalculateLevel_At200Xp_ReturnsLevel2()
    {
        // 200 is the exact Level 2 threshold (100 * 2 * 1).
        // Validates that floating-point precision in sqrt doesn't push it below 2.
        LeaderboardService.CalculateLevel(200).Should().Be(2);
    }

    [Fact]
    public void CalculateLevel_At599Xp_ReturnsLevel2()
    {
        LeaderboardService.CalculateLevel(599).Should().Be(2);
    }

    [Fact]
    public void CalculateLevel_At600Xp_ReturnsLevel3()
    {
        LeaderboardService.CalculateLevel(600).Should().Be(3);
    }

    [Fact]
    public void CalculateLevel_At1199Xp_ReturnsLevel3()
    {
        LeaderboardService.CalculateLevel(1199).Should().Be(3);
    }

    [Fact]
    public void CalculateLevel_At1200Xp_ReturnsLevel4()
    {
        LeaderboardService.CalculateLevel(1200).Should().Be(4);
    }

    [Fact]
    public void CalculateLevel_At2000Xp_ReturnsLevel5()
    {
        LeaderboardService.CalculateLevel(2000).Should().Be(5);
    }

    [Theory]
    [InlineData(200, 2)]
    [InlineData(600, 3)]
    [InlineData(1200, 4)]
    [InlineData(2000, 5)]
    [InlineData(3000, 6)]
    [InlineData(4200, 7)]
    public void CalculateLevel_AtThresholdBoundaries_ReturnsCorrectLevel(int xp, int expected)
    {
        // threshold(n) = 100 * n * (n - 1) — each value is the minimum XP for that level
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
