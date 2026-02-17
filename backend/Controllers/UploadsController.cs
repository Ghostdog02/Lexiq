using Backend.Api.Dtos;
using Backend.Api.Models;
using Backend.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace Backend.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UploadsController(FileUploadsService fileUploadService) : ControllerBase
{
    private readonly FileUploadsService _fileUploadService = fileUploadService;

    private string BaseUrl => $"{Request.Scheme}://{Request.Host}";

    private static readonly Dictionary<string, string> DefaultContentTypes = new()
    {
        ["image"] = "image/png",
        ["document"] = "application/pdf",
        ["video"] = "video/mp4",
        ["audio"] = "audio/mpeg",
        ["file"] = "application/octet-stream",
    };

    [HttpPost("{fileType}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(string fileType, IFormFile file)
    {
        var result = await _fileUploadService.UploadFileAsync(file, fileType, BaseUrl);
        return BuildEditorJsFileResponse(result);
    }

    [HttpPost("any")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadAnyFile(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var fileType = _fileUploadService.DetermineFileType(extension);

        var result = await _fileUploadService.UploadFileAsync(file, fileType, BaseUrl);
        return BuildEditorJsFileResponse(result);
    }

    [HttpPost("{fileType}-by-url")]
    public async Task<IActionResult> UploadByUrl(
        string fileType,
        [FromBody] FileUrlRequestDto request
    )
    {
        var result = await _fileUploadService.UploadFileByUrlAsync(request.Url, fileType, BaseUrl);
        return BuildEditorJsFileResponse(result);
    }

    [HttpGet("{fileType}/{filename}")]
    public IActionResult GetFileByFilename(string fileType, string filename)
    {
        var path = _fileUploadService.GetFilePhysicalPath(filename, fileType);

        if (path == null)
            return NotFound(new ErrorResponseDto(0, $"{fileType} not found."));

        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(path, out var contentType))
            contentType = DefaultContentTypes.GetValueOrDefault(
                fileType,
                "application/octet-stream"
            );

        SetCorsHeaders();

        if (fileType == "image")
            Response.Headers["Cache-Control"] = "public, max-age=31536000, immutable";

        return PhysicalFile(path, contentType, enableRangeProcessing: true);
    }

    [HttpGet("{filename}")]
    public IActionResult GetAnyFileByFilename(string filename)
    {
        var path = _fileUploadService.FindFilePhysicalPath(filename);

        if (path == null)
            return NotFound(new ErrorResponseDto(0, "File not found."));

        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(path, out var contentType))
            contentType = "application/octet-stream";

        SetCorsHeaders();

        return PhysicalFile(path, contentType, enableRangeProcessing: true);
    }

    [HttpGet("list/{fileType}")]
    public async Task<IActionResult> GetFilesByType(
        string fileType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10
    )
    {
        var result = await _fileUploadService.GetFilesByTypeAsync(
            fileType,
            page,
            pageSize,
            BaseUrl
        );
        return BuildListResponse(result);
    }

    [HttpGet("list/all")]
    public async Task<IActionResult> GetAllFiles(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10
    )
    {
        var result = await _fileUploadService.GetAllFilesAsync(page, pageSize, BaseUrl);
        return BuildListResponse(result);
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
            return BadRequest(new ErrorResponseDto(0, result.Message));

        var response = new FileListResponseDto(
            Success: 1,
            Data: result.Files.Select(f => new FileItemDto(
                f.Url,
                f.Name,
                f.Size,
                f.Extension,
                f.Title
            )),
            Pagination: new PaginationDto(
                result.Page,
                result.PageSize,
                result.TotalCount,
                result.TotalPages
            )
        );

        return Ok(response);
    }

    private IActionResult BuildEditorJsFileResponse(FileUploadResult result)
    {
        if (!result.IsSuccess)
            return BadRequest(new ErrorResponseDto(0, result.Message));

        return Ok(
            new EditorJsFileUploadResponse
            {
                Success = 1,
                File = new EditorJsFileData
                {
                    Url = result.Url,
                    Size = result.Size,
                    Name = result.Name,
                    Extension = result.Extension,
                },
            }
        );
    }
}
