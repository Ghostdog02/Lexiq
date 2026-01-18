using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Database.Entities
{
    public enum QuestionType
    {
        MultipleChoice,
        FillInBlank,
        Translation,
    }

    public abstract class Question
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ExerciseId { get; set; }

        [Required]
        [MaxLength(1000)]
        public required string QuestionText { get; set; }

        [MaxLength(500)]
        public string? QuestionAudioUrl { get; set; }

        [MaxLength(500)]
        public string? QuestionImageUrl { get; set; }

        [Required]
        public int OrderIndex { get; set; } // Position within the exercise (1, 2, 3...)

        public int Points { get; set; } = 10;

        [MaxLength(1000)]
        public string? Explanation { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(ExerciseId))]
        public Exercise Exercise { get; set; } = null!;

        public abstract bool IsAnswerCorrect(string userAnswer);
    }
}
