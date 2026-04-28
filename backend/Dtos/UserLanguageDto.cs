namespace Backend.Api.Dtos;

public record UserLanguageDto
{
    public required string UserId { get; init; }

    public required string LanguageId { get; init; }

    public required string LanguageName { get; init; }

    public string? FlagIconUrl { get; init; }

    public required DateTime EnrolledAt { get; init; }

    public required int CompletedLessons { get; init; }

    public required int TotalLessons { get; init; }
}

public record EnrollLanguageDto
{
    public required string LanguageId { get; init; }
}
