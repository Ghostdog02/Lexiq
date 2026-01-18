namespace Backend.Api.Dtos;

public record UpdateUserDto(string FullName, string PhoneNumber, DateTime LastLoginDate);

public record UserDetailsDto(
    string Email,
    DateTime LastLoginDate,
    string FullName,
    DateTime RegistrationDate
);

public record CreateUserDto(
    string Email,
    string FullName,
    string SecurityStamp,
    string ConcurrencyStamp,
    string PhoneNumber,
    DateTime RegistrationDate
);
