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
            entity.Language?.LanguageName ?? string.Empty,
            entity.Title,
            entity.Description,
            entity.EstimatedDurationHours,
            entity.OrderIndex,
            entity.Lessons.Count
        );
    }

    public static LessonDto ToDto(
        this Lesson entity,
        LessonProgressSummary? progress = null,
        Dictionary<string, UserExerciseProgressDto>? exerciseProgress = null,
        bool? isLockedOverride = null
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
            isLockedOverride ?? entity.IsLocked,
            entity.Exercises.Select(e => e.ToDto(exerciseProgress)).ToList(),
            CompletedExercises: progress?.CompletedExercises,
            EarnedXp: progress?.EarnedXp,
            TotalPossibleXp: progress?.TotalPossibleXp,
            IsCompleted: progress?.IsCompleted
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
                null,
                progress,
                tf.Statement,
                tf.ImageUrl,
                tf.Options.Select(o => new ExerciseOptionDto(
                        o.ExerciseOptionId,
                        o.OptionText,
                        o.IsCorrect,
                        o.Explanation
                    ))
                    .ToList()
            ),
            ImageChoiceExercise ice => new ImageChoiceExerciseDto(
                ice.ExerciseId,
                ice.LessonId,
                ice.Instructions,
                ice.DifficultyLevel,
                ice.Points,
                null,
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
                progress,
                ame.Pairs.Select(p => new AudioMatchPairDto(
                        p.AudioMatchPairId,
                        p.AudioUrl,
                        p.ImageUrl,
                        p.Explanation
                    ))
                    .ToList()
            ),
            _ => new MultipleChoiceExerciseDto(
                entity.ExerciseId,
                entity.LessonId,
                entity.Instructions,
                entity.DifficultyLevel,
                entity.Points,
                null,
                progress,
                entity.Options.Select(o => new ExerciseOptionDto(
                    o.ExerciseOptionId,
                    o.OptionText,
                    o.IsCorrect,
                    o.Explanation
                )).ToList()
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