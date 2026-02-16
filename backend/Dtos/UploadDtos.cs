namespace Backend.Api.Dtos;

public record FileItemDto(
    string Url,
    string Name,
    long Size,
    string Extension,
    string Title
);

public record PaginationDto(
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages
);

public record FileListResponseDto(
    int Success,
    IEnumerable<FileItemDto> Data,
    PaginationDto Pagination
);

public record FileUrlRequestDto(string Url);

public record ErrorResponseDto(int Success, string Message);
