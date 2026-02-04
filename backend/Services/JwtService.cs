using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Backend.Database.Entities.Users;
using Microsoft.IdentityModel.Tokens;

namespace Backend.Api.Services;

public interface IJwtService
{
    string GenerateToken(User user, IList<string> roles);
    int ExpirationHours { get; }
}

public class JwtService : IJwtService
{
    private readonly string _secretKey;
    private readonly string _issuer;
    private readonly string _audience;

    public int ExpirationHours { get; }

    public JwtService()
    {
        _secretKey =
            Environment.GetEnvironmentVariable("JWT_SECRET")
            ?? throw new InvalidOperationException("JWT_SECRET not found in environment variables");
        _issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "lexiq-api";
        _audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "lexiq-frontend";
        ExpirationHours = int.TryParse(
            Environment.GetEnvironmentVariable("JWT_EXPIRATION_HOURS"),
            out var hours
        )
            ? hours
            : 24;
    }

    public string GenerateToken(User user, IList<string> roles)
    {
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secretKey));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Name, user.UserName ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(ExpirationHours),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
