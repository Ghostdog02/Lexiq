using Backend.Api.Services;
using Backend.Database;
using Backend.Tests.Helpers;
using Backend.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Backend.Tests.Integration.Services;

/// <summary>
/// Integration tests for FileUploadsService.DeleteOrphanedAudioAsync.
///
/// Verifies that files not referenced by any DB exercise are deleted, referenced files kept,
/// young files (within grace window) are kept, and failures on individual files do not abort the scan.
/// </summary>
public class DeleteOrphanedAudioTests(DatabaseFixture fixture)
    : IClassFixture<DatabaseFixture>,
        IAsyncLifetime
{
    private readonly DatabaseFixture _fixture = fixture;
    private BackendDbContext _ctx = null!;
    private FakeClock _clock = null!;
    private FileUploadsService _sut = null!;
    private string _audioFolder = null!;

    public async ValueTask InitializeAsync()
    {
        _ctx = _fixture.CreateDbContext();
        await DbSeeder.ClearLeaderboardDataAsync(_ctx, _fixture.SystemUserId);

        _clock = new FakeClock();

        // Temp directory as wwwroot
        var root = Path.Combine(Path.GetTempPath(), "lex-orphan-" + Guid.NewGuid().ToString("N"));
        _audioFolder = Path.Combine(root, "uploads", "audio");
        Directory.CreateDirectory(_audioFolder);

        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.WebRootPath).Returns(root);
        mockEnv.Setup(e => e.ContentRootPath).Returns(root);

        _sut = new FileUploadsService(
            mockEnv.Object,
            NullLogger<FileUploadsService>.Instance,
            _ctx,
            _clock
        );
    }

    public async ValueTask DisposeAsync()
    {
        var root = Path.GetDirectoryName(_audioFolder)!;
        var rootParent = Path.GetDirectoryName(root)!;
        if (Directory.Exists(rootParent))
            try { Directory.Delete(rootParent, recursive: true); } catch { /* best effort */ }

        await _ctx.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    private string WriteFile(string filename, DateTime? creationTime = null)
    {
        var path = Path.Combine(_audioFolder, filename);
        File.WriteAllBytes(path, [0x00, 0x01]);
        if (creationTime.HasValue)
        {
            var fi = new FileInfo(path);
            fi.CreationTimeUtc = creationTime.Value;
        }
        return path;
    }

    [Fact]
    public async Task FileWithNoDbReference_IsDeleted()
    {
        // Arrange
        var orphanPath = WriteFile("orphan.mp3", _clock.UtcNow.AddHours(-2));

        // Act
        var deleted = await _sut.DeleteOrphanedAudioAsync(TimeSpan.FromHours(1));

        // Assert
        deleted.Should().Be(1);
        File.Exists(orphanPath).Should().BeFalse();
    }

    [Fact]
    public async Task FileReferencedByListeningExercise_IsKept()
    {
        // Arrange — seed a ListeningExercise whose AudioUrl maps to the file
        var filename = $"{Guid.NewGuid()}.mp3";
        var audioUrl = $"/api/uploads/audio/{filename}";
        var filePath = WriteFile(filename, _clock.UtcNow.AddHours(-2));

        await DbSeeder.SeedListeningExerciseAsync(_ctx, _fixture.LessonId, audioUrl);

        // Act
        var deleted = await _sut.DeleteOrphanedAudioAsync(TimeSpan.FromHours(1));

        // Assert
        deleted.Should().Be(0);
        File.Exists(filePath).Should().BeTrue();
    }

    [Fact]
    public async Task FileReferencedByAudioMatchPair_IsKept()
    {
        // Arrange
        var filename = $"{Guid.NewGuid()}.mp3";
        var audioUrl = $"/api/uploads/audio/{filename}";
        var filePath = WriteFile(filename, _clock.UtcNow.AddHours(-2));

        await DbSeeder.SeedAudioMatchingExerciseAsync(
            _ctx, _fixture.LessonId,
            [
                new DbSeeder.AudioMatchPairData(audioUrl, "/img/1.jpg", true, "OK"),
                new DbSeeder.AudioMatchPairData("/api/uploads/audio/other.mp3", "/img/2.jpg", false, "No"),
            ]
        );

        // Act
        var deleted = await _sut.DeleteOrphanedAudioAsync(TimeSpan.FromHours(1));

        // Assert
        File.Exists(filePath).Should().BeTrue(because: "file is referenced by an AudioMatchPair");
    }

    [Fact]
    public async Task MultipleOrphans_AllDeletedInOnePass()
    {
        // Arrange
        var paths = new List<string>();
        for (var i = 0; i < 3; i++)
            paths.Add(WriteFile($"orphan{i}.mp3", _clock.UtcNow.AddHours(-2)));

        // Act
        var deleted = await _sut.DeleteOrphanedAudioAsync(TimeSpan.FromHours(1));

        // Assert
        deleted.Should().Be(3);
        paths.Should().AllSatisfy(p => File.Exists(p).Should().BeFalse());
    }

    [Fact]
    public async Task FileYoungerThanGraceWindow_IsKept()
    {
        // Arrange — creation time is within grace window (30 min ago, window = 1 h)
        var filePath = WriteFile("young.mp3", _clock.UtcNow.AddMinutes(-30));

        // Act
        var deleted = await _sut.DeleteOrphanedAudioAsync(TimeSpan.FromHours(1));

        // Assert
        deleted.Should().Be(0);
        File.Exists(filePath).Should().BeTrue(because: "file is newer than grace window");
    }

    [Fact]
    public async Task EmptyAudioFolder_NoException_ZeroDeleted()
    {
        // Arrange — folder exists but is empty (no files created)

        // Act
        Func<Task> act = () => _sut.DeleteOrphanedAudioAsync(TimeSpan.FromHours(1));

        // Assert
        await act.Should().NotThrowAsync();
        var deleted = await _sut.DeleteOrphanedAudioAsync(TimeSpan.FromHours(1));
        deleted.Should().Be(0);
    }

    [Fact]
    public async Task MixOfOrphansAndReferenced_OnlyOrphansDeleted()
    {
        // Arrange
        var referencedFilename = $"{Guid.NewGuid()}.mp3";
        var referencedUrl = $"/api/uploads/audio/{referencedFilename}";
        var referencedPath = WriteFile(referencedFilename, _clock.UtcNow.AddHours(-2));
        var orphanPath = WriteFile("orphan-mixed.mp3", _clock.UtcNow.AddHours(-2));

        await DbSeeder.SeedListeningExerciseAsync(_ctx, _fixture.LessonId, referencedUrl);

        // Act
        var deleted = await _sut.DeleteOrphanedAudioAsync(TimeSpan.FromHours(1));

        // Assert
        deleted.Should().Be(1);
        File.Exists(referencedPath).Should().BeTrue();
        File.Exists(orphanPath).Should().BeFalse();
    }
}
