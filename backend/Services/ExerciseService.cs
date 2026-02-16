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
        if (string.IsNullOrEmpty(dto.LessonId))
            throw new ArgumentException("LessonId is required when creating an exercise directly.");

        int orderIndex = dto.OrderIndex ?? await GetNextOrderIndexForLessonAsync(dto.LessonId);

        var exercise = MapToEntity(dto, dto.LessonId, orderIndex);

        _context.Exercises.Add(exercise);
        await _context.SaveChangesAsync();
        return exercise;
    }

    public Exercise MapToEntity(CreateExerciseDto dto, string lessonId, int orderIndex)
    {
        Exercise exercise = dto switch
        {
            CreateMultipleChoiceExerciseDto mcDto => new MultipleChoiceExercise
            {
                LessonId = lessonId,
                Title = mcDto.Title,
                Instructions = mcDto.Instructions,
                EstimatedDurationMinutes = mcDto.EstimatedDurationMinutes,
                DifficultyLevel = mcDto.DifficultyLevel,
                Points = mcDto.Points,
                OrderIndex = orderIndex,
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
                LessonId = lessonId,
                Title = fibDto.Title,
                Instructions = fibDto.Instructions,
                EstimatedDurationMinutes = fibDto.EstimatedDurationMinutes,
                DifficultyLevel = fibDto.DifficultyLevel,
                Points = fibDto.Points,
                OrderIndex = orderIndex,
                Explanation = fibDto.Explanation,
                Text = fibDto.Text,
                CorrectAnswer = fibDto.CorrectAnswer,
                AcceptedAnswers = fibDto.AcceptedAnswers,
                CaseSensitive = fibDto.CaseSensitive,
                TrimWhitespace = fibDto.TrimWhitespace,
            },
            CreateListeningExerciseDto lDto => new ListeningExercise
            {
                LessonId = lessonId,
                Title = lDto.Title,
                Instructions = lDto.Instructions,
                EstimatedDurationMinutes = lDto.EstimatedDurationMinutes,
                DifficultyLevel = lDto.DifficultyLevel,
                Points = lDto.Points,
                OrderIndex = orderIndex,
                Explanation = lDto.Explanation,
                AudioUrl = lDto.AudioUrl,
                CorrectAnswer = lDto.CorrectAnswer,
                AcceptedAnswers = lDto.AcceptedAnswers,
                CaseSensitive = lDto.CaseSensitive,
                MaxReplays = lDto.MaxReplays,
            },
            CreateTranslationExerciseDto tDto => new TranslationExercise
            {
                LessonId = lessonId,
                Title = tDto.Title,
                Instructions = tDto.Instructions,
                EstimatedDurationMinutes = tDto.EstimatedDurationMinutes,
                DifficultyLevel = tDto.DifficultyLevel,
                Points = tDto.Points,
                OrderIndex = orderIndex,
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

    public async Task<bool> UnlockNextExerciseAsync(string currentExerciseId)
    {
        var currentExercise = await _context.Exercises.FindAsync(currentExerciseId);
        if (currentExercise == null)
            return false;

        // Find the next exercise in the same lesson by OrderIndex
        var nextExercise = await _context
            .Exercises.Where(e =>
                e.LessonId == currentExercise.LessonId
                && e.OrderIndex > currentExercise.OrderIndex
            )
            .OrderBy(e => e.OrderIndex)
            .FirstOrDefaultAsync();

        if (nextExercise == null)
            return false; // No next exercise in this lesson

        if (!nextExercise.IsLocked)
            return false; // Already unlocked

        nextExercise.IsLocked = false;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> UnlockFirstExerciseInLessonAsync(string lessonId)
    {
        var firstExercise = await _context
            .Exercises.Where(e => e.LessonId == lessonId)
            .OrderBy(e => e.OrderIndex)
            .FirstOrDefaultAsync();

        if (firstExercise == null)
            return false; // No exercises in this lesson

        if (!firstExercise.IsLocked)
            return false; // Already unlocked

        firstExercise.IsLocked = false;
        await _context.SaveChangesAsync();

        return true;
    }

    private async Task<int> GetNextOrderIndexForLessonAsync(string lessonId)
    {
        var maxOrderIndex = await _context
            .Exercises.Where(e => e.LessonId == lessonId)
            .MaxAsync(e => (int?)e.OrderIndex);

        return (maxOrderIndex ?? -1) + 1;
    }
}
