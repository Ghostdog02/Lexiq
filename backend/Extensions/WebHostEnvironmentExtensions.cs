namespace Backend.Api.Extensions;

public static class WebHostEnvironmentExtensions
{
    /// <summary>
    /// Ensures upload directory structure exists for file storage.
    /// Creates directories for images, documents, videos, audio, and files.
    /// </summary>
    public static void EnsureUploadDirectoryStructure(this IWebHostEnvironment environment)
    {
        var basePath = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        var uploadFolders = new[] { "images", "documents", "videos", "audio", "files" };

        foreach (var folder in uploadFolders)
        {
            var path = Path.Combine(basePath, "uploads", folder);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                Console.WriteLine($"✅ Created upload directory: {path}");
            }
        }

        Console.WriteLine($"✅ Upload directory structure verified at: {basePath}/uploads/");
    }
}
