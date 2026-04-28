namespace Backend.Api.Dtos;

public record UserRoleDto
{
    public required string UserId { get; init; }

    public required string RoleName { get; init; }
}

public record UserRoleDetailsDto
{
    public required string Email { get; init; }

    public required string FullName { get; init; }

    public required string Role { get; init; }
}
