using System.Text;
using Backend.Database.Entities;
using Backend.Dtos;

namespace Backend.Mapping
{
    public static class UserMapping
    {   
        public static UserDetailsDto MapUserToDto(this User user)
        {
            UserDetailsDto dto = new(
                user.Id,
                user.Email!,
                user.LastLoginDate,
                user.UserName!,
                user.RegistrationDate
            );

            return dto;
        }

        public static IQueryable<UserDetailsDto> MapUsersToDtos(this IQueryable<User> users)
        {
            return users.Select(user => user.MapUserToDto());
        }

        public static User ToEntity(this UserCreationDto dto)
        {
            string cleanFullName = dto.FullName.RemoveInvalidCharacters();

            return new User
            {
                Email = dto.Email,
                UserName = cleanFullName,
                SecurityStamp = dto.SecurityStamp,
                ConcurrencyStamp = dto.ConcurrencyStamp,
                PhoneNumber = dto.PhoneNumber,
                PhoneNumberConfirmed = dto.PhoneNumber != null || dto.PhoneNumber != string.Empty,
                RegistrationDate = dto.RegistrationDate,
                NormalizedEmail = dto.Email.Normalize(),
                NormalizedUserName = cleanFullName.Normalize(),
            };
        }

        private static string RemoveInvalidCharacters(this string name)
        {
            StringBuilder builder = new();

            foreach (var character in name)
            {
                if (char.IsDigit(character) || char.IsLetter(character))
                {
                    builder.Append(character);
                }
            }

            return builder.ToString();
        }

        public static User ToEntity(this UpdatedUserDto dto, string id)
        {
            return new User
            {
                Id = id,
                UserName = dto.FullName,
                PhoneNumber = dto.PhoneNumber,
                PhoneNumberConfirmed = dto.PhoneNumber != null || dto.PhoneNumber != string.Empty,
            };
        }
    }
}
