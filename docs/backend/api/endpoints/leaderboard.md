# Leaderboard & Progress Endpoints

Gamification features including leaderboards, XP tracking, levels, and streaks.

## Endpoints

### GET /api/leaderboard

Get leaderboard rankings with optional time filtering.

**Authentication:** Not required (but includes current user rank if authenticated)

**Query Parameters:**
| Parameter | Type | Required | Default | Description |
|-----------|------|----------|---------|-------------|
| `timeFrame` | enum | No | `AllTime` | Filter by time period: `Weekly`, `Monthly`, `AllTime` |

**Request:**
```http
GET /api/leaderboard?timeFrame=Weekly HTTP/1.1
Host: localhost:8080
Cookie: AuthToken=eyJhbGciOiJIUzI1NiIs... (optional)
```

**Success Response (200 OK):**
```json
{
  "timeFrame": "Weekly",
  "entries": [
    {
      "rank": 1,
      "userId": "user1",
      "userName": "alice_rossi",
      "totalXp": 1250,
      "level": 6,
      "currentStreak": 7,
      "longestStreak": 14,
      "avatarUrl": "/api/user/user1/avatar",
      "rankChange": 2
    },
    {
      "rank": 2,
      "userId": "user2",
      "userName": "bob_bianchi",
      "totalXp": 980,
      "level": 5,
      "currentStreak": 3,
      "longestStreak": 8,
      "avatarUrl": null,
      "rankChange": -1
    },
    {
      "rank": 3,
      "userId": "user3",
      "userName": "carlo_ferrari",
      "totalXp": 850,
      "level": 5,
      "currentStreak": 5,
      "longestStreak": 5,
      "avatarUrl": "/api/user/user3/avatar",
      "rankChange": 0
    }
  ],
  "currentUserEntry": {
    "rank": 15,
    "userId": "currentUser",
    "userName": "you",
    "totalXp": 450,
    "level": 3,
    "currentStreak": 2,
    "longestStreak": 4,
    "avatarUrl": "/api/user/currentUser/avatar",
    "rankChange": 3
  }
}
```

**Response Fields:**
| Field | Type | Description |
|-------|------|-------------|
| `timeFrame` | enum | Echoes request parameter |
| `entries` | LeaderboardEntry[] | Top users (max 100) |
| `entries[].rank` | integer | Position on leaderboard (1-based) |
| `entries[].userId` | string (GUID) | User ID |
| `entries[].userName` | string | Display name (fallback: email → "Unknown") |
| `entries[].totalXp` | integer | Total XP earned in time period |
| `entries[].level` | integer | Computed level (formula: `floor((1 + sqrt(1 + totalXp/25)) / 2)`) |
| `entries[].currentStreak` | integer | Current consecutive days with activity |
| `entries[].longestStreak` | integer | Best streak achieved |
| `entries[].avatarUrl` | string \| null | Avatar endpoint URL (null if no avatar) |
| `entries[].rankChange` | integer | Change vs previous period (+3 = up 3 ranks, -2 = down 2 ranks, 0 = no change) |
| `currentUserEntry` | LeaderboardEntry \| null | Authenticated user's entry (null if not logged in) |

**Time Period Calculations:**

| TimeFrame | Period | Comparison Period |
|-----------|--------|-------------------|
| `Weekly` | Last 7 days (Mon-Sun) | Previous week (8-14 days ago) |
| `Monthly` | Last 30 days | Previous 30 days (31-60 days ago) |
| `AllTime` | All time | N/A (rankChange always 0) |

**Example:**
```bash
# Get weekly leaderboard
curl http://localhost:8080/api/leaderboard?timeFrame=Weekly

# Get all-time leaderboard (no auth)
curl http://localhost:8080/api/leaderboard?timeFrame=AllTime

# Get monthly leaderboard with auth (includes current user)
curl http://localhost:8080/api/leaderboard?timeFrame=Monthly \
  -b "AuthToken=eyJhbGciOiJIUzI1NiIs..."
```

---

### GET /api/user/xp

Get current authenticated user's XP and progress.

**Authentication:** Required
**Roles:** Any authenticated user

**Request:**
```http
GET /api/user/xp HTTP/1.1
Host: localhost:8080
Cookie: AuthToken=eyJhbGciOiJIUzI1NiIs...
```

**Success Response (200 OK):**
```json
{
  "userId": "currentUser",
  "totalXp": 1250,
  "level": 6,
  "xpForCurrentLevel": 216,
  "xpForNextLevel": 289,
  "percentToNextLevel": 74,
  "currentStreak": 7,
  "longestStreak": 14,
  "lastActivityDate": "2026-03-15T14:30:00Z"
}
```

