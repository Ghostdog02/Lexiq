# Exercise Endpoints

Base path: `/api/exercises`

Exercise management, answer submission, and progress tracking. Supports polymorphic exercise types (MultipleChoice, FillInBlank, Listening, Translation) with type-specific validation.

## Endpoints


### GET /api/exercises/{id}

Get a single exercise by ID with user progress.

**Authentication:** Required
**Roles:** Any authenticated user

**URL Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `id` | string (GUID) | Exercise ID |

**Request:**
```http
GET /api/exercises/ex1 HTTP/1.1
Host: localhost:8080
Cookie: AuthToken=eyJhbGciOiJIUzI1NiIs...
```

**Success Response (200 OK):**
```json
{
  "type": "MultipleChoice",
  "id": "ex1",
  "lessonId": "lesson1",
  "title": "Choose the correct greeting",
  "instructions": "Select the appropriate formal greeting",
  "estimatedDurationMinutes": 2,
  "difficultyLevel": "Beginner",
  "points": 10,
  "orderIndex": 0,
  "explanation": "Buongiorno is used until afternoon",
  "isLocked": false,
  "userProgress": {
    "exerciseId": "ex1",
    "isCompleted": true,
    "pointsEarned": 10,
    "completedAt": "2025-03-14T10:30:00Z"
  },
  "options": [
    {
      "id": "opt1",
      "optionText": "Buongiorno",
      "isCorrect": true,
      "orderIndex": 0
    },
    {
      "id": "opt2",
      "optionText": "Ciao",
      "isCorrect": false,
      "orderIndex": 1
    }
  ]
}
```

**Error Responses:**

**403 Forbidden:**
```json
{
  "message": "Exercise is locked. Complete previous exercises to unlock."
}
```

**Note:** Admin and ContentCreator roles bypass lock check.

**404 Not Found:**
```json
{
  "message": "Exercise not found",
  "statusCode": 404,
  "detail": null
}
```

---

### POST /api/exercises/{exerciseId}/submit

Submit an answer for an exercise and receive validation result.

**Authentication:** Required
**Roles:** Any authenticated user

**URL Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `exerciseId` | string (GUID) | Exercise ID |

**Request Body:**
```json
{
  "answer": "opt1"
}
```

**Request Body Fields:**
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `answer` | string | Yes | Answer content (format depends on exercise type) |

**Answer Format by Exercise Type:**

**MultipleChoice:**
- Submit the **option ID** (GUID), not the option text
- Example: `{ "answer": "opt1" }`

**FillInBlank:**
- Submit the text answer
- Backend performs case-insensitive comparison if `caseSensitive: false`
- Whitespace trimmed if `trimWhitespace: true`
- Example: `{ "answer": "Ciao" }`

**Listening:**
- Submit the transcribed text
- Validated against `correctAnswer` and `acceptedAnswers`
- Example: `{ "answer": "Come stai?" }`

**Translation:**
- Submit the translated text
- Uses Levenshtein distance similarity matching
- Must meet `matchingThreshold` (e.g., 0.85 = 85% similar)
- Example: `{ "answer": "Buonasera, come sta?" }`

**Request Examples:**

**MultipleChoice:**
```http
POST /api/exercises/ex1/submit HTTP/1.1
Host: localhost:8080
Cookie: AuthToken=eyJhbGciOiJIUzI1NiIs...
Content-Type: application/json

{
  "answer": "opt1"
}
```

**FillInBlank:**
```http
POST /api/exercises/ex2/submit HTTP/1.1
Host: localhost:8080
Cookie: AuthToken=eyJhbGciOiJIUzI1NiIs...
Content-Type: application/json

{
  "answer": "Ciao"
}
```

**Listening:**
```http
POST /api/exercises/ex3/submit HTTP/1.1
Host: localhost:8080
Cookie: AuthToken=eyJhbGciOiJIUzI1NiIs...
Content-Type: application/json

{
  "answer": "Come stai?"
}
```

