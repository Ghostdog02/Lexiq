namespace Backend.Api.Models;

public class FileListResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<FileInfo> Files { get; set; } = [];
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
    public string Message { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Extension { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;

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
