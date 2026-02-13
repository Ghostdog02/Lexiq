using System.Text.Json.Serialization;
using Backend.Database.Entities.Exercises;

namespace Backend.Api.Dtos;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(MultipleChoiceExerciseDto), typeDiscriminator: "MultipleChoice")]
[JsonDerivedType(typeof(FillInBlankExerciseDto), typeDiscriminator: "FillInBlank")]
[JsonDerivedType(typeof(ListeningExerciseDto), typeDiscriminator: "Listening")]
[JsonDerivedType(typeof(TranslationExerciseDto), typeDiscriminator: "Translation")]
public abstract record ExerciseDto(
    string Id,
    string LessonId,
    string Title,
    string? Instructions,
    int? EstimatedDurationMinutes,
    DifficultyLevel DifficultyLevel,
    int Points,
    int OrderIndex,
    string? Explanation,
    bool IsLocked,
    UserExerciseProgressDto? UserProgress
);

public record MultipleChoiceExerciseDto(
    string Id,
    string LessonId,
    string Title,
    string? Instructions,
    int? EstimatedDurationMinutes,
    DifficultyLevel DifficultyLevel,
    int Points,
    int OrderIndex,
    string? Explanation,
    bool IsLocked,
    UserExerciseProgressDto? UserProgress,
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
        Explanation,
        IsLocked,
        UserProgress
    );

public record FillInBlankExerciseDto(
    string Id,
    string LessonId,
    string Title,
    string? Instructions,
    int? EstimatedDurationMinutes,
    DifficultyLevel DifficultyLevel,
    int Points,
    int OrderIndex,
    string? Explanation,
    bool IsLocked,
    UserExerciseProgressDto? UserProgress,
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
        Explanation,
        IsLocked,
        UserProgress
    );

public record ListeningExerciseDto(
    string Id,
    string LessonId,
    string Title,
    string? Instructions,
    int? EstimatedDurationMinutes,
    DifficultyLevel DifficultyLevel,
    int Points,
    int OrderIndex,
    string? Explanation,
    bool IsLocked,
    UserExerciseProgressDto? UserProgress,
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
        Explanation,
        IsLocked,
        UserProgress
    );

public record TranslationExerciseDto(
    string Id,
    string LessonId,
    string Title,
    string? Instructions,
    int? EstimatedDurationMinutes,
    DifficultyLevel DifficultyLevel,
    int Points,
    int OrderIndex,
    string? Explanation,
    bool IsLocked,
    UserExerciseProgressDto? UserProgress,
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
        Explanation,
        IsLocked,
        UserProgress
    );

public record ExerciseOptionDto(string Id, string OptionText, bool IsCorrect, int OrderIndex);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(CreateMultipleChoiceExerciseDto), typeDiscriminator: "MultipleChoice")]
[JsonDerivedType(typeof(CreateFillInBlankExerciseDto), typeDiscriminator: "FillInBlank")]
[JsonDerivedType(typeof(CreateListeningExerciseDto), typeDiscriminator: "Listening")]
[JsonDerivedType(typeof(CreateTranslationExerciseDto), typeDiscriminator: "Translation")]
public abstract record CreateExerciseDto(
    string? LessonId, // Null when nested inside CreateLessonDto (lesson assigns its own ID)
    string Title,
    string? Instructions,
    int? EstimatedDurationMinutes,
    DifficultyLevel DifficultyLevel,
    int Points,
    int? OrderIndex, // Optional - auto-calculated if null
    string? Explanation
);

public record CreateMultipleChoiceExerciseDto(
    string? LessonId,
    string Title,
    string? Instructions,
    int? EstimatedDurationMinutes,
    DifficultyLevel DifficultyLevel,
    int Points,
    int? OrderIndex, // Optional - auto-calculated if null
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
    string? LessonId,
    string Title,
    string? Instructions,
    int? EstimatedDurationMinutes,
    DifficultyLevel DifficultyLevel,
    int Points,
    int? OrderIndex, // Optional - auto-calculated if null
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
    string? LessonId,
    string Title,
    string? Instructions,
    int? EstimatedDurationMinutes,
    DifficultyLevel DifficultyLevel,
    int Points,
    int? OrderIndex, // Optional - auto-calculated if null
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
    string? LessonId,
    string Title,
    string? Instructions,
    int? EstimatedDurationMinutes,
    DifficultyLevel DifficultyLevel,
    int Points,
    int? OrderIndex, // Optional - auto-calculated if null
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

public record UserExerciseProgressDto(
    string ExerciseId,
    bool IsCompleted,
    int PointsEarned,
    DateTime? CompletedAt
);
