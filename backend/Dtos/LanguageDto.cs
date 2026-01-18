namespace Backend.Api.Dtos;

public class LanguageDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? FlagIconUrl { get; set; }
    public int CourseCount { get; set; }
}

public class CreateLanguageDto
{
    public required string Name { get; set; }
    public string? FlagIconUrl { get; set; }
}
