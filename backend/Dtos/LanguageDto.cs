namespace Backend.Api.Dtos;

public record LanguageDto(int Id, string Name, string? FlagIconUrl, int CourseCount);

public record CreateLanguageDto(string Name, string? FlagIconUrl);
