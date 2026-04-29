using System.Text.Json.Serialization;
using Backend.Database.Entities.Exercises;

namespace Backend.Api.Dtos;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(FillInBlankExerciseDto), typeDiscriminator: "FillInBlank")]
[JsonDerivedType(typeof(ListeningExerciseDto), typeDiscriminator: "Listening")]
[JsonDerivedType(typeof(TrueFalseExerciseDto), typeDiscriminator: "TrueFalse")]
[JsonDerivedType(typeof(ImageChoiceExerciseDto), typeDiscriminator: "ImageChoice")]
[JsonDerivedType(typeof(AudioMatchingExerciseDto), typeDiscriminator: "AudioMatching")]
public abstract record ExerciseDto(
    string Id,
    string LessonId,
    string Title,
    string? Question,
    int? EstimatedDurationMinutes,
    DifficultyLevel DifficultyLevel,
    int Points,
    int OrderIndex,
    string? Explanation,
    bool IsLocked,
    UserExerciseProgressDto? UserProgress
);

public record FillInBlankExerciseDto(
    string Id,
    string LessonId,
    string Title,
    string? Question,
    int? EstimatedDurationMinutes,
    DifficultyLevel DifficultyLevel,
    int Points,
    int OrderIndex,
    string? Explanation,
    bool IsLocked,
    UserExerciseProgressDto? UserProgress,
    string Text,
    List<ExerciseOptionDto> Options
)
    : ExerciseDto(
        Id,
        LessonId,
        Title,
        Question,
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
    string? Question,
    int? EstimatedDurationMinutes,
    DifficultyLevel DifficultyLevel,
    int Points,
    int OrderIndex,
    string? Explanation,
    bool IsLocked,
    UserExerciseProgressDto? UserProgress,
    string AudioUrl,
    int MaxReplays,
    List<ExerciseOptionDto> Options
)
    : ExerciseDto(
        Id,
        LessonId,
        Title,
        Question,
        EstimatedDurationMinutes,
        DifficultyLevel,
        Points,
        OrderIndex,
        Explanation,
        IsLocked,
        UserProgress
    );

public record TrueFalseExerciseDto(
    string Id,
    string LessonId,
    string Title,
    string? Question,
    int? EstimatedDurationMinutes,
    DifficultyLevel DifficultyLevel,
    int Points,
    int OrderIndex,
    string? Explanation,
    bool IsLocked,
    UserExerciseProgressDto? UserProgress,
    string Statement,
    bool CorrectAnswer,
    string? ImageUrl
)
    : ExerciseDto(
        Id,
        LessonId,
        Title,
        Question,
        EstimatedDurationMinutes,
        DifficultyLevel,
        Points,
        OrderIndex,
        Explanation,
        IsLocked,
        UserProgress
    );

public record ImageChoiceExerciseDto(
    string Id,
    string LessonId,
    string Title,
    string? Question,
    int? EstimatedDurationMinutes,
    DifficultyLevel DifficultyLevel,
    int Points,
    int OrderIndex,
    string? Explanation,
    bool IsLocked,
    UserExerciseProgressDto? UserProgress,
    List<ImageOptionDto> Options
)
    : ExerciseDto(
        Id,
        LessonId,
        Title,
        Question,
        EstimatedDurationMinutes,
        DifficultyLevel,
        Points,
        OrderIndex,
        Explanation,
        IsLocked,
        UserProgress
    );

public record AudioMatchingExerciseDto(
    string Id,
    string LessonId,
    string Title,
    string? Question,
    int? EstimatedDurationMinutes,
    DifficultyLevel DifficultyLevel,
    int Points,
    int OrderIndex,
    string? Explanation,
    bool IsLocked,
    UserExerciseProgressDto? UserProgress,
    List<AudioMatchPairDto> Pairs
)
    : ExerciseDto(
        Id,
        LessonId,
        Title,
        Question,
        EstimatedDurationMinutes,
        DifficultyLevel,
        Points,
        OrderIndex,
        Explanation,
        IsLocked,
        UserProgress
    );

public record ExerciseOptionDto(string Id, string OptionText, bool IsCorrect, string Explanation);

