using System.Net;
using System.Net.Http.Json;
using Backend.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Backend.Tests.Controllers;

/// <summary>
/// E2E tests for avatar upload and retrieval via UserController endpoints.
///
/// Covers:
///   - PUT /api/user/avatar file validation, upsert behavior, authentication
///   - GET /api/user/{userId}/avatar binary retrieval, 404 handling
/// </summary>
public class UserAvatarUploadTests(DatabaseFixture fixture)
    : ControllerTestBase(fixture),
        IClassFixture<DatabaseFixture>
{
    private HttpClient _client = null!;
    private string _userId = null!;

    public override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();
        await ClearTestDataAsync();

        var (userId, token) = await CreateAuthenticatedUserAsync(
            "avataruser",
            "avatar@test.com",
            "Student"
        );
        _userId = userId;
        _client = CreateClient(token);
    }

    public override async ValueTask DisposeAsync()
    {
        _client.Dispose();
        await base.DisposeAsync();
    }

    #region PUT /api/user/avatar - Upload

    [Fact]
    public async Task PutAvatar_ValidImage_CreatesNewAvatar()
    {
        // Arrange
        var imageBytes = CreateTestImageBytes();
        var content = CreateMultipartFormData(imageBytes, "avatar.jpg", "image/jpeg");

        // Act
        var response = await _client.PutAsync(
            "/api/user/avatar",
            content,
            TestContext.Current.CancellationToken
        );

        // Assert
        response
            .StatusCode.Should()
            .Be(
                HttpStatusCode.OK,
                because: "valid avatar upload should succeed for authenticated user"
            );

        var responseBody = await response
            .Content.ReadFromJsonAsync<AvatarUploadResponse>(
                cancellationToken: TestContext.Current.CancellationToken
            );
        responseBody.Should().NotBeNull();
        responseBody!
            .AvatarUrl.Should()
            .Be(
                $"/api/user/{_userId}/avatar",
                because: "response should return the GET endpoint for the newly uploaded avatar"
            );

        // Verify avatar is retrievable
        var getResponse = await _client.GetAsync(
            responseBody.AvatarUrl,
            TestContext.Current.CancellationToken
        );
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var retrievedBytes = await getResponse.Content.ReadAsByteArrayAsync(
            TestContext.Current.CancellationToken
        );
        retrievedBytes
            .Should()
            .Equal(imageBytes, because: "uploaded avatar should be stored and retrievable");
    }

    [Fact]
    public async Task PutAvatar_ValidImage_UpdatesExistingAvatar()
    {
        // Arrange - upload initial avatar
        var initialImageBytes = CreateTestImageBytes();
        var initialContent = CreateMultipartFormData(
            initialImageBytes,
            "initial.jpg",
            "image/jpeg"
        );
        await _client.PutAsync(
            "/api/user/avatar",
            initialContent,
            TestContext.Current.CancellationToken
        );

        // Act - upload replacement avatar
        var newImageBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var newContent = CreateMultipartFormData(newImageBytes, "updated.png", "image/png");
        var response = await _client.PutAsync(
            "/api/user/avatar",
            newContent,
            TestContext.Current.CancellationToken
        );

        // Assert
        response
            .StatusCode.Should()
            .Be(
                HttpStatusCode.OK,
                because: "uploading a new avatar should update existing record"
            );

        // Verify the new avatar replaced the old one
        var getResponse = await _client.GetAsync(
            $"/api/user/{_userId}/avatar",
            TestContext.Current.CancellationToken
        );
        var retrievedBytes = await getResponse.Content.ReadAsByteArrayAsync(
            TestContext.Current.CancellationToken
        );
        retrievedBytes
            .Should()
            .Equal(
                newImageBytes,
                because: "upsert should replace the previous avatar with the new upload"
            );
        retrievedBytes
            .Should()
            .NotEqual(initialImageBytes, because: "old avatar should be replaced, not kept");
    }

    [Fact]
    public async Task PutAvatar_ExceedsSizeLimit_Returns400()
    {
        // Arrange - 1MB + 1 byte
        var oversizedImage = new byte[1 * 1024 * 1024 + 1];
        var content = CreateMultipartFormData(oversizedImage, "large.jpg", "image/jpeg");

        // Act
        var response = await _client.PutAsync(
            "/api/user/avatar",
            content,
            TestContext.Current.CancellationToken
        );

        // Assert
        response
            .StatusCode.Should()
            .Be(
                HttpStatusCode.BadRequest,
                because: "avatar exceeding 1MB size limit should be rejected"
            );
    }

    [Fact]
    public async Task PutAvatar_InvalidFileType_Returns400()
    {
        // Arrange - unsupported file type (.bmp)
        var imageBytes = new byte[] { 0x42, 0x4D };
        var content = CreateMultipartFormData(imageBytes, "avatar.bmp", "image/bmp");

        // Act
        var response = await _client.PutAsync(
            "/api/user/avatar",
            content,
            TestContext.Current.CancellationToken
        );

        // Assert
        response
            .StatusCode.Should()
            .Be(
                HttpStatusCode.BadRequest,
                because: ".bmp is not in the allowed avatar file types (jpg, jpeg, png, gif, webp)"
            );
    }

    [Fact]
    public async Task PutAvatar_EmptyFile_Returns400()
    {
        // Arrange
        var emptyBytes = Array.Empty<byte>();
        var content = CreateMultipartFormData(emptyBytes, "empty.jpg", "image/jpeg");

        // Act
        var response = await _client.PutAsync(
            "/api/user/avatar",
            content,
            TestContext.Current.CancellationToken
        );

        // Assert
        response
            .StatusCode.Should()
            .Be(
                HttpStatusCode.BadRequest,
                because: "empty files cannot be valid avatars and should be rejected"
            );
    }

    [Fact]
    public async Task PutAvatar_NoAuthToken_Returns401()
    {
        // Arrange - client without authentication
        var unauthenticatedClient = CreateClient();
        var imageBytes = CreateTestImageBytes();
        var content = CreateMultipartFormData(imageBytes, "avatar.jpg", "image/jpeg");

        // Act
        var response = await unauthenticatedClient.PutAsync(
            "/api/user/avatar",
            content,
            TestContext.Current.CancellationToken
        );

        // Assert
        response
            .StatusCode.Should()
            .Be(
                HttpStatusCode.Unauthorized,
                because: "PUT /api/user/avatar requires authentication via JWT cookie"
            );
    }

    [Theory]
    [InlineData("avatar.jpg", "image/jpeg")]
    [InlineData("avatar.jpeg", "image/jpeg")]
    [InlineData("avatar.png", "image/png")]
    [InlineData("avatar.gif", "image/gif")]
    [InlineData("avatar.webp", "image/webp")]
    public async Task PutAvatar_AllowedFileTypes_Returns200(string filename, string contentType)
    {
        // Arrange
        var imageBytes = CreateTestImageBytes();
        var content = CreateMultipartFormData(imageBytes, filename, contentType);

        // Act
        var response = await _client.PutAsync(
            "/api/user/avatar",
            content,
            TestContext.Current.CancellationToken
        );

        // Assert
        response
            .StatusCode.Should()
            .Be(HttpStatusCode.OK, because: $"{filename} is a valid avatar file type");
    }

    #endregion

    #region GET /api/user/{userId}/avatar - Retrieval

    [Fact]
    public async Task GetAvatar_ExistingAvatar_Returns200WithImageBytes()
    {
        // Arrange - upload avatar first
        var imageBytes = CreateTestImageBytes();
        var uploadContent = CreateMultipartFormData(imageBytes, "avatar.jpg", "image/jpeg");
        await _client.PutAsync(
            "/api/user/avatar",
            uploadContent,
            TestContext.Current.CancellationToken
        );

        // Act
        var response = await _client.GetAsync(
            $"/api/user/{_userId}/avatar",
            TestContext.Current.CancellationToken
        );

        // Assert
        response
            .StatusCode.Should()
            .Be(HttpStatusCode.OK, because: "existing avatar should be retrievable");
        response
            .Content.Headers.ContentType!
            .MediaType.Should()
            .Be(
                "image/jpeg",
                because: "content type should match the uploaded avatar's MIME type"
            );

        var retrievedBytes = await response.Content.ReadAsByteArrayAsync(
            TestContext.Current.CancellationToken
        );
        retrievedBytes
            .Should()
            .Equal(imageBytes, because: "GET endpoint should return the exact binary data");

        // Verify Cache-Control header is set
        response
            .Headers.CacheControl!
            .Public.Should()
            .BeTrue(because: "avatars should be publicly cacheable for performance");
        response
            .Headers.CacheControl!
            .MaxAge.Should()
            .Be(
                TimeSpan.FromDays(1),
                because: "Cache-Control header should set 24-hour expiry"
            );
    }

    [Fact]
    public async Task GetAvatar_NoAvatar_Returns404()
    {
        // Arrange - user has no avatar

        // Act
        var response = await _client.GetAsync(
            $"/api/user/{_userId}/avatar",
            TestContext.Current.CancellationToken
        );

        // Assert
        response
            .StatusCode.Should()
            .Be(HttpStatusCode.NotFound, because: "user without avatar should return 404");
    }

    [Fact]
    public async Task GetAvatar_NonexistentUser_Returns404()
    {
        // Arrange
        var nonexistentUserId = Guid.NewGuid().ToString();

        // Act
        var response = await _client.GetAsync(
            $"/api/user/{nonexistentUserId}/avatar",
            TestContext.Current.CancellationToken
        );

        // Assert
        response
            .StatusCode.Should()
            .Be(
                HttpStatusCode.NotFound,
                because: "requesting avatar for a nonexistent user should return 404"
            );
    }

    [Fact]
    public async Task GetAvatar_NoAuthToken_Returns401()
    {
        // Arrange - client without authentication
        var unauthenticatedClient = CreateClient();

        // Act
        var response = await unauthenticatedClient.GetAsync(
            $"/api/user/{_userId}/avatar",
            TestContext.Current.CancellationToken
        );

        // Assert
        response
            .StatusCode.Should()
            .Be(
                HttpStatusCode.Unauthorized,
                because: "GET /api/user/{userId}/avatar requires authentication"
            );
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a minimal valid JPEG image (2 bytes - JPEG magic number).
    /// </summary>
    private static byte[] CreateTestImageBytes() => [0xFF, 0xD8];

    /// <summary>
    /// Creates multipart/form-data content for file upload.
    /// </summary>
    private static MultipartFormDataContent CreateMultipartFormData(
        byte[] fileBytes,
        string filename,
        string contentType
    )
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
            contentType
        );

        // Parameter name MUST match controller action parameter name
        content.Add(fileContent, "file", filename);
        return content;
    }

    /// <summary>
    /// DTO matching the shape of PUT /api/user/avatar response.
    /// </summary>
    private record AvatarUploadResponse(string AvatarUrl);

    #endregion
}
