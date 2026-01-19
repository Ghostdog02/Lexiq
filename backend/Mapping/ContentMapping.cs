using Backend.Api.Dtos;
using Backend.Database.Entities;
using Backend.Database.Entities.Questions;

namespace Backend.Api.Mapping;

public static class ContentMappingExtensions
{
    // Language
    public static LanguageDto ToDto(this Language entity)
    {
        return new LanguageDto(entity.Name, entity.FlagIconUrl, entity.Courses.Count);
    }

    // Course
    public static CourseDto ToDto(this Course entity)
    {
        return new CourseDto(
            entity.Language?.Name ?? string.Empty,
            entity.Title,
            entity.Description,
            entity.EstimatedDurationHours,
            entity.OrderIndex,
            entity.Lessons.Count
        );
    }

    // Lesson
    public static LessonDto ToDto(this Lesson entity)
    {
        return new LessonDto(
            entity.Course?.Title ?? string.Empty,
            entity.Title,
            entity.Description,
            entity.EstimatedDurationMinutes,
            entity.OrderIndex,
            entity.LessonMediaUrl,
            entity.LessonTextUrl,
            entity.IsLocked,
            entity.Exercises.Count
        );
    }

    // Exercise
    public static ExerciseDto ToDto(this Exercise entity)
    {
        return new ExerciseDto(
            entity.Id,
            entity.LessonId,
            entity.Title,
            entity.Instructions,
            entity.EstimatedDurationMinutes,
            entity.DifficultyLevel,
            entity.Points,
            entity.OrderIndex,
            entity.Questions.Count
        );
    }

    // Question
    public static QuestionDto ToDto(this Question entity)
    {
        return entity switch
        {
            MultipleChoiceQuestion mcq => new MultipleChoiceQuestionDto(
                mcq.Exercise?.Title ?? string.Empty,
                mcq.QuestionText,
                mcq.QuestionAudioUrl,
                mcq.QuestionImageUrl,
                mcq.OrderIndex,
                mcq.Points,
                mcq.Explanation,
                mcq.Options.Select(o => new QuestionOptionDto(
                        o.Id,
                        o.OptionText,
                        o.IsCorrect,
                        o.OrderIndex
                    ))
                    .ToList()
            ),
            FillInBlankQuestion fib => new FillInBlankQuestionDto(
                fib.Exercise?.Title ?? string.Empty,
                fib.QuestionText,
                fib.QuestionAudioUrl,
                fib.QuestionImageUrl,
                fib.OrderIndex,
                fib.Points,
                fib.Explanation,
                fib.CorrectAnswer
            ),
            TranslationQuestion tq => new TranslationQuestionDto(
                tq.Exercise?.Title ?? string.Empty,
                tq.QuestionText,
                tq.QuestionAudioUrl,
                tq.QuestionImageUrl,
                tq.OrderIndex,
                tq.Points,
                tq.Explanation,
                tq.SourceLanguageCode,
                tq.TargetLanguageCode
            ),
            ListeningQuestion lq => new ListeningQuestionDto(
                lq.Exercise?.Title ?? string.Empty,
                lq.QuestionText,
                lq.QuestionAudioUrl,
                lq.QuestionImageUrl,
                lq.OrderIndex,
                lq.Points,
                lq.Explanation,
                lq.AudioUrl,
                lq.CorrectAnswer
            ),
            _ => throw new NotImplementedException(
                $"Mapping for question type {entity.GetType().Name} is not implemented"
            ),
        };
    }

    // UserLanguage
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
