using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Database.Entities.Users;

public class UserAvatar
{
    [Key]
    [MaxLength(450)]
    public required string UserId { get; set; }

    [Required]
    [ForeignKey(nameof(UserId))]
    public required User User { get; set; }

    [Required]
    public required byte[] Data { get; set; }

    [Required]
    [MaxLength(50)]
    public required string ContentType { get; set; }
}
