using Backend.Database.Entities;

namespace Backend.Api.Dtos;

public record ExerciseDto(
    int Id,
    int LessonId,
    string Title,
    string? Instructions,
    int? EstimatedDurationMinutes,
    DifficultyLevel? DifficultyLevel,
    int Points,
    int OrderIndex,
    int QuestionCount
);

public record CreateExerciseDto(
    int LessonId,
    string Title,
    string? Instructions,
    int? EstimatedDurationMinutes,
    DifficultyLevel? DifficultyLevel,
    int Points,
    int OrderIndex
);

public record UpdateExerciseDto(
    string? Title,
    string? Instructions,
    int? EstimatedDurationMinutes,
    DifficultyLevel? DifficultyLevel,
    int? Points,
    int? OrderIndex
);