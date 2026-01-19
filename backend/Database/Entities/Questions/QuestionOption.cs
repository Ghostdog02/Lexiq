using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Database.Entities.Questions
{
    public class QuestionOption
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int QuestionId { get; set; }

        [Required]
        [MaxLength(500)]
        public required string OptionText { get; set; }

        [Required]
        public bool IsCorrect { get; set; } = false;

        [Required]
        public int OrderIndex { get; set; } // Display order (A, B, C, D)

        [ForeignKey(nameof(QuestionId))]
        public MultipleChoiceQuestion Question { get; set; } = null!;
    }
}
