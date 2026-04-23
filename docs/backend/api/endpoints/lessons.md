# Lesson Endpoints

Base path: `/api/lessons`

Lesson management, progress tracking, and completion operations.

## Endpoints

### GET /api/lessons/course/{courseId}

Get all lessons for a specific course with user progress.

**Authentication:** Required
**Roles:** Any authenticated user

**URL Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `courseId` | string (GUID) | Course ID |

**Request:**
```http
GET /api/lessons/course/abc123 HTTP/1.1
Host: localhost:8080
Cookie: AuthToken=eyJhbGciOiJIUzI1NiIs...
```

**Success Response (200 OK):**
```json
[
  {
    "id": "lesson1",
    "courseId": "abc123",
    "title": "Greetings and Introductions",
    "description": "Learn basic Italian greetings",
    "estimatedDurationMinutes": 15,
    "orderIndex": 0,
    "isLocked": false,
    "isCompleted": true,
    "completedExercises": 10,
    "totalExercises": 10,
    "xpEarned": 100,
    "percentComplete": 100
  },
  {
    "id": "lesson2",
    "courseId": "abc123",
    "title": "Numbers and Counting",
    "description": "Master Italian numbers 1-100",
    "estimatedDurationMinutes": 20,
    "orderIndex": 1,
    "isLocked": false,
    "isCompleted": false,
    "completedExercises": 3,
    "totalExercises": 10,
    "xpEarned": 30,
    "percentComplete": 30
  },
  {
    "id": "lesson3",
    "courseId": "abc123",
    "title": "Food and Dining",
    "description": "Restaurant vocabulary and phrases",
    "estimatedDurationMinutes": 25,
    "orderIndex": 2,
    "isLocked": true,
    "isCompleted": false,
    "completedExercises": 0,
    "totalExercises": 12,
    "xpEarned": 0,
    "percentComplete": 0
  }
]
```

**Response Fields:**
| Field | Type | Description |
|-------|------|-------------|
| `id` | string (GUID) | Unique lesson identifier |
| `courseId` | string (GUID) | Parent course ID |
| `title` | string | Lesson title |
| `description` | string | Lesson description |
| `estimatedDurationMinutes` | integer | Estimated completion time |
| `orderIndex` | integer | Position in course (0-based) |
| `isLocked` | boolean | `true` if lesson not yet unlocked for user |
| `isCompleted` | boolean | `true` if user met 70% completion threshold |
| `completedExercises` | integer | Number of exercises completed correctly |
| `totalExercises` | integer | Total exercises in lesson |
| `xpEarned` | integer | XP earned from this lesson |
| `percentComplete` | integer | Completion percentage (0-100) |

**Error Responses:**

**401 Unauthorized:**
```json
{
  "message": "User is not authorized.",
  "statusCode": 401,
  "detail": null
}
```

**404 Not Found:**
```json
{
  "message": "Course 'abc123' not found",
  "statusCode": 404,
  "detail": null
}
```

---

### GET /api/lessons/{lessonId}

Get full lesson details including exercises and user progress.

**Authentication:** Required
**Roles:** Any authenticated user

**URL Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `lessonId` | string (GUID) | Lesson ID |

**Request:**
```http
GET /api/lessons/lesson1 HTTP/1.1
Host: localhost:8080
Cookie: AuthToken=eyJhbGciOiJIUzI1NiIs...
```

**Success Response (200 OK):**
```json
{
  "id": "lesson1",
  "courseId": "abc123",
  "title": "Greetings and Introductions",
  "description": "Learn basic Italian greetings",
  "estimatedDurationMinutes": 15,
  "orderIndex": 0,
  "lessonContent": {
    "blocks": [
      {
        "type": "header",
        "data": { "text": "Greetings in Italian", "level": 2 }
      },
      {
        "type": "paragraph",
        "data": { "text": "Italian greetings vary based on formality..." }
      }
    ]
  },
  "isLocked": false,
  "isCompleted": true,
  "completedExercises": 10,
  "totalExercises": 10,
  "xpEarned": 100,
  "percentComplete": 100,
  "exercises": [
    {
      "type": "FillInBlank",
      "id": "ex1",
      "lessonId": "lesson1",
      "question": "Complete: ____ mi chiamo Marco",
      "orderIndex": 0,
      "pointValue": 10,
      "isLocked": false,
      "blanks": ["Ciao"],
      "isCompleted": true,
      "userAnswer": "Ciao",
      "isCorrect": true
    }
  ]
}
```

