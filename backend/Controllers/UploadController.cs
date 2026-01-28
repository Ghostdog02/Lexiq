using Backend.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadController(IFileUploadService fileUploadService) : ControllerBase
    {
        private readonly IFileUploadService _fileUploadService = fileUploadService;

        private string BaseUrl => $"{Request.Scheme}://{Request.Host}";

        [HttpPost("image")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadImage(IFormFile image)
        {
            System.Console.WriteLine("Uploading an image");
            var result = await _fileUploadService.UploadFileAsync(image, "image", BaseUrl);
            return BuildResponse(result);
        }

        [HttpPost("document")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadDocument(IFormFile document)
        {
            var result = await _fileUploadService.UploadFileAsync(document, "document", BaseUrl);
            return BuildResponse(result);
        }

        [HttpPost("video")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadVideo(IFormFile video)
        {
            var result = await _fileUploadService.UploadFileAsync(video, "video", BaseUrl);
            return BuildResponse(result);
        }

        [HttpPost("audio")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadAudio(IFormFile audio)
        {
            var result = await _fileUploadService.UploadFileAsync(audio, "audio", BaseUrl);
            return BuildResponse(result);
        }

        [HttpPost("file")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            var result = await _fileUploadService.UploadFileAsync(file, "file", BaseUrl);
            return BuildResponse(result);
        }

        [HttpPost("any")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadAnyFile(IFormFile file)
        {
            var extension = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileType = _fileUploadService.DetermineFileType(extension);

            var result = await _fileUploadService.UploadFileAsync(file, fileType, BaseUrl);
            return BuildResponse(result);
        }

        [HttpPost("image-by-url")]
        public async Task<IActionResult> UploadImageByUrl([FromBody] FileUrlRequest request)
        {
            var result = await _fileUploadService.UploadFileByUrlAsync(
                request.Url,
                "image",
                BaseUrl
            );
            return BuildResponse(result);
        }

        [HttpPost("file-by-url")]
        public async Task<IActionResult> UploadFileByUrl([FromBody] FileUrlRequest request)
        {
            var result = await _fileUploadService.UploadFileByUrlAsync(
                request.Url,
                "file",
                BaseUrl
            );
            return BuildResponse(result);
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

        // GET endpoints - List files by type
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
        public async Task<IActionResult> GetImageByFilename(string filename)
        {
            var result = await _fileUploadService.GetFileByFilenameAsync(
                filename,
                "image",
                BaseUrl
            );
            return BuildResponse(result);
        }

        [HttpGet("document/{filename}")]
        public async Task<IActionResult> GetDocumentByFilename(string filename)
        {
            var result = await _fileUploadService.GetFileByFilenameAsync(
                filename,
                "document",
                BaseUrl
            );
            return BuildResponse(result);
        }

        [HttpGet("video/{filename}")]
        public async Task<IActionResult> GetVideoByFilename(string filename)
        {
            var result = await _fileUploadService.GetFileByFilenameAsync(
                filename,
                "video",
                BaseUrl
            );
            return BuildResponse(result);
        }

        [HttpGet("audio/{filename}")]
        public async Task<IActionResult> GetAudioByFilename(string filename)
        {
            var result = await _fileUploadService.GetFileByFilenameAsync(
                filename,
                "audio",
                BaseUrl
            );
            return BuildResponse(result);
        }

        [HttpGet("file/{filename}")]
        public async Task<IActionResult> GetFileByFilename(string filename)
        {
            var result = await _fileUploadService.GetFileByFilenameAsync(filename, "file", BaseUrl);
            return BuildResponse(result);
        }

        [HttpGet("{filename}")]
        public async Task<IActionResult> GetAnyFileByFilename(string filename)
        {
            var result = await _fileUploadService.GetFileByFilenameAsync(filename, null, BaseUrl);
            return BuildResponse(result);
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
    }

    public class FileUrlRequest
    {
        public string Url { get; set; } = string.Empty;
    }
}
