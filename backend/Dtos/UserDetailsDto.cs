namespace Backend.Dtos
{
    public record UserDetailsDto(
        string Id,
        string Email,
        DateTime LastLoginDate,
        string FullName,
        DateTime RegistrationDate
    );
}
