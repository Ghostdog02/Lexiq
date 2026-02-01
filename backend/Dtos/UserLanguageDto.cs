namespace Backend.Api.Dtos;

public record UserLanguageDto(
    string UserId,
    string LanguageId,
    string LanguageName,
    string? FlagIconUrl,
    DateTime EnrolledAt,
    int CompletedLessons,
    int TotalLessons
);

public record EnrollLanguageDto(
    string LanguageId
);