public record ImageOptionDto(string Id, string ImageUrl, string AltText, bool IsCorrect, string Explanation);

public record AudioMatchPairDto(string Id, string AudioUrl, string ImageUrl, string Explanation);

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(CreateFillInBlankExerciseDto), typeDiscriminator: "FillInBlank")]
[JsonDerivedType(typeof(CreateListeningExerciseDto), typeDiscriminator: "Listening")]
[JsonDerivedType(typeof(CreateTrueFalseExerciseDto), typeDiscriminator: "TrueFalse")]
[JsonDerivedType(typeof(CreateImageChoiceExerciseDto), typeDiscriminator: "ImageChoice")]
[JsonDerivedType(typeof(CreateAudioMatchingExerciseDto), typeDiscriminator: "AudioMatching")]
public abstract record CreateExerciseDto(
    string? LessonId, 
    string Title,
    string? Question,
    int? EstimatedDurationMinutes,
    DifficultyLevel DifficultyLevel,
    int Points,
    int? OrderIndex, 
    string? Explanation
);

public record CreateFillInBlankExerciseDto(
    string? LessonId,
    string Title,
    string? Question,
    int? EstimatedDurationMinutes,
    DifficultyLevel DifficultyLevel,
    int Points,
    int? OrderIndex,
    string? Explanation,
    string Text,
    List<CreateExerciseOptionDto> Options
)
    : CreateExerciseDto(
        LessonId,
        Title,
        Question,
        EstimatedDurationMinutes,
        DifficultyLevel,
        Points,
        OrderIndex,
        Explanation
    );

public record CreateListeningExerciseDto(
    string? LessonId,
    string Title,
    string? Question,
    int? EstimatedDurationMinutes,
    DifficultyLevel DifficultyLevel,
    int Points,
    int? OrderIndex,
    string? Explanation,
    string AudioUrl,
    int MaxReplays,
    List<CreateExerciseOptionDto> Options
)
    : CreateExerciseDto(
        LessonId,
        Title,
        Question,
        EstimatedDurationMinutes,
        DifficultyLevel,
        Points,
        OrderIndex,
        Explanation
    );

public record CreateTrueFalseExerciseDto(
    string? LessonId,
    string Title,
    string? Question,
    int? EstimatedDurationMinutes,
    DifficultyLevel DifficultyLevel,
    int Points,
    int? OrderIndex,
    string? Explanation,
    string Statement,
    bool CorrectAnswer,
    string? ImageUrl
)
    : CreateExerciseDto(
        LessonId,
        Title,
        Question,
        EstimatedDurationMinutes,
        DifficultyLevel,
        Points,
        OrderIndex,
        Explanation
    );

public record CreateImageChoiceExerciseDto(
    string? LessonId,
    string Title,
    string? Question,
    int? EstimatedDurationMinutes,
    DifficultyLevel DifficultyLevel,
    int Points,
    int? OrderIndex,
    string? Explanation,
    List<CreateImageOptionDto> Options
)
    : CreateExerciseDto(
        LessonId,
        Title,
        Question,
        EstimatedDurationMinutes,
        DifficultyLevel,
        Points,
        OrderIndex,
        Explanation
    );

public record CreateAudioMatchingExerciseDto(
    string? LessonId,
    string Title,
    string? Question,
    int? EstimatedDurationMinutes,
    DifficultyLevel DifficultyLevel,
    int Points,
    int? OrderIndex,
    string? Explanation,
    List<CreateAudioMatchPairDto> Pairs
)
    : CreateExerciseDto(
        LessonId,
        Title,
        Question,
        EstimatedDurationMinutes,
        DifficultyLevel,
        Points,
        OrderIndex,
        Explanation
    );

public record CreateExerciseOptionDto(string OptionText, bool IsCorrect, string Explanation);

public record CreateImageOptionDto(string ImageUrl, string AltText, bool IsCorrect, string Explanation);

public record CreateAudioMatchPairDto(string AudioUrl, string ImageUrl, string Explanation);

public record UpdateExerciseDto(
    string? Title,
    string? Question,
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
