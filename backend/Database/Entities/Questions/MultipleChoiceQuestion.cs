using Backend.Database.Entities;

namespace Backend.Database.Entities.Questions
{
    public class MultipleChoiceQuestion : Question
    {
        public List<QuestionOption> Options { get; set; } = [];
    }
}
