using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace YourApp.Services
{
    public interface IFileUploadService
    {
        Task<FileUploadResult> UploadFileAsync(IFormFile file, string fileType, string baseUrl);
        Task<FileUploadResult> UploadFileByUrlAsync(string url, string fileType, string baseUrl);
        string DetermineFileType(string extension);
    }

    public class FileUploadService : IFileUploadService
    {
        private readonly IWebHostEnvironment _environment;
        private readonly Dictionary<string, FileTypeConfig> _fileTypeConfigs;

        public FileUploadService(IWebHostEnvironment environment)
        {
            _environment = environment;
            _fileTypeConfigs = InitializeFileTypeConfigs();
        }

        private Dictionary<string, FileTypeConfig> InitializeFileTypeConfigs()
        {
            return new Dictionary<string, FileTypeConfig>
            {
                ["image"] = new FileTypeConfig
                {
                    AllowedExtensions = new[]
                    {
                        ".jpg",
                        ".jpeg",
                        ".png",
                        ".gif",
                        ".webp",
                        ".svg",
                        ".bmp",
                    },
                    Folder = "images",
                    MaxSize = 5 * 1024 * 1024, // 5MB
                },
                ["document"] = new FileTypeConfig
                {
                    AllowedExtensions = new[]
                    {
                        ".pdf",
                        ".doc",
                        ".docx",
                        ".xls",
                        ".xlsx",
                        ".ppt",
                        ".pptx",
                        ".txt",
                    },
                    Folder = "documents",
                    MaxSize = 10 * 1024 * 1024, // 10MB
                },
                ["video"] = new FileTypeConfig
                {
                    AllowedExtensions = new[] { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".webm" },
                    Folder = "videos",
                    MaxSize = 50 * 1024 * 1024, // 50MB
                },
                ["audio"] = new FileTypeConfig
                {
                    AllowedExtensions = new[] { ".mp3", ".wav", ".ogg", ".m4a", ".flac" },
                    Folder = "audio",
                    MaxSize = 10 * 1024 * 1024, // 10MB
                },
                ["file"] = new FileTypeConfig
                {
                    AllowedExtensions = new[]
                    {
                        ".pdf",
                        ".doc",
                        ".docx",
                        ".xls",
                        ".xlsx",
                        ".zip",
                        ".rar",
                        ".txt",
                        ".csv",
                    },
                    Folder = "files",
                    MaxSize = 10 * 1024 * 1024, // 10MB
                },
            };
        }

        public async Task<FileUploadResult> UploadFileAsync(
            IFormFile file,
            string fileType,
            string baseUrl
        )
        {
            // Validate file exists
            if (file == null || file.Length == 0)
            {
                return FileUploadResult.Failure("No file uploaded");
            }

            // Get configuration for file type
            if (!_fileTypeConfigs.TryGetValue(fileType, out var config))
            {
                return FileUploadResult.Failure("Invalid file type");
            }

            // Validate file size
            if (file.Length > config.MaxSize)
            {
                return FileUploadResult.Failure(
                    $"File size exceeds limit of {config.MaxSize / (1024 * 1024)}MB"
                );
            }

            // Validate file extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!config.AllowedExtensions.Contains(extension))
            {
                return FileUploadResult.Failure(
                    $"Invalid file type. Allowed: {string.Join(", ", config.AllowedExtensions)}"
                );
            }

            try
            {
                // Create uploads directory
                var uploadsFolder = Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    config.Folder
                );
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Generate unique filename
                var originalFileName = Path.GetFileNameWithoutExtension(file.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Build URL
                var fileUrl = $"{baseUrl}/uploads/{config.Folder}/{uniqueFileName}";

                return FileUploadResult.Success(
                    url: fileUrl,
                    name: file.FileName,
                    size: file.Length,
                    extension: extension.TrimStart('.'),
                    title: originalFileName
                );
            }
            catch (Exception ex)
            {
                return FileUploadResult.Failure($"Upload failed: {ex.Message}");
            }
        }

        public async Task<FileUploadResult> UploadFileByUrlAsync(
            string url,
            string fileType,
            string baseUrl
        )
        {
            if (string.IsNullOrEmpty(url))
            {
                return FileUploadResult.Failure("URL is required");
            }

            // Get configuration for file type
            if (!_fileTypeConfigs.TryGetValue(fileType, out var config))
            {
                return FileUploadResult.Failure("Invalid file type");
            }

            try
            {
                using var httpClient = new HttpClient();
                var fileBytes = await httpClient.GetByteArrayAsync(url);

                // Validate size
                if (fileBytes.Length > config.MaxSize)
                {
                    return FileUploadResult.Failure("File size exceeds limit");
                }

                // Determine extension from URL
                var extension = Path.GetExtension(new Uri(url).LocalPath).ToLowerInvariant();
                if (
                    string.IsNullOrEmpty(extension) || !config.AllowedExtensions.Contains(extension)
                )
                {
                    extension = config.AllowedExtensions.First(); // Use default extension
                }

                // Create uploads directory
                var uploadsFolder = Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    config.Folder
                );
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Generate unique filename and save
                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                await File.WriteAllBytesAsync(filePath, fileBytes);

                // Build URL
                var fileUrl = $"{baseUrl}/uploads/{config.Folder}/{uniqueFileName}";

                return FileUploadResult.Success(
                    url: fileUrl,
                    name: uniqueFileName,
                    size: fileBytes.Length,
                    extension: extension.TrimStart('.'),
                    title: uniqueFileName
                );
            }
            catch (Exception ex)
            {
                return FileUploadResult.Failure($"Upload failed: {ex.Message}");
            }
        }

        public string DetermineFileType(string extension)
        {
            foreach (var config in _fileTypeConfigs)
            {
                if (config.Value.AllowedExtensions.Contains(extension))
                {
                    return config.Key;
                }
            }
            return "file"; // Default to generic file type
        }
    }

    public class FileTypeConfig
    {
        public string[] AllowedExtensions { get; set; }
        public string Folder { get; set; }
        public long MaxSize { get; set; }
    }

    public class FileUploadResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public string Url { get; set; }
        public string Name { get; set; }
        public long Size { get; set; }
        public string Extension { get; set; }
        public string Title { get; set; }

        public static FileUploadResult Success(
            string url,
            string name,
            long size,
            string extension,
            string title
        )
        {
            return new FileUploadResult
            {
                IsSuccess = true,
                Url = url,
                Name = name,
                Size = size,
                Extension = extension,
                Title = title,
            };
        }

        public static FileUploadResult Failure(string message)
        {
            return new FileUploadResult { IsSuccess = false, Message = message };
        }
    }
}
