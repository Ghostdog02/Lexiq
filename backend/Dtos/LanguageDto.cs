namespace Backend.Api.Dtos;

public record LanguageDto(string LanguageName, string? FlagIconUrl, int CourseCount);

public record CreateLanguageDto(string LanguageName, string? FlagIconUrl);
