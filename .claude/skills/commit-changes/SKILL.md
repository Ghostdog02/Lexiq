---
name: commit-changes
description: Use when the user wants to commit pending work — phrases like "commit the changes", "commit and push", "stage everything", "wrap this up". Analyzes the working tree, groups files into logical commits by concern (feature vs docs vs tests vs migrations), drafts concise commit messages (imperative subject ≤72 chars + up to 4 body bullets), creates the commits, and asks for explicit confirmation before pushing to origin. Strips all Claude attribution (no Co-Authored-By, no 🤖 footer).
---

# commit-changes

Group the working tree into clean, logically-separated commits, write tight messages, and (with confirmation) push to origin.

## When to invoke

Trigger when the user signals "we're done coding, package it up": *commit*, *commit and push*, *stage and commit*, *wrap this up*, *let's land this*, *split into commits*, *push these changes*.

Skip if `git status` is clean.

---

## Procedure

### 1. Snapshot the working tree

Run **in parallel**:

```bash
git status
git diff --stat
git diff           # staged + unstaged
git log -10 --oneline
git branch --show-current
```

Goals:
- Know every modified / added / deleted / untracked file.
- See what was actually changed (don't trust filenames alone — open hunks for anything non-obvious).
- Match the project's recent commit-message tone.

If anything looks like a sensitive file (`.env`, `*.pem`, `password.txt`, `credentials.*`), flag it to the user before staging.

### 2. Group files into commits

Apply these rules in order (from `.claude/rules/rules.md`):

1. **Never mix concerns.** Feature code, docs, tests, and migrations go in **separate** commits.
2. **Order by dependency.** Infrastructure / migrations / core fixes first; tests and docs last.
3. **One concern per commit** — if a single concern still spans many files, that's fine; if it spans many *unrelated* files, split further.

Standard groupings:

| Group | Typical contents |
|-------|------------------|
| Core change | Feature code, bug fix, refactor — the actual implementation |
| Migration / schema | EF migration files + entity changes + `ENTITIES_DOCUMENTATION.md` |
| Tests | New / updated test files |
| Docs | `CLAUDE.md`, `.claude/rules/*.md`, READMEs |
| CI/CD | `.github/workflows/*`, `docker-compose*.yml`, `Dockerfile`, `scripts/*` |

For test suites, follow the test-suite split in `.claude/rules/rules.md` (infrastructure → unit → integration → E2E, each its own commit).

Present the proposed grouping as a numbered list and **wait for the user to confirm** before staging. Example:

```
Proposed commits (in order):

1. Refactor exercise types and unify Question/FillInBlank naming
   - backend/Database/Entities/Exercises/*.cs
   - backend/Dtos/ExerciseDto.cs
   - backend/Services/ExerciseService.cs

2. Update ENTITIES_DOCUMENTATION for exercise refactor
   - backend/Database/ENTITIES_DOCUMENTATION.md

3. Document exercise redesign plan
   - .claude/EXERCISE_REDESIGN_PLAN.md

OK to proceed?
```

### 3. Write commit messages

Format (from `.claude/rules/rules.md`):

```
<Imperative subject line, capitalized, ≤72 chars>

- <bullet 1: what + why>
- <bullet 2>
- <bullet 3>
- <bullet 4>
```

Constraints:
- **Subject**: imperative mood ("Add", "Fix", "Refactor"), capitalized, no trailing period, ≤72 chars.
- **Body**: optional, max **4 bullet rows**. Each bullet ≤72 chars. Use `-` markers.
- Bullets explain the *why*, not a file list — reviewers can already see the diff.
- **No** `Co-Authored-By: Claude`. **No** `🤖 Generated with Claude Code`. **No** emoji unless explicitly requested.
- Don't reference the current task / issue number / PR — that belongs in the PR description.
- Match the tone of `git log -10`.

Trivial commits (single-line fixes, typo, version bump) may use a one-line subject only — no body.

### 4. Stage and commit

For each group, stage explicit paths (never `git add -A` / `git add .`):

```bash
git add path/one path/two
git commit -m "$(cat <<'EOF'
Subject line here

- bullet one
- bullet two
EOF
)"
```

If a pre-commit hook fails, **fix the underlying issue and create a new commit** — never `--amend`, never `--no-verify`.

### 5. Verify

```bash
git log -<N> --oneline   # N = number of commits made
git status                # should be clean (or note remaining untracked files)
```

### 6. Push — ALWAYS confirm

Show the user the commit list and ask explicitly:

```
Created N commits on <branch>:

  abc1234  Subject one
  def5678  Subject two

Push to origin/<branch>? (yes/no)
```

Only on explicit `yes`:

```bash
git push origin <branch>          # current branch
# or, first push:
git push -u origin <branch>
```

Refuse force-push on `master` / `main`. For other branches, only force-push when the user explicitly asks AND the branch is solely theirs.

---

## Don'ts

- ❌ `git add -A`, `git add .`, `git add *`
- ❌ `--amend` after a hook failure
- ❌ `--no-verify`, `--no-gpg-sign`
- ❌ `Co-Authored-By: Claude <…>` line
- ❌ `🤖 Generated with [Claude Code]` footer
- ❌ Pushing without confirmation
- ❌ Mixing feature + docs + tests in one commit
- ❌ Committing `.env`, `password.txt`, or other secrets without flagging them first
