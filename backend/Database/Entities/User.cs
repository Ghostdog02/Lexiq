using Microsoft.AspNetCore.Identity;

namespace Backend.Database.Entities;

public class User : IdentityUser
{
    public DateTime RegistrationDate { get; set; }

    public DateTime LastLoginDate { get; set; }

    public List<UserLanguage> UserLanguages { get; set; } = new List<UserLanguage>();
    
}
