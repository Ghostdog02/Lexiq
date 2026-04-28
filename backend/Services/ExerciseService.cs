using Backend.Api.Dtos;
using Backend.Api.Mapping;
using Backend.Database;
using Backend.Database.Entities.Exercises;
using Microsoft.EntityFrameworkCore;

namespace Backend.Api.Services;

public class ExerciseService(BackendDbContext context, ContentMapping mapper)
{
    private readonly BackendDbContext _context = context;
    private readonly ContentMapping _mapper = mapper;

    public async Task<List<Exercise>> GetExercisesByLessonIdAsync(string lessonId)
    {
        return await _context
            .Exercises.Where(e => e.LessonId == lessonId)
            .Include(e => (e as ListeningExercise)!.Options)
            .Include(e => (e as ImageChoiceExercise)!.Options)
            .Include(e => (e as AudioMatchingExercise)!.Pairs)
            .OrderBy(e => e.OrderIndex)
            .ToListAsync();
    }

    public async Task<Exercise?> GetExerciseByIdAsync(string id)
    {
        return await _context
            .Exercises
            .Include(e => (e as ListeningExercise)!.Options)
            .Include(e => (e as ImageChoiceExercise)!.Options)
            .Include(e => (e as AudioMatchingExercise)!.Pairs)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<Exercise> CreateExerciseAsync(CreateExerciseDto dto)
    {
        if (string.IsNullOrEmpty(dto.LessonId))
            throw new ArgumentException("LessonId is required when creating an exercise directly.");

        var exercise = MapToEntity(dto, dto.LessonId);

        _context.Exercises.Add(exercise);
        await _context.SaveChangesAsync();
        return exercise;
    }

    public Exercise MapToEntity(CreateExerciseDto dto, string lessonId)
    {
        Exercise exercise = dto switch
        {
            CreateFillInBlankExerciseDto fibDto => new FillInBlankExercise
            {
                LessonId = lessonId,
                Title = fibDto.Title,
                Question = fibDto.Question,
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
                WordBank = fibDto.WordBank,
            },
            CreateListeningExerciseDto lDto => new ListeningExercise
            {
                LessonId = lessonId,
                Title = lDto.Title,
                Question = lDto.Question,
                EstimatedDurationMinutes = lDto.EstimatedDurationMinutes,
                DifficultyLevel = lDto.DifficultyLevel,
                Points = lDto.Points,
                OrderIndex = orderIndex,
                Explanation = lDto.Explanation,
                AudioUrl = lDto.AudioUrl,
                MaxReplays = lDto.MaxReplays,
                Options = lDto.Options.Select(o => new ExerciseOption
                {
                    OptionText = o.OptionText,
                    IsCorrect = o.IsCorrect,
                    OrderIndex = o.OrderIndex,
                }).ToList(),
            },
            CreateTrueFalseExerciseDto tfDto => new TrueFalseExercise
            {
                LessonId = lessonId,
                Title = tfDto.Title,
                Question = tfDto.Question,
                EstimatedDurationMinutes = tfDto.EstimatedDurationMinutes,
                DifficultyLevel = tfDto.DifficultyLevel,
                Points = tfDto.Points,
                OrderIndex = orderIndex,
                Explanation = tfDto.Explanation,
                Statement = tfDto.Statement,
                CorrectAnswer = tfDto.CorrectAnswer,
                ImageUrl = tfDto.ImageUrl,
            },
            CreateImageChoiceExerciseDto icDto => new ImageChoiceExercise
            {
                LessonId = lessonId,
                Title = icDto.Title,
                Question = icDto.Question,
                EstimatedDurationMinutes = icDto.EstimatedDurationMinutes,
                DifficultyLevel = icDto.DifficultyLevel,
                Points = icDto.Points,
                OrderIndex = orderIndex,
                Explanation = icDto.Explanation,
                Options = icDto.Options.Select(o => new ImageOption
                {
                    ImageUrl = o.ImageUrl,
                    AltText = o.AltText,
                    IsCorrect = o.IsCorrect,
                    OrderIndex = o.OrderIndex,
                }).ToList(),
            },
            CreateAudioMatchingExerciseDto amDto => new AudioMatchingExercise
            {
                LessonId = lessonId,
                Title = amDto.Title,
                Question = amDto.Question,
                EstimatedDurationMinutes = amDto.EstimatedDurationMinutes,
                DifficultyLevel = amDto.DifficultyLevel,
                Points = amDto.Points,
                OrderIndex = orderIndex,
                Explanation = amDto.Explanation,
                Pairs = amDto.Pairs.Select((p, i) => new AudioMatchPair
                {
                    AudioUrl = p.AudioUrl,
                    ImageUrl = p.ImageUrl,
                    OrderIndex = p.OrderIndex,
                }).ToList(),
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

        if (dto.Question != null)
            exercise.Question = dto.Question;

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

        var nextExercise = await _context
            .Exercises.Where(e =>
                e.LessonId == currentExercise.LessonId
                && e.OrderIndex > currentExercise.OrderIndex
            )
            .OrderBy(e => e.OrderIndex)
            .FirstOrDefaultAsync();

        if (nextExercise == null)
            return false;

        if (!nextExercise.IsLocked)
            return false;

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
            return false;

        if (!firstExercise.IsLocked)
            return false;

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
