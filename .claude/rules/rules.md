# Rules & Conventions

Project-specific rules and conventions for Lexiq development.

## Git & Commit Guidelines

### Commit Message Format

**Structure:**
```
<subject line>

[optional body with bullet points]
```

**Requirements:**
- **Subject line**: Imperative mood, concise (max 72 chars), capitalized
  - ✅ "Add path traversal protection to file upload service"
  - ✅ "Fix authentication middleware user loading"
  - ❌ "Added some changes" (vague)
  - ❌ "fixed bug" (not capitalized, vague)

- **Body** (optional): Use bullet points for details
  - Explain WHAT changed and WHY
  - Use `-` for bullet points
  - Keep lines under 72 characters

- **NO Co-Authored-By**: Never add "Co-Authored-By: Claude" line to commits

**Examples:**
```
Consolidate CI/CD workflow steps and fix branch references

- Merge multiple SCP file transfers into a single step (reduced SSH overhead)
- Remove duplicate deployment steps in continuous-delivery.yml
- Fix branch references in development.yml from fix/ci-cd/feature/ci-cd to fix/refactor
- Simplify environment variable setup and script execution
```

```
Add IP address redaction to deployment logs

- Implement mask_ips() function to redact IPv4 addresses from logs
- Apply masking to apt, docker compose, and error output
- Prevents sensitive infrastructure details from leaking in public logs
```

### Branching Strategy

- **`master`**: Main production branch
- **`fix/*`**: Bug fixes and refactoring
- **`feature/*`**: New features
- **`hotfix/*`**: Critical production fixes

### When to Commit

- **Do commit**: After completing logical units of work
  - Single feature implementation
  - Bug fix with tests
  - Refactoring that maintains functionality
  - Documentation updates

- **Don't commit**:
  - Work in progress (unless explicitly requested)
  - Broken or failing code
  - Multiple unrelated changes (split into separate commits)

### Pull Request Guidelines

- **Title**: Same format as commit messages (concise, imperative)
- **Description**:
  ```markdown
  ## Summary
  - Bullet points describing changes

  ## Test plan
  - [ ] Manual testing steps
  - [ ] Automated tests added/updated
  ```
- **Base branch**: Usually `master`
- **Review**: Required before merge

### Commit Grouping

When multiple changes exist:
1. **Group logically** by concern/feature
2. **Commit separately** — don't mix unrelated changes
3. **Order matters** — dependencies first (infrastructure before tests, bug fix before tests that exercise the fix)

Example grouping for a general feature:
- Group 1: Bug fix or core change → Commit
- Group 2: Documentation updates → Commit
- Group 3: Tests → Commit (or split further by category)

#### Test Suite Grouping

When adding a new test suite, split into these commits:

| Commit | Contents |
|--------|----------|
| `Add <Name> test infrastructure` | `.csproj`, `.sln` update, `DatabaseFixture`, builders, seeders, project exclusions in parent `.csproj` |
| `Add <Class> unit tests` | Pure unit test classes (no DB, no fixture) — one commit per class if substantial |
| `Add <Class> integration tests` | DB-backed integration test classes — one commit per test class |
| `Document <bug/pattern> in CLAUDE.md` | Any CLAUDE.md or RULES.md additions — separate from code changes |

**Rationale:** Reviewers can validate infrastructure, unit tests, and integration tests independently. Infrastructure commits are the dependency — they must go first.

Example for LeaderboardService tests:
```
Add Backend.Tests project and test infrastructure     ← csproj, sln, DatabaseFixture, UserBuilder, DbSeeder
Add CalculateLevel unit tests                          ← pure unit, no DB
Add GetStreak integration tests                        ← Testcontainers integration
Add GetLeaderboard integration tests                   ← Testcontainers integration
```

#### Large Test File Grouping

When adding a large test file (>500 lines or >40 tests), split into logical commit groups:

| Commit | Contents |
|--------|----------|
| `Add <Class> test infrastructure` | Test class setup, `IAsyncLifetime`, helper methods, mocks |
| `Add <Class> security tests` | Security-focused tests (path traversal, sanitization, authentication) |
| `Add <Class> validation tests` | Input validation, size limits, type checking |
| `Add <Class> functional tests` | Core functionality, file type tests, success paths |

**Rationale:** Large test files (e.g., FileUploadsServiceTests with 57 tests) are easier to review when split by concern. Each commit is independently verifiable and focused on one aspect of the system under test.

Example for FileUploadsService tests:
```
Add FileUploadsService test infrastructure              ← setup, teardown, helpers, mocks
Add FileUploadsService security tests                   ← path traversal, GUID filenames, sanitization
Add FileUploadsService validation tests                 ← null/empty, size limits, extension validation
Add FileUploadsService file type, URL, and path tests  ← all file types, URL upload, physical path retrieval
```

## Testing

See [`backend/Tests/CLAUDE.md`](../../backend/Tests/CLAUDE.md) for full test project documentation.

### Quick Commands

```bash
# All tests (requires Docker)
cd backend && dotnet test Tests/Backend.Tests.csproj --logger "console;verbosity=normal"

# Single class
dotnet test Tests/Backend.Tests.csproj --filter "FullyQualifiedName~GetLeaderboardTests"

# Unit tests only (no Docker)
dotnet test Tests/Backend.Tests.csproj --filter "FullyQualifiedName~CalculateLevelTests"
```

### Test Conventions

- **Never `UseInMemoryDatabase`** — always Testcontainers (real SQL Server behaviour)
- **xUnit v3**: `IAsyncLifetime` methods return `ValueTask`, not `Task`
- **`IClassFixture<DatabaseFixture>`**: shares the container; `IAsyncLifetime` on the test class reseeds per test
- **`fixture.ExerciseIds`**: always use these for `UserExerciseProgress` rows — FK is enforced on INSERT
- **`UserBuilder`**: always use for creating test users — sets Identity's required normalized fields