**Response Fields:**
| Field | Type | Description |
|-------|------|-------------|
| `userId` | string (GUID) | User ID |
| `totalXp` | integer | Total XP earned |
| `level` | integer | Current level |
| `xpForCurrentLevel` | integer | XP required to reach current level |
| `xpForNextLevel` | integer | XP required to reach next level |
| `percentToNextLevel` | integer | Progress toward next level (0-100) |
| `currentStreak` | integer | Current consecutive days with activity |
| `longestStreak` | integer | Best streak achieved |
| `lastActivityDate` | string (ISO 8601) | Most recent exercise completion |

**Error Responses:**

**401 Unauthorized:**
```json
{
  "message": "You are not authorized to perform this action.",
  "statusCode": 401,
  "detail": null
}
```

---

### GET /api/user/{userId}/xp

Get any user's XP and progress (public endpoint).

**Authentication:** Not required

**URL Parameters:**
| Parameter | Type | Description |
|-----------|------|-------------|
| `userId` | string (GUID) | User ID |

**Request:**
```http
GET /api/user/user1/xp HTTP/1.1
Host: localhost:8080
```

**Success Response (200 OK):**
```json
{
  "userId": "user1",
  "totalXp": 1250,
  "level": 6,
  "currentStreak": 7,
  "longestStreak": 14
}
```

**Note:** Public endpoint returns less data than authenticated `/xp` endpoint (no level progress %, no last activity date).

**Error Responses:**

**404 Not Found:**
```json
{
  "message": "User 'user999' not found",
  "statusCode": 404,
  "detail": null
}
```

---

## Gamification Mechanics

### Level Calculation

Levels are computed dynamically from total XP using a square root formula:

```
level = floor((1 + sqrt(1 + totalXp / 25)) / 2)
```

**Level progression table:**

| Level | XP Required | XP Range |
|-------|-------------|----------|
| 1 | 0 | 0-24 |
| 2 | 25 | 25-99 |
| 3 | 100 | 100-224 |
| 4 | 225 | 225-399 |
| 5 | 400 | 400-624 |
| 6 | 625 | 625-899 |
| 7 | 900 | 900-1224 |
| 8 | 1225 | 1225-1599 |
| 9 | 1600 | 1600-2024 |
| 10 | 2025 | 2025+ |

Formula ensures:
- Early levels require less XP (fast initial progress)
- Higher levels require more XP (sustainable long-term growth)
- No level cap (infinite progression)

### Streak Calculation

Streaks count consecutive days with at least one correct exercise submission.

**Rules:**
1. **Starts at 1** when user completes first exercise on a new day
2. **Increments by 1** when user completes exercise on consecutive day
3. **Resets to 0** if user skips 2+ days (1-day grace period)
4. **Grace period**: Yesterday's activity still counts toward streak
5. **Timezone**: All dates compared in UTC

**Example:**
```
March 10: Completes exercise → streak = 1
March 11: Completes exercise → streak = 2
March 12: Completes exercise → streak = 3
March 13: No activity → grace period (streak still 3)
March 14: Completes exercise → streak = 4
March 15: No activity
March 16: No activity → streak = 0 (reset)
March 17: Completes exercise → streak = 1 (new streak)
```

**Longest streak** tracks the best streak ever achieved (never resets).

### XP Earning

XP is earned by correctly answering exercises:

1. **First correct answer**: Award full `pointValue` (default 10 XP)
2. **Subsequent submissions**: Award 0 XP (idempotent - prevents XP farming)
3. **Wrong answers**: Award 0 XP (can retry infinitely)
4. **Admin bypass**: Admins earn XP even on locked exercises (for testing)

**XP caching:**
- `User.TotalPointsEarned` is materialized (incremented on first correct submission)
- Avoids expensive SUM query on leaderboard requests
- Source of truth: `UserExerciseProgress.PointsEarned` (summed for verification)

### Rank Change Calculation

Rank change compares current period vs equivalent previous period:

**Weekly example:**
```
Current week (Mar 9-15): User has 500 XP → rank 10
Previous week (Mar 2-8): User had 300 XP → rank 15
Rank change: 15 - 10 = +5 (up 5 ranks)
```

**Implementation:**
- No snapshot tables (stateless comparison)
- Query last period's data on-demand
- Compare ranks: `previousRank - currentRank`
- Positive = moved up, negative = moved down, 0 = no change

**All-time:**
- Always `rankChange: 0` (no previous period to compare)

---

## Leaderboard UI Integration

### Typical Frontend Flow

```
1. User opens leaderboard page
   ↓
2. Frontend: GET /api/leaderboard?timeFrame=Weekly
   ↓
3. If user logged in, response includes currentUserEntry
   ↓
4. Frontend renders:
   - Top 100 users in entries[]
   - Current user's rank highlighted (even if rank > 100)
   ↓
5. User switches to "Monthly" tab
   ↓
6. Frontend: GET /api/leaderboard?timeFrame=Monthly
   ↓
7. Re-render with monthly data
```

### Avatar Handling

Leaderboard response includes `avatarUrl` but NOT avatar bytes:

