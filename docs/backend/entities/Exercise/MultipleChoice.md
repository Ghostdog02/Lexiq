# MultipleChoiceExercise

**Inherits:** `Exercise`
**Discriminator:** `"MultipleChoiceExercise"`

## Purpose

Select the correct option from multiple choices.

## Properties

| Property | Type | Relationship | Description |
|----------|------|--------------|-------------|
| `Options` | `List<ExerciseOption>` | One-to-Many | Answer choices |

## Related Entity: ExerciseOption

| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` (GUID) | Option identifier |
| `ExerciseId` | `string` | Parent exercise |
| `OptionText` | `string` (Max 200) | Display text |
| `IsCorrect` | `bool` | Correct answer flag |
| `OrderIndex` | `int` | Display order |

## Validation Rules

User submits **option ID** (GUID), not text:

```csharp
var isCorrect = exercise.Options
    .FirstOrDefault(o => o.Id == submittedAnswer)
    ?.IsCorrect ?? false;
```

### Outcomes
- Correct option ID → `IsCorrect = true`
- Wrong option ID → `IsCorrect = false`
- Invalid option ID → `IsCorrect = false`

## Test Coverage

3 tests in `ExerciseValidationTests.cs`:
- Correct option ID returns true
- Wrong option ID returns false
- Invalid option ID returns false

## Example

```csharp
var exercise = new MultipleChoiceExercise
{
    Title = "Which is correct?",
    Options = new List<ExerciseOption>
    {
        new() { OptionText = "Ciao", IsCorrect = true, OrderIndex = 0 },
        new() { OptionText = "Hola", IsCorrect = false, OrderIndex = 1 },
        new() { OptionText = "Bonjour", IsCorrect = false, OrderIndex = 2 }
    }
};

// User submits: options[0].Id → Correct
// User submits: options[1].Id → Wrong
```
