namespace Lexiq.Api.Dtos
{
    public record UserCreationDto(
        int Id,
        string Email,
        string FullName,
        string SecurityStamp,
        string ConcurrencyStamp,
        string PhoneNumber,
        DateTime RegistrationDate
    );
}
