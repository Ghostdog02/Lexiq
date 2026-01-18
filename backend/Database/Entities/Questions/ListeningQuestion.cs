using System.ComponentModel.DataAnnotations;

namespace Backend.Database.Entities.Questions
{
    public class ListeningQuestion : Question
    {
        [Required]
        [MaxLength(500)]
        public required string AudioUrl { get; set; }

        [Required]
        [MaxLength(500)]
        public required string CorrectAnswer { get; set; }

        [MaxLength(1000)]
        public string? AcceptedAnswers { get; set; }

        [Required]
        public bool CaseSensitive { get; set; } = false;

        /// <summary>
        /// Number of times user can replay audio
        /// </summary>
        [Range(1, 10)]
        public int MaxReplays { get; set; } = 3;
    }
}
