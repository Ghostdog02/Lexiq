---
name: create-pr
description: Use when the user wants to open a pull request — phrases like "create a PR", "open a pull request", "raise a PR", "submit for review". Determines the correct base branch (asks the user if not obvious — defaults to the repo's default branch), diffs the current branch against it, drafts a PR using Lexiq's strict template (Summary / Why / Changes / Test plan / Risk & rollback / Screenshots), pushes the branch if needed, and runs `gh pr create`. Strips ALL Claude attribution — no `🤖 Generated with Claude Code` footer, no `Co-Authored-By: Claude`, no emoji.
---

# create-pr

Open a pull request with the project's enforced structure. Works against any base branch — `master`, `develop`, a release branch, or another feature branch.

## When to invoke

Trigger phrases: *create a PR*, *open a pull request*, *raise a PR*, *submit for review*, *PR this*, *let's get this reviewed*.

---

## Procedure

### 1. Determine the base branch

Resolution order:

1. **User-specified** — if the user says "PR into `develop`" / "target `release/v2`", use that.
2. **Branch-name convention** — if the current branch is `hotfix/*`, default to whichever production branch the project uses (`master` here). For everything else, fall through.
3. **Repo default** — read it from GitHub:
   ```bash
   gh repo view --json defaultBranchRef -q .defaultBranchRef.name
   ```
4. **Confirm with the user** before proceeding if the resolved base isn't `master`. Show: `"Base: <branch> — OK?"`.

For Lexiq the typical base is `master`, but the skill must not hard-code it.

Pre-check: there must be at least one commit on the current branch ahead of the chosen base. If not, stop and tell the user.

### 2. Gather context (run in parallel)

Replace `<base>` with the resolved base branch from step 1.

```bash
git status
git branch --show-current
git fetch origin <base>
git log origin/<base>..HEAD --oneline
git diff origin/<base>...HEAD --stat
git diff origin/<base>...HEAD
```

Read **every** commit on the branch, not just the latest — the PR summarises the whole branch.

### 3. Confirm branch is pushed

```bash
git status -sb     # check ahead/behind
```

If the local branch isn't on origin or is ahead:

```bash
git push -u origin <branch>     # first push
# or
git push origin <branch>
```

Confirm with the user before pushing if the branch tracks an upstream that has diverged.

### 4. Draft title

- Same rules as commit subjects: imperative, capitalized, ≤70 chars.
- Summarise the **whole branch**, not just the last commit.
- No emoji, no ticket prefix unless the project uses them (Lexiq doesn't).

### 5. Draft body using this exact template

```markdown
## Summary

<2–4 sentences. What this PR does and the problem it solves. Plain prose, no bullets here.>

## Why

<1–3 bullets on motivation: the bug, the limitation, the user-facing need, or the architectural improvement. Link to the issue if one exists.>

- ...
- ...

## Changes

<Bulleted list of meaningful changes grouped by area. Skip trivia like "updated imports".>

- **Backend** — ...
- **Frontend** — ...
- **Database** — ...
- **CI/CD** — ...
- **Docs** — ...

## Test plan

<Concrete, verifiable steps. Mix of automated and manual.>

- [ ] `dotnet test backend/Tests/Backend.Tests.csproj` passes
- [ ] Manual: <flow to exercise the change end-to-end>
- [ ] Manual: <edge case>
- [ ] Production check (post-merge): <what to watch in logs / metrics>

## Risk & rollback

<One short paragraph. What could break, blast radius, how to revert (revert commit / migration rollback / feature flag toggle).>

## Screenshots / recordings

<Only if there are UI changes. Otherwise delete this section entirely. Use HTML <img> tags or markdown image links — never paste base64.>
```

Rules for the body:
- **Omit empty sections** (e.g. "Screenshots" for backend-only PRs). Don't ship a section with only `N/A`.
- **No** `🤖 Generated with [Claude Code]` footer.
- **No** `Co-Authored-By: Claude …`.
- **No** emoji anywhere unless the user explicitly requests them.
- **No** generated banners, signatures, or tool attributions of any kind.
- Don't reference `claude` / `claude-code` / `Anthropic` in the body.

### 6. Show the user the draft

Print the resolved base, the title, and the body. Wait for confirmation before running `gh`.

### 7. Create the PR

```bash
gh pr create \
  --base <base> \
  --head <branch> \
  --title "<title>" \
  --body "$(cat <<'EOF'
## Summary

...

## Why

- ...

## Changes

- ...

## Test plan

- [ ] ...

## Risk & rollback

...
EOF
)"
```

If the user prefers a draft PR: append `--draft`.

### 8. Return the URL

`gh pr create` prints the PR URL on stdout — surface it to the user as the final line of your reply.

---

## Don'ts

- ❌ `🤖 Generated with [Claude Code](https://claude.com/claude-code)` footer
- ❌ `Co-Authored-By: Claude <noreply@anthropic.com>`
- ❌ Any mention of Claude / Claude Code / Anthropic in the title or body
- ❌ Empty placeholder sections ("Screenshots: N/A")
- ❌ Force-pushing to land the PR
- ❌ Hard-coding the base branch — always resolve via user / convention / repo default
- ❌ Auto-merging — leave that to the user
