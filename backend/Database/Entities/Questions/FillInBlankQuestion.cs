using System.ComponentModel.DataAnnotations;
using Backend.Database.Entities;

namespace Backend.Database.Entities.Questions
{
    public class FillInBlankQuestion : Question
    {
        [Required]
        [MaxLength(500)]
        public required string CorrectAnswer { get; set; }

        [MaxLength(1000)]
        public string? AcceptedAnswers { get; set; }

        [Required]
        public bool CaseSensitive { get; set; } = false;

        [Required]
        public bool TrimWhitespace { get; set; } = true;
    }
}
