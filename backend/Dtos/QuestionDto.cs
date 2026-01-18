using System.Text.Json.Serialization;

namespace Backend.Api.Dtos;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "questionType")]
[JsonDerivedType(typeof(MultipleChoiceQuestionDto), typeDiscriminator: "MultipleChoice")]
[JsonDerivedType(typeof(FillInBlankQuestionDto), typeDiscriminator: "FillInBlank")]
[JsonDerivedType(typeof(TranslationQuestionDto), typeDiscriminator: "Translation")]
[JsonDerivedType(typeof(ListeningQuestionDto), typeDiscriminator: "Listening")]
public abstract record QuestionDto(
    int Id,
    int ExerciseId,
    string QuestionText,
    string? QuestionAudioUrl,
    string? QuestionImageUrl,
    int OrderIndex,
    int Points,
    string? Explanation
);

public record MultipleChoiceQuestionDto(
    int Id,
    int ExerciseId,
    string QuestionText,
    string? QuestionAudioUrl,
    string? QuestionImageUrl,
    int OrderIndex,
    int Points,
    string? Explanation,
    List<QuestionOptionDto> Options
)
    : QuestionDto(
        Id,
        ExerciseId,
        QuestionText,
        QuestionAudioUrl,
        QuestionImageUrl,
        OrderIndex,
        Points,
        Explanation
    );

public record FillInBlankQuestionDto(
    int Id,
    int ExerciseId,
    string QuestionText,
    string? QuestionAudioUrl,
    string? QuestionImageUrl,
    int OrderIndex,
    int Points,
    string? Explanation,
    string CorrectAnswer
)
    : QuestionDto(
        Id,
        ExerciseId,
        QuestionText,
        QuestionAudioUrl,
        QuestionImageUrl,
        OrderIndex,
        Points,
        Explanation
    );

public record TranslationQuestionDto(
    int Id,
    int ExerciseId,
    string QuestionText,
    string? QuestionAudioUrl,
    string? QuestionImageUrl,
    int OrderIndex,
    int Points,
    string? Explanation,
    string SourceLanguageCode,
    string TargetLanguageCode
)
    : QuestionDto(
        Id,
        ExerciseId,
        QuestionText,
        QuestionAudioUrl,
        QuestionImageUrl,
        OrderIndex,
        Points,
        Explanation
    );

public record ListeningQuestionDto(
    int Id,
    int ExerciseId,
    string QuestionText,
    string? QuestionAudioUrl,
    string? QuestionImageUrl,
    int OrderIndex,
    int Points,
    string? Explanation,
    string AudioUrl,
    string CorrectAnswer
)
    : QuestionDto(
        Id,
        ExerciseId,
        QuestionText,
        QuestionAudioUrl,
        QuestionImageUrl,
        OrderIndex,
        Points,
        Explanation
    );

public record QuestionOptionDto(int Id, string OptionText, bool IsCorrect, int OrderIndex);

// Create DTOs - kept flat for easier binding, but converted to record
public record CreateQuestionDto(
    int ExerciseId,
    string QuestionText,
    string? QuestionAudioUrl,
    string? QuestionImageUrl,
    int OrderIndex,
    int Points,
    string? Explanation,
    string QuestionType, // "MultipleChoice", "FillInBlank", "Translation", "Listening"
    // Specifics (Nullable)
    List<CreateQuestionOptionDto>? Options,
    string? CorrectAnswer,
    string? AcceptedAnswers,
    bool? CaseSensitive,
    bool? TrimWhitespace,
    string? SourceLanguageCode,
    string? TargetLanguageCode,
    double? MatchingThreshold,
    string? AudioUrl,
    int? MaxReplays
);

public record CreateQuestionOptionDto(string OptionText, bool IsCorrect, int OrderIndex);
