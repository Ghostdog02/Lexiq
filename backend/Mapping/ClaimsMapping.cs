using System.Security.Claims;
using Backend.Api.Dtos;

namespace Backend.Api.Mapping;

public static class ClaimsMapping
{
    public static ClaimsDto ToClaimsDto(this IEnumerable<Claim> claims)
    {
        if (!claims.Any())
        {
            throw new ArgumentException($"Given claims are either null or empty");
        }

        var nameClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name);
        var emailClaim = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email);

        if (nameClaim == null)
        {
            throw new ArgumentException("Required claim name is missing.");
        }

        if (emailClaim == null)
        {
            throw new ArgumentException("Required claim email is missing.");
        }

        return new ClaimsDto(nameClaim.Value, emailClaim.Value);
    }

    public static CreateUserDto ToUserCreationDto(this ClaimsDto dto)
    {
        var userCreationDto = new CreateUserDto(
            dto.Email!,
            dto.FullName!,
            Guid.NewGuid().ToString(), // SecurityStamp
            Guid.NewGuid().ToString(), // ConcurrencyStamp
            string.Empty,
            DateTime.Now
        );

        return userCreationDto;
    }
}