**Error Responses:**

**401 Unauthorized:**
```json
{
  "message": "User was not logged in",
  "statusCode": 401,
  "detail": null
}
```

**404 Not Found:**
```json
{
  "message": "Lesson not found",
  "statusCode": 404,
  "detail": null
}
```

---

### POST /api/lessons/{lessonId}/complete

Attempt to complete a lesson. Unlocks next lesson if 70%+ XP threshold met.

**Authentication:** Required
**Roles:** Any authenticated user

**URL Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `lessonId` | string (GUID) | Lesson ID to complete |

**Request:**
```http
POST /api/lessons/lesson1/complete HTTP/1.1
Host: localhost:8080
Cookie: AuthToken=eyJhbGciOiJIUzI1NiIs...
```

**Success Response (200 OK) - Threshold Met:**
```json
{
  "isComplete": true,
  "completedExercises": 8,
  "totalExercises": 10,
  "percentComplete": 80,
  "xpEarned": 80,
  "totalXp": 100,
  "nextLessonId": "lesson2",
  "nextLessonUnlocked": true,
  "message": "Congratulations! You've completed this lesson."
}
```

**Success Response (200 OK) - Threshold Not Met:**
```json
{
  "isComplete": false,
  "completedExercises": 6,
  "totalExercises": 10,
  "percentComplete": 60,
  "xpEarned": 60,
  "totalXp": 100,
  "nextLessonId": null,
  "nextLessonUnlocked": false,
  "message": "Complete at least 70% of exercises (7/10) to unlock the next lesson."
}
```

**Response Fields:**
| Field | Type | Description |
|-------|------|-------------|
| `isComplete` | boolean | `true` if ≥70% threshold met |
| `completedExercises` | integer | Count of correctly completed exercises |
| `totalExercises` | integer | Total exercises in lesson |
| `percentComplete` | integer | Completion percentage (0-100) |
| `xpEarned` | integer | XP earned from completed exercises |
| `totalXp` | integer | Total possible XP in lesson |
| `nextLessonId` | string \| null | Next lesson ID (null if last lesson) |
| `nextLessonUnlocked` | boolean | `true` if next lesson was unlocked |
| `message` | string | User-friendly completion message |

**Error Responses:**

**404 Not Found:**
```json
{
  "message": "Lesson 'lesson999' not found",
  "statusCode": 404,
  "detail": null
}
```

---

### GET /api/lessons/{lessonId}/next

Get the next lesson after the current one (cross-course navigation).

**Authentication:** Required
**Roles:** Any authenticated user

**URL Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `lessonId` | string (GUID) | Current lesson ID |

**Request:**
```http
GET /api/lessons/lesson1/next HTTP/1.1
Host: localhost:8080
Cookie: AuthToken=eyJhbGciOiJIUzI1NiIs...
```

**Success Response (200 OK) - Next Lesson Exists:**
```json
{
  "id": "lesson2",
  "courseId": "abc123",
  "title": "Numbers and Counting",
  "description": "Master Italian numbers 1-100",
  "estimatedDurationMinutes": 20,
  "orderIndex": 1,
  "isLocked": false
}
```

**Success Response (200 OK) - Last Lesson:**
```json
{
  "message": "This is the last lesson in the language"
}
```

**Navigation Logic:**
1. Check for next lesson in same course (by `orderIndex`)
2. If last lesson in course, find next course in language
3. Return first lesson of next course
4. If no next course, return "last lesson" message

---

### GET /api/lessons/{lessonId}/exercises

Get all exercises for a specific lesson.

**Authentication:** Required
**Roles:** Any authenticated user

