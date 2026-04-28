namespace Backend.Api.Dtos;

public record FileItemDto
{
    public required string Url { get; init; }

    public required string Name { get; init; }

    public required long Size { get; init; }

    public required string Extension { get; init; }

    public required string Title { get; init; }
}

public record PaginationDto
{
    public required int Page { get; init; }

    public required int PageSize { get; init; }

    public required int TotalCount { get; init; }

    public required int TotalPages { get; init; }
}

public record FileListResponseDto
{
    public required int Success { get; init; }

    public required IEnumerable<FileItemDto> Data { get; init; }

    public required PaginationDto Pagination { get; init; }
}

public record FileUrlRequestDto
{
    public required string Url { get; init; }
}

public record ErrorResponseDto
{
    public required int Success { get; init; }

    public required string Message { get; init; }
}
