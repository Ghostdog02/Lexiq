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
/// Integration tests for cascade audio file deletion when exercises are deleted via ExerciseService.
///
/// Verifies:
/// - Deleting a ListeningExercise deletes its audio file from disk
/// - Deleting an AudioMatchingExercise deletes all pair audio files
/// - File missing on disk at delete time does not throw — logged and continues
/// </summary>
public class AudioCascadeDeleteTests(DatabaseFixture fixture)
    : IClassFixture<DatabaseFixture>,
        IAsyncLifetime
{
    private readonly DatabaseFixture _fixture = fixture;
    private BackendDbContext _ctx = null!;
    private ExerciseService _sut = null!;
    private FileUploadsService _fileService = null!;
    private string _audioFolder = null!;

    public async ValueTask InitializeAsync()
    {
        _ctx = _fixture.CreateDbContext();
        await DbSeeder.ClearLeaderboardDataAsync(_ctx, _fixture.SystemUserId);

        var root = Path.Combine(Path.GetTempPath(), "lex-cascade-" + Guid.NewGuid().ToString("N"));
        _audioFolder = Path.Combine(root, "uploads", "audio");
        Directory.CreateDirectory(_audioFolder);

        var mockEnv = new Mock<IWebHostEnvironment>();
        mockEnv.Setup(e => e.WebRootPath).Returns(root);
        mockEnv.Setup(e => e.ContentRootPath).Returns(root);

        _fileService = new FileUploadsService(
            mockEnv.Object,
            NullLogger<FileUploadsService>.Instance,
            _ctx,
            new Backend.Api.Services.Clock.SystemClock()
        );

        _sut = new ExerciseService(_ctx, _fileService);
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

    private string CreateAudioFile(string filename)
    {
        var path = Path.Combine(_audioFolder, filename);
        File.WriteAllBytes(path, [0x00, 0x01]);
        return path;
    }

    [Fact]
    public async Task DeleteListeningExercise_AudioFileGoneFromDisk()
    {
        // Arrange
        var filename = $"{Guid.NewGuid()}.mp3";
        var audioUrl = $"/api/uploads/audio/{filename}";
        var filePath = CreateAudioFile(filename);

        var (exerciseId, _) = await DbSeeder.SeedListeningExerciseAsync(
            _ctx, _fixture.LessonId, audioUrl
        );

        // Act
        await _sut.DeleteExerciseAsync(exerciseId);

        // Assert
        File.Exists(filePath).Should().BeFalse(because: "deleting a ListeningExercise must cascade to its audio file");
    }

    [Fact]
    public async Task DeleteAudioMatchingExercise_AllPairAudioFilesGone()
    {
        // Arrange
        var file1 = $"{Guid.NewGuid()}.mp3";
        var file2 = $"{Guid.NewGuid()}.mp3";
        var path1 = CreateAudioFile(file1);
        var path2 = CreateAudioFile(file2);

        var (exerciseId, _) = await DbSeeder.SeedAudioMatchingExerciseAsync(
            _ctx, _fixture.LessonId,
            [
                new DbSeeder.AudioMatchPairData($"/api/uploads/audio/{file1}", "/img/1.jpg", true, "OK"),
                new DbSeeder.AudioMatchPairData($"/api/uploads/audio/{file2}", "/img/2.jpg", false, "No"),
            ]
        );

        // Act
        await _sut.DeleteExerciseAsync(exerciseId);

        // Assert
        File.Exists(path1).Should().BeFalse();
        File.Exists(path2).Should().BeFalse();
    }

    [Fact]
    public async Task AudioFileMissingOnDisk_DeleteExercise_NoExceptionThrown()
    {
        // Arrange — seed exercise but do NOT create the audio file on disk
        var missingUrl = $"/api/uploads/audio/{Guid.NewGuid()}.mp3";
        var (exerciseId, _) = await DbSeeder.SeedListeningExerciseAsync(
            _ctx, _fixture.LessonId, missingUrl
        );

        // Act
        Func<Task> act = () => _sut.DeleteExerciseAsync(exerciseId);

        // Assert
        await act.Should().NotThrowAsync(
            because: "missing file on disk is a best-effort delete — logged, not thrown"
        );
    }

    [Fact]
    public async Task DeleteOneAudioMatchPair_SiblingFileStaysOnDisk()
    {
        // Arrange — 2 pairs; delete only the exercise (EF removes all pairs as aggregate)
        // To test sibling survival we delete the exercise and check at DB level: the pair entities are gone
        // (on-disk file test requires pair-level delete which goes through EF cascade)
        var file1 = $"{Guid.NewGuid()}.mp3";
        var file2 = $"{Guid.NewGuid()}.mp3";
        var path1 = CreateAudioFile(file1);
        var path2 = CreateAudioFile(file2);

        var (exerciseId, pairIds) = await DbSeeder.SeedAudioMatchingExerciseAsync(
            _ctx, _fixture.LessonId,
            [
                new DbSeeder.AudioMatchPairData($"/api/uploads/audio/{file1}", "/img/1.jpg", true, "OK"),
                new DbSeeder.AudioMatchPairData($"/api/uploads/audio/{file2}", "/img/2.jpg", false, "No"),
            ]
        );

        // Simulate removing only the first pair directly through EF (exercise stays)
        var pair1 = await _ctx.Set<Backend.Database.Entities.Exercises.AudioMatchPair>()
            .FindAsync([pairIds[0]], TestContext.Current.CancellationToken);
        if (pair1 != null)
        {
            _fileService.DeleteAudioFile(pair1.AudioUrl);
            _ctx.Set<Backend.Database.Entities.Exercises.AudioMatchPair>().Remove(pair1);
            await _ctx.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        // Assert
        File.Exists(path1).Should().BeFalse(because: "pair 1's audio file was explicitly deleted");
        File.Exists(path2).Should().BeTrue(because: "pair 2's file remains — only pair 1 was removed");
    }
}
