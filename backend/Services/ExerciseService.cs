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
            .Include(e => (e as FillInBlankExercise)!.Options)
            .Include(e => (e as ListeningExercise)!.Options)
            .Include(e => (e as ImageChoiceExercise)!.Options)
            .Include(e => (e as AudioMatchingExercise)!.Pairs)
            .ToListAsync();
    }

    public async Task<Exercise?> GetExerciseByIdAsync(string id)
    {
        return await _context
            .Exercises
            .Include(e => (e as FillInBlankExercise)!.Options)
            .Include(e => (e as ListeningExercise)!.Options)
            .Include(e => (e as ImageChoiceExercise)!.Options)
            .Include(e => (e as AudioMatchingExercise)!.Pairs)
            .FirstOrDefaultAsync(e => e.ExerciseId == id);
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
                Instructions = fibDto.Instructions,
                DifficultyLevel = fibDto.DifficultyLevel,
                Points = fibDto.Points,
                Text = fibDto.Text,
                Options = fibDto
                    .Options.Select(o => new ExerciseOption
                    {
                        OptionText = o.OptionText,
                        IsCorrect = o.IsCorrect,
                        Explanation = o.Explanation,
                    })
                    .ToList(),
            },
            CreateListeningExerciseDto lDto => new ListeningExercise
            {
                LessonId = lessonId,
                Instructions = lDto.Instructions,
                DifficultyLevel = lDto.DifficultyLevel,
                Points = lDto.Points,
                AudioUrl = lDto.AudioUrl,
                MaxReplays = lDto.MaxReplays,
                Options = lDto
                    .Options.Select(o => new ExerciseOption
                    {
                        OptionText = o.OptionText,
                        IsCorrect = o.IsCorrect,
                        Explanation = o.Explanation,
                    })
                    .ToList(),
            },
            CreateTrueFalseExerciseDto tfDto => new TrueFalseExercise
            {
                LessonId = lessonId,
                Instructions = tfDto.Instructions,
                DifficultyLevel = tfDto.DifficultyLevel,
                Points = tfDto.Points,
                Statement = tfDto.Statement,
                CorrectAnswer = tfDto.CorrectAnswer,
                ImageUrl = tfDto.ImageUrl,
                Explanation = tfDto.Explanation ?? string.Empty,
            },
            CreateImageChoiceExerciseDto icDto => new ImageChoiceExercise
            {
                LessonId = lessonId,
                Instructions = icDto.Instructions,
                DifficultyLevel = icDto.DifficultyLevel,
                Points = icDto.Points,
                Options = icDto
                    .Options.Select(o => new ImageOption
                    {
                        ImageUrl = o.ImageUrl,
                        AltText = o.AltText,
                        IsCorrect = o.IsCorrect,
                        Explanation = o.Explanation,
                    })
                    .ToList(),
            },
            CreateAudioMatchingExerciseDto amDto => new AudioMatchingExercise
            {
                LessonId = lessonId,
                Instructions = amDto.Instructions,
                DifficultyLevel = amDto.DifficultyLevel,
                Points = amDto.Points,
                Pairs = amDto
                    .Pairs.Select(p => new AudioMatchPair
                    {
                        AudioUrl = p.AudioUrl,
                        ImageUrl = p.ImageUrl,
                        Explanation = p.Explanation,
                    })
                    .ToList(),
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

        if (dto.Instructions != null)
            exercise.Instructions = dto.Instructions;

        if (dto.DifficultyLevel.HasValue)
            exercise.DifficultyLevel = dto.DifficultyLevel.Value;

        if (dto.Points.HasValue)
            exercise.Points = dto.Points.Value;

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

        // Find the next exercise in the same lesson by CreatedAt
        var nextExercise = await _context
            .Exercises.Where(e =>
                e.LessonId == currentExercise.LessonId
                && e.CreatedAt > currentExercise.CreatedAt
            )
            .OrderBy(e => e.CreatedAt)
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
            .OrderBy(e => e.CreatedAt)
            .FirstOrDefaultAsync();

        if (firstExercise == null)
            return false; // No exercises in this lesson

        if (!firstExercise.IsLocked)
            return false; // Already unlocked

        firstExercise.IsLocked = false;
        await _context.SaveChangesAsync();

        return true;
    }
}
