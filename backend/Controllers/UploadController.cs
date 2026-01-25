using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadController(IWebHostEnvironment environment) : ControllerBase
    {
        private readonly IWebHostEnvironment _environment = environment;
        private readonly long _maxFileSize = 10 * 1024 * 1024; // 10MB

        // File type configurations
        private readonly Dictionary<string, FileTypeConfig> _fileTypeConfigs = new()
        {
            ["image"] = new FileTypeConfig
            {
                AllowedExtensions =
                [
                    ".jpg",
                    ".jpeg",
                    ".png",
                    ".gif",
                    ".webp",
                    ".svg",
                    ".bmp",
                ],
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

        [HttpPost("image")]
        public async Task<IActionResult> UploadImage([FromForm] IFormFile image)
        {
            return await UploadFileByType(image, "image");
        }

        [HttpPost("document")]
        public async Task<IActionResult> UploadDocument([FromForm] IFormFile document)
        {
            return await UploadFileByType(document, "document");
        }

        [HttpPost("video")]
        public async Task<IActionResult> UploadVideo([FromForm] IFormFile video)
        {
            return await UploadFileByType(video, "video");
        }

        [HttpPost("audio")]
        public async Task<IActionResult> UploadAudio([FromForm] IFormFile audio)
        {
            return await UploadFileByType(audio, "audio");
        }

        [HttpPost("file")]
        public async Task<IActionResult> UploadFile([FromForm] IFormFile file)
        {
            return await UploadFileByType(file, "file");
        }

        [HttpPost("any")]
        public async Task<IActionResult> UploadAnyFile([FromForm] IFormFile file)
        {
            // Determine file type based on extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileType = DetermineFileType(extension);

            return await UploadFileByType(file, fileType);
        }

        private async Task<IActionResult> UploadFileByType(IFormFile file, string fileType)
        {
            try
            {
                // Validate file exists
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { success = 0, message = "No file uploaded" });
                }

                // Get configuration for file type
                if (!_fileTypeConfigs.TryGetValue(fileType, out var config))
                {
                    return BadRequest(new { success = 0, message = "Invalid file type" });
                }

                // Validate file size
                if (file.Length > config.MaxSize)
                {
                    return BadRequest(
                        new
                        {
                            success = 0,
                            message = $"File size exceeds limit of {config.MaxSize / (1024 * 1024)}MB",
                        }
                    );
                }

                // Validate file extension
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!config.AllowedExtensions.Contains(extension))
                {
                    return BadRequest(
                        new
                        {
                            success = 0,
                            message = $"Invalid file type. Allowed: {string.Join(", ", config.AllowedExtensions)}",
                        }
                    );
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

                // Generate unique filename
                var originalFileName = Path.GetFileNameWithoutExtension(file.FileName);
                var uniqueFileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                var fileUrl =
                    $"{Request.Scheme}://{Request.Host}/uploads/{config.Folder}/{uniqueFileName}";

                return Ok(
                    new
                    {
                        success = 1,
                        file = new
                        {
                            url = fileUrl,
                            name = file.FileName,
                            size = file.Length,
                            extension = extension.TrimStart('.'),
                            title = originalFileName,
                        },
                    }
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = 0, message = ex.Message });
            }
        }

        [HttpPost("image-by-url")]
        public async Task<IActionResult> UploadImageByUrl([FromBody] FileUrlRequest request)
        {
            return await UploadFileByUrl(request, "image");
        }

        [HttpPost("file-by-url")]
        public async Task<IActionResult> UploadFileByUrl([FromBody] FileUrlRequest request)
        {
            return await UploadFileByUrl(request, "file");
        }

        private async Task<IActionResult> UploadFileByUrl(FileUrlRequest request, string fileType)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Url))
                {
                    return BadRequest(new { success = 0, message = "URL is required" });
                }

                // Get configuration for file type
                if (!_fileTypeConfigs.TryGetValue(fileType, out var config))
                {
                    return BadRequest(new { success = 0, message = "Invalid file type" });
                }

                using var httpClient = new HttpClient();
                var fileBytes = await httpClient.GetByteArrayAsync(request.Url);

                // Validate size
                if (fileBytes.Length > config.MaxSize)
                {
                    return BadRequest(new { success = 0, message = "File size exceeds limit" });
                }

                // Determine extension from URL
                var extension = Path.GetExtension(new Uri(request.Url).LocalPath)
                    .ToLowerInvariant();
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
                await System.IO.File.WriteAllBytesAsync(filePath, fileBytes);

                // Build URL
                var fileUrl =
                    $"{Request.Scheme}://{Request.Host}/uploads/{config.Folder}/{uniqueFileName}";

                return Ok(
                    new
                    {
                        success = 1,
                        file = new
                        {
                            url = fileUrl,
                            size = fileBytes.Length,
                            extension = extension.TrimStart('.'),
                            name = uniqueFileName,
                        },
                    }
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = 0, message = ex.Message });
            }
        }

        private string DetermineFileType(string extension)
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

    public class FileUrlRequest
    {
        public string Url { get; set; }
    }
}
