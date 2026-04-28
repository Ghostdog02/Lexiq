namespace Backend.Api.Dtos;

public record UpdateUserDto
{
    public required string FullName { get; init; }

    public required string PhoneNumber { get; init; }

    public required DateTime LastLoginDate { get; init; }
}

public record UserDetailsDto
{
    public required string Email { get; init; }

    public required DateTime LastLoginDate { get; init; }

    public required string FullName { get; init; }

    public required DateTime RegistrationDate { get; init; }
}

public record UserManagementUpdateDto
{
    public required string UserId { get; init; }

    public required string UserName { get; init; }

    public required string Email { get; init; }

    public required string PhoneNumber { get; init; }

    public required DateTime LastLoginDate { get; init; }
}

public record UserXpDto
{
    public required string UserId { get; init; }

    public required int TotalXp { get; init; }

    public required int CompletedExercises { get; init; }

    public DateTime? LastActivityAt { get; init; }
}
