using Backend.Api.Services;
using Backend.Database;
using Backend.Database.Entities;
using Backend.Database.Entities.Users;
using Backend.Tests.Builders;
using Backend.Tests.Helpers;
using Backend.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend.Tests.Services;

/// <summary>
/// Integration tests for UserLanguageService: enrollment, unenrollment, composite key behavior.
/// </summary>
public class UserLanguageServiceTests(DatabaseFixture fixture)
    : IClassFixture<DatabaseFixture>,
        IAsyncLifetime
{
    private readonly DatabaseFixture _fixture = fixture;
    private BackendDbContext _ctx = null!;
    private UserLanguageService _sut = null!;
    private string _userId = null!;
    private string _languageId = null!;

    public async ValueTask InitializeAsync()
    {
        _ctx = _fixture.CreateDbContext();
        _sut = new UserLanguageService(_ctx);

        // Clear test data from previous runs
        await DbSeeder.ClearLeaderboardDataAsync(_ctx, _fixture.SystemUserId);

        // Create test user
        var user = new UserBuilder()
            .WithUserName("enrolltest")
            .WithEmail("enroll@test.com")
            .Build();
        _ctx.Users.Add(user);

        // Get existing language from fixture
        var language = await _ctx.Languages.FirstAsync(TestContext.Current.CancellationToken);
        _languageId = language.LanguageId;
        _userId = user.Id;

        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _ctx.DisposeAsync();
    }

    #region EnrollUserAsync

    [Fact]
    public async Task EnrollUserAsync_ValidLanguage_CreatesUserLanguage()
    {
        // Act
        var result = await _sut.EnrollUserAsync(_userId, _languageId);

        // Assert
        result
            .Should()
            .NotBeNull(
                because: "enrolling in a valid language should create a UserLanguage record"
            );
        result!.UserId.Should().Be(_userId);
        result.LanguageId.Should().Be(_languageId);
        result
            .EnrolledAt.Should()
            .BeCloseTo(
                DateTime.UtcNow,
                TimeSpan.FromSeconds(5),
                because: "EnrolledAt timestamp should be set to current UTC time"
            );

        // Verify in database
        var dbRecord = await _ctx.UserLanguages.FindAsync(
            new object[] { _userId, _languageId },
            TestContext.Current.CancellationToken
        );
        dbRecord.Should().NotBeNull();
    }

    [Fact]
    public async Task EnrollUserAsync_AlreadyEnrolled_ReturnsExisting()
    {
        // Arrange
        var firstEnrollment = await _sut.EnrollUserAsync(_userId, _languageId);
        var firstEnrolledAt = firstEnrollment!.EnrolledAt;

        // Wait a moment to ensure timestamps would differ if a new record were created
        await Task.Delay(100, TestContext.Current.CancellationToken);

        // Act
        var secondEnrollment = await _sut.EnrollUserAsync(_userId, _languageId);

        // Assert
        secondEnrollment.Should().NotBeNull();
        secondEnrollment!.UserId.Should().Be(_userId);
        secondEnrollment.LanguageId.Should().Be(_languageId);
        secondEnrollment
            .EnrolledAt.Should()
            .Be(
                firstEnrolledAt,
                because: "idempotent enrollment should return the existing record without updating the timestamp"
            );

        // Verify only one record exists
        var allEnrollments = await _ctx
            .UserLanguages.Where(ul => ul.UserId == _userId && ul.LanguageId == _languageId)
            .ToListAsync(TestContext.Current.CancellationToken);
        allEnrollments
            .Should()
            .HaveCount(
                1,
                because: "duplicate enrollment should not create a second UserLanguage record"
            );
    }

    [Fact]
    public async Task EnrollUserAsync_InvalidLanguage_ReturnsNull()
    {
        // Arrange
        var nonExistentLanguageId = Guid.NewGuid().ToString();

        // Act
        var result = await _sut.EnrollUserAsync(_userId, nonExistentLanguageId);

        // Assert
        result.Should().BeNull(because: "enrolling in a non-existent language should return null");

        // Verify no record was created
        var dbRecord = await _ctx.UserLanguages.FindAsync(
            new object[] { _userId, nonExistentLanguageId },
            TestContext.Current.CancellationToken
        );
        dbRecord.Should().BeNull();
    }

    [Fact]
    public async Task EnrollUserAsync_SetsEnrolledAtTimestamp()
    {
        // Arrange
        var beforeEnrollment = DateTime.UtcNow;

        // Act
        var result = await _sut.EnrollUserAsync(_userId, _languageId);
        var afterEnrollment = DateTime.UtcNow;

        // Assert
        result.Should().NotBeNull();
        result!.EnrolledAt.Should().BeOnOrAfter(beforeEnrollment).And.BeOnOrBefore(afterEnrollment);
    }

    #endregion

    #region UnenrollUserAsync

    [Fact]
    public async Task UnenrollUserAsync_ExistingEnrollment_ReturnsTrue()
    {
        // Arrange
        await _sut.EnrollUserAsync(_userId, _languageId);

        // Act
        var result = await _sut.UnenrollUserAsync(_userId, _languageId);

        // Assert
        result.Should().BeTrue(because: "unenrolling from an existing enrollment should succeed");

        // Verify record was deleted
        var dbRecord = await _ctx.UserLanguages.FindAsync(
            new object[] { _userId, _languageId },
            TestContext.Current.CancellationToken
        );
        dbRecord.Should().BeNull(because: "unenrollment should delete the UserLanguage record");
    }

    [Fact]
    public async Task UnenrollUserAsync_NonExistentEnrollment_ReturnsFalse()
    {
        // Arrange
        var nonExistentLanguageId = Guid.NewGuid().ToString();

        // Act
        var result = await _sut.UnenrollUserAsync(_userId, nonExistentLanguageId);

        // Assert
        result
            .Should()
            .BeFalse(because: "unenrolling from a non-existent enrollment should return false");
    }

    #endregion

    #region GetUserLanguagesAsync

    [Fact]
    public async Task GetUserLanguagesAsync_IncludesLanguageNavigation()
    {
        // Arrange
        await _sut.EnrollUserAsync(_userId, _languageId);

        // Act
        var result = await _sut.GetUserLanguagesAsync(_userId);

        // Assert
        result.Should().HaveCount(1);
        result[0]
            .Language.Should()
            .NotBeNull(
                because: "GetUserLanguagesAsync should eager-load the Language navigation property"
            );
        result[0].Language.Id.Should().Be(_languageId);
    }

    [Fact]
    public async Task GetUserLanguagesAsync_NoEnrollments_ReturnsEmptyList()
    {
        // Act
        var result = await _sut.GetUserLanguagesAsync(_userId);

        // Assert
        result.Should().BeEmpty(because: "a user with no enrollments should return an empty list");
    }

    [Fact]
    public async Task GetUserLanguagesAsync_MultipleEnrollments_ReturnsAll()
    {
        // Arrange
        // Create a second language
        var secondLanguage = new Language { Name = "Spanish", FlagIconUrl = null };
        _ctx.Languages.Add(secondLanguage);
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        await _sut.EnrollUserAsync(_userId, _languageId);
        await _sut.EnrollUserAsync(_userId, secondLanguage.Id);

        // Act
        var result = await _sut.GetUserLanguagesAsync(_userId);

        // Assert
        result.Should().HaveCount(2, because: "user should have enrollments in both languages");
        result
            .Should()
            .Contain(
                ul => ul.LanguageId == _languageId,
                because: "first enrollment should be in result"
            );
        result
            .Should()
            .Contain(
                ul => ul.LanguageId == secondLanguage.Id,
                because: "second enrollment should be in result"
            );
    }

    #endregion

    #region Composite Key Enforcement

    [Fact]
    public async Task EnrollUserAsync_CompositeKeyUniqueness_EnforcedByDatabase()
    {
        // Arrange
        var enrollment = new UserLanguage
        {
            UserId = _userId,
            LanguageId = _languageId,
            EnrolledAt = DateTime.UtcNow,
        };
        _ctx.UserLanguages.Add(enrollment);
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Clear change tracker to allow adding a duplicate entity with the same key
        _ctx.ChangeTracker.Clear();

        // Act
        var duplicateEnrollment = new UserLanguage
        {
            UserId = _userId,
            LanguageId = _languageId,
            EnrolledAt = DateTime.UtcNow,
        };
        _ctx.UserLanguages.Add(duplicateEnrollment);
        var act = async () => await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Assert
        await act.Should()
            .ThrowAsync<DbUpdateException>(
                because: "database should enforce composite key uniqueness and reject duplicate (UserId, LanguageId) pairs"
            );
    }

    #endregion
}
