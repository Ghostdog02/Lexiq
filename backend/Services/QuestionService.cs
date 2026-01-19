using Backend.Api.Dtos;
using Backend.Database;
using Backend.Database.Entities;
using Backend.Database.Entities.Questions;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class QuestionService(BackendDbContext context)
{
    private readonly BackendDbContext _context = context;

    public async Task<List<Question>> GetQuestionsByExerciseIdAsync(int exerciseId)
    {
        return await _context
            .Questions.Where(q => q.ExerciseId == exerciseId)
            .OrderBy(q => q.OrderIndex)
            .ToListAsync();
    }

    public async Task<Question?> GetQuestionByIdAsync(int id)
    {
        return await _context.Questions.FindAsync(id);
    }

    public async Task<Question> CreateQuestionAsync(CreateQuestionDto dto)
    {
        var exercise = await _context.Exercises.FirstOrDefaultAsync(e => e.Title == dto.ExerciseName);
        if (exercise == null)
        {
            throw new ArgumentException($"Exercise '{dto.ExerciseName}' not found.");
        }

        Question question;

        switch (dto.QuestionType)
        {
            case "MultipleChoice":
                var mcq = new MultipleChoiceQuestion
                {
                    ExerciseId = exercise.Id,
                    QuestionText = dto.QuestionText,
                    QuestionAudioUrl = dto.QuestionAudioUrl,
                    QuestionImageUrl = dto.QuestionImageUrl,
                    OrderIndex = dto.OrderIndex,
                    Points = dto.Points,
                    Explanation = dto.Explanation,
                    Options = []
                };
                if (dto.Options != null)
                {
                    foreach (var optDto in dto.Options)
                    {
                        mcq.Options.Add(
                            new QuestionOption
                            {
                                OptionText = optDto.OptionText,
                                IsCorrect = optDto.IsCorrect,
                                OrderIndex = optDto.OrderIndex
                            }
                        );
                    }
                }
                question = mcq;
                break;

            case "FillInBlank":
                question = new FillInBlankQuestion
                {
                    ExerciseId = exercise.Id,
                    QuestionText = dto.QuestionText,
                    QuestionAudioUrl = dto.QuestionAudioUrl,
                    QuestionImageUrl = dto.QuestionImageUrl,
                    OrderIndex = dto.OrderIndex,
                    Points = dto.Points,
                    Explanation = dto.Explanation,
                    CorrectAnswer = dto.CorrectAnswer ?? string.Empty,
                    AcceptedAnswers = dto.AcceptedAnswers,
                    CaseSensitive = dto.CaseSensitive ?? false,
                    TrimWhitespace = dto.TrimWhitespace ?? true
                };
                break;

            case "Translation":
                question = new TranslationQuestion
                {
                    ExerciseId = exercise.Id,
                    QuestionText = dto.QuestionText,
                    QuestionAudioUrl = dto.QuestionAudioUrl,
                    QuestionImageUrl = dto.QuestionImageUrl,
                    OrderIndex = dto.OrderIndex,
                    Points = dto.Points,
                    Explanation = dto.Explanation,
                    SourceLanguageCode = dto.SourceLanguageCode ?? "en",
                    TargetLanguageCode = dto.TargetLanguageCode ?? "en",
                    MatchingThreshold = dto.MatchingThreshold ?? 0.85
                };
                break;

            case "Listening":
                question = new ListeningQuestion
                {
                    ExerciseId = exercise.Id,
                    QuestionText = dto.QuestionText,
                    QuestionAudioUrl = dto.QuestionAudioUrl,
                    QuestionImageUrl = dto.QuestionImageUrl,
                    OrderIndex = dto.OrderIndex,
                    Points = dto.Points,
                    Explanation = dto.Explanation,
                    AudioUrl = dto.AudioUrl ?? string.Empty,
                    CorrectAnswer = dto.CorrectAnswer ?? string.Empty,
                    MaxReplays = dto.MaxReplays ?? 3,
                    CaseSensitive = dto.CaseSensitive ?? false
                };
                break;

            default:
                throw new ArgumentException("Invalid question type");
        }

        _context.Questions.Add(question);
        await _context.SaveChangesAsync();
        return question;
    }

    public async Task<bool> DeleteQuestionAsync(int id)
    {
        var question = await _context.Questions.FindAsync(id);
        if (question == null)
            return false;

        _context.Questions.Remove(question);
        await _context.SaveChangesAsync();
        return true;
    }
}
