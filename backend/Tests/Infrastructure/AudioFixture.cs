using Microsoft.AspNetCore.Http;

namespace Backend.Tests.Infrastructure;

/// <summary>
/// Provides minimal valid audio byte arrays for unit / integration test uploads.
/// Each array is the smallest well-formed header for the given format.
/// </summary>
public static class AudioFixture
{
    // Minimal ID3v2 + MPEG frame header — recognized by most parsers as MP3
    public static readonly byte[] ValidMp3 = [
        0x49, 0x44, 0x33, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // ID3v2.3 header
        0xFF, 0xFB, 0x90, 0x00, 0x00, 0x00, 0x00, 0x00,              // MPEG frame sync
    ];

    // RIFF/WAVE header with a trivial 8-bit PCM chunk
    public static readonly byte[] ValidWav = [
        0x52, 0x49, 0x46, 0x46, // "RIFF"
        0x24, 0x00, 0x00, 0x00, // chunk size
        0x57, 0x41, 0x56, 0x45, // "WAVE"
        0x66, 0x6D, 0x74, 0x20, // "fmt "
        0x10, 0x00, 0x00, 0x00, // PCM sub-chunk size
        0x01, 0x00,             // PCM format
        0x01, 0x00,             // mono
        0x44, 0xAC, 0x00, 0x00, // 44100 Hz
        0x44, 0xAC, 0x00, 0x00, // byte rate
        0x01, 0x00,             // block align
        0x08, 0x00,             // 8-bit
        0x64, 0x61, 0x74, 0x61, // "data"
        0x04, 0x00, 0x00, 0x00, // data size
        0x80, 0x80, 0x80, 0x80, // silence samples
    ];

    // Minimal OGG page header (captures file)
    public static readonly byte[] ValidOgg = [
        0x4F, 0x67, 0x67, 0x53, // "OggS"
        0x00, 0x02, 0x00, 0x00, // version, type, position
        0x00, 0x00, 0x00, 0x00, // granule position
        0x00, 0x00, 0x00, 0x00, // serial
        0x00, 0x00, 0x00, 0x00, // sequence
        0x00, 0x00, 0x00, 0x00, // checksum
        0x01, 0x1E,             // segment table
        0x01, 0x76, 0x6F, 0x72, // payload start
    ];

    // Minimal M4A (MPEG-4 audio) ftyp box
    public static readonly byte[] ValidM4a = [
        0x00, 0x00, 0x00, 0x20, // box size 32
        0x66, 0x74, 0x79, 0x70, // "ftyp"
        0x4D, 0x34, 0x41, 0x20, // "M4A "
        0x00, 0x00, 0x00, 0x00, // minor version
        0x4D, 0x34, 0x41, 0x20, // compatible brand
        0x69, 0x73, 0x6F, 0x6D, // "isom"
        0x69, 0x73, 0x6F, 0x32, // "iso2"
        0x00, 0x00, 0x00, 0x00, // padding
    ];

    // Minimal FLAC stream marker
    public static readonly byte[] ValidFlac = [
        0x66, 0x4C, 0x61, 0x43, // "fLaC"
        0x80, 0x00, 0x00, 0x22, // STREAMINFO block header
        0x00, 0x12, 0x00, 0x12, // min/max block sizes
        0x00, 0x00, 0x09, 0x00, 0x00, 0x09, // min/max frame sizes
        0x0A, 0xC4, 0x42, 0xF0, 0x00, 0x00, // sample rate, channels, depth
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // total samples
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, // MD5 (partial)
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00,
    ];

    public static IFormFile BuildAudioFormFile(string extension, byte[] bytes, string fieldName = "file")
    {
        var stream = new MemoryStream(bytes);
        var contentType = extension switch
        {
            ".mp3" => "audio/mpeg",
            ".wav" => "audio/wav",
            ".ogg" => "audio/ogg",
            ".m4a" => "audio/mp4",
            ".flac" => "audio/flac",
            _ => "application/octet-stream",
        };
        return new FormFile(stream, 0, bytes.Length, fieldName, $"test{extension}")
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType,
        };
    }
}
