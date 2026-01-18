using System.ComponentModel.DataAnnotations;
using Backend.Database.Entities;

namespace Backend.Database.Entities.Questions
{


    /// <summary>
    /// Fill in the blank - user types the answer
    /// </summary>
    public class FillInBlankQuestion : Question
    {
        [Required]
        [MaxLength(500)]
        public required string CorrectAnswer { get; set; }

        /// <summary>
        /// Accepted alternative answers (comma-separated or JSON array)
        /// </summary>
        [MaxLength(1000)]
        public string? AcceptedAnswers { get; set; }

        [Required]
        public bool CaseSensitive { get; set; } = false;

        [Required]
        public bool TrimWhitespace { get; set; } = true;

        public override bool IsAnswerCorrect(string userAnswer)
        {
            var processedAnswer = TrimWhitespace ? userAnswer.Trim() : userAnswer;
            var correctAnswer = TrimWhitespace ? CorrectAnswer.Trim() : CorrectAnswer;

            var comparison = CaseSensitive
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;

            if (string.Equals(processedAnswer, correctAnswer, comparison))
                return true;

            // Check alternative answers
            if (!string.IsNullOrWhiteSpace(AcceptedAnswers))
            {
                var alternatives = AcceptedAnswers.Split(',', StringSplitOptions.TrimEntries);
                return alternatives.Any(alt => string.Equals(processedAnswer, alt, comparison));
            }

            return false;
        }
    }
}