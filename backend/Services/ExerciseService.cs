using Backend.Api.Dtos;
using Backend.Database;
using Backend.Database.Entities.Exercises;
using Microsoft.EntityFrameworkCore;

namespace Backend.Api.Services;

public class ExerciseService(BackendDbContext context)
{
    private readonly BackendDbContext _context = context;

    public async Task<List<Exercise>> GetExercisesByLessonIdAsync(string lessonId)
    {
        return await _context
            .Exercises.Where(e => e.LessonId == lessonId)
            .Include(e => (e as MultipleChoiceExercise)!.Options)
            .OrderBy(e => e.OrderIndex)
            .ToListAsync();
    }

    public async Task<Exercise?> GetExerciseByIdAsync(string id)
    {
        return await _context
            .Exercises.Include(e => (e as MultipleChoiceExercise)!.Options)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<Exercise> CreateExerciseAsync(CreateExerciseDto dto)
    {
        Exercise exercise = dto switch
        {
            CreateMultipleChoiceExerciseDto mcDto => new MultipleChoiceExercise
            {
                LessonId = mcDto.LessonId,
                Title = mcDto.Title,
                Instructions = mcDto.Instructions,
                EstimatedDurationMinutes = mcDto.EstimatedDurationMinutes,
                DifficultyLevel = mcDto.DifficultyLevel,
                Points = mcDto.Points,
                OrderIndex = mcDto.OrderIndex,
                Explanation = mcDto.Explanation,
                Options = mcDto
                    .Options.Select(o => new ExerciseOption
                    {
                        OptionText = o.OptionText,
                        IsCorrect = o.IsCorrect,
                        OrderIndex = o.OrderIndex,
                    })
                    .ToList(),
            },
            CreateFillInBlankExerciseDto fibDto => new FillInBlankExercise
            {
                LessonId = fibDto.LessonId,
                Title = fibDto.Title,
                Instructions = fibDto.Instructions,
                EstimatedDurationMinutes = fibDto.EstimatedDurationMinutes,
                DifficultyLevel = fibDto.DifficultyLevel,
                Points = fibDto.Points,
                OrderIndex = fibDto.OrderIndex,
                Explanation = fibDto.Explanation,
                Text = fibDto.Text,
                CorrectAnswer = fibDto.CorrectAnswer,
                AcceptedAnswers = fibDto.AcceptedAnswers,
                CaseSensitive = fibDto.CaseSensitive,
                TrimWhitespace = fibDto.TrimWhitespace,
            },
            CreateListeningExerciseDto lDto => new ListeningExercise
            {
                LessonId = lDto.LessonId,
                Title = lDto.Title,
                Instructions = lDto.Instructions,
                EstimatedDurationMinutes = lDto.EstimatedDurationMinutes,
                DifficultyLevel = lDto.DifficultyLevel,
                Points = lDto.Points,
                OrderIndex = lDto.OrderIndex,
                Explanation = lDto.Explanation,
                AudioUrl = lDto.AudioUrl,
                CorrectAnswer = lDto.CorrectAnswer,
                AcceptedAnswers = lDto.AcceptedAnswers,
                CaseSensitive = lDto.CaseSensitive,
                MaxReplays = lDto.MaxReplays,
            },
            CreateTranslationExerciseDto tDto => new TranslationExercise
            {
                LessonId = tDto.LessonId,
                Title = tDto.Title,
                Instructions = tDto.Instructions,
                EstimatedDurationMinutes = tDto.EstimatedDurationMinutes,
                DifficultyLevel = tDto.DifficultyLevel,
                Points = tDto.Points,
                OrderIndex = tDto.OrderIndex,
                Explanation = tDto.Explanation,
                SourceText = tDto.SourceText,
                TargetText = tDto.TargetText,
                SourceLanguageCode = tDto.SourceLanguageCode,
                TargetLanguageCode = tDto.TargetLanguageCode,
                MatchingThreshold = tDto.MatchingThreshold,
            },
            _ => throw new ArgumentException("Unknown exercise type", nameof(dto)),
        };

        exercise.CreatedAt = DateTime.UtcNow;

        _context.Exercises.Add(exercise);
        await _context.SaveChangesAsync();
        return exercise;
    }

    public async Task<Exercise?> UpdateExerciseAsync(string id, UpdateExerciseDto dto)
    {
        var exercise = await _context.Exercises.FindAsync(id);
        if (exercise == null)
            return null;

        if (dto.Title != null)
            exercise.Title = dto.Title;

        if (dto.Instructions != null)
            exercise.Instructions = dto.Instructions;

        if (dto.EstimatedDurationMinutes.HasValue)
            exercise.EstimatedDurationMinutes = dto.EstimatedDurationMinutes.Value;

        if (dto.DifficultyLevel.HasValue)
            exercise.DifficultyLevel = dto.DifficultyLevel.Value;

        if (dto.Points.HasValue)
            exercise.Points = dto.Points.Value;

        if (dto.OrderIndex.HasValue)
            exercise.OrderIndex = dto.OrderIndex.Value;

        if (dto.Explanation != null)
            exercise.Explanation = dto.Explanation;

        await _context.SaveChangesAsync();
        return exercise;
    }

    public async Task<bool> DeleteExerciseAsync(string id)
    {
        var exercise = await _context.Exercises.FindAsync(id);
        if (exercise == null)
            return false;

        _context.Exercises.Remove(exercise);
        await _context.SaveChangesAsync();
        return true;
    }
}
