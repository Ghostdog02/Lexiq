using System.Text.Json.Serialization;
using Backend.Database.Entities.Exercises;

namespace Backend.Api.Dtos;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(FillInBlankExerciseDto), typeDiscriminator: "FillInBlank")]
[JsonDerivedType(typeof(ListeningExerciseDto), typeDiscriminator: "Listening")]
[JsonDerivedType(typeof(TrueFalseExerciseDto), typeDiscriminator: "TrueFalse")]
[JsonDerivedType(typeof(ImageChoiceExerciseDto), typeDiscriminator: "ImageChoice")]
[JsonDerivedType(typeof(AudioMatchingExerciseDto), typeDiscriminator: "AudioMatching")]
public abstract record ExerciseDto
{
    public required string ExerciseId { get; init; }
    public required string LessonId { get; init; }
    public required string Instructions { get; init; }
    public required DifficultyLevel DifficultyLevel { get; init; }
    public required int Points { get; init; }
    public required bool IsLocked { get; init; }
    public required DateTime CreatedAt { get; init; }
    public UserExerciseProgressDto? UserProgress { get; init; }
}

public record FillInBlankExerciseDto : ExerciseDto
{
    public required string Text { get; init; }
    public required List<ExerciseOptionDto> Options { get; init; }
}

public record ListeningExerciseDto : ExerciseDto
{
    public required string AudioUrl { get; init; }
    public required int MaxReplays { get; init; }
    public required List<ExerciseOptionDto> Options { get; init; }
}

public record TrueFalseExerciseDto : ExerciseDto
{
    public required string Statement { get; init; }
    public required bool CorrectAnswer { get; init; }
    public string? ImageUrl { get; init; }
    public required string Explanation { get; init; }
}

public record ImageOptionDto
{
    public required string ImageOptionId { get; init; }
    public required string ImageUrl { get; init; }
    public required string AltText { get; init; }
    public required bool IsCorrect { get; init; }
    public required string Explanation { get; init; }
}

public record ImageChoiceExerciseDto : ExerciseDto
{
    public required List<ImageOptionDto> Options { get; init; }
}

public record AudioMatchPairDto
{
    public required string AudioMatchPairId { get; init; }
    public required string AudioUrl { get; init; }
    public required string ImageUrl { get; init; }
    public required string Explanation { get; init; }
}

public record AudioMatchingExerciseDto : ExerciseDto
{
    public required List<AudioMatchPairDto> Pairs { get; init; }
}

public record ExerciseOptionDto
{
    public required string ExerciseOptionId { get; init; }
    public required string OptionText { get; init; }
    public required bool IsCorrect { get; init; }
    public required string Explanation { get; init; }
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(CreateFillInBlankExerciseDto), typeDiscriminator: "FillInBlank")]
[JsonDerivedType(typeof(CreateListeningExerciseDto), typeDiscriminator: "Listening")]
[JsonDerivedType(typeof(CreateTrueFalseExerciseDto), typeDiscriminator: "TrueFalse")]
[JsonDerivedType(typeof(CreateImageChoiceExerciseDto), typeDiscriminator: "ImageChoice")]
[JsonDerivedType(typeof(CreateAudioMatchingExerciseDto), typeDiscriminator: "AudioMatching")]
public abstract record CreateExerciseDto
{
    public string? LessonId { get; init; }
    public required string Instructions { get; init; }
    public required DifficultyLevel DifficultyLevel { get; init; }
    public required int Points { get; init; }
}

public record CreateFillInBlankExerciseDto : CreateExerciseDto
{
    public required string Text { get; init; }
    public required List<CreateExerciseOptionDto> Options { get; init; }
}

public record CreateListeningExerciseDto : CreateExerciseDto
{
    public required string AudioUrl { get; init; }
    public required int MaxReplays { get; init; }
    public required List<CreateExerciseOptionDto> Options { get; init; }
}

public record CreateTrueFalseExerciseDto : CreateExerciseDto
{
    public required string Statement { get; init; }
    public required bool CorrectAnswer { get; init; }
    public string? ImageUrl { get; init; }
    public required string Explanation { get; init; }
}

public record CreateImageOptionDto
{
    public required string ImageUrl { get; init; }
    public required string AltText { get; init; }
    public required bool IsCorrect { get; init; }
    public required string Explanation { get; init; }
}

public record CreateImageChoiceExerciseDto : CreateExerciseDto
{
    public required List<CreateImageOptionDto> Options { get; init; }
}

public record CreateAudioMatchPairDto
{
    public required string AudioUrl { get; init; }
    public required string ImageUrl { get; init; }
    public required string Explanation { get; init; }
}

public record CreateAudioMatchingExerciseDto : CreateExerciseDto
{
    public required List<CreateAudioMatchPairDto> Pairs { get; init; }
}

public record CreateExerciseOptionDto
{
    public required string OptionText { get; init; }
    public required bool IsCorrect { get; init; }
    public required string Explanation { get; init; }
}

public record UpdateExerciseDto
{
    public string? Instructions { get; init; }
    public DifficultyLevel? DifficultyLevel { get; init; }
    public int? Points { get; init; }
}

public record UserExerciseProgressDto
{
    public required string ExerciseId { get; init; }
    public required bool IsCompleted { get; init; }
    public required int PointsEarned { get; init; }
    public DateTime? CompletedAt { get; init; }
}