**URL Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `lessonId` | string (GUID) | Lesson ID |

**Request:**
```http
GET /api/lessons/lesson1/exercises HTTP/1.1
Host: localhost:8080
Cookie: AuthToken=eyJhbGciOiJIUzI1NiIs...
```

**Success Response (200 OK):**
```json
[
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
    "options": [
      {
        "id": "opt1",
        "optionText": "Buongiorno",
        "isCorrect": true,
        "orderIndex": 0
      }
    ]
  }
]
```

**Response Fields:**
See [Exercise Endpoints](exercises.md) for complete ExerciseDto field documentation including all exercise types (MultipleChoice, FillInBlank, Listening, Translation).

**Error Responses:**

**401 Unauthorized:**
```json
{
  "message": "User is not authorized.",
  "statusCode": 401,
  "detail": null
}
```

---

### GET /api/lessons/{lessonId}/progress

Get user progress for all exercises in a lesson.

**Authentication:** Required
**Roles:** Any authenticated user

**URL Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `lessonId` | string (GUID) | Lesson ID |

**Request:**
```http
GET /api/lessons/lesson1/progress HTTP/1.1
Host: localhost:8080
Cookie: AuthToken=eyJhbGciOiJIUzI1NiIs...
```

**Success Response (200 OK):**
```json
[
  {
    "exerciseId": "ex1",
    "isCompleted": true,
    "pointsEarned": 10,
    "completedAt": "2025-03-14T10:30:00Z"
  },
  {
    "exerciseId": "ex2",
    "isCompleted": false,
    "pointsEarned": 0,
    "completedAt": null
  }
]
```

**Note:** Returns progress for ALL exercises in the lesson, including those never attempted (with `isCompleted: false`, `pointsEarned: 0`, `completedAt: null`).

**Response Fields:**
| Field | Type | Description |
|-------|------|-------------|
| `exerciseId` | string (GUID) | Exercise ID |
| `isCompleted` | boolean | `true` if answered correctly |
| `pointsEarned` | integer | XP earned (0 if incorrect or not attempted) |
| `completedAt` | DateTime? | UTC timestamp of first correct submission |

**Error Responses:**

**401 Unauthorized:**
```json
{
  "message": "User not authenticated"
}
```

---

### GET /api/lessons/{lessonId}/submissions

Get submission results for all exercises in a lesson (includes correct answers for failed attempts and lesson progress summary).

**Authentication:** Required
**Roles:** Any authenticated user

**URL Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `lessonId` | string (GUID) | Lesson ID |

**Request:**
```http
GET /api/lessons/lesson1/submissions HTTP/1.1
Host: localhost:8080
Cookie: AuthToken=eyJhbGciOiJIUzI1NiIs...
```

**Success Response (200 OK):**
```json
[
  {
    "isCorrect": true,
    "pointsEarned": 10,
    "correctAnswer": null,
    "explanation": "Buongiorno is the formal morning greeting",
    "lessonProgress": {
      "completedExercises": 7,
      "totalExercises": 10,
      "earnedXp": 70,
      "totalPossibleXp": 100,
      "completionPercentage": 0.70,
      "meetsCompletionThreshold": true
    }
  },
  {
    "isCorrect": false,
    "pointsEarned": 0,
    "correctAnswer": "Ciao",
    "explanation": "Mi chiamo means 'my name is'",
    "lessonProgress": {
      "completedExercises": 7,
      "totalExercises": 10,
      "earnedXp": 70,
      "totalPossibleXp": 100,
      "completionPercentage": 0.70,
      "meetsCompletionThreshold": true
    }
  }
]
```

**Response Fields:**
| Field | Type | Description |
|-------|------|-------------|
| `isCorrect` | boolean | `true` if answered correctly |
| `pointsEarned` | integer | XP earned (0 if incorrect or not attempted) |
| `correctAnswer` | string? | Correct answer (only shown if `isCorrect: false`) |
| `explanation` | string? | Explanation text |
| `lessonProgress` | LessonProgressSummary | Current lesson-wide progress |