**Translation:**
```http
POST /api/exercises/ex4/submit HTTP/1.1
Host: localhost:8080
Cookie: AuthToken=eyJhbGciOiJIUzI1NiIs...
Content-Type: application/json

{
  "answer": "Buonasera, come sta?"
}
```

**Success Response (200 OK) - Correct Answer:**
```json
{
  "isCorrect": true,
  "pointsEarned": 10,
  "correctAnswer": null,
  "explanation": "Buongiorno is the formal morning greeting"
}
```

**Success Response (200 OK) - Incorrect Answer:**
```json
{
  "isCorrect": false,
  "pointsEarned": 0,
  "correctAnswer": "Buongiorno",
  "explanation": "Buongiorno is the formal morning greeting"
}
```

**Response Fields:**
| Field | Type | Description |
|-------|------|-------------|
| `isCorrect` | boolean | `true` if answer validated correctly |
| `pointsEarned` | integer | XP earned (0 if incorrect) |
| `correctAnswer` | string? | Correct answer (only shown if `isCorrect: false`) |
| `explanation` | string? | Explanation text (shown for both correct/incorrect) |

**Side Effects on Correct Submission:**

1. **Progress upsert:** Creates or updates `UserExerciseProgress` row
   - Sets `isCompleted: true`, `pointsEarned: <exercise.Points>`
   - Preserves original `completedAt` timestamp (first correct submission)
2. **XP increment:** Adds points to `User.TotalPointsEarned` (only on first correct submission)
3. **Achievement unlock check:** Triggers achievement evaluation after XP update
4. **Next exercise unlock:** Calls `ExerciseService.UnlockNextExerciseAsync()`
   - Unlocks exercise with `orderIndex + 1` in same lesson
   - Idempotent (safe if already unlocked)

**Side Effects on Incorrect Submission:**

1. **Progress upsert:** Creates or updates row with `isCompleted: false`, `pointsEarned: 0`
2. **Retry allowed:** User can resubmit infinitely without penalty
3. **No unlocking:** Next exercise remains locked

**Error Responses:**

**400 Bad Request:**
```json
{
  "message": "Answer cannot be empty"
}
```

**401 Unauthorized:**
```json
{
  "message": "User not authenticated"
}
```

**403 Forbidden - Locked Lesson:**
```json
{
  "message": "Cannot submit answers for a locked lesson"
}
```

**403 Forbidden - Locked Exercise:**
```json
{
  "message": "Cannot submit answers for a locked exercise"
}
```

**Note:** Admin and ContentCreator roles bypass both lock checks.

**404 Not Found:**
```json
{
  "message": "Exercise not found"
}
```

---

### POST /api/exercises

Create a new exercise.

**Authentication:** Required
**Roles:** Admin, ContentCreator

**Request Body (MultipleChoice):**
```http
POST /api/exercises HTTP/1.1
Host: localhost:8080
Cookie: AuthToken=eyJhbGciOiJIUzI1NiIs...
Content-Type: application/json

{
  "type": "MultipleChoice",
  "lessonId": "lesson1",
  "title": "Choose the correct verb",
  "instructions": "Select the correct conjugation of 'essere'",
  "estimatedDurationMinutes": 3,
  "difficultyLevel": "Beginner",
  "points": 10,
  "orderIndex": null,
  "explanation": "Sono is the first-person singular form",
  "options": [
    {
      "optionText": "Sono",
      "isCorrect": true,
      "orderIndex": 0
    },
    {
      "optionText": "Sei",
      "isCorrect": false,
      "orderIndex": 1
    },
    {
      "optionText": "È",
      "isCorrect": false,
      "orderIndex": 2
    }
  ]
}
```

**Request Body (FillInBlank):**
```json
{
  "type": "FillInBlank",
  "lessonId": "lesson1",
  "title": "Complete the phrase",
  "instructions": "Fill in the missing word",
  "estimatedDurationMinutes": 2,
  "difficultyLevel": "Beginner",
  "points": 10,
  "orderIndex": null,
  "explanation": "Grazie means thank you",
  "text": "_____ mille!",
  "correctAnswer": "Grazie",
  "acceptedAnswers": "grazie,Grazia",
  "caseSensitive": false,
  "trimWhitespace": true
}
```

