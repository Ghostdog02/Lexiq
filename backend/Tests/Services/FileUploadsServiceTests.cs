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
        _testUploadPath = Path.Combine(Path.GetTempPath(), "lexiq-test-uploads", Guid.NewGuid().ToString());
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
