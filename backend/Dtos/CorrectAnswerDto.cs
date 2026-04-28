namespace Backend.Api.Dtos;

/// <summary>
/// Response DTO for the correct answer endpoint (test/content creator use)
/// </summary>
public record CorrectAnswerDto
{
    public string? CorrectAnswer { get; init; }
}
