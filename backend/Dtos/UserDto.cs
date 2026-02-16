namespace Backend.Api.Dtos;

public record UpdateUserDto(string FullName, string PhoneNumber, DateTime LastLoginDate);

public record UserDetailsDto(
    string Email,
    DateTime LastLoginDate,
    string FullName,
    DateTime RegistrationDate
);

public record UserXpDto(
    string UserId,
    int TotalXp,
    int CompletedExercises,
    DateTime? LastActivityAt
);
