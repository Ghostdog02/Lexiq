using Backend.Database.Entities;

namespace Backend.Database.Entities.Questions
{
    /// <summary>
    /// Multiple choice question with predefined options
    /// </summary>
    public class MultipleChoiceQuestion : Question
    {
        public List<QuestionOption> Options { get; set; } = [];

        public override bool IsAnswerCorrect(string userAnswer)
        {
            var correctOption = Options.FirstOrDefault(o => o.IsCorrect);
            return correctOption?.Id.ToString() == userAnswer;
        }
    }
}
