namespace Backend.Api.Dtos;

public record GoogleLoginRequestDto
{
    public required string IdToken { get; init; }
}

public record AuthStatusResponseDto
{
    public required string Message { get; init; }

    public required bool IsLogged { get; init; }
}

public record IsAdminResponseDto
{
    public required bool IsAdmin { get; init; }

    public required List<string> Roles { get; init; }
}
