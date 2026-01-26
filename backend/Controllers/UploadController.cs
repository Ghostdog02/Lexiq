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

            var response = new {
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