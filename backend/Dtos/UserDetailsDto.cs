namespace DevGuard.Api.Dtos
{
    public record UserDetailsDto(
        int Id,
        string Email,
        DateTime LastLoginDate,
        string FullName,
        DateTime RegistrationDate
    );
}