**LessonProgressSummary:**
| Field | Type | Description |
|-------|------|-------------|
| `completedExercises` | integer | Count of correctly completed exercises |
| `totalExercises` | integer | Total exercises in lesson |
| `earnedXp` | integer | XP earned from completed exercises |
| `totalPossibleXp` | integer | Total possible XP in lesson |
| `completionPercentage` | number | Completion percentage (0.0-1.0) |
| `meetsCompletionThreshold` | boolean | `true` if ≥70% threshold met |

**Note:** Returns submission results for ALL exercises in the lesson, not just attempted ones. Use this to restore user state after session restart.

**Error Responses:**

**401 Unauthorized:**
```json
{
  "message": "User not authenticated"
}
```

---

### POST /api/lessons

Create a new lesson with optional exercises.

**Authentication:** Required
**Roles:** Admin, ContentCreator

**Request:**
```http
POST /api/lessons HTTP/1.1
Host: localhost:8080
Cookie: AuthToken=eyJhbGciOiJIUzI1NiIs...
Content-Type: application/json

{
  "courseId": "abc123",
  "title": "Past Tense Verbs",
  "description": "Learn to conjugate Italian past tense",
  "estimatedDurationMinutes": 30,
  "orderIndex": null,
  "content": {
    "blocks": [
      {
        "type": "header",
        "data": { "text": "Introduction to Past Tense", "level": 2 }
      }
    ]
  },
  "exercises": [
    {
      "type": "FillInBlank",
      "question": "Complete: Ieri io ___ al cinema",
      "correctAnswer": "sono andato",
      "pointValue": 10,
      "orderIndex": 0
    }
  ]
}
```

**Request Body:**
| Field | Type | Required | Description |
|-------|------|----------|-------------|
| `courseId` | string (GUID) | Yes | Parent course ID |
| `title` | string | Yes | Lesson title |
| `description` | string | No | Lesson description |
| `estimatedDurationMinutes` | integer | No | Estimated time to complete |
| `orderIndex` | integer \| null | No | Position in course (auto-calculated if null) |
| `content` | EditorJS object | No | Lesson content in EditorJS format |
| `exercises` | ExerciseDto[] | No | Exercises to create (nested) |

**Success Response (201 Created):**
```json
{
  "id": "lesson99",
  "courseId": "abc123",
  "title": "Past Tense Verbs",
  "description": "Learn to conjugate Italian past tense",
  "estimatedDurationMinutes": 30,
  "orderIndex": 5,
  "isLocked": true
}
```

**Response Headers:**
```
Location: /api/lessons/lesson99
```

**Error Responses:**

**400 Bad Request:**
```json
{
  "message": "Course with ID 'abc123' not found.",
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

### PUT /api/lessons/{lessonId}

Update an existing lesson (partial update).

**Authentication:** Required
**Roles:** Admin, ContentCreator

**URL Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `lessonId` | string (GUID) | Lesson ID to update |

**Request:**
```http
PUT /api/lessons/lesson1 HTTP/1.1
Host: localhost:8080
Cookie: AuthToken=eyJhbGciOiJIUzI1NiIs...
Content-Type: application/json

