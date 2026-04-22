# TranslationExercise

**Inherits:** `Exercise`
**Discriminator:** `"TranslationExercise"`

## Purpose

Translate text from source to target language using fuzzy matching with Levenshtein distance algorithm. Allows for minor spelling errors while maintaining translation accuracy.

## Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `SourceText` | `string` (Max 1000) | Required | Text to translate from |
| `TargetText` | `string` (Max 1000) | Required | Expected translation |
| `SourceLanguageCode` | `string` (Max 10) | Required | ISO code (e.g., "en") |
| `TargetLanguageCode` | `string` (Max 10) | Required | ISO code (e.g., "it") |
| `MatchingThreshold` | `double` | 0.85 | Similarity threshold (0.0-1.0) |

## Levenshtein Distance Algorithm

```csharp
similarity = (longerLength - distance) / longerLength

// Example: "grazie" vs "grazi"
// Distance = 1 (one deletion)
// Length = 6
// Similarity = (6 - 1) / 6 = 0.833 = 83.3%
```

### Threshold Guidelines

| Threshold | Leniency | Example (10-char word) |
|-----------|----------|------------------------|
| 0.7 (70%) | Lenient | Accepts 3 errors |
| 0.8 (80%) | Moderate | Accepts 2 errors |
| 0.9 (90%) | Strict | Accepts 1 error |
| 1.0 (100%) | Exact | No errors allowed |

## Validation Rules

- **Always case-insensitive**: "Ciao" == "ciao"
- **Always trims whitespace**: "  ciao  " == "ciao"
- **Empty strings**: `"" == ""` returns true (100% similarity)

## Test Coverage

8 tests in `ExerciseValidationTests.cs`:
- Exact match (100% similarity)
- One character difference scenarios
- Below/above threshold edge cases
- Multiple substitutions
- Empty string handling

## Examples

### Lenient Matching (70%)
```csharp
TargetText = "buongiorno"
MatchingThreshold = 0.7

"buongirno"  // 90% → ✓ Pass
"buon"       // 40% → ✗ Fail
```

### Strict Matching (90%)
```csharp
TargetText = "grazie"
MatchingThreshold = 0.9

"grazie"  // 100% → ✓ Pass
"grazi"   // 83%  → ✗ Fail (below 90%)
"graz"    // 67%  → ✗ Fail
```

## Common Pattern

```csharp
var exercise = new TranslationExercise
{
    Title = "Translate: Good morning",
    SourceText = "Good morning",
    TargetText = "buongiorno",
    SourceLanguageCode = "en",
    TargetLanguageCode = "it",
    MatchingThreshold = 0.8  // 80% similarity required
};
// Accepts: "buongiorno", "buongirno" (90%), etc.
// Rejects: "buon" (40%)
```
