using Backend.Api.Models;
using Backend.Api.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Backend.Tests.Services;

/// <summary>
/// Unit tests for FileUploadsService security, validation, and file handling.
///
/// Verifies:
///   - Path traversal attack prevention
///   - Filename sanitization
///   - File type and size validation per category
///   - Upload by URL validation
///   - Physical path security checks
/// </summary>
public class FileUploadsServiceTests : IAsyncLifetime
{
    private readonly string _testUploadPath;
    private FileUploadsService _sut = null!;

    public FileUploadsServiceTests()
    {
        // Use a temp directory for test uploads
        _testUploadPath = Path.Combine(
            Path.GetTempPath(),
            "lexiq-test-uploads",
            Guid.NewGuid().ToString()
        );
    }

    public async ValueTask InitializeAsync()
    {
        // Create test upload directory structure
        Directory.CreateDirectory(_testUploadPath);

        // Mock IWebHostEnvironment
        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.WebRootPath).Returns(_testUploadPath);
        mockEnv.Setup(e => e.ContentRootPath).Returns(_testUploadPath);

        _sut = new FileUploadsService(mockEnv.Object);

        await Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        // Clean up test directory
        if (Directory.Exists(_testUploadPath))
        {
            try
            {
                Directory.Delete(_testUploadPath, recursive: true);
            }
            
            catch
            {
                // Best effort cleanup - ignore errors
            }
        }