**Request Body (Listening):**
```json
{
  "type": "Listening",
  "lessonId": "lesson1",
  "title": "Listen and transcribe",
  "instructions": "Type what you hear in the audio",
  "estimatedDurationMinutes": 5,
  "difficultyLevel": "Intermediate",
  "points": 15,
  "orderIndex": null,
  "explanation": "Common restaurant phrase",
  "audioUrl": "/api/uploads/audio/restaurant.mp3",
  "correctAnswer": "Un tavolo per due",
  "acceptedAnswers": "un tavolo per 2,tavolo per due",
  "caseSensitive": false,
  "maxReplays": 3
}
```

**Request Body (Translation):**
```json
{
  "type": "Translation",
  "lessonId": "lesson1",
  "title": "Translate to Italian",
  "instructions": "Translate the English phrase",
  "estimatedDurationMinutes": 4,
  "difficultyLevel": "Advanced",
  "points": 20,
  "orderIndex": null,
  "explanation": "Formal introduction phrase",
  "sourceText": "My name is Maria",
  "targetText": "Mi chiamo Maria",
  "sourceLanguageCode": "en",
  "targetLanguageCode": "it",
  "matchingThreshold": 0.85
}
```

**Request Body Fields (Common to All Types):**
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `type` | string | Yes | Exercise type: `"MultipleChoice"` \| `"FillInBlank"` \| `"Listening"` \| `"Translation"` |
| `lessonId` | string (GUID) | Yes | Parent lesson ID (required for standalone creation) |
| `title` | string | Yes | Exercise title |
| `instructions` | string? | No | Instructions for the student |
| `estimatedDurationMinutes` | integer? | No | Estimated completion time |
| `difficultyLevel` | enum string | Yes | `"Beginner"` \| `"Intermediate"` \| `"Advanced"` |
| `points` | integer | Yes | XP awarded on correct completion |
| `orderIndex` | integer? | No | Position in lesson (auto-calculated if null) |
| `explanation` | string? | No | Shown after submission |

**Type-Specific Fields:**

**MultipleChoice:**
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `options` | CreateExerciseOptionDto[] | Yes | Answer choices (at least one must be correct) |

**CreateExerciseOptionDto:**
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `optionText` | string | Yes | Display text for the option |
| `isCorrect` | boolean | Yes | `true` if this is the correct answer |
| `orderIndex` | integer | Yes | Display order (0-based) |

**FillInBlank:**
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `text` | string | Yes | Text with blank (use `_____` for blank marker) |
| `correctAnswer` | string | Yes | Primary correct answer |
| `acceptedAnswers` | string? | No | Comma-separated alternative answers |
| `caseSensitive` | boolean | Yes | `true` for case-sensitive matching |
| `trimWhitespace` | boolean | Yes | `true` to trim whitespace before validation |

**Listening:**
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `audioUrl` | string | Yes | URL to audio file (upload via `/api/uploads/audio` first) |
| `correctAnswer` | string | Yes | Expected transcription |
| `acceptedAnswers` | string? | No | Comma-separated alternative transcriptions |
| `caseSensitive` | boolean | Yes | `true` for case-sensitive matching |
| `maxReplays` | integer | Yes | Max replays (0 = unlimited) |

**Translation:**
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `sourceText` | string | Yes | Text to translate from |
| `targetText` | string | Yes | Expected translation |
| `sourceLanguageCode` | string | Yes | ISO 639-1 code (e.g., `"en"`) |
| `targetLanguageCode` | string | Yes | ISO 639-1 code (e.g., `"it"`) |
| `matchingThreshold` | number | Yes | Similarity threshold (0.0-1.0, default 0.85) |

