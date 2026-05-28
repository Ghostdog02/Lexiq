using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Backend.Database.Entities.Users;

public class User : IdentityUser
{
    public DateTime RegistrationDate { get; set; }

    public DateTime LastLoginDate { get; set; }

    public List<UserLanguage> UserLanguages { get; set; } = [];

    public List<UserExerciseProgress> ExerciseProgress { get; set; } = [];

    public int TotalPointsEarned { get; set; }

    public int Hearts { get; set; } = 5;

    public int TimesOnTop { get; set; } = 0;

    public DateTime? LastTimesOnTopAt { get; set; }

    public DateTime LastHeartResetAt { get; set; } = DateTime.UtcNow;

    public UserAvatar? Avatar { get; set; }
}
