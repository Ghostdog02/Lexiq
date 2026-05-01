using Backend.Api.Services;
using Backend.Database;
using Backend.Database.Entities.Users;
using Backend.Tests.Builders;
using Backend.Tests.Helpers;
using Backend.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Backend.Tests.Services;

/// <summary>
/// Integration tests for AvatarService: validation, upsert, retrieval, Google download, batch checks.
/// </summary>
public class AvatarServiceTests(DatabaseFixture fixture)
    : IClassFixture<DatabaseFixture>,
        IAsyncLifetime
{
    private readonly DatabaseFixture _fixture = fixture;
    private BackendDbContext _ctx = null!;
    private AvatarService _sut = null!;
    private string _userId = null!;

    public async ValueTask InitializeAsync()
    {
        _ctx = _fixture.CreateDbContext();

        // AvatarService requires IHttpClientFactory for DownloadAvatarAsync
        var httpClientFactory = new ServiceCollection()
            .AddHttpClient()
            .BuildServiceProvider()
            .GetRequiredService<IHttpClientFactory>();

        _sut = new AvatarService(_ctx, httpClientFactory, NullLogger<AvatarService>.Instance);

        // Clear test data from previous runs
        await DbSeeder.ClearLeaderboardDataAsync(_ctx, _fixture.SystemUserId);

        // Create test user
        var user = new UserBuilder()
            .WithUserName("avatartest")
            .WithEmail("avatar@test.com")
            .Build();
        _ctx.Users.Add(user);
        _userId = user.Id;

        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        await _ctx.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    #region ValidateAvatarFile - Static Validation

    [Theory]
    [InlineData(".jpg")]
    [InlineData(".jpeg")]
    [InlineData(".png")]
    [InlineData(".gif")]
    [InlineData(".webp")]
    public void ValidateAvatarFile_ValidExtensions_ReturnsTrue(string extension)
    {
        // Arrange
        var file = CreateMockFile($"avatar{extension}", 500_000, "image/jpeg");

        // Act
        var (isValid, error) = AvatarService.ValidateAvatarFile(file.Object);

        // Assert
        isValid
            .Should()
            .BeTrue(because: $"{extension} is a supported image format for avatar uploads");
        error.Should().BeNull();
    }

    [Fact]
    public void ValidateAvatarFile_EmptyFile_ReturnsFalseWithError()
    {
        // Arrange
        var file = CreateMockFile("avatar.png", 0, "image/png");

        // Act
        var (isValid, error) = AvatarService.ValidateAvatarFile(file.Object);

        // Assert
        isValid.Should().BeFalse(because: "empty files cannot be valid avatars");
        error.Should().Be("File is empty");
    }

    [Fact]
    public void ValidateAvatarFile_ExceedsSizeLimit_ReturnsFalseWithError()
    {
        // Arrange - 1MB + 1 byte
        var file = CreateMockFile("avatar.png", 1 * 1024 * 1024 + 1, "image/png");

        // Act
        var (isValid, error) = AvatarService.ValidateAvatarFile(file.Object);

        // Assert
        isValid
            .Should()
            .BeFalse(because: "avatar file size limit is 1MB — prevents large binary bloat");
        error.Should().Contain("exceeds 1MB limit");
    }

    [Theory]
    [InlineData(".bmp")]
    [InlineData(".svg")]
    [InlineData(".tiff")]
    [InlineData(".pdf")]
    public void ValidateAvatarFile_UnsupportedExtension_ReturnsFalseWithError(string extension)
    {
        // Arrange
        var file = CreateMockFile($"avatar{extension}", 100_000, "application/octet-stream");

        // Act
        var (isValid, error) = AvatarService.ValidateAvatarFile(file.Object);

        // Assert
        isValid
            .Should()
            .BeFalse(because: $"{extension} is not in the allowed avatar formats list");
        error.Should().Contain("Invalid file type");
    }

    [Fact]
    public void ValidateAvatarFile_ExactSizeLimit_ReturnsTrue()
    {
        // Arrange - exactly 1MB
        var file = CreateMockFile("avatar.png", 1 * 1024 * 1024, "image/png");

        // Act
        var (isValid, error) = AvatarService.ValidateAvatarFile(file.Object);

        // Assert
        isValid.Should().BeTrue(because: "1MB is the maximum allowed size, not over the limit");
        error.Should().BeNull();
    }

    #endregion

    #region GetContentType - Static Mapping

    [Theory]
    [InlineData("avatar.jpg", "image/jpeg")]
    [InlineData("avatar.jpeg", "image/jpeg")]
    [InlineData("avatar.png", "image/png")]
    [InlineData("avatar.gif", "image/gif")]
    [InlineData("avatar.webp", "image/webp")]
    [InlineData("AVATAR.PNG", "image/png")]
    public void GetContentType_ValidExtension_ReturnsCorrectContentType(
        string filename,
        string expectedContentType
    )
    {
        // Arrange
        var file = CreateMockFile(filename, 100, "ignored");

        // Act
        var contentType = AvatarService.GetContentType(file.Object);

        // Assert
        contentType
            .Should()
            .Be(
                expectedContentType,
                because: "content type mapping should match file extension for proper MIME type serving"
            );
    }

    [Fact]
    public void GetContentType_UnknownExtension_ReturnsDefaultJpeg()
    {
        // Arrange
        var file = CreateMockFile("avatar.unknown", 100, "ignored");

        // Act
        var contentType = AvatarService.GetContentType(file.Object);

        // Assert
        contentType
            .Should()
            .Be(
                "image/jpeg",
                because: "unknown extensions should fall back to a safe default MIME type"
            );
    }

    #endregion

    #region UpsertAvatarAsync - Create and Update

    [Fact]
    public async Task UpsertAvatarAsync_NewAvatar_CreatesUserAvatar()
    {
        // Arrange
        var imageBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };

        // Act
        await _sut.UpsertAvatarAsync(_userId, imageBytes, "image/jpeg");

        // Assert - verify in database
        var avatar = await _ctx.UserAvatars.FindAsync(
            [_userId],
            TestContext.Current.CancellationToken
        );

        avatar.Should().NotBeNull(because: "upsert should create a new UserAvatar record");
        avatar!.UserId.Should().Be(_userId);
        avatar.Data.Should().Equal(imageBytes);
        avatar.ContentType.Should().Be("image/jpeg");
    }

    [Fact]
    public async Task UpsertAvatarAsync_ExistingAvatar_UpdatesData()
    {
        // Arrange - insert initial avatar
        await DbSeeder.AddAvatarAsync(_ctx, _userId);

        var originalAvatar = await _ctx.UserAvatars.FindAsync(
            [_userId],
            TestContext.Current.CancellationToken
        );
        var originalData = originalAvatar!.Data;

        var newImageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };

        // Act
        await _sut.UpsertAvatarAsync(_userId, newImageBytes, "image/png");

        // Assert - verify update
        _ctx.ChangeTracker.Clear();
        var updatedAvatar = await _ctx.UserAvatars.FindAsync(
            [_userId],
            TestContext.Current.CancellationToken
        );

        updatedAvatar.Should().NotBeNull();
        updatedAvatar!.Data.Should().NotEqual(originalData);
        updatedAvatar.Data.Should().Equal(newImageBytes);
        updatedAvatar
            .ContentType.Should()
            .Be("image/png", because: "upsert should update both data and content type");

        // Verify no duplicate record created
        var allAvatars = await _ctx
            .UserAvatars.Where(a => a.UserId == _userId)
            .ToListAsync(TestContext.Current.CancellationToken);
        allAvatars
            .Should()
            .HaveCount(1, because: "upsert should update existing record, not create a duplicate");
    }

    #endregion

    #region GetAvatarAsync - Retrieval

    [Fact]
    public async Task GetAvatarAsync_ExistingAvatar_ReturnsDataAndContentType()
    {
        // Arrange
        await DbSeeder.AddAvatarAsync(_ctx, _userId);

        // Act
        var (data, contentType) = await _sut.GetAvatarAsync(_userId);

        // Assert
        data.Should()
            .NotBeNull(because: "GetAvatarAsync should return binary data for an existing avatar");
        data!.Length.Should().BeGreaterThan(0);
        contentType.Should().Be("image/jpeg");
    }

    [Fact]
    public async Task GetAvatarAsync_NoAvatar_ReturnsNull()
    {
        // Arrange - user has no avatar

        // Act
        var (data, contentType) = await _sut.GetAvatarAsync(_userId);

        // Assert
        data.Should().BeNull(because: "user without avatar should return null data");
        contentType.Should().BeNull();
    }

    #endregion

    #region HasAvatarAsync - Existence Check

    [Fact]
    public async Task HasAvatarAsync_UserWithAvatar_ReturnsTrue()
    {
        // Arrange
        await DbSeeder.AddAvatarAsync(_ctx, _userId);

        // Act
        var hasAvatar = await _sut.HasAvatarAsync(_userId);

        // Assert
        hasAvatar
            .Should()
            .BeTrue(because: "HasAvatarAsync should return true when UserAvatar record exists");
    }

    [Fact]
    public async Task HasAvatarAsync_UserWithoutAvatar_ReturnsFalse()
    {
        // Arrange - user has no avatar

        // Act
        var hasAvatar = await _sut.HasAvatarAsync(_userId);

        // Assert
        hasAvatar
            .Should()
            .BeFalse(because: "user without avatar should return false from HasAvatarAsync");
    }

    #endregion

    #region GetUsersWithAvatarsAsync - Batch Check

    [Fact]
    public async Task GetUsersWithAvatarsAsync_ReturnsOnlyUsersWithAvatars()
    {
        // Arrange - create multiple users
        var user2 = new UserBuilder().WithUserName("user2").WithEmail("user2@test.com").Build();
        var user3 = new UserBuilder().WithUserName("user3").WithEmail("user3@test.com").Build();
        _ctx.Users.AddRange(user2, user3);
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Add avatar for user1 and user3 only
        await DbSeeder.AddAvatarAsync(_ctx, _userId);
        await DbSeeder.AddAvatarAsync(_ctx, user3.Id);

        // Act
        var usersWithAvatars = await _sut.GetUsersWithAvatarsAsync([_userId, user2.Id, user3.Id]);

        // Assert
        usersWithAvatars
            .Should()
            .HaveCount(
                2,
                because: "only 2 out of 3 users have avatars — batch check should filter correctly"
            );
        usersWithAvatars.Should().Contain(_userId);
        usersWithAvatars.Should().Contain(user3.Id);
        usersWithAvatars.Should().NotContain(user2.Id, because: "user2 does not have an avatar");
    }

    [Fact]
    public async Task GetUsersWithAvatarsAsync_EmptyList_ReturnsEmpty()
    {
        // Arrange
        var emptyList = new List<string>();

        // Act
        var usersWithAvatars = await _sut.GetUsersWithAvatarsAsync(emptyList);

        // Assert
        usersWithAvatars
            .Should()
            .BeEmpty(because: "querying with an empty user list should return empty set");
    }

    [Fact]
    public async Task GetUsersWithAvatarsAsync_NoAvatars_ReturnsEmpty()
    {
        // Arrange - create users without avatars
        var user2 = new UserBuilder().WithUserName("user2").WithEmail("user2@test.com").Build();
        _ctx.Users.Add(user2);
        await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);

        // Act
        var usersWithAvatars = await _sut.GetUsersWithAvatarsAsync([_userId, user2.Id]);

        // Assert
        usersWithAvatars
            .Should()
            .BeEmpty(because: "none of the users have avatars, so batch check returns empty");
    }

    #endregion

    #region DownloadAvatarAsync - Google URL Download

    [Fact]
    public async Task DownloadAvatarAsync_InvalidUrl_ReturnsNull()
    {
        // Arrange
        var invalidUrl = "not-a-url";

        // Act
        var (data, contentType) = await _sut.DownloadAvatarAsync(invalidUrl);

        // Assert
        data.Should()
            .BeNull(
                because: "DownloadAvatarAsync should handle invalid URLs gracefully without throwing"
            );
        contentType.Should().BeNull();
    }

    [Fact]
    public async Task DownloadAvatarAsync_UnreachableUrl_ReturnsNull()
    {
        // Arrange - domain that won't resolve
        var unreachableUrl = "https://nonexistent.invalid.domain.test/avatar.jpg";

        // Act
        var (data, contentType) = await _sut.DownloadAvatarAsync(unreachableUrl);

        // Assert
        data.Should()
            .BeNull(
                because: "DownloadAvatarAsync should return null when HTTP request fails instead of throwing"
            );
        contentType.Should().BeNull();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a mock IFormFile for validation tests.
    /// </summary>
    private static Mock<IFormFile> CreateMockFile(string filename, long length, string contentType)
    {
        var mock = new Mock<IFormFile>();
        mock.Setup(f => f.FileName).Returns(filename);
        mock.Setup(f => f.Length).Returns(length);
        mock.Setup(f => f.ContentType).Returns(contentType);

        // Mock CopyToAsync for upload tests
        mock.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        return mock;
    }

    #endregion
}