        await Task.CompletedTask;
        GC.SuppressFinalize(this);
    }

    // ── Security Tests ──────────────────────────────────────────────────────

    [Fact]
    public async Task UploadFileAsync_PathTraversalInFilename_Stripped()
    {
        // Arrange - Path.GetFileName() automatically strips directory components
        var file = CreateMockFile("../../../etc/passwd.png", 100, "image/png");

        // Act
        var result = await _sut.UploadFileAsync(file.Object, "image", "http://test");

        // Assert
        result
            .IsSuccess.Should()
            .BeTrue(
                because: "Path.GetFileName('../../../etc/passwd.png') returns 'passwd.png', stripping path traversal sequences — files are saved with GUID names anyway"
            );
        result
            .Name.Should()
            .Be("passwd.png", because: "SanitizeFilename returns Path.GetFileName result");
    }

    [Fact]
    public async Task UploadFileAsync_FilenameWithEmbeddedDots_Rejects()
    {
        // Arrange - filename itself contains ".." after path stripping
        var maliciousFile = CreateMockFile("file..name.png", 100, "image/png");

        // Act
        var result = await _sut.UploadFileAsync(maliciousFile.Object, "image", "http://test");

        // Assert
        result
            .IsSuccess.Should()
            .BeFalse(
                because: "SanitizeFilename rejects filenames containing '..' even after Path.GetFileName() to prevent obfuscated path traversal"
            );
        result.Message.Should().Contain("Invalid filename");
    }

    [Fact]
    public async Task UploadFileAsync_SlashInFilename_Stripped()
    {
        // Arrange - Path.GetFileName() treats "/" as path separator and strips it
        var file = CreateMockFile("directory/malicious.png", 100, "image/png");

        // Act
        var result = await _sut.UploadFileAsync(file.Object, "image", "http://test");

        // Assert
        result
            .IsSuccess.Should()
            .BeTrue(
                because: "Path.GetFileName('directory/malicious.png') returns 'malicious.png' on Linux"
            );
        result.Name.Should().Be("malicious.png");
    }

    [Fact]
    public async Task UploadFileAsync_FilenameWithEmbeddedBackslash_Rejects()
    {
        // Arrange - filename itself contains "\\" after path stripping
        var maliciousFile = CreateMockFile("file\\name.png", 100, "image/png");

        // Act
        var result = await _sut.UploadFileAsync(maliciousFile.Object, "image", "http://test");

        // Assert
        result
            .IsSuccess.Should()
            .BeFalse(
                because: "SanitizeFilename rejects filenames containing '\\' even after Path.GetFileName() to prevent path manipulation"
            );
        result.Message.Should().Contain("Invalid filename");
    }

    [Fact]
    public async Task UploadFileAsync_GuidFilenames_PreventDirectoryEscape()
    {
        // Arrange
        var file = CreateMockFile("user-provided-name.png", 100, "image/png");

        // Act
        var result = await _sut.UploadFileAsync(file.Object, "image", "http://test");

        // Assert
        result.IsSuccess.Should().BeTrue();
        var savedFilename = Path.GetFileName(result.Url);
        savedFilename
            .Should()
            .NotBe(
                "user-provided-name.png",
                because: "service generates GUID filenames to prevent any filename-based attacks"
            );
        Guid.TryParse(Path.GetFileNameWithoutExtension(savedFilename), out _)
            .Should()
            .BeTrue(because: "saved filename is a GUID");
    }

    [Fact]
    public async Task GetFilePhysicalPath_PathTraversalAttempt_ReturnsNull()
    {
        // Arrange
        var safeFilename = "test.png";
        await CreateTestFileAsync("images", safeFilename, 100);

        // Act - try to escape uploads directory with path traversal
        var result = _sut.GetFilePhysicalPath("../../../etc/passwd", "image");

        // Assert
        result
            .Should()
            .BeNull(
                because: "IsPathWithinUploadsDirectory must prevent path traversal to access files outside uploads folder"
            );
    }

    [Fact]
    public async Task GetFileByFilenameAsync_PathTraversalAttempt_ReturnsFalse()
    {
        // Arrange
        var safeFilename = "test.png";
        await CreateTestFileAsync("images", safeFilename, 100);

        // Act
        var result = await _sut.GetFileByFilenameAsync(
            "../../../etc/passwd",
            "image",
            "http://test"
        );

        // Assert
        result
            .IsSuccess.Should()
            .BeFalse(
                because: "GetFileByFilenameAsync must reject path traversal attempts via IsPathWithinUploadsDirectory check"
            );
    }

    // ── Validation Tests ────────────────────────────────────────────────────

    [Fact]
    public async Task UploadFileAsync_NullFile_ReturnsFailure()
    {
        // Arrange
        IFormFile? nullFile = null;

        // Act
        var result = await _sut.UploadFileAsync(nullFile!, "image", "http://test");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result
            .Message.Should()
            .Be("No file uploaded", because: "null file is rejected at the start of validation");
    }

    [Fact]
    public async Task UploadFileAsync_EmptyFile_ReturnsFailure()
    {
        // Arrange
        var emptyFile = CreateMockFile("empty.png", 0, "image/png");

        // Act
        var result = await _sut.UploadFileAsync(emptyFile.Object, "image", "http://test");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result
            .Message.Should()
            .Be(
                "No file uploaded",
                because: "zero-length files are rejected (file.Length == 0 check)"
            );
    }

    [Fact]
    public async Task UploadFileAsync_InvalidFileType_ReturnsFailure()
    {
        // Arrange
        var file = CreateMockFile("test.png", 100, "image/png");

        // Act
        var result = await _sut.UploadFileAsync(file.Object, "invalid-type", "http://test");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result
            .Message.Should()
            .Be("Invalid file type", because: "fileType must match a key in _fileTypeConfigs");
    }

    [Fact]
    public async Task UploadFileAsync_ImageExceedsSizeLimit_ReturnsFailure()
    {
        // Arrange - image limit is 5MB
        var oversizedFile = CreateMockFile("large.png", 6 * 1024 * 1024, "image/png");

        // Act
        var result = await _sut.UploadFileAsync(oversizedFile.Object, "image", "http://test");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result
            .Message.Should()
            .Contain(
                "File size exceeds limit of 5MB",
                because: "image file type has 5MB max size in config"
            );
    }

    [Fact]
    public async Task UploadFileAsync_DocumentExceedsSizeLimit_ReturnsFailure()
    {
        // Arrange - document limit is 10MB
        var oversizedFile = CreateMockFile("large.pdf", 11 * 1024 * 1024, "application/pdf");

        // Act
        var result = await _sut.UploadFileAsync(oversizedFile.Object, "document", "http://test");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("File size exceeds limit of 10MB");
    }

    [Fact]
    public async Task UploadFileAsync_VideoExceedsSizeLimit_ReturnsFailure()
    {
        // Arrange - video limit is 50MB
        var oversizedFile = CreateMockFile("large.mp4", 51 * 1024 * 1024, "video/mp4");

        // Act
        var result = await _sut.UploadFileAsync(oversizedFile.Object, "video", "http://test");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("File size exceeds limit of 50MB");
    }

    [Fact]
    public async Task UploadFileAsync_AudioExceedsSizeLimit_ReturnsFailure()
    {
        // Arrange - audio limit is 10MB
        var oversizedFile = CreateMockFile("large.mp3", 11 * 1024 * 1024, "audio/mpeg");

        // Act
        var result = await _sut.UploadFileAsync(oversizedFile.Object, "audio", "http://test");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("File size exceeds limit of 10MB");
    }

    [Fact]
    public async Task UploadFileAsync_InvalidImageExtension_ReturnsFailure()
    {
        // Arrange
        var file = CreateMockFile("document.pdf", 100, "application/pdf");

        // Act
        var result = await _sut.UploadFileAsync(file.Object, "image", "http://test");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result
            .Message.Should()
            .Contain(
                "Invalid file type",
                because: ".pdf is not in the allowed extensions for image file type"
            );
        result
            .Message.Should()
            .Contain(
                ".jpg, .jpeg, .png, .gif, .webp, .svg, .bmp",
                because: "error message lists allowed extensions"
            );
    }

    [Fact]
    public async Task UploadFileAsync_InvalidDocumentExtension_ReturnsFailure()
    {
        // Arrange
        var file = CreateMockFile("image.png", 100, "image/png");

        // Act
        var result = await _sut.UploadFileAsync(file.Object, "document", "http://test");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Contain("Invalid file type");
        result
            .Message.Should()
            .Contain(
                ".pdf, .doc, .docx, .xls, .xlsx, .ppt, .pptx, .txt",
                because: "document allowed extensions list"
            );
    }

    // ── Helper Methods ──────────────────────────────────────────────────────

    private static Mock<IFormFile> CreateMockFile(string filename, long length, string contentType)
    {
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns(filename);
        fileMock.Setup(f => f.Length).Returns(length);
        fileMock.Setup(f => f.ContentType).Returns(contentType);

        // Setup CopyToAsync to write dummy bytes
        fileMock
            .Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Returns(
                (Stream stream, CancellationToken ct) =>
                {
                    var dummyData = new byte[length];
                    return stream.WriteAsync(dummyData, 0, (int)length, ct);
                }
            );

        return fileMock;
    }

    private async Task CreateTestFileAsync(string subfolder, string filename, long size)
    {
        var folderPath = Path.Combine(_testUploadPath, "uploads", subfolder);
        Directory.CreateDirectory(folderPath);

        var filePath = Path.Combine(folderPath, filename);
        var dummyData = new byte[size];
        await File.WriteAllBytesAsync(filePath, dummyData, TestContext.Current.CancellationToken);
    }
}
