using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Backend.Api.Services;
using Backend.Database.Entities.Users;
using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Backend.Tests.Services;

/// <summary>
/// Pure unit tests for JwtService.GenerateToken.
/// No database or HTTP needed — sets env vars, generates a token, parses and validates it.
///
/// JWT_SECRET must be ≥32 UTF-8 bytes for HS256.
/// </summary>
public class JwtServiceTests : IDisposable
{
    private const string TestSecret = "test-super-secret-key-minimum-32-chars-long!";
    private const string TestIssuer = "test-issuer";
    private const string TestAudience = "test-audience";

    private readonly JwtService _sut;

    public JwtServiceTests()
    {
        Environment.SetEnvironmentVariable("JWT_SECRET", TestSecret);
        Environment.SetEnvironmentVariable("JWT_ISSUER", TestIssuer);
        Environment.SetEnvironmentVariable("JWT_AUDIENCE", TestAudience);
        Environment.SetEnvironmentVariable("JWT_EXPIRATION_HOURS", "24");
        _sut = new JwtService();
    }

    public void Dispose()
    {
        Environment.SetEnvironmentVariable("JWT_SECRET", null);
        Environment.SetEnvironmentVariable("JWT_ISSUER", null);
        Environment.SetEnvironmentVariable("JWT_AUDIENCE", null);
        Environment.SetEnvironmentVariable("JWT_EXPIRATION_HOURS", null);
        GC.SuppressFinalize(this);
    }

    private static User MakeUser() =>
        new()
        {
            Id = Guid.NewGuid().ToString(),
            Email = "user@example.com",
            UserName = "testuser",
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
        };

    /// <summary>Validates the JWT and returns the parsed token — throws on any validation failure.</summary>
    private static JwtSecurityToken ValidateAndParse(string jwt)
    {
        var handler = new JwtSecurityTokenHandler();
        handler.ValidateToken(
            jwt,
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = TestIssuer,
                ValidateAudience = true,
                ValidAudience = TestAudience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestSecret)),
            },
            out var validatedToken
        );
        
        return (JwtSecurityToken)validatedToken;
    }

    [Fact]
    public void GenerateToken_ContainsSubClaim()
    {
        var user = MakeUser();
        var token = ValidateAndParse(_sut.GenerateToken(user, []));
        token.Subject.Should().Be(user.Id);
    }

    [Fact]
    public void GenerateToken_ContainsEmailClaim()
    {
        var user = MakeUser();
        var token = ValidateAndParse(_sut.GenerateToken(user, []));
        token
            .Claims.First(c => c.Type == JwtRegisteredClaimNames.Email)
            .Value.Should()
            .Be(user.Email);
    }

    [Fact]
    public void GenerateToken_ContainsNameClaim()
    {
        var user = MakeUser();
        var token = ValidateAndParse(_sut.GenerateToken(user, []));
        token
            .Claims.First(c => c.Type == JwtRegisteredClaimNames.Name)
            .Value.Should()
            .Be(user.UserName);
    }

    [Fact]
    public void GenerateToken_ContainsJtiClaim()
    {
        var user = MakeUser();
        var token = ValidateAndParse(_sut.GenerateToken(user, []));
        token
            .Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)
            ?.Value.Should()
            .NotBeNullOrEmpty();
    }

    [Fact]
    public void GenerateToken_ContainsRoleClaims()
    {
        var user = MakeUser();
        var token = ValidateAndParse(_sut.GenerateToken(user, ["Admin", "ContentCreator"]));
        // ASP.NET Core maps ClaimTypes.Role to the full URI
        var roles = token
            .Claims.Where(c =>
                c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
            )
            .Select(c => c.Value);
        roles.Should().BeEquivalentTo(["Admin", "ContentCreator"]);
    }

    [Fact]
    public void GenerateToken_WithEmptyRoles_HasNoRoleClaims()
    {
        var user = MakeUser();
        var token = ValidateAndParse(_sut.GenerateToken(user, []));
        token
            .Claims.Where(c =>
                c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role"
            )
            .Should()
            .BeEmpty();
    }

    [Fact]
    public void GenerateToken_ExpiresInConfiguredHours()
    {
        // This is the same value used as the AuthToken cookie Expires attribute
        // (AuthController calls SetAuthCookie(token, DateTime.UtcNow.AddHours(ExpirationHours)))
        var before = DateTime.UtcNow;
        var token = ValidateAndParse(_sut.GenerateToken(MakeUser(), []));
        token.ValidTo.Should().BeCloseTo(before.AddHours(24), precision: TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void GenerateToken_CanBeValidatedWithCorrectKey()
    {
        var act = () => ValidateAndParse(_sut.GenerateToken(MakeUser(), []));
        act.Should().NotThrow();
    }

    [Fact]
    public void GenerateToken_FailsValidationWithWrongKey()
    {
        var jwt = _sut.GenerateToken(MakeUser(), []);
        var wrongKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes("wrong-key-also-at-least-32-chars!!")
        );
        var handler = new JwtSecurityTokenHandler();
        var act = () =>
            handler.ValidateToken(
                jwt,
                new TokenValidationParameters
                {
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ValidateLifetime = false,
                    IssuerSigningKey = wrongKey,
                },
                out _
            );
        act.Should().Throw<SecurityTokenSignatureKeyNotFoundException>();
    }

    [Fact]
    public void ExpirationHours_DefaultsTo24WhenEnvVarNotSet()
    {
        Environment.SetEnvironmentVariable("JWT_EXPIRATION_HOURS", null);
        new JwtService().ExpirationHours.Should().Be(24);
    }

    [Fact]
    public void ExpirationHours_ReadsFromEnvironmentVariable()
    {
        Environment.SetEnvironmentVariable("JWT_EXPIRATION_HOURS", "48");
        new JwtService().ExpirationHours.Should().Be(48);
    }

    [Fact]
    public void Constructor_ThrowsWhenJwtSecretNotSet()
    {
        Environment.SetEnvironmentVariable("JWT_SECRET", null);
        var act = () => new JwtService();
        act.Should().Throw<InvalidOperationException>().WithMessage("*JWT_SECRET*");
    }
}
