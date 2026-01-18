using System.ComponentModel.DataAnnotations;
using Backend.Database.Entities;

namespace Backend.Database.Entities.Questions
{

    /// <summary>
    /// Listening comprehension - answer based on audio
    /// </summary>
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

        public override bool IsAnswerCorrect(string userAnswer)
        {
            var comparison = CaseSensitive
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;

            if (string.Equals(userAnswer.Trim(), CorrectAnswer.Trim(), comparison))
                return true;

            if (!string.IsNullOrWhiteSpace(AcceptedAnswers))
            {
                var alternatives = AcceptedAnswers.Split(',', StringSplitOptions.TrimEntries);
                return alternatives.Any(alt => string.Equals(userAnswer.Trim(), alt, comparison));
            }

            return false;
        }
    }
}