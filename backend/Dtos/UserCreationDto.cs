namespace Backend.Dtos;

public record UserCreationDto(
    string Email,
    string FullName,
    string SecurityStamp,
    string ConcurrencyStamp,
    string PhoneNumber,
    DateTime RegistrationDate
);
