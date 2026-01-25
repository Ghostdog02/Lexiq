using Backend.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UploadController(IFileUploadService fileUploadService) : ControllerBase
    {
        private readonly IFileUploadService _fileUploadService = fileUploadService;

        [HttpPost("image")]
        public async Task<IActionResult> UploadImage(IFormFile image)
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var result = await _fileUploadService.UploadFileAsync(image, "image", baseUrl);
            return BuildResponse(result);
        }

        [HttpPost("document")]
        public async Task<IActionResult> UploadDocument([FromForm] IFormFile document)
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var result = await _fileUploadService.UploadFileAsync(document, "document", baseUrl);
            return BuildResponse(result);
        }

        [HttpPost("video")]
        public async Task<IActionResult> UploadVideo([FromForm] IFormFile video)
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var result = await _fileUploadService.UploadFileAsync(video, "video", baseUrl);
            return BuildResponse(result);
        }

        [HttpPost("audio")]
        public async Task<IActionResult> UploadAudio([FromForm] IFormFile audio)
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var result = await _fileUploadService.UploadFileAsync(audio, "audio", baseUrl);
            return BuildResponse(result);
        }

        [HttpPost("file")]
        public async Task<IActionResult> UploadFile([FromForm] IFormFile file)
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var result = await _fileUploadService.UploadFileAsync(file, "file", baseUrl);
            return BuildResponse(result);
        }

        [HttpPost("any")]
        public async Task<IActionResult> UploadAnyFile([FromForm] IFormFile file)
        {
            var extension = System.IO.Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileType = _fileUploadService.DetermineFileType(extension);

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var result = await _fileUploadService.UploadFileAsync(file, fileType, baseUrl);
            return BuildResponse(result);
        }

        [HttpPost("image-by-url")]
        public async Task<IActionResult> UploadImageByUrl([FromBody] FileUrlRequest request)
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var result = await _fileUploadService.UploadFileByUrlAsync(
                request.Url,
                "image",
                baseUrl
            );
            return BuildResponse(result);
        }

        [HttpPost("file-by-url")]
        public async Task<IActionResult> UploadFileByUrl([FromBody] FileUrlRequest request)
        {
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var result = await _fileUploadService.UploadFileByUrlAsync(
                request.Url,
                "file",
                baseUrl
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

            Console.WriteLine(response);

            return Ok(
                response
            );
        }
    }

    public class FileUrlRequest
    {
        public string Url { get; set; }
    }
}
