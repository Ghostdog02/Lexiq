namespace Backend.Api.Dtos;

public record GoogleLoginDto
{
    public required string Email { get; init; }

    public required string UserName { get; init; }
}
