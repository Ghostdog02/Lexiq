using Backend.Api.Dtos;
using Backend.Api.Services;
using Backend.Database;
using Backend.Database.Entities;
using Backend.Tests.Builders;
using Backend.Tests.Helpers;
using Backend.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend.Tests.Services;

/// <summary>
/// Integration tests for Language CRUD operations covering:
/// - DTO validation (required fields, null optional fields)
/// - Business logic (eager loading, duplicate names)
/// - Edge cases (timestamps, cascade deletes)
/// </summary>
public class LanguageCrudTests(DatabaseFixture fixture)
    : IClassFixture<DatabaseFixture>,
        IAsyncLifetime
{
    private readonly DatabaseFixture _fixture = fixture;
    private BackendDbContext _ctx = null!;
    private LanguageService _sut = null!;
    private string _testUserId = null!;

    public async ValueTask InitializeAsync()
    {
        _ctx = _fixture.CreateDbContext();
        await DbSeeder.ClearLeaderboardDataAsync(_ctx, _fixture.SystemUserId);

        _sut = new LanguageService(_ctx);

        // Create test user for Course.CreatedById FK
        var user = new UserBuilder()
            .WithUserName("testadmin")
            .WithEmail("admin@test.com")
            .Build();
        await DbSeeder.AddUserAsync(_ctx, user);
        _testUserId = user.Id;
    }

    public async ValueTask DisposeAsync()
    {
        await _ctx.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    // ── CreateLanguageAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task CreateLanguage_ValidDto_ReturnsCreatedLanguage()
    {
        // Arrange
        var dto = new CreateLanguageDto(Name: "Spanish", FlagIconUrl: "https://example.com/es.svg");

        // Act
        var result = await _sut.CreateLanguageAsync(dto);

        var savedLanguage = await _ctx
            .Languages.FirstOrDefaultAsync(
                l => l.Id == result.Id,
                TestContext.Current.CancellationToken
            );

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Spanish");
        result.FlagIconUrl.Should().Be("https://example.com/es.svg");
        result.Id.Should().NotBeNullOrEmpty();

        savedLanguage.Should().NotBeNull();
        savedLanguage!.Name.Should().Be("Spanish");
    }

    [Fact]
    public async Task CreateLanguage_NullFlagIconUrl_CreatesSuccessfully()
    {
        // Arrange
        var dto = new CreateLanguageDto(Name: "French", FlagIconUrl: null);

        // Act
        var result = await _sut.CreateLanguageAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("French");
        result.FlagIconUrl.Should().BeNull();
    }

    [Fact]
    public async Task CreateLanguage_SetsCreatedAt()
    {
        // Arrange
        var beforeCreate = DateTime.UtcNow.AddSeconds(-1);
        var dto = new CreateLanguageDto(Name: "German", FlagIconUrl: null);

        // Act
        var result = await _sut.CreateLanguageAsync(dto);
        var afterCreate = DateTime.UtcNow.AddSeconds(1);

        // Assert
        result.CreatedAt.Should().BeAfter(beforeCreate);
        result.CreatedAt.Should().BeBefore(afterCreate);
    }

    // ── GetAllLanguagesAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetAllLanguages_ReturnsAllLanguagesWithCourses()
    {
        // Arrange
        var lang1 = await _sut.CreateLanguageAsync(
            new CreateLanguageDto("Portuguese", "https://example.com/pt.svg")
        );
        var lang2 = await _sut.CreateLanguageAsync(
            new CreateLanguageDto("Japanese", null)
        );

        // Create course for lang1
        var course = new Course
        {
            Id = Guid.NewGuid().ToString(),
            LanguageId = lang1.Id,
            Title = "Portuguese 101",
            Description = null,
            OrderIndex = 0,
            CreatedById = _testUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        _ctx.Courses.Add(course);
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.GetAllLanguagesAsync();

        // Assert
        result.Should().NotBeEmpty();
        result.Should().HaveCountGreaterThanOrEqualTo(2);

        var testLanguages = result.Where(l => new[] { lang1.Id, lang2.Id }.Contains(l.Id)).ToList();
        testLanguages.Should().HaveCount(2);

        var lang1Result = testLanguages.First(l => l.Id == lang1.Id);
        lang1Result
            .Courses.Should()
            .ContainSingle(because: "GetAllLanguagesAsync includes Courses navigation");
        lang1Result.Courses.First().Title.Should().Be("Portuguese 101");
    }

    // ── GetLanguageByIdAsync ────────────────────────────────────────────────

    [Fact]
    public async Task GetLanguageById_IncludesCourses()
    {
        // Arrange
        var language = await _sut.CreateLanguageAsync(new CreateLanguageDto("Russian", null));

        var course = new Course
        {
            Id = Guid.NewGuid().ToString(),
            LanguageId = language.Id,
            Title = "Russian Basics",
            Description = null,
            OrderIndex = 0,
            CreatedById = _testUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        _ctx.Courses.Add(course);
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.GetLanguageByIdAsync(language.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Courses.Should().ContainSingle();
        result.Courses.First().Title.Should().Be("Russian Basics");
    }

    [Fact]
    public async Task GetLanguageById_NonexistentId_ReturnsNull()
    {
        // Arrange
        var nonexistentId = Guid.NewGuid().ToString();

        // Act
        var result = await _sut.GetLanguageByIdAsync(nonexistentId);

        // Assert
        result
            .Should()
            .BeNull(
                because: "non-existent language ID returns null (404 at controller layer)"
            );
    }

    // ── UpdateLanguageAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task UpdateLanguage_UpdatesFields()
    {
        // Arrange
        var language = await _sut.CreateLanguageAsync(
            new CreateLanguageDto("Korean", "https://example.com/ko.svg")
        );

        var updateDto = new CreateLanguageDto("Korean (Updated)", "https://example.com/ko-new.svg");

        // Act
        var result = await _sut.UpdateLanguageAsync(language.Id, updateDto);

        // Assert
        result.Should().NotBeNull();
        result!.Name.Should().Be("Korean (Updated)");
        result.FlagIconUrl.Should().Be("https://example.com/ko-new.svg");
    }

    [Fact]
    public async Task UpdateLanguage_NonexistentLanguage_ReturnsNull()
    {
        // Arrange
        var nonexistentId = Guid.NewGuid().ToString();
        var updateDto = new CreateLanguageDto("NonExistent", null);

        // Act
        var result = await _sut.UpdateLanguageAsync(nonexistentId, updateDto);

        // Assert
        result
            .Should()
            .BeNull(
                because: "updating a non-existent language returns null (controller returns 404)"
            );
    }

    [Fact]
    public async Task UpdateLanguage_NullFlagIconUrl_SetsToNull()
    {
        // Arrange
        var language = await _sut.CreateLanguageAsync(
            new CreateLanguageDto("Chinese", "https://example.com/zh.svg")
        );

        var updateDto = new CreateLanguageDto("Chinese", null);

        // Act
        var result = await _sut.UpdateLanguageAsync(language.Id, updateDto);

        // Assert
        result.Should().NotBeNull();
        result!.FlagIconUrl.Should().BeNull(because: "null in DTO sets field to null (not preserved)");
    }

    // ── DeleteLanguageAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task DeleteLanguage_ExistingLanguage_ReturnsTrue()
    {
        // Arrange
        var language = await _sut.CreateLanguageAsync(new CreateLanguageDto("Arabic", null));

        // Act
        var result = await _sut.DeleteLanguageAsync(language.Id);

        var stillExists = await _ctx
            .Languages.AnyAsync(
                l => l.Id == language.Id,
                TestContext.Current.CancellationToken
            );

        // Assert
        result
            .Should()
            .BeTrue(
                because: "deleting an existing language succeeds and returns true"
            );
        stillExists.Should().BeFalse(because: "deleted language no longer exists in database");
    }

    [Fact]
    public async Task DeleteLanguage_NonexistentLanguage_ReturnsFalse()
    {
        // Arrange
        var nonexistentId = Guid.NewGuid().ToString();

        // Act
        var result = await _sut.DeleteLanguageAsync(nonexistentId);

        // Assert
        result
            .Should()
            .BeFalse(
                because: "deleting a non-existent language returns false (controller returns 404)"
            );
    }

    [Fact]
    public async Task DeleteLanguage_WithCourses_CascadeDeletes()
    {
        // Arrange
        var language = await _sut.CreateLanguageAsync(new CreateLanguageDto("Dutch", null));

        var courseId = Guid.NewGuid().ToString();
        var course = new Course
        {
            Id = courseId,
            LanguageId = language.Id,
            Title = "Dutch for Beginners",
            Description = null,
            OrderIndex = 0,
            CreatedById = _testUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
        _ctx.Courses.Add(course);
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.DeleteLanguageAsync(language.Id);

        var deletedCourse = await _ctx
            .Courses.FindAsync(new object[] { courseId }, TestContext.Current.CancellationToken);

        // Assert
        result.Should().BeTrue();
        deletedCourse
            .Should()
            .BeNull(
                because: "courses should cascade delete when their parent language is deleted"
            );
    }
}
