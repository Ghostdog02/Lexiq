using Backend.Api.Dtos;
using Backend.Database.Entities;
using Backend.Database.Entities.Questions;

namespace Backend.Api.Mapping;

public static class ContentMappingExtensions
{
    // Language
    public static LanguageDto ToDto(this Language entity)
    {
        return new LanguageDto
        {
            Id = entity.Id,
            Name = entity.Name,
            FlagIconUrl = entity.FlagIconUrl,
            CourseCount = entity.Courses.Count
        };
    }

    // Course
    public static CourseDto ToDto(this Course entity)
    {
        return new CourseDto
        {
            Id = entity.Id,
            LanguageId = entity.LanguageId,
            LanguageName = entity.Language?.Name ?? string.Empty,
            Title = entity.Title,
            Description = entity.Description,
            EstimatedDurationHours = entity.EstimatedDurationHours,
            OrderIndex = entity.OrderIndex,
            LessonCount = entity.Lessons.Count
        };
    }

    // Lesson
    public static LessonDto ToDto(this Lesson entity)
    {
        return new LessonDto
        {
            Id = entity.Id,
            CourseId = entity.CourseId,
            Title = entity.Title,
            Description = entity.Description,
            EstimatedDurationMinutes = entity.EstimatedDurationMinutes,
            OrderIndex = entity.OrderIndex,
            LessonMediaUrl = entity.LessonMediaUrl,
            LessonTextUrl = entity.LessonTextUrl,
            IsLocked = entity.IsLocked,
            ExerciseCount = entity.Exercises.Count
        };
    }

    // Exercise
    public static ExerciseDto ToDto(this Exercise entity)
    {
        return new ExerciseDto
        {
            Id = entity.Id,
            LessonId = entity.LessonId,
            Title = entity.Title,
            Instructions = entity.Instructions,
            EstimatedDurationMinutes = entity.EstimatedDurationMinutes,
            DifficultyLevel = entity.DifficultyLevel,
            Points = entity.Points,
            OrderIndex = entity.OrderIndex,
            QuestionCount = entity.Questions.Count
        };
    }

    // Question
    public static QuestionDto ToDto(this Question entity)
    {
        var dto = new QuestionDto
        {
            Id = entity.Id,
            ExerciseId = entity.ExerciseId,
            QuestionText = entity.QuestionText,
            QuestionAudioUrl = entity.QuestionAudioUrl,
            QuestionImageUrl = entity.QuestionImageUrl,
            OrderIndex = entity.OrderIndex,
            Points = entity.Points,
            Explanation = entity.Explanation,
            QuestionType = entity.GetType().Name // Simplified type name
        };

        switch (entity)
        {
            case MultipleChoiceQuestion mcq:
                dto.QuestionType = "MultipleChoice";
                dto.Options = mcq.Options.Select(o => new QuestionOptionDto
                {
                    Id = o.Id,
                    OptionText = o.OptionText,
                    IsCorrect = o.IsCorrect,
                    OrderIndex = o.OrderIndex
                }).ToList();
                break;
            case FillInBlankQuestion fib:
                dto.QuestionType = "FillInBlank";
                dto.CorrectAnswer = fib.CorrectAnswer;
                break;
            case TranslationQuestion tq:
                dto.QuestionType = "Translation";
                dto.SourceLanguageCode = tq.SourceLanguageCode;
                dto.TargetLanguageCode = tq.TargetLanguageCode;
                break;
            case ListeningQuestion lq:
                dto.QuestionType = "Listening";
                dto.AudioUrl = lq.AudioUrl;
                dto.CorrectAnswer = lq.CorrectAnswer;
                break;
        }

        return dto;
    }

    // UserLanguage
    public static UserLanguageDto ToDto(this UserLanguage entity)
    {
        return new UserLanguageDto
        {
            UserId = entity.UserId,
            LanguageId = entity.LanguageId,
            LanguageName = entity.Language?.Name ?? string.Empty,
            FlagIconUrl = entity.Language?.FlagIconUrl,
            EnrolledAt = entity.EnrolledAt
        };
    }
}
