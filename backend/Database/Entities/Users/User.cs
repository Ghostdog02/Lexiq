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

    public int Hearts { get; set; } = 3;

    public DateTime LastHeartResetAt { get; set; } = DateTime.UtcNow;

    [Required]
    public required UserAvatar Avatar { get; set; }
}
