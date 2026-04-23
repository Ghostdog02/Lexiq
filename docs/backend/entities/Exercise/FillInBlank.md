# FillInBlankExercise

**Inherits:** `Exercise`
**Discriminator:** `"FillInBlankExercise"`

## Purpose

Complete a sentence or phrase with the correct word. Supports flexible matching with case sensitivity, whitespace trimming, and alternative answers.

## Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Text` | `string` (Max 500) | Required | Text with blank placeholder |
| `CorrectAnswer` | `string` (Max 200) | Required | Primary expected answer |
| `AcceptedAnswers` | `string?` | null | Comma-separated alternatives |
| `CaseSensitive` | `bool` | false | Whether case must match |
| `TrimWhitespace` | `bool` | true | Whether to trim input |

## Validation Rules

### Case Sensitivity
```csharp
CaseSensitive = false  // Default
"answer" == "Answer" == "ANSWER"  // ✓

CaseSensitive = true
"Answer" == "Answer"  // ✓
"answer" != "Answer"  // ✗
```

### Whitespace Trimming
```csharp
TrimWhitespace = true  // Default
"  answer  " → "answer"  // Trimmed

TrimWhitespace = false
"  answer  " != "answer"  // Exact match required
```

### Accepted Answers
```csharp
CorrectAnswer = "hello"
AcceptedAnswers = "hi,hey,howdy"

// Valid: "hello", "hi", "hey", "howdy"
// Invalid: "greetings"
```

## Test Coverage

9 tests in `ExerciseValidationTests.cs`:
- Case sensitivity on/off
- Whitespace trimming on/off
- AcceptedAnswers parsing
- AcceptedAnswers whitespace handling
- Empty AcceptedAnswers

## Example

```csharp
var exercise = new FillInBlankExercise
{
    Title = "Capital of Italy",
    Text = "The capital of Italy is ___",
    CorrectAnswer = "Rome",
    AcceptedAnswers = "roma",
    CaseSensitive = false,
    TrimWhitespace = true
};
// Accepts: "rome", "Rome", "roma", "  ROME  "
```
