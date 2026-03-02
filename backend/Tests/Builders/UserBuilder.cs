using Backend.Database.Entities.Users;

namespace Backend.Tests.Builders;

/// <summary>
/// Fluent builder for User entities that bypasses UserManager.
/// ASP.NET Core Identity enforces unique indexes on NormalizedUserName and
/// NormalizedEmail — these must be set manually when inserting via DbContext.
/// </summary>
public class UserBuilder
{
    private string _id = Guid.NewGuid().ToString();
    private string? _userName = null;
    private string? _normalizedUserName = null;
    private string? _email = null;
    private string? _normalizedEmail = null;
    private int _totalPointsEarned = 0;

    public UserBuilder WithId(string id)
    {
        _id = id;
        return this;
    }

    public UserBuilder WithTotalPoints(int points)
    {
        _totalPointsEarned = points;
        return this;
    }

    public UserBuilder WithUserName(string name)
    {
        _userName = name;
        _normalizedUserName = name.ToUpperInvariant();
        return this;
    }

    public UserBuilder WithEmail(string email)
    {
        _email = email;
        _normalizedEmail = email.ToUpperInvariant();
        return this;
    }

    /// <summary>Sets UserName and NormalizedUserName to null (fallback-to-email scenario).</summary>
    public UserBuilder WithNullUserName()
    {
        _userName = null;
        _normalizedUserName = null;
        return this;
    }

    /// <summary>Sets Email and NormalizedEmail to null (fallback-to-Unknown scenario).</summary>
    public UserBuilder WithNullEmail()
    {
        _email = null;
        _normalizedEmail = null;
        return this;
    }

    public User Build() =>
        new()
        {
            Id = _id,
            UserName = _userName,
            NormalizedUserName = _normalizedUserName,
            Email = _email,
            NormalizedEmail = _normalizedEmail,
            SecurityStamp = Guid.NewGuid().ToString(),
            ConcurrencyStamp = Guid.NewGuid().ToString(),
            RegistrationDate = DateTime.UtcNow,
            LastLoginDate = DateTime.UtcNow,
            TotalPointsEarned = _totalPointsEarned,
        };
}
