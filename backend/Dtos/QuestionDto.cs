namespace Backend.Api.Dtos;

public class QuestionDto
{
    public int Id { get; set; }
    public int ExerciseId { get; set; }
    public required string QuestionText { get; set; }
    public string? QuestionAudioUrl { get; set; }
    public string? QuestionImageUrl { get; set; }
    public int OrderIndex { get; set; }
    public int Points { get; set; }
    public string? Explanation { get; set; }
    public string QuestionType { get; set; } = string.Empty;
    
    // Type specific fields (optional/nullable)
    public List<QuestionOptionDto>? Options { get; set; } // For MultipleChoice
    public string? CorrectAnswer { get; set; } // For FillInBlank, Listening, Translation (hidden in client usually, but included here for admin/check)
    public string? SourceLanguageCode { get; set; } // Translation
    public string? TargetLanguageCode { get; set; } // Translation
    public string? AudioUrl { get; set; } // Listening
}

public class QuestionOptionDto
{
    public int Id { get; set; }
    public required string OptionText { get; set; }
    public bool IsCorrect { get; set; }
    public int OrderIndex { get; set; }
}

public class CreateQuestionDto
{
    public int ExerciseId { get; set; }
    public required string QuestionText { get; set; }
    public string? QuestionAudioUrl { get; set; }
    public string? QuestionImageUrl { get; set; }
    public int OrderIndex { get; set; }
    public int Points { get; set; }
    public string? Explanation { get; set; }
    public required string QuestionType { get; set; } // "MultipleChoice", "FillInBlank", "Translation", "Listening"

    // Specifics
    public List<CreateQuestionOptionDto>? Options { get; set; }
    public string? CorrectAnswer { get; set; }
    public string? AcceptedAnswers { get; set; }
    public bool? CaseSensitive { get; set; }
    public bool? TrimWhitespace { get; set; }
    public string? SourceLanguageCode { get; set; }
    public string? TargetLanguageCode { get; set; }
    public double? MatchingThreshold { get; set; }
    public string? AudioUrl { get; set; }
    public int? MaxReplays { get; set; }
}

public class CreateQuestionOptionDto
{
    public required string OptionText { get; set; }
    public bool IsCorrect { get; set; }
    public int OrderIndex { get; set; }
}
