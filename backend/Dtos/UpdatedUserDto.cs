namespace Backend.Dtos
{
    public record UpdatedUserDto(string FullName,
                                string PhoneNumber,
                                DateTime LastLoginDate);
}
