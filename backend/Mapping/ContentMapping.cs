using Backend.Api.Dtos;
using Backend.Database.Entities;
using Backend.Database.Entities.Exercises;
using Backend.Database.Entities.Users;

namespace Backend.Api.Mapping;

public static class ContentMappingExtensions
{
    public static LanguageDto ToDto(this Language entity)
    {
        return new LanguageDto(entity.LanguageName, entity.FlagIconUrl, entity.Courses.Count);
    }

    public static CourseDto ToDto(this Course entity)
    {
        return new CourseDto(
            entity.CourseId,
            entity.Title,
            entity.Language?.LanguageName ?? string.Empty,
            entity.Description,
            entity.EstimatedDurationHours,
            entity.OrderIndex,
            entity.Lessons.Count
        );
    }

    public static LessonDto ToDto(
        this Lesson entity,
        LessonProgressSummary? progress = null,
        Dictionary<string, UserExerciseProgressDto>? exerciseProgress = null
    )
    {
        return new LessonDto(
            entity.LessonId,
            entity.CourseId,
            entity.Course?.Title ?? string.Empty,
            entity.Title,
            null, // Description removed from Lesson entity
            entity.EstimatedDurationMinutes,
            0, // OrderIndex removed from return DTO
            entity.LessonContent,  // Editor.js JSON content
            entity.IsLocked,
            entity.Exercises.Select(e => e.ToDto(exerciseProgress)).ToList(),
            CompletedExercises: progress?.CompletedExercises,
            EarnedXp: progress?.EarnedXp,
            TotalPossibleXp: progress?.TotalPossibleXp,
            IsCompleted: progress?.MeetsCompletionThreshold
        );
    }

    public static ExerciseDto ToDto(
        this Exercise entity,
        Dictionary<string, UserExerciseProgressDto>? userProgress = null
    )
    {
        var progress = userProgress?.GetValueOrDefault(entity.ExerciseId);

        return entity switch
        {
            FillInBlankExercise fib => new FillInBlankExerciseDto(
                fib.ExerciseId,
                fib.LessonId,
                fib.Instructions,
                fib.DifficultyLevel,
                fib.Points,
                null,
                fib.IsLocked,
                progress,
                fib.Text,
                fib.Options.Select(o => new ExerciseOptionDto(
                        o.ExerciseOptionId,
                        o.OptionText,
                        o.IsCorrect,
                        o.Explanation
                    ))
                    .ToList()
            ),
            ListeningExercise le => new ListeningExerciseDto(
                le.ExerciseId,
                le.LessonId,
                le.Instructions,
                le.DifficultyLevel,
                le.Points,
                null,
                le.IsLocked,
                progress,
                le.AudioUrl,
                le.MaxReplays,
                le.Options.Select(o => new ExerciseOptionDto(
                        o.ExerciseOptionId,
                        o.OptionText,
                        o.IsCorrect,
                        o.Explanation
                    ))
                    .ToList()
            ),
            TrueFalseExercise tf => new TrueFalseExerciseDto(
                tf.ExerciseId,
                tf.LessonId,
                tf.Instructions,
                tf.DifficultyLevel,
                tf.Points,
                tf.Explanation,
                tf.IsLocked,
                progress,
                tf.Statement,
                tf.CorrectAnswer,
                tf.ImageUrl
            ),
            ImageChoiceExercise ice => new ImageChoiceExerciseDto(
                ice.ExerciseId,
                ice.LessonId,
                ice.Instructions,
                ice.DifficultyLevel,
                ice.Points,
                null,
                ice.IsLocked,
                progress,
                ice.Options.Select(o => new ImageOptionDto(
                        o.ImageOptionId,
                        o.ImageUrl,
                        o.AltText,
                        o.IsCorrect,
                        o.Explanation
                    ))
                    .ToList()
            ),
            AudioMatchingExercise ame => new AudioMatchingExerciseDto(
                ame.ExerciseId,
                ame.LessonId,
                ame.Instructions,
                ame.DifficultyLevel,
                ame.Points,
                null,
                ame.IsLocked,
                progress,
                ame.Pairs.Select(p => new AudioMatchPairDto(
                        p.AudioMatchPairId,
                        p.AudioUrl,
                        p.ImageUrl,
                        p.Explanation
                    ))
                    .ToList()
            ),
            _ => throw new NotImplementedException(
                $"Mapping for exercise type {entity.GetType().Name} is not implemented"
            ),
        };
    }

    public static UserLanguageDto ToDto(this UserLanguage entity)
    {
        return new UserLanguageDto(
            entity.UserId,
            entity.LanguageId,
            entity.Language?.LanguageName ?? string.Empty,
            entity.Language?.FlagIconUrl,
            entity.EnrolledAt,
            0, // Placeholder
            0 // Placeholder
        );
    }
}