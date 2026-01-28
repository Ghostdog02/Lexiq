namespace Backend.Api.Services;

public interface IFileUploadService
{
    // Existing methods (keep these)
    Task<FileUploadResult> UploadFileAsync(IFormFile file, string fileType, string baseUrl);
    Task<FileUploadResult> UploadFileByUrlAsync(string url, string fileType, string baseUrl);
    string DetermineFileType(string extension);

    // New GET methods
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
}

public class FileListResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<FileInfo> Files { get; set; } = new();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}

public class FileInfo
{
    public string Url { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Extension { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
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
