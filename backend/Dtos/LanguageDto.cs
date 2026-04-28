namespace Backend.Api.Dtos;

public record LanguageDto
{
    public required string LanguageName { get; init; }

    public string? FlagIconUrl { get; init; }

    public required int CourseCount { get; init; }
}

public record CreateLanguageDto
{
    public required string LanguageName { get; init; }

    public string? FlagIconUrl { get; init; }
}
