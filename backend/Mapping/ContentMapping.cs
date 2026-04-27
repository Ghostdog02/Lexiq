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

    public static LessonDto ToDto(
        this Lesson entity,
        LessonProgressSummary? progress = null,
        Dictionary<string, UserExerciseProgressDto>? exerciseProgress = null
    )
    {
        return new LessonDto(
            entity.Id,
            entity.CourseId,
            entity.Course?.Title ?? string.Empty,
            entity.Title,
            entity.Description,
            entity.EstimatedDurationMinutes,
            entity.OrderIndex,
            entity.LessonContent,
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
        var progress = userProgress?.GetValueOrDefault(entity.Id);

        return entity switch
        {
            FillInBlankExercise fib => new FillInBlankExerciseDto(
                fib.Id,
                fib.LessonId,
                fib.Title,
                fib.Question,
                fib.EstimatedDurationMinutes,
                fib.DifficultyLevel,
                fib.Points,
                fib.OrderIndex,
                fib.Explanation,
                fib.IsLocked,
                progress,
                fib.Text,
                fib.CorrectAnswer,
                fib.AcceptedAnswers,
                fib.CaseSensitive,
                fib.TrimWhitespace,
                fib.WordBank
            ),
            ListeningExercise le => new ListeningExerciseDto(
                le.Id,
                le.LessonId,
                le.Title,
                le.Question,
                le.EstimatedDurationMinutes,
                le.DifficultyLevel,
                le.Points,
                le.OrderIndex,
                le.Explanation,
                le.IsLocked,
                progress,
                le.AudioUrl,
                le.MaxReplays,
                le.Options.Select(o => new ExerciseOptionDto(o.Id, o.OptionText, o.IsCorrect, o.OrderIndex)).ToList()
            ),
            TrueFalseExercise tf => new TrueFalseExerciseDto(
                tf.Id,
                tf.LessonId,
                tf.Title,
                tf.Question,
                tf.EstimatedDurationMinutes,
                tf.DifficultyLevel,
                tf.Points,
                tf.OrderIndex,
                tf.Explanation,
                tf.IsLocked,
                progress,
                tf.Statement,
                tf.CorrectAnswer,
                tf.ImageUrl
            ),
            ImageChoiceExercise ic => new ImageChoiceExerciseDto(
                ic.Id,
                ic.LessonId,
                ic.Title,
                ic.Question,
                ic.EstimatedDurationMinutes,
                ic.DifficultyLevel,
                ic.Points,
                ic.OrderIndex,
                ic.Explanation,
                ic.IsLocked,
                progress,
                ic.Options.Select(o => new ImageOptionDto(o.Id, o.ImageUrl, o.AltText, o.IsCorrect, o.OrderIndex)).ToList()
            ),
            AudioMatchingExercise am => new AudioMatchingExerciseDto(
                am.Id,
                am.LessonId,
                am.Title,
                am.Question,
                am.EstimatedDurationMinutes,
                am.DifficultyLevel,
                am.Points,
                am.OrderIndex,
                am.Explanation,
                am.IsLocked,
                progress,
                am.Pairs.Select(p => new AudioMatchPairDto(p.Id, p.AudioUrl, p.ImageUrl, p.OrderIndex)).ToList()
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
            0,
            0
        );
    }
}
