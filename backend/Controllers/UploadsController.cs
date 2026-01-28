using Backend.Api.Models;
using Backend.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace Backend.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadsController(FileUploadsService fileUploadService)
        : ControllerBase
    {
        private readonly FileUploadsService _fileUploadService = fileUploadService;

        private string BaseUrl => $"{Request.Scheme}://{Request.Host}";

        [HttpPost("image")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadImage(IFormFile image)
        {
            Console.WriteLine("Uploading an image");
            var result = await _fileUploadService.UploadFileAsync(image, "image", BaseUrl);
            return BuildEditorJsFileResponse(result);
        }

        [HttpPost("document")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadDocument(IFormFile document)
        {
            var result = await _fileUploadService.UploadFileAsync(document, "document", BaseUrl);
            return BuildEditorJsFileResponse(result);
        }

        [HttpPost("video")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadVideo(IFormFile video)
        {
            var result = await _fileUploadService.UploadFileAsync(video, "video", BaseUrl);
            return BuildEditorJsFileResponse(result);
        }

        [HttpPost("audio")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadAudio(IFormFile audio)
        {
            var result = await _fileUploadService.UploadFileAsync(audio, "audio", BaseUrl);
            return BuildEditorJsFileResponse(result);
        }

        [HttpPost("file")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            var result = await _fileUploadService.UploadFileAsync(file, "file", BaseUrl);
            return BuildEditorJsFileResponse(result);
        }

        [HttpPost("any")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadAnyFile(IFormFile file)
        {
            var extension = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileType = _fileUploadService.DetermineFileType(extension);

            var result = await _fileUploadService.UploadFileAsync(file, fileType, BaseUrl);
            return BuildEditorJsFileResponse(result);
        }

        [HttpPost("image-by-url")]
        public async Task<IActionResult> UploadImageByUrl([FromBody] FileUrlRequest request)
        {
            var result = await _fileUploadService.UploadFileByUrlAsync(
                request.Url,
                "image",
                BaseUrl
            );
            return BuildEditorJsFileResponse(result);
        }

        [HttpPost("file-by-url")]
        public async Task<IActionResult> UploadFileByUrl([FromBody] FileUrlRequest request)
        {
            var result = await _fileUploadService.UploadFileByUrlAsync(
                request.Url,
                "file",
                BaseUrl
            );
            return BuildEditorJsFileResponse(result);
        }

        [HttpGet("image")]
        public async Task<IActionResult> GetImages(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10
        )
        {
            var result = await _fileUploadService.GetFilesByTypeAsync(
                "image",
                page,
                pageSize,
                BaseUrl
            );
            return BuildListResponse(result);
        }

        [HttpGet("document")]
        public async Task<IActionResult> GetDocuments(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10
        )
        {
            var result = await _fileUploadService.GetFilesByTypeAsync(
                "document",
                page,
                pageSize,
                BaseUrl
            );
            return BuildListResponse(result);
        }

        [HttpGet("video")]
        public async Task<IActionResult> GetVideos(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10
        )
        {
            var result = await _fileUploadService.GetFilesByTypeAsync(
                "video",
                page,
                pageSize,
                BaseUrl
            );
            return BuildListResponse(result);
        }

        [HttpGet("audio")]
        public async Task<IActionResult> GetAudios(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10
        )
        {
            var result = await _fileUploadService.GetFilesByTypeAsync(
                "audio",
                page,
                pageSize,
                BaseUrl
            );
            return BuildListResponse(result);
        }

        [HttpGet("file")]
        public async Task<IActionResult> GetFiles(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10
        )
        {
            var result = await _fileUploadService.GetFilesByTypeAsync(
                "file",
                page,
                pageSize,
                BaseUrl
            );
            return BuildListResponse(result);
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllFiles(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10
        )
        {
            var result = await _fileUploadService.GetAllFilesAsync(page, pageSize, BaseUrl);
            return BuildListResponse(result);
        }

        // GET endpoints - Get specific file by filename
        [HttpGet("image/{filename}")]
        public IActionResult GetImageByFilename(string filename)
        {
            var path = _fileUploadService.GetFilePhysicalPath(filename, "image");

            if (path == null)
                return NotFound("Image not found.");

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(path, out var contentType))
            {
                contentType = "image/png";
            }

            SetCorsHeaders();

            return PhysicalFile(path, contentType, enableRangeProcessing: true);
        }

        [HttpGet("document/{filename}")]
        public IActionResult GetDocumentByFilename(string filename)
        {
            var path = _fileUploadService.GetFilePhysicalPath(filename, "document");

            if (path == null)
                return NotFound("Document not found.");

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(path, out var contentType))
            {
                contentType = "application/pdf";
            }

            SetCorsHeaders();

            return PhysicalFile(path, contentType, enableRangeProcessing: true);
        }

        [HttpGet("video/{filename}")]
        public IActionResult GetVideoByFilename(string filename)
        {
            var path = _fileUploadService.GetFilePhysicalPath(filename, "video");

            if (path == null)
                return NotFound("Video not found.");

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(path, out var contentType))
            {
                contentType = "video/mp4";
            }

            SetCorsHeaders();

            return PhysicalFile(path, contentType, enableRangeProcessing: true);
        }

        [HttpGet("audio/{filename}")]
        public IActionResult GetAudioByFilename(string filename)
        {
            var path = _fileUploadService.GetFilePhysicalPath(filename, "audio");

            if (path == null)
                return NotFound("Audio file not found.");

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(path, out var contentType))
            {
                contentType = "audio/mpeg"; // Default to MP3
            }

            SetCorsHeaders();

            return PhysicalFile(path, contentType, enableRangeProcessing: true);
        }

        [HttpGet("file/{filename}")]
        public IActionResult GetFileByFilename(string filename)
        {
            var path = _fileUploadService.GetFilePhysicalPath(filename, "file");

            if (path == null)
                return NotFound("File not found.");

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(path, out var contentType))
            {
                contentType = "application/octet-stream";
            }

            SetCorsHeaders();

            return PhysicalFile(path, contentType, enableRangeProcessing: true);
        }

        [HttpGet("{filename}")]
        public IActionResult GetAnyFileByFilename(string filename)
        {
            var path = _fileUploadService.FindFilePhysicalPath(filename);

            if (path == null)
                return NotFound("File not found.");

            var provider = new FileExtensionContentTypeProvider();
            if (!provider.TryGetContentType(path, out var contentType))
            {
                contentType = "application/octet-stream";
            }

            SetCorsHeaders();

            return PhysicalFile(path, contentType, enableRangeProcessing: true);
        }

        private void SetCorsHeaders()
        {
            Response.Headers.Append("Access-Control-Allow-Origin", "*");
            Response.Headers.Append("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            Response.Headers.Append("Access-Control-Allow-Headers", "Content-Type, Accept");
            Response.Headers.Append("Cross-Origin-Resource-Policy", "cross-origin");
        }

        private IActionResult BuildListResponse(FileListResult result)
        {
            if (!result.IsSuccess)
            {
                return BadRequest(new { success = 0, message = result.Message });
            }

            var response = new
            {
                success = 1,
                data = result.Files.Select(f => new
                {
                    url = f.Url,
                    name = f.Name,
                    size = f.Size,
                    extension = f.Extension,
                    title = f.Title,
                }),
                pagination = new
                {
                    page = result.Page,
                    pageSize = result.PageSize,
                    totalCount = result.TotalCount,
                    totalPages = result.TotalPages,
                },
            };

            return Ok(response);
        }

        /// <summary>
        /// Build Editor.js specific file upload response
        /// </summary>
        private IActionResult BuildEditorJsFileResponse(FileUploadResult result)
        {
            if (!result.IsSuccess)
            {
                return BadRequest(new { success = 0, message = result.Message });
            }

            var response = new EditorJsFileUploadResponse
            {
                Success = 1,
                File = new EditorJsFileData
                {
                    Url = result.Url,
                    Size = result.Size,
                    Name = result.Name,
                    Extension = result.Extension,
                },
            };

            return Ok(response);
        }

        private IActionResult BuildResponse(FileUploadResult result)
        {
            if (!result.IsSuccess)
            {
                return BadRequest(new { success = 0, message = result.Message });
            }

            var response = new
            {
                success = 1,
                file = new
                {
                    url = result.Url,
                    name = result.Name,
                    size = result.Size,
                    extension = result.Extension,
                    title = result.Title,
                },
            };

            return Ok(response);
        }
    }

    public class FileUrlRequest
    {
        public string Url { get; set; } = string.Empty;
    }
}