**Success Response (201 Created):**
```json
{
  "type": "MultipleChoice",
  "id": "ex99",
  "lessonId": "lesson1",
  "title": "Choose the correct verb",
  "instructions": "Select the correct conjugation of 'essere'",
  "estimatedDurationMinutes": 3,
  "difficultyLevel": "Beginner",
  "points": 10,
  "orderIndex": 5,
  "explanation": "Sono is the first-person singular form",
  "isLocked": true,
  "userProgress": null,
  "options": [
    {
      "id": "opt99",
      "optionText": "Sono",
      "isCorrect": true,
      "orderIndex": 0
    }
  ]
}
```

**Response Headers:**
```
Location: /api/exercises/ex99
```

**Error Responses:**

**400 Bad Request - Missing LessonId:**
```json
{
  "message": "LessonId is required when creating an exercise directly."
}
```

**403 Forbidden:**
```json
{
  "message": "You are not authorized to perform this action.",
  "statusCode": 403,
  "detail": null
}
```

---

### PUT /api/exercises/{id}

Update an existing exercise (partial update).

**Authentication:** Required
**Roles:** Admin, ContentCreator

**URL Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `id` | string (GUID) | Exercise ID to update |

**Request:**
```http
PUT /api/exercises/ex1 HTTP/1.1
Host: localhost:8080
Cookie: AuthToken=eyJhbGciOiJIUzI1NiIs...
Content-Type: application/json

{
  "title": "Choose the formal greeting",
  "points": 15,
  "difficultyLevel": "Intermediate"
}
```

**Request Body (all fields optional):**
| Field | Type | Description |
|-------|------|-------------|
| `title` | string? | New title |
| `instructions` | string? | New instructions |
| `estimatedDurationMinutes` | integer? | New duration estimate |
| `difficultyLevel` | enum string? | `"Beginner"` \| `"Intermediate"` \| `"Advanced"` |
| `points` | integer? | New XP value |
| `orderIndex` | integer? | New position in lesson |
| `explanation` | string? | New explanation text |

**Note:** Type-specific fields (e.g., `options`, `correctAnswer`) cannot be updated via this endpoint. Exercise type is immutable.

**Success Response (200 OK):**
```json
{
  "type": "MultipleChoice",
  "id": "ex1",
  "lessonId": "lesson1",
  "title": "Choose the formal greeting",
  "instructions": "Select the appropriate formal greeting",
  "estimatedDurationMinutes": 2,
  "difficultyLevel": "Intermediate",
  "points": 15,
  "orderIndex": 0,
  "explanation": "Buongiorno is used until afternoon",
  "isLocked": false,
  "userProgress": null,
  "options": [...]
}
```

**Error Responses:**

**404 Not Found:**
```json
{
  "message": "Exercise not found",
  "statusCode": 404,
  "detail": null
}
```

**403 Forbidden:**
```json
{
  "message": "You are not authorized to perform this action.",
  "statusCode": 403,
  "detail": null
}
```

---

### DELETE /api/exercises/{id}

Delete an exercise.

**Authentication:** Required
**Roles:** Admin, ContentCreator

**URL Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `id` | string (GUID) | Exercise ID to delete |

**Request:**
```http
DELETE /api/exercises/ex1 HTTP/1.1
Host: localhost:8080
Cookie: AuthToken=eyJhbGciOiJIUzI1NiIs...
```

**Success Response (204 No Content):**

No body returned.

**Error Responses:**

**404 Not Found:**
```json
{
  "message": "Exercise not found",
  "statusCode": 404,
  "detail": null
}
```

**403 Forbidden:**
```json
{
  "message": "You are not authorized to perform this action.",
  "statusCode": 403,
  "detail": null
}
```

---

### GET /api/exercises/{id}/correct-answer

Get the correct answer for an exercise (useful for E2E tests and content creators).

**Authentication:** Required
**Roles:** Any authenticated user

**URL Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `id` | string (GUID) | Exercise ID |

