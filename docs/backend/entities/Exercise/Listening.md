# ListeningExercise

**Inherits:** `Exercise`
**Discriminator:** `"ListeningExercise"`

## Purpose

Listen to audio and transcribe what you hear. Similar to FillInBlank but always trims whitespace.

## Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `AudioUrl` | `string` (Max 500) | Required | URL to audio file |
| `CorrectAnswer` | `string` (Max 200) | Required | Expected transcription |
| `AcceptedAnswers` | `string?` | null | Comma-separated alternatives |
| `CaseSensitive` | `bool` | false | Whether case must match |

## Validation Rules

### Key Difference from FillInBlank
- **Always trims whitespace** (hardcoded `TrimWhitespace = true`)
- Cannot be disabled

### Otherwise Same as FillInBlank
- Case sensitivity configurable
- AcceptedAnswers comma-separated
- Alternatives trimmed

## Test Coverage

4 tests in `ExerciseValidationTests.cs`:
- Case sensitivity on/off
- Always trims whitespace (verified)
- AcceptedAnswers parsing

## Example

```csharp
var exercise = new ListeningExercise
{
    Title = "Listen and type",
    AudioUrl = "https://example.com/audio.mp3",
    CorrectAnswer = "buongiorno",
    AcceptedAnswers = "buon giorno",
    CaseSensitive = false
};
// Accepts: "  buongiorno  ", "Buongiorno", "buon giorno"
```
