using Backend.Api.Dtos;
using Backend.Database;
using Backend.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public class ExerciseService(BackendDbContext context)
{
    private readonly BackendDbContext _context = context;

    public async Task<List<Exercise>> GetExercisesByLessonIdAsync(int lessonId)
    {
        return await _context
            .Exercises.Where(e => e.LessonId == lessonId)
            .Include(e => e.Questions)
            .OrderBy(e => e.OrderIndex)
            .ToListAsync();
    }

    public async Task<Exercise?> GetExerciseByIdAsync(int id)
    {
        return await _context
            .Exercises.Include(e => e.Questions)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

            public async Task<Exercise> CreateExerciseAsync(CreateExerciseDto dto)
            {
                var exercise = new Exercise
                {
                    LessonId = dto.LessonId,
                    Title = dto.Title,
                    Instructions = dto.Instructions,
                    EstimatedDurationMinutes = dto.EstimatedDurationMinutes,
                    DifficultyLevel = dto.DifficultyLevel,
                    Points = dto.Points,
                    OrderIndex = dto.OrderIndex,
                    CreatedAt = DateTime.UtcNow
                };
    
                _context.Exercises.Add(exercise);
                await _context.SaveChangesAsync();
                return exercise;
            }
    
            public async Task<Exercise?> UpdateExerciseAsync(int id, UpdateExerciseDto dto)
            {
                var exercise = await _context.Exercises.FindAsync(id);
                if (exercise == null)
                    return null;
    
                if (dto.Title != null) exercise.Title = dto.Title;
                if (dto.Instructions != null) exercise.Instructions = dto.Instructions;
                if (dto.EstimatedDurationMinutes.HasValue) exercise.EstimatedDurationMinutes = dto.EstimatedDurationMinutes.Value;
                if (dto.DifficultyLevel.HasValue) exercise.DifficultyLevel = dto.DifficultyLevel.Value;
                if (dto.Points.HasValue) exercise.Points = dto.Points.Value;
                if (dto.OrderIndex.HasValue) exercise.OrderIndex = dto.OrderIndex.Value;
    
                await _context.SaveChangesAsync();
                return exercise;
            }
    
            public async Task<bool> DeleteExerciseAsync(int id)
            {
                var exercise = await _context.Exercises.FindAsync(id);
                if (exercise == null)
                    return false;
    
                _context.Exercises.Remove(exercise);
                await _context.SaveChangesAsync();
                return true;
            }}
