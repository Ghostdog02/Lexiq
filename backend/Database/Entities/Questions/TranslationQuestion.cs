using System.ComponentModel.DataAnnotations;

namespace Backend.Database.Entities.Questions
{


    /// <summary>
    /// Translation exercise - translate from source to target language
    /// </summary>
    public class TranslationQuestion : Question
    {
        [Required]
        [MaxLength(500)]
        public required string CorrectTranslation { get; set; }

        /// <summary>
        /// Alternative correct translations (JSON array)
        /// </summary>
        [MaxLength(2000)]
        public string? AlternativeTranslations { get; set; }

        [Required]
        [MaxLength(10)]
        public required string SourceLanguageCode { get; set; } // e.g., "en", "es"

        [Required]
        [MaxLength(10)]
        public required string TargetLanguageCode { get; set; } // e.g., "en", "es"

        /// <summary>
        /// How strict the matching should be (0.0 to 1.0)
        /// Lower = more lenient
        /// </summary>
        [Range(0.0, 1.0)]
        public double MatchingThreshold { get; set; } = 0.85;

        public override bool IsAnswerCorrect(string userAnswer)
        {
            // Basic exact match (you might want to implement fuzzy matching)
            if (
                string.Equals(
                    userAnswer.Trim(),
                    CorrectTranslation.Trim(),
                    StringComparison.OrdinalIgnoreCase
                )
            )
                return true;

            // Check alternatives
            if (!string.IsNullOrWhiteSpace(AlternativeTranslations))
            {
                var alternatives = AlternativeTranslations.Split(',', StringSplitOptions.TrimEntries);
                return alternatives.Any(alt =>
                    string.Equals(userAnswer.Trim(), alt, StringComparison.OrdinalIgnoreCase)
                );
            }

            // TODO: Implement fuzzy matching using MatchingThreshold
            // You could use Levenshtein distance or similar algorithm
            return false;
        }
    }
}