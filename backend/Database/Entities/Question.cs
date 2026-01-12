using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Backend.Database.Entities
{
    public enum QuestionType
    {
        MultipleChoice,
        FillInBlank,
        Translation
    }

    public class Question
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ExerciseId { get; set; }

        [Required]
        [MaxLength(1000)]
        public required string QuestionText { get; set; }

        [MaxLength(500)]
        public string? QuestionAudioUrl { get; set; } // For listening exercises

        [MaxLength(500)]
        public string? QuestionImageUrl { get; set; } // For visual questions

        [MaxLength(500)]
        public string? CorrectAnswer { get; set; } // For fill-in-blank
        
        [Required]
        public int OrderIndex { get; set; } // Position within the exercise (1, 2, 3...)

        public int Points { get; set; } = 0; // Points for this question

        [Required]
        public QuestionType ExerciseType { get; set; }

        [MaxLength(1000)]
        public string? Explanation { get; set; } // Shown after answering

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey(nameof(ExerciseId))]
        public Exercise Exercise { get; set; } = null!;

        public List<QuestionOption> QuestionOptions { get; set; } = []; // For multiple choice
    }
}
