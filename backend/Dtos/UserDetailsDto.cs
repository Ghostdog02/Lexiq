namespace Backend.Api.Dtos;

public record UserDetailsDto(
    string Email,
    DateTime LastLoginDate,
    string FullName,
    DateTime RegistrationDate
);
