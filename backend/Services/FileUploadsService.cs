using Backend.Api.Models;
using FileInfo = Backend.Api.Models.FileInfo;

namespace Backend.Api.Services
{
    public class FileUploadsService(IWebHostEnvironment environment) : IFileUploadsService
    {
        private readonly IWebHostEnvironment _environment = environment;
        private readonly Dictionary<string, FileTypeConfig> _fileTypeConfigs = InitializeFileTypeConfigs();

        private static Dictionary<string, FileTypeConfig> InitializeFileTypeConfigs()
        {
            return new Dictionary<string, FileTypeConfig>
            {
                ["image"] = new FileTypeConfig
                {
                    AllowedExtensions = [".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg", ".bmp"],
                    Folder = "images",
                    MaxSize = 5 * 1024 * 1024, // 5MB
                },
                ["document"] = new FileTypeConfig
                {
                    AllowedExtensions =
                    [
                        ".pdf",
                        ".doc",
                        ".docx",
                        ".xls",
                        ".xlsx",
                        ".ppt",
                        ".pptx",
                        ".txt",
                    ],
                    Folder = "documents",
                    MaxSize = 10 * 1024 * 1024, // 10MB
                },
                ["video"] = new FileTypeConfig
                {
                    AllowedExtensions = [".mp4", ".avi", ".mov", ".wmv", ".flv", ".webm"],
                    Folder = "videos",
                    MaxSize = 50 * 1024 * 1024, // 50MB
                },
                ["audio"] = new FileTypeConfig
                {
                    AllowedExtensions = [".mp3", ".wav", ".ogg", ".m4a", ".flac"],
                    Folder = "audio",
                    MaxSize = 10 * 1024 * 1024, // 10MB
                },
                ["file"] = new FileTypeConfig
                {
                    AllowedExtensions =
                    [
                        ".pdf",
                        ".doc",
                        ".docx",
                        ".xls",
                        ".xlsx",
                        ".zip",
                        ".rar",
                        ".txt",
                        ".csv",
                    ],
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
            if (file == null || file.Length == 0)
            {
                return FileUploadResult.Failure("No file uploaded");
            }

            if (!_fileTypeConfigs.TryGetValue(fileType, out var config))
            {
                return FileUploadResult.Failure("Invalid file type");
            }

            if (file.Length > config.MaxSize)
            {
                return FileUploadResult.Failure(
                    $"File size exceeds limit of {config.MaxSize / (1024 * 1024)}MB"
                );
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!config.AllowedExtensions.Contains(extension))
            {
                return FileUploadResult.Failure(
                    $"Invalid file type. Allowed: {string.Join(", ", config.AllowedExtensions)}"
                );
            }

            try
            {
                var basePath =
                    _environment.WebRootPath
                    ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
                var uploadsFolder = Path.Combine(basePath, "uploads", config.Folder);

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Generate unique filename
                var originalFileName = Path.GetFileNameWithoutExtension(file.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Build URL - serve via controller endpoint
                var fileUrl = $"/api/uploads/{fileType}/{uniqueFileName}";

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
                var basePath =
                    _environment.WebRootPath
                    ?? Path.Combine(_environment.ContentRootPath, "wwwroot");
                var uploadsFolder = Path.Combine(basePath, "uploads", config.Folder);

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // Generate unique filename and save
                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                await File.WriteAllBytesAsync(filePath, fileBytes);

                // Build URL - serve via controller endpoint
                var fileUrl = $"/api/uploads/{fileType}/{uniqueFileName}";

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

        public async Task<FileListResult> GetFilesByTypeAsync(
            string fileType,
            int page,
            int pageSize,
            string baseUrl
        )
        {
            try
            {
                // TODO: Replace with your actual data access logic
                // This is a sample implementation - adjust based on your storage mechanism
                // (database, file system, cloud storage, etc.)

                // Example: Query database for files of specific type
                // var query = _dbContext.Files.Where(f => f.FileType == fileType);
                // var totalCount = await query.CountAsync();
                // var files = await query
                //     .OrderByDescending(f => f.UploadedAt)
                //     .Skip((page - 1) * pageSize)
                //     .Take(pageSize)
                //     .ToListAsync();

                // For demonstration purposes only:
                var files = new List<FileInfo>();
                var totalCount = 0;

                return new FileListResult
                {
                    IsSuccess = true,
                    Files = files,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                };
            }
            catch (Exception ex)
            {
                return new FileListResult
                {
                    IsSuccess = false,
                    Message = $"Error retrieving files: {ex.Message}",
                };
            }
        }

        public async Task<FileListResult> GetAllFilesAsync(int page, int pageSize, string baseUrl)
        {
            try
            {
                // TODO: Replace with your actual data access logic
                // Example: Query database for all files
                // var query = _dbContext.Files;
                // var totalCount = await query.CountAsync();
                // var files = await query
                //     .OrderByDescending(f => f.UploadedAt)
                //     .Skip((page - 1) * pageSize)
                //     .Take(pageSize)
                //     .ToListAsync();

                // For demonstration purposes only:
                var files = new List<FileInfo>();
                var totalCount = 0;

                return new FileListResult
                {
                    IsSuccess = true,
                    Files = files,
                    Page = page,
                    PageSize = pageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                };
            }
            catch (Exception ex)
            {
                return new FileListResult
                {
                    IsSuccess = false,
                    Message = $"Error retrieving files: {ex.Message}",
                };
            }
        }

        public async Task<FileUploadResult> GetFileByFilenameAsync(
            string filename,
            string? fileType,
            string baseUrl
        )
        {
            try
            {
                var basePath =
                    _environment.WebRootPath
                    ?? Path.Combine(_environment.ContentRootPath, "wwwroot");

                // If fileType is specified, search in that folder only
                if (!string.IsNullOrEmpty(fileType) && _fileTypeConfigs.TryGetValue(fileType, out var config))
                {
                    var filePath = Path.Combine(basePath, "uploads", config.Folder, filename);

                    if (File.Exists(filePath))
                    {
                        var fileInfo = new System.IO.FileInfo(filePath);
                        var extension = Path.GetExtension(filename).TrimStart('.');

                        return FileUploadResult.Success(
                            url: $"{baseUrl}/api/uploads/{fileType}/{filename}",
                            name: filename,
                            size: fileInfo.Length,
                            extension: extension,
                            title: Path.GetFileNameWithoutExtension(filename)
                        );
                    }
                }
                else
                {
                    // Search all folders if no specific type provided
                    foreach (var typeConfig in _fileTypeConfigs)
                    {
                        var filePath = Path.Combine(basePath, "uploads", typeConfig.Value.Folder, filename);

                        if (File.Exists(filePath))
                        {
                            var fileInfo = new System.IO.FileInfo(filePath);
                            var extension = Path.GetExtension(filename).TrimStart('.');

                            return FileUploadResult.Success(
                                url: $"{baseUrl}/api/uploads/{typeConfig.Key}/{filename}",
                                name: filename,
                                size: fileInfo.Length,
                                extension: extension,
                                title: Path.GetFileNameWithoutExtension(filename)
                            );
                        }
                    }
                }

                return FileUploadResult.Failure("File not found");
            }
            catch (Exception ex)
            {
                return FileUploadResult.Failure($"Error retrieving file: {ex.Message}");
            }
        }

        public string? GetFilePhysicalPath(string filename, string fileType)
        {
            try
            {
                if (!_fileTypeConfigs.TryGetValue(fileType, out var config))
                {
                    return null;
                }

                var basePath =
                    _environment.WebRootPath
                    ?? Path.Combine(_environment.ContentRootPath, "wwwroot");

                var filePath = Path.Combine(basePath, "uploads", config.Folder, filename);

                return File.Exists(filePath) ? filePath : null;
            }
            catch
            {
                return null;
            }
        }

        public string? FindFilePhysicalPath(string filename)
        {
            try
            {
                var basePath =
                    _environment.WebRootPath
                    ?? Path.Combine(_environment.ContentRootPath, "wwwroot");

                foreach (var typeConfig in _fileTypeConfigs)
                {
                    var filePath = Path.Combine(basePath, "uploads", typeConfig.Value.Folder, filename);
                    if (File.Exists(filePath))
                    {
                        return filePath;
                    }
                }

                return null;
            }
            catch
            {
                return null;
            }
        }
    }

    public class FileTypeConfig
    {
        public string[] AllowedExtensions { get; set; }
        public string Folder { get; set; }
        public long MaxSize { get; set; }
    }
}
