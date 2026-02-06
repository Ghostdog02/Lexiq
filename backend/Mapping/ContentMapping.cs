using Backend.Api.Dtos;
using Backend.Database.Entities;
using Backend.Database.Entities.Exercises;
using Backend.Database.Entities.Users;

namespace Backend.Api.Mapping;

public static class ContentMappingExtensions
{
    public static LanguageDto ToDto(this Language entity)
    {
        return new LanguageDto(entity.Name, entity.FlagIconUrl, entity.Courses.Count);
    }

    public static CourseDto ToDto(this Course entity)
    {
        return new CourseDto(
            entity.Id,
            entity.Title,
            entity.Language?.Name ?? string.Empty,
            entity.Description,
            entity.EstimatedDurationHours,
            entity.OrderIndex,
            entity.Lessons.Count
        );
    }

    public static LessonDto ToDto(this Lesson entity)
    {
        return new LessonDto(
            entity.Id,
            entity.CourseId,
            entity.Course?.Title ?? string.Empty,
            entity.Title,
            entity.Description,
            entity.EstimatedDurationMinutes,
            entity.OrderIndex,
            entity.LessonContent,  // Editor.js JSON content
            entity.IsLocked,
            entity.Exercises.Count
        );
    }

    public static ExerciseDto ToDto(this Exercise entity)
    {
        return entity switch
        {
            MultipleChoiceExercise mce => new MultipleChoiceExerciseDto(
                mce.Id,
                mce.LessonId,
                mce.Title,
                mce.Instructions,
                mce.EstimatedDurationMinutes,
                mce.DifficultyLevel,
                mce.Points,
                mce.OrderIndex,
                mce.Explanation,
                mce.Options.Select(o => new ExerciseOptionDto(
                        o.Id,
                        o.OptionText,
                        o.IsCorrect,
                        o.OrderIndex
                    ))
                    .ToList()
            ),
            FillInBlankExercise fib => new FillInBlankExerciseDto(
                fib.Id,
                fib.LessonId,
                fib.Title,
                fib.Instructions,
                fib.EstimatedDurationMinutes,
                fib.DifficultyLevel,
                fib.Points,
                fib.OrderIndex,
                fib.Explanation,
                fib.Text,
                fib.CorrectAnswer,
                fib.AcceptedAnswers,
                fib.CaseSensitive,
                fib.TrimWhitespace
            ),
            ListeningExercise le => new ListeningExerciseDto(
                le.Id,
                le.LessonId,
                le.Title,
                le.Instructions,
                le.EstimatedDurationMinutes,
                le.DifficultyLevel,
                le.Points,
                le.OrderIndex,
                le.Explanation,
                le.AudioUrl,
                le.CorrectAnswer,
                le.AcceptedAnswers,
                le.CaseSensitive,
                le.MaxReplays
            ),
            TranslationExercise te => new TranslationExerciseDto(
                te.Id,
                te.LessonId,
                te.Title,
                te.Instructions,
                te.EstimatedDurationMinutes,
                te.DifficultyLevel,
                te.Points,
                te.OrderIndex,
                te.Explanation,
                te.SourceText,
                te.TargetText,
                te.SourceLanguageCode,
                te.TargetLanguageCode,
                te.MatchingThreshold
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
            entity.Language?.Name ?? string.Empty,
            entity.Language?.FlagIconUrl,
            entity.EnrolledAt,
            0, // Placeholder
            0 // Placeholder
        );
    }
}