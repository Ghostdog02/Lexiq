namespace Backend.Api.Dtos;

public class UserLanguageDto
{
    public required string UserId { get; set; }
    public int LanguageId { get; set; }
    public string LanguageName { get; set; } = string.Empty;
    public string? FlagIconUrl { get; set; }
    public DateTime EnrolledAt { get; set; }
    public int CompletedLessons { get; set; } // Calculated field
    public int TotalLessons { get; set; } // Calculated field
}

public class EnrollLanguageDto
{
    public int LanguageId { get; set; }
}
