using System.ComponentModel.DataAnnotations;

namespace Backend.Database.Entities.Users;

public class UserAvatar
{
    [Key]
    public string UserId { get; set; } = string.Empty;

    public User User { get; set; } = null!;

    [Required]
    public byte[] Data { get; set; } = [];

    [Required]
    [MaxLength(50)]
    public string ContentType { get; set; } = "image/jpeg";
}
