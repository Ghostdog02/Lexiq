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
2. **Commit separately** - don't mix unrelated changes
3. **Order matters** - dependencies first

Example grouping:
- Group 1: Documentation updates → Commit
- Group 2: Security fix → Commit
- Group 3: Refactoring → Commit