```javascript
// ✅ CORRECT - Use avatarUrl in img src
<img src={entry.avatarUrl} alt={entry.userName} />

// ❌ WRONG - Don't fetch avatar bytes separately
const avatar = await fetch(`/api/user/${entry.userId}/avatar`);
```

**Performance:**
- Leaderboard query batch-checks `UserAvatars` table for existence
- Only returns URL if avatar exists (null otherwise)
- Browser caches avatar images (24h Cache-Control header)

---

## Common Scenarios

### Scenario: First-Time User

```
User signs up → 0 XP, Level 1, Streak 0
  ↓
Completes first exercise → 10 XP, Level 1, Streak 1
  ↓
Completes 5 more exercises → 60 XP, Level 2, Streak 1
  ↓
Next day, completes 1 exercise → 70 XP, Level 2, Streak 2
```

### Scenario: Weekly Competition

```
Monday: Alice (500 XP) is rank 1, Bob (400 XP) is rank 2
  ↓
Tuesday: Bob earns 150 XP → Bob (550 XP) rank 1, Alice (500 XP) rank 2
  ↓
Wednesday: Alice earns 200 XP → Alice (700 XP) rank 1, Bob (550 XP) rank 2
  ↓
Next Monday (new week): Both start at 0 XP for weekly board
```

### Scenario: Streak Maintenance

```
Day 1-5: User completes at least 1 exercise daily → streak = 5
  ↓
Day 6: User forgets (no activity) → streak still 5 (grace period)
  ↓
Day 7: User completes exercise → streak = 6 (grace period saved it!)
  ↓
Day 8-9: User skips both days → streak = 0 (reset)
```

### Scenario: Level Up

```
User at 95 XP (Level 2) → needs 5 more XP for Level 3
  ↓
Completes FillInBlank (10 XP) → 105 XP
  ↓
Level recalculated: floor((1 + sqrt(1 + 105/25)) / 2) = 3
  ↓
Frontend shows "Level Up!" animation
```

---

## Performance Considerations

### Leaderboard Query Optimization

```sql
-- EF Core generates optimized query with:
-- 1. JOIN Users + UserExerciseProgress
-- 2. GROUP BY user fields
-- 3. SUM(PointsEarned) WHERE CompletedAt in time window
-- 4. ORDER BY TotalXp DESC
-- 5. LIMIT 100

-- Indexes:
-- - UserExerciseProgress(UserId, CompletedAt) for time filtering
-- - Users(Id) for JOIN
```

**Warning:**
- Weekly/Monthly queries scan `UserExerciseProgress` with date filter
- Can be slow on large datasets (100k+ progress rows)
- Consider periodic materialized view refresh in production

### Avatar Existence Check

Leaderboard query batch-checks `UserAvatars` without loading bytes:

```csharp
var userIds = entries.Select(e => e.UserId).ToList();
var avatarExistence = await _context.UserAvatars
    .Where(a => userIds.Contains(a.UserId))
    .Select(a => a.UserId)
    .ToListAsync();

// Set avatarUrl only if avatar exists
foreach (var entry in entries)
{
    entry.AvatarUrl = avatarExistence.Contains(entry.UserId)
        ? $"/api/user/{entry.UserId}/avatar"
        : null;
}
```

Avoids loading varbinary(max) data for 100 users.

---

## Testing

### Seed Test Data

```sql
-- Create user progress for testing
INSERT INTO UserExerciseProgress (UserId, ExerciseId, IsCompleted, PointsEarned, CompletedAt)
VALUES
  ('user1', 'ex1', 1, 10, '2026-03-10'),
  ('user1', 'ex2', 1, 10, '2026-03-11'),
  ('user1', 'ex3', 1, 10, '2026-03-12'),
  ('user2', 'ex1', 1, 10, '2026-03-13');

-- Update materialized XP
UPDATE Users SET TotalPointsEarned = 30 WHERE Id = 'user1';
UPDATE Users SET TotalPointsEarned = 10 WHERE Id = 'user2';
```

### Verify Streak Calculation

```bash
# Today: 2026-03-15
# User completed exercises on: 3/9, 3/10, 3/11, 3/13, 3/14

# Expected current streak: 3 days (3/13, 3/14, 3/15 grace)
# Expected longest streak: 3 days

curl http://localhost:8080/api/user/user1/xp
# Should return: currentStreak: 3, longestStreak: 3
```

### Verify Rank Change

```bash
# Create progress in two weeks:
# Week 1 (Mar 2-8): User1=100XP (rank 2), User2=200XP (rank 1)
# Week 2 (Mar 9-15): User1=300XP (rank 1), User2=150XP (rank 2)

curl 'http://localhost:8080/api/leaderboard?timeFrame=Weekly'
# User1: rank=1, rankChange=+1 (was rank 2)
# User2: rank=2, rankChange=-1 (was rank 1)
```
