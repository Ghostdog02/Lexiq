namespace DevGuard.Api.Dtos
{
    public record UpdatedUserDto(string FullName,
                                string PhoneNumber,
                                DateTime LastLoginDate);
}
