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
/// Integration tests for Course CRUD operations covering:
/// - DTO validation (required fields, invalid language, empty/null values)
/// - Business logic (OrderIndex handling, FK validation, partial updates)
/// - Edge cases (multiple courses, timestamps, ordering, cascade deletes)
/// </summary>
public class CourseCrudTests : IClassFixture<DatabaseFixture>, IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;
    private BackendDbContext _ctx = null!;
    private CourseService _sut = null!;
    private string _testUserId = null!;
    private string _languageId = null!;
    private const string ItalianLanguageName = "Italian";

    public CourseCrudTests(DatabaseFixture fixture) => _fixture = fixture;

    public async ValueTask InitializeAsync()
    {
        _ctx = _fixture.CreateDbContext();
        await DbSeeder.ClearLeaderboardDataAsync(_ctx, _fixture.SystemUserId);

        _sut = new CourseService(_ctx);

        // Create test user for CreatedById FK
        var user = new UserBuilder()
            .WithUserName("testadmin")
            .WithEmail("admin@test.com")
            .Build();
        await DbSeeder.AddUserAsync(_ctx, user);
        _testUserId = user.Id;

        // Get the fixture's Italian language
        var language = await _ctx.Languages.FirstAsync(TestContext.Current.CancellationToken);
        _languageId = language.LanguageId;
    }

    public async ValueTask DisposeAsync()
    {
        await _ctx.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    #region CreateCourseAsync - DTO Validation

    [Fact]
    public async Task CreateCourseAsync_ValidDto_ReturnsCreatedCourse()
    {
        // Arrange
        var dto = new CreateCourseDto(
            LanguageName: ItalianLanguageName,
            Title: "Italian for Beginners",
            Description: "Learn the basics of Italian",
            EstimatedDurationHours: 40,
            OrderIndex: 0
        );

        // Act
        var result = await _sut.CreateCourseAsync(dto, _testUserId);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Italian for Beginners");
        result.Description.Should().Be("Learn the basics of Italian");
        result.EstimatedDurationHours.Should().Be(40);
        result.OrderIndex.Should().Be(0);
        result.LanguageId.Should().Be(_languageId);
        result.CreatedById.Should().Be(_testUserId);
    }

    [Fact]
    public async Task CreateCourseAsync_InvalidLanguageName_ThrowsArgumentException()
    {
        // Arrange
        var dto = new CreateCourseDto(
            LanguageName: "NonexistentLanguage",
            Title: "Test Course",
            Description: null,
            EstimatedDurationHours: null,
            OrderIndex: 0
        );

        // Act
        var act = async () => await _sut.CreateCourseAsync(dto, _testUserId);

        // Assert
        await act.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("Language 'NonexistentLanguage' not found.",
                because: "CourseService validates language existence before creation");
    }

    [Fact]
    public async Task CreateCourseAsync_NullOptionalFields_CreatesSuccessfully()
    {
        // Arrange
        var dto = new CreateCourseDto(
            LanguageName: ItalianLanguageName,
            Title: "Minimal Course",
            Description: null,
            EstimatedDurationHours: null,
            OrderIndex: 0
        );

        // Act
        var result = await _sut.CreateCourseAsync(dto, _testUserId);

        // Assert
        result.Should().NotBeNull();
        result.Title.Should().Be("Minimal Course");
        result.Description.Should().BeNull();
        result.EstimatedDurationHours.Should().BeNull();
    }

    [Fact]
    public async Task CreateCourse_SetsCreatedAtAndUpdatedAt()
    {
        // Arrange
        var beforeCreate = DateTime.UtcNow.AddSeconds(-1);
        var dto = new CreateCourseDto(
            LanguageName: ItalianLanguageName,
            Title: "Timestamp Test",
            Description: null,
            EstimatedDurationHours: null,
            OrderIndex: 0
        );

        // Act
        var result = await _sut.CreateCourseAsync(dto, _testUserId);
        var afterCreate = DateTime.UtcNow.AddSeconds(1);

        // Assert
        result.CreatedAt.Should().BeAfter(beforeCreate);
        result.CreatedAt.Should().BeBefore(afterCreate);
        result.UpdatedAt.Should().BeCloseTo(result.CreatedAt, TimeSpan.FromMilliseconds(10),
            because: "CreatedAt and UpdatedAt are both set to UtcNow on creation");
    }

    #endregion

    #region GetAllCoursesAsync - Business Logic

    [Fact]
    public async Task GetAllCourses_ReturnsCoursesOrderedByOrderIndex()
    {
        // Arrange
        await _sut.CreateCourseAsync(
            new CreateCourseDto(ItalianLanguageName, "Course C", null, null, 2),
            _testUserId
        );
        await _sut.CreateCourseAsync(
            new CreateCourseDto(ItalianLanguageName, "Course A", null, null, 0),
            _testUserId
        );
        await _sut.CreateCourseAsync(
            new CreateCourseDto(ItalianLanguageName, "Course B", null, null, 1),
            _testUserId
        );

        // Act
        var result = await _sut.GetAllCoursesAsync();

        // Assert
        result.Should().HaveCountGreaterThanOrEqualTo(3,
            because: "three courses were created in this test plus the fixture course");
        var testCourses = result.Where(c => c.Title.StartsWith("Course ")).ToList();
        testCourses.Should().HaveCount(3);
        testCourses[0].Title.Should().Be("Course A");
        testCourses[1].Title.Should().Be("Course B");
        testCourses[2].Title.Should().Be("Course C");
    }

    [Fact]
    public async Task CreateMultipleCourses_SameLanguage_OrderedByOrderIndex()
    {
        // Arrange
        var course1 = await _sut.CreateCourseAsync(
            new CreateCourseDto(ItalianLanguageName, "First", null, null, 0),
            _testUserId
        );
        var course2 = await _sut.CreateCourseAsync(
            new CreateCourseDto(ItalianLanguageName, "Second", null, null, 1),
            _testUserId
        );
        var course3 = await _sut.CreateCourseAsync(
            new CreateCourseDto(ItalianLanguageName, "Third", null, null, 2),
            _testUserId
        );

        // Act
        var allCourses = await _sut.GetAllCoursesAsync();

        // Assert
        var orderedCourses = allCourses
            .Where(c => new[] { course1.CourseId, course2.CourseId, course3.CourseId }.Contains(c.Id))
            .ToList();

        orderedCourses.Should().HaveCount(3);
        orderedCourses[0].Id.Should().Be(course1.CourseId);
        orderedCourses[1].Id.Should().Be(course2.CourseId);
        orderedCourses[2].Id.Should().Be(course3.CourseId);
    }

    #endregion

    #region GetCourseByIdAsync - Business Logic

    [Fact]
    public async Task GetCourseById_IncludesLanguageAndLessons()
    {
        // Arrange
        var course = await _sut.CreateCourseAsync(
            new CreateCourseDto(ItalianLanguageName, "Course with Lessons", null, null, 0),
            _testUserId
        );

        var lesson = new Lesson
        {
            Id = Guid.NewGuid().ToString(),
            CourseId = course.CourseId,
            Title = "Test Lesson",
            LessonContent = "{}",
            OrderIndex = 0,
        };
        _ctx.Lessons.Add(lesson);
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.GetCourseByIdAsync(course.CourseId);

        // Assert
        result.Should().NotBeNull();
        result!.Language.Should().NotBeNull();
        result.Language.Name.Should().Be(ItalianLanguageName);
        result.Lessons.Should().ContainSingle();
        result.Lessons.First().Title.Should().Be("Test Lesson");
    }

    [Fact]
    public async Task GetCourseById_NonexistentId_ReturnsNull()
    {
        // Arrange
        var nonexistentId = Guid.NewGuid().ToString();

        // Act
        var result = await _sut.GetCourseByIdAsync(nonexistentId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region UpdateCourseAsync - Business Logic

    [Fact]
    public async Task UpdateCourse_PartialFields_OnlyUpdatesProvidedFields()
    {
        // Arrange
        var course = await _sut.CreateCourseAsync(
            new CreateCourseDto(ItalianLanguageName, "Original Title", "Original Description", 30, 0),
            _testUserId
        );

        var dto = new UpdateCourseDto(
            LanguageName: ItalianLanguageName,
            Title: "Updated Title",
            Description: null,
            EstimatedDurationHours: null,
            OrderIndex: null
        );

        // Act
        var result = await _sut.UpdateCourseAsync(course.CourseId, dto);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Updated Title");
        result.Description.Should().Be("Original Description",
            because: "null in UpdateCourseDto should not overwrite existing value");
        result.EstimatedDurationHours.Should().Be(30,
            because: "null in UpdateCourseDto should not overwrite existing value");
        result.OrderIndex.Should().Be(0,
            because: "null in UpdateCourseDto should not overwrite existing value");
    }

    [Fact]
    public async Task UpdateCourse_NonexistentCourse_ReturnsNull()
    {
        // Arrange
        var nonexistentId = Guid.NewGuid().ToString();
        var dto = new UpdateCourseDto(
            LanguageName: ItalianLanguageName,
            Title: "Updated",
            Description: null,
            EstimatedDurationHours: null,
            OrderIndex: null
        );

        // Act
        var result = await _sut.UpdateCourseAsync(nonexistentId, dto);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateCourse_UpdatesTimestamp()
    {
        // Arrange
        var course = await _sut.CreateCourseAsync(
            new CreateCourseDto(ItalianLanguageName, "Original", null, null, 0),
            _testUserId
        );

        var originalUpdatedAt = course.UpdatedAt;
        await Task.Delay(100); // Ensure timestamp difference

        var dto = new UpdateCourseDto(
            LanguageName: ItalianLanguageName,
            Title: "Updated Title",
            Description: null,
            EstimatedDurationHours: null,
            OrderIndex: null
        );

        // Act
        var result = await _sut.UpdateCourseAsync(course.CourseId, dto);

        // Assert
        result.Should().NotBeNull();
        result!.UpdatedAt.Should().BeAfter(originalUpdatedAt,
            because: "UpdatedAt must change when the course is updated");
    }

    [Fact]
    public async Task UpdateCourse_AllFields_UpdatesSuccessfully()
    {
        // Arrange
        var course = await _sut.CreateCourseAsync(
            new CreateCourseDto(ItalianLanguageName, "Original", "Old description", 20, 0),
            _testUserId
        );

        var dto = new UpdateCourseDto(
            LanguageName: ItalianLanguageName,
            Title: "New Title",
            Description: "New Description",
            EstimatedDurationHours: 50,
            OrderIndex: 5
        );

        // Act
        var result = await _sut.UpdateCourseAsync(course.CourseId, dto);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("New Title");
        result.Description.Should().Be("New Description");
        result.EstimatedDurationHours.Should().Be(50);
        result.OrderIndex.Should().Be(5);
    }

    [Fact]
    public async Task UpdateCourse_NullFields_KeepsOriginalValues()
    {
        // Arrange
        var course = await _sut.CreateCourseAsync(
            new CreateCourseDto(ItalianLanguageName, "Original Title", "Original Desc", 40, 3),
            _testUserId
        );

        var dto = new UpdateCourseDto(
            LanguageName: ItalianLanguageName,
            Title: null,
            Description: null,
            EstimatedDurationHours: null,
            OrderIndex: null
        );

        // Act
        var result = await _sut.UpdateCourseAsync(course.CourseId, dto);

        // Assert
        result.Should().NotBeNull();
        result!.Title.Should().Be("Original Title",
            because: "null Title in DTO should not overwrite existing value");
        result.Description.Should().Be("Original Desc",
            because: "null Description in DTO should not overwrite existing value");
        result.EstimatedDurationHours.Should().Be(40,
            because: "null EstimatedDurationHours in DTO should not overwrite existing value");
        result.OrderIndex.Should().Be(3,
            because: "null OrderIndex in DTO should not overwrite existing value");
    }

    #endregion

    #region DeleteCourseAsync - Business Logic

    [Fact]
    public async Task DeleteCourse_ExistingCourse_ReturnsTrue()
    {
        // Arrange
        var course = await _sut.CreateCourseAsync(
            new CreateCourseDto(ItalianLanguageName, "To Delete", null, null, 0),
            _testUserId
        );

        // Act
        var result = await _sut.DeleteCourseAsync(course.CourseId);

        // Assert
        result.Should().BeTrue();

        var deleted = await _ctx.Courses.FindAsync(
            new object[] { course.CourseId },
            TestContext.Current.CancellationToken
        );
        deleted.Should().BeNull();
    }

    [Fact]
    public async Task DeleteCourse_NonexistentCourse_ReturnsFalse()
    {
        // Arrange
        var nonexistentId = Guid.NewGuid().ToString();

        // Act
        var result = await _sut.DeleteCourseAsync(nonexistentId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteCourse_WithLessons_CascadeDeletes()
    {
        // Arrange
        var course = await _sut.CreateCourseAsync(
            new CreateCourseDto(ItalianLanguageName, "Course with Lesson", null, null, 0),
            _testUserId
        );

        var lessonId = Guid.NewGuid().ToString();
        var lesson = new Lesson
        {
            Id = lessonId,
            CourseId = course.CourseId,
            Title = "Lesson to cascade delete",
            LessonContent = "{}",
            OrderIndex = 0,
        };
        _ctx.Lessons.Add(lesson);
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var result = await _sut.DeleteCourseAsync(course.CourseId);

        // Assert
        result.Should().BeTrue();

        var deletedLesson = await _ctx.Lessons.FindAsync(
            new object[] { lessonId },
            TestContext.Current.CancellationToken
        );
        deletedLesson.Should().BeNull(
            because: "lessons should cascade delete when their parent course is deleted");
    }

    #endregion
}