**Request:**
```http
GET /api/exercises/ex1/correct-answer HTTP/1.1
Host: localhost:8080
Cookie: AuthToken=eyJhbGciOiJIUzI1NiIs...
```

**Success Response (200 OK):**
```json
{
  "correctAnswer": "Buongiorno"
}
```

**Response Fields:**
| Field | Type | Description |
|-------|------|-------------|
| `correctAnswer` | string? | The correct answer for the exercise (null if exercise has no correct option) |

**Correct Answer by Exercise Type:**

**MultipleChoice:**
- Returns `OptionText` of the first option where `IsCorrect = true`
- Example: `{ "correctAnswer": "Buongiorno" }`

**FillInBlank:**
- Returns the `CorrectAnswer` property
- Example: `{ "correctAnswer": "Ciao" }`

**Listening:**
- Returns the `CorrectAnswer` property
- Example: `{ "correctAnswer": "Come stai?" }`

**Translation:**
- Returns the `TargetText` property (the expected translation)
- Example: `{ "correctAnswer": "Buonasera, come sta?" }`

**Error Responses:**

**404 Not Found:**
```json
{
  "message": "Exercise not found",
  "statusCode": 404,
  "detail": null
}
```

**401 Unauthorized:**
```json
{
  "message": "User not authenticated"
}
```

