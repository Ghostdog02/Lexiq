using System.Text.Json.Serialization;
using Backend.Database.Entities.Exercises;

namespace Backend.Api.Dtos;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(MultipleChoiceExerciseDto), typeDiscriminator: "MultipleChoice")]
[JsonDerivedType(typeof(FillInBlankExerciseDto), typeDiscriminator: "FillInBlank")]
[JsonDerivedType(typeof(ListeningExerciseDto), typeDiscriminator: "Listening")]
[JsonDerivedType(typeof(TranslationExerciseDto), typeDiscriminator: "Translation")]
public abstract record ExerciseDto(
    int Id,
    int LessonId,
    string Title,
    string? Instructions,
    int? EstimatedDurationMinutes,
    DifficultyLevel DifficultyLevel,
    int Points,
    int OrderIndex,
    string? Explanation
);

public record MultipleChoiceExerciseDto(
    int Id,
    int LessonId,
    string Title,
    string? Instructions,
    int? EstimatedDurationMinutes,
    DifficultyLevel DifficultyLevel,
    int Points,
    int OrderIndex,
    string? Explanation,
    List<ExerciseOptionDto> Options
)
    : ExerciseDto(
        Id,
        LessonId,
        Title,
        Instructions,
        EstimatedDurationMinutes,
        DifficultyLevel,
        Points,
        OrderIndex,
        Explanation
    );

public record FillInBlankExerciseDto(
    int Id,
    int LessonId,
    string Title,
    string? Instructions,
    int? EstimatedDurationMinutes,
    DifficultyLevel DifficultyLevel,
    int Points,
    int OrderIndex,
    string? Explanation,
    string Text,
    string CorrectAnswer,
    string? AcceptedAnswers,
    bool CaseSensitive,
    bool TrimWhitespace
)
    : ExerciseDto(
        Id,
        LessonId,
        Title,
        Instructions,
        EstimatedDurationMinutes,
        DifficultyLevel,
        Points,
        OrderIndex,
        Explanation
    );

public record ListeningExerciseDto(
    int Id,
    int LessonId,
    string Title,
    string? Instructions,
    int? EstimatedDurationMinutes,
    DifficultyLevel DifficultyLevel,
    int Points,
    int OrderIndex,
    string? Explanation,
    string AudioUrl,
    string CorrectAnswer,
    string? AcceptedAnswers,
    bool CaseSensitive,
    int MaxReplays
)
    : ExerciseDto(
        Id,
        LessonId,
        Title,
        Instructions,
        EstimatedDurationMinutes,
        DifficultyLevel,
        Points,
        OrderIndex,
        Explanation
    );

public record TranslationExerciseDto(
    int Id,
    int LessonId,
    string Title,
    string? Instructions,
    int? EstimatedDurationMinutes,
    DifficultyLevel DifficultyLevel,
    int Points,
    int OrderIndex,
    string? Explanation,
    string SourceText,
    string TargetText,
    string SourceLanguageCode,
    string TargetLanguageCode,
    double MatchingThreshold
)
    : ExerciseDto(
        Id,
        LessonId,
        Title,
        Instructions,
        EstimatedDurationMinutes,
        DifficultyLevel,
        Points,
        OrderIndex,
        Explanation
    );

public record ExerciseOptionDto(int Id, string OptionText, bool IsCorrect, int OrderIndex);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(CreateMultipleChoiceExerciseDto), typeDiscriminator: "MultipleChoice")]
[JsonDerivedType(typeof(CreateFillInBlankExerciseDto), typeDiscriminator: "FillInBlank")]
[JsonDerivedType(typeof(CreateListeningExerciseDto), typeDiscriminator: "Listening")]
[JsonDerivedType(typeof(CreateTranslationExerciseDto), typeDiscriminator: "Translation")]
public abstract record CreateExerciseDto(
    int LessonId,
    string Title,
    string? Instructions,
    int? EstimatedDurationMinutes,
    DifficultyLevel DifficultyLevel,
    int Points,
    int OrderIndex,
    string? Explanation
);

public record CreateMultipleChoiceExerciseDto(
    int LessonId,
    string Title,
    string? Instructions,
    int? EstimatedDurationMinutes,
    DifficultyLevel DifficultyLevel,
    int Points,
    int OrderIndex,
    string? Explanation,
    List<CreateExerciseOptionDto> Options
)
    : CreateExerciseDto(
        LessonId,
        Title,
        Instructions,
        EstimatedDurationMinutes,
        DifficultyLevel,
        Points,
        OrderIndex,
        Explanation
    );

public record CreateFillInBlankExerciseDto(
    int LessonId,
    string Title,
    string? Instructions,
    int? EstimatedDurationMinutes,
    DifficultyLevel DifficultyLevel,
    int Points,
    int OrderIndex,
    string? Explanation,
    string Text,
    string CorrectAnswer,
    string? AcceptedAnswers,
    bool CaseSensitive,
    bool TrimWhitespace
)
    : CreateExerciseDto(
        LessonId,
        Title,
        Instructions,
        EstimatedDurationMinutes,
        DifficultyLevel,
        Points,
        OrderIndex,
        Explanation
    );

public record CreateListeningExerciseDto(
    int LessonId,
    string Title,
    string? Instructions,
    int? EstimatedDurationMinutes,
    DifficultyLevel DifficultyLevel,
    int Points,
    int OrderIndex,
    string? Explanation,
    string AudioUrl,
    string CorrectAnswer,
    string? AcceptedAnswers,
    bool CaseSensitive,
    int MaxReplays
)
    : CreateExerciseDto(
        LessonId,
        Title,
        Instructions,
        EstimatedDurationMinutes,
        DifficultyLevel,
        Points,
        OrderIndex,
        Explanation
    );

public record CreateTranslationExerciseDto(
    int LessonId,
    string Title,
    string? Instructions,
    int? EstimatedDurationMinutes,
    DifficultyLevel DifficultyLevel,
    int Points,
    int OrderIndex,
    string? Explanation,
    string SourceText,
    string TargetText,
    string SourceLanguageCode,
    string TargetLanguageCode,
    double MatchingThreshold
)
    : CreateExerciseDto(
        LessonId,
        Title,
        Instructions,
        EstimatedDurationMinutes,
        DifficultyLevel,
        Points,
        OrderIndex,
        Explanation
    );

public record CreateExerciseOptionDto(string OptionText, bool IsCorrect, int OrderIndex);

public record UpdateExerciseDto(
    string? Title,
    string? Instructions,
    int? EstimatedDurationMinutes,
    DifficultyLevel? DifficultyLevel,
    int? Points,
    int? OrderIndex,
    string? Explanation
);
