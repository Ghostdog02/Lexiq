using Microsoft.AspNetCore.Identity;

namespace Backend.Database.Entities.Users;

public class User : IdentityUser
{
    public DateTime RegistrationDate { get; set; }

    public DateTime LastLoginDate { get; set; }

    public List<UserLanguage> UserLanguages { get; set; } = [];

    public List<UserExerciseProgress> ExerciseProgress { get; set; } = [];

    public int TotalPointsEarned { get; set; }
}
