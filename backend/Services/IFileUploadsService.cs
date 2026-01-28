using Backend.Api.Models;

namespace Backend.Api.Services;

public interface IFileUploadsService
{
    Task<FileUploadResult> UploadFileAsync(IFormFile file, string fileType, string baseUrl);
    Task<FileUploadResult> UploadFileByUrlAsync(string url, string fileType, string baseUrl);
    string DetermineFileType(string extension);

    Task<FileListResult> GetFilesByTypeAsync(
        string fileType,
        int page,
        int pageSize,
        string baseUrl
    );
    Task<FileListResult> GetAllFilesAsync(int page, int pageSize, string baseUrl);
    Task<FileUploadResult> GetFileByFilenameAsync(
        string filename,
        string? fileType,
        string baseUrl
    );
    string? GetFilePhysicalPath(string filename, string fileType);
    string? FindFilePhysicalPath(string filename);
}