{
  "title": "Greetings and Farewells",
  "description": "Updated description",
  "estimatedDurationMinutes": 20
}
```

**Request Body (all fields optional):**
| Field | Type | Description |
|-------|------|-------------|
| `courseId` | string (GUID) | Move to different course |
| `title` | string | New title |
| `description` | string | New description |
| `estimatedDurationMinutes` | integer | New duration estimate |
| `orderIndex` | integer | New position |
| `lessonContent` | EditorJS object | New content |

**Success Response (200 OK):**
```json
{
  "id": "lesson1",
  "courseId": "abc123",
  "title": "Greetings and Farewells",
  "description": "Updated description",
  "estimatedDurationMinutes": 20,
  "orderIndex": 0
}
```

**Error Responses:**

**404 Not Found:**
```json
{
  "message": "Lesson not found",
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

### DELETE /api/lessons/{lessonId}

Delete a lesson and all associated exercises.

**Authentication:** Required
**Roles:** Admin, ContentCreator

**URL Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `lessonId` | string (GUID) | Lesson ID to delete |

**Request:**
```http
DELETE /api/lessons/lesson1 HTTP/1.1
Host: localhost:8080
Cookie: AuthToken=eyJhbGciOiJIUzI1NiIs...
```

**Success Response (204 No Content):**

No body returned.

**Error Responses:**

**404 Not Found:**
```json
{
  "message": "Lesson not found",
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

### POST /api/lessons/{lessonId}/unlock

Manually unlock a lesson (admin/testing purposes).

**Authentication:** Required
**Roles:** Admin only

**URL Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `lessonId` | string (GUID) | Lesson ID to unlock |

**Request:**
```http
POST /api/lessons/lesson3/unlock HTTP/1.1
Host: localhost:8080
Cookie: AuthToken=eyJhbGciOiJIUzI1NiIs...
```

**Success Response (200 OK):**
```json
{
  "message": "Lesson unlocked successfully"
}
```

**Side Effect:**
- Sets `lesson.IsLocked = false`
- Unlocks first exercise in lesson (via `ExerciseService.UnlockFirstExerciseInLessonAsync`)
- Idempotent (safe to call multiple times)

**Error Responses:**

**403 Forbidden:**
```json
{
  "message": "You are not authorized to perform this action.",
  "statusCode": 403,
  "detail": null
}
```

---

## Business Rules

### Lesson Unlocking

1. **First lesson in first course**: Unlocked for all users (seed data)
2. **Subsequent lessons**: Unlock when previous lesson completed (≥70% XP)
3. **Cross-course unlocking**: Completing last lesson in course unlocks first lesson of next course
4. **Manual unlock**: Admin can bypass via `POST /unlock` endpoint

### Completion Threshold

- **Required**: 70% of total lesson XP
- **Example**: Lesson with 10 exercises (100 XP total) requires 7 correct (70 XP)
- **Retries**: Unlimited - wrong answers don't count against completion
- **First exercise**: Unlocked when lesson unlocks
- **Rest of exercises**: Unlock sequentially on completion

### OrderIndex Auto-Calculation

When `orderIndex` is `null` in create request:
```
orderIndex = MAX(existing_lessons.orderIndex) + 1
```

If course has no lessons yet, starts at `0`.

### EditorJS Content Format

Lesson content uses EditorJS JSON format:

```json
{
  "blocks": [
    { "type": "header", "data": { "text": "Title", "level": 2 } },
    { "type": "paragraph", "data": { "text": "Content..." } },
    { "type": "list", "data": { "style": "unordered", "items": ["Item 1", "Item 2"] } },
    { "type": "image", "data": { "url": "/uploads/image.png", "caption": "Caption" } }
  ]
}
```

## Common Workflows

### Student Completes Lesson

```
1. Student completes 8/10 exercises (80% XP)
   ↓
2. POST /api/lessons/{lessonId}/complete
   ↓
3. Backend calculates: 80% ≥ 70% threshold ✓
   ↓
4. Backend sets lesson as completed for user
   ↓
5. Backend unlocks next lesson (if exists)
   ↓
6. Backend unlocks first exercise of next lesson
   ↓
7. Response: { isComplete: true, nextLessonUnlocked: true }
   ↓
8. Frontend shows "Lesson Complete!" modal
   ↓
9. Frontend navigates to next lesson
```

### ContentCreator Creates Lesson

```
1. Creator opens lesson editor
   ↓
2. Fills in title, description, content (EditorJS)
   ↓
3. Adds exercises (MultipleChoice, FillInBlank, etc.)
   ↓
4. POST /api/lessons with nested exercises
   ↓
5. Backend creates lesson with isLocked=true
   ↓
6. Backend creates all exercises (first unlocked, rest locked)
   ↓
7. Backend auto-calculates orderIndex
   ↓
8. Response: lesson DTO with new ID
   ↓
9. Frontend navigates to lesson preview
```