**Use Cases:**
- **E2E Testing:** Tests can programmatically verify correct answers without parsing exercise details
- **Content Creators:** Preview correct answers without manually solving exercises
- **Students:** Can access correct answers (students already see answers after wrong submissions, so this doesn't introduce new security vulnerabilities)

**Edge Case:**
If a `MultipleChoice` exercise has no option with `IsCorrect = true`, returns `{ "correctAnswer": null }`.

---



## Business Rules

### Exercise Unlocking

1. **First exercise in lesson:** Unlocked when lesson is unlocked
2. **Subsequent exercises:** Unlock sequentially on completion (by `orderIndex`)
3. **Sequential enforcement:** Must complete Exercise N to unlock Exercise N+1
4. **Idempotent:** Unlock methods safe to call multiple times
5. **Manual unlock:** No admin endpoint exists (unlock happens automatically on submission)
6. **Admin bypass:** Admin and ContentCreator roles can view and submit answers to locked exercises

### Answer Validation

**MultipleChoice:**
- Validates that submitted option ID exists and `isCorrect: true`
- Option IDs (GUIDs) are stable across updates
- Frontend must submit option ID, not option text

**FillInBlank:**
- Primary validation against `correctAnswer`
- Falls back to `acceptedAnswers` (comma-separated)
- `caseSensitive: false` → case-insensitive comparison
- `trimWhitespace: true` → trims leading/trailing whitespace

**Listening:**
- Same validation logic as FillInBlank
- `maxReplays: 0` → unlimited replays (frontend enforces replay limit)
- Always trims whitespace regardless of `trimWhitespace` flag

**Translation:**
- Uses Levenshtein distance for fuzzy matching
- Calculates similarity: `(longer.length - distance) / longer.length`
- Must meet `matchingThreshold` (e.g., 0.85 = 85% similar)
- Always case-insensitive and whitespace-trimmed

### Progress Tracking

**Upsert Pattern:**
- `UserExerciseProgress` uses composite PK: `(UserId, ExerciseId)`
- `SubmitAnswerAsync` performs upsert: find existing or create new
- `CompletedAt` preserved on first correct submission (never overwritten)
- Wrong answers update row to `isCompleted: false`, `pointsEarned: 0`

**XP Increment:**
- `User.TotalPointsEarned` incremented only on first correct submission
- Prevents double-counting on retry after initially correct answer
- Cached aggregate avoids expensive `SUM` queries for leaderboard

**Infinite Retries:**
- No penalty for wrong answers
- Student can resubmit as many times as needed
- Only first correct submission counts for XP and unlocking

### OrderIndex Auto-Calculation

When `orderIndex` is `null` in create request:
```
orderIndex = MAX(existing_exercises.orderIndex) + 1
```

If lesson has no exercises yet, starts at `0`.

### Polymorphic JSON Serialization

**Type Discriminator MUST Be First:**
- System.Text.Json requires `"type"` as the first property in JSON
- Backend uses `OkPolymorphic<T>()` helper to set `DeclaredType`
- Frontend mapping: `return { type: ExerciseType.X, ...base }` (NOT `{ ...base, type: ... }`)

**Deserialization:**
- `[JsonPolymorphic]` attribute maps `"type"` string to concrete DTO class
- `"MultipleChoice"` → `MultipleChoiceExerciseDto`
- `"FillInBlank"` → `FillInBlankExerciseDto`
- `"Listening"` → `ListeningExerciseDto`
- `"Translation"` → `TranslationExerciseDto`

---

## Common Workflows

### Student Submits Answer (Correct)

```
1. Student fills in answer field
   ↓
2. Frontend submits: POST /api/exercises/{exerciseId}/submit
   { "answer": "opt1" }
   ↓
3. Backend validates: MultipleChoice → check option.IsCorrect
   ↓
4. Backend upserts UserExerciseProgress (isCompleted: true, pointsEarned: 10)
   ↓
5. Backend increments User.TotalPointsEarned (first correct only)
   ↓
6. Backend checks and unlocks achievements
   ↓
7. Backend unlocks next exercise (orderIndex + 1)
   ↓
8. Response: { isCorrect: true, pointsEarned: 10, correctAnswer: null }
   ↓
9. Frontend shows "Correct!" feedback with explanation
   ↓
10. Frontend enables next exercise button
```

### Student Submits Answer (Incorrect)

```
1. Student submits wrong answer
   ↓
2. POST /api/exercises/{exerciseId}/submit
   ↓
3. Backend validates: fails
   ↓
4. Backend upserts UserExerciseProgress (isCompleted: false, pointsEarned: 0)
   ↓
5. Response: { isCorrect: false, pointsEarned: 0, correctAnswer: "Buongiorno" }
   ↓
6. Frontend shows "Incorrect" feedback with correct answer
   ↓
7. Frontend keeps submit button enabled (allows retry)
   ↓
8. Student can retry infinitely
```

### Student Restores Progress After Session Restart

```
1. Student navigates to lesson
   ↓
2. Frontend calls: GET /api/exercises/lesson/{lessonId}
   ↓
3. Backend returns exercises with userProgress embedded
   ↓
4. Frontend maps exercises:
   - isCompleted: true → show checkmark, pre-fill answer
   - isLocked: true → disable exercise
   ↓
5. Student picks up where they left off
```

### ContentCreator Creates MultipleChoice Exercise

```
1. Creator opens exercise editor
   ↓
2. Selects "MultipleChoice" type
   ↓
3. Fills in title, instructions, points, difficulty
   ↓
4. Adds 3 options (sets one as correct)
   ↓
5. POST /api/exercises
   {
     "type": "MultipleChoice",
     "lessonId": "lesson1",
     "title": "Choose the greeting",
     "points": 10,
     "orderIndex": null,
     "options": [
       { "optionText": "Buongiorno", "isCorrect": true, "orderIndex": 0 },
       { "optionText": "Ciao", "isCorrect": false, "orderIndex": 1 },
       { "optionText": "Arrivederci", "isCorrect": false, "orderIndex": 2 }
     ]
   }
   ↓
6. Backend creates Exercise with auto-calculated orderIndex
   ↓
7. Backend creates 3 ExerciseOption rows (each gets GUID id)
   ↓
8. Backend sets isLocked: true (manual unlock or complete previous)
   ↓
9. Response: exercise DTO with new ID
   ↓
10. Frontend navigates to exercise preview
```

### ContentCreator Creates FillInBlank Exercise

```
1. Creator opens exercise editor
   ↓
2. Selects "FillInBlank" type
   ↓
3. Fills in text with blank marker: "_____ mi chiamo Marco"
   ↓
4. Sets correctAnswer: "Ciao"
   ↓
5. Sets acceptedAnswers: "ciao,salve"
   ↓
6. Sets caseSensitive: false, trimWhitespace: true
   ↓
7. POST /api/exercises
   {
     "type": "FillInBlank",
     "lessonId": "lesson1",
     "title": "Complete the sentence",
     "text": "_____ mi chiamo Marco",
     "correctAnswer": "Ciao",
     "acceptedAnswers": "ciao,salve",
     "caseSensitive": false,
     "trimWhitespace": true
   }
   ↓
8. Backend creates FillInBlankExercise
   ↓
9. Response: exercise DTO
   ↓
10. Frontend shows preview
```

### Admin Tests Locked Exercise

```
1. Admin navigates to locked lesson
   ↓
2. GET /api/exercises/lesson/{lessonId}
   ↓
3. Backend checks: user.CanBypassLocksAsync() → true
   ↓
4. Returns all exercises (including locked ones)
   ↓
5. Admin clicks locked exercise
   ↓
6. GET /api/exercises/{id}
   ↓
7. Backend bypasses lock check (Admin role)
   ↓
8. Returns exercise DTO
   ↓
9. Admin submits answer
   ↓
10. POST /api/exercises/{id}/submit
   ↓
11. Backend bypasses lock check in SubmitAnswerAsync
   ↓
12. Validates answer, returns result (no unlock side effects for admin)
```

---

## Performance Considerations

### Batch Progress Queries

**Inefficient (N+1 queries):**
```
GET /api/exercises/lesson/lesson1
GET /api/exercises/lesson/lesson2
GET /api/exercises/lesson/lesson3
```

**Efficient (single query):**
```
GET /api/lessons/course/{courseId}
  → Returns lessons with embedded progress via GetProgressForLessonsAsync
```

### Expensive Operations

**`GetFullLessonProgressAsync` is expensive:**
- Issues GroupJoin across all exercises in lesson (~6 DB operations)
- Call ONLY at:
  - Lesson load (once per navigation)
  - Lesson complete endpoint
  - Dedicated progress endpoints
- **NEVER call inside `SubmitAnswerAsync`** (would run on every submission)

**`GetLessonSubmissionsAsync` returns all exercises:**
- Returns submission results for EVERY exercise, not just attempted ones
- Frontend must filter appropriately for display
- Use only for session restoration, not per-submission updates

---

## Testing Notes

### Integration Test Fixtures

**DatabaseFixture seeds:**
- 40 exercises across 4 types (10 each)
- `ExerciseIds[0-9]`: FillInBlank
- `ExerciseIds[10-19]`: MultipleChoice (with 3 options each)
- `ExerciseIds[20-29]`: Listening
- `ExerciseIds[30-39]`: Translation

**Why 40 exercises?**
- `UserExerciseProgress` PK is `(UserId, ExerciseId)`
- Streak tests need one distinct `ExerciseId` per calendar day
- 10 iterations per type enables comprehensive type-specific validation testing

**Test Helper:**
```csharp
var exercises = await GetExercisesForLessonAsync(Fixture.ExerciseIds[0]);
var firstEx = exercises.First(e => e.OrderIndex == 0);
var submitResult = await SubmitAnswerAsync(firstEx.Id, "answer");
```

### Polymorphic Deserialization in Tests

**Always pass `JsonOptions`:**
```csharp
// WRONG — uses default options, fails on abstract ExerciseDto
var dto = await response.Content.ReadFromJsonAsync<ExerciseDto>(
    cancellationToken: TestContext.Current.CancellationToken
);

// CORRECT — uses application's JsonSerializerOptions
var dto = await response.Content.ReadFromJsonAsync<ExerciseDto>(
    JsonOptions,
    TestContext.Current.CancellationToken
);
```

This applies to both `ReadFromJsonAsync<ExerciseDto>` and `ReadFromJsonAsync<List<ExerciseDto>>`.
