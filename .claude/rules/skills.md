# Skills, Tools & Agent Strategies

When and how to use specific skills, MCP tools, agents, and subagents in Lexiq.

> **Debugging playbooks moved to** [`troubleshooting.md`](./troubleshooting.md).
> **Cross-cutting bug patterns in** [`common-gotchas.md`](./common-gotchas.md).

---

## Project Skills (Anthropic Agent Skills)

Located at `.claude/skills/<name>/SKILL.md`. Auto-discovered, triggered on description match.

| Skill | Use for |
|-------|---------|
| `commit-changes` | Analyze the working tree, group changes logically, write commits, push (with confirmation) |
| `create-pr` | Open a PR with the project's strict template; strips Claude attribution |

---

## Plugin Skills

### `frontend-dev`

Install via Claude Code settings, then invoke via the Skill tool.

Trigger for: new Angular components, layout/SCSS changes, signals/forms/animations refactors, design-system updates, Angular-specific debugging. Until the plugin is installed, handle frontend tasks directly with Edit/Write/Bash guided by `frontend/CLAUDE.md`.

### `feature-dev:feature-dev`

Backend feature work touching 3+ files: new endpoint (controller + service + DTOs), new entity / migration / mapping, OAuth or external integration.

### `claude-md-improver`

Run after a chain of multiple commits to audit all CLAUDE.md files for drift.

### Playwright (plugin)

Use **only** for live UI verification: navigate → interact → screenshot. Don't use it to write `.spec.ts` files (use Edit/Write).

---

## MCP Tools

### Context7 — library docs

Up-to-date docs for ASP.NET Core, EF Core, Angular, third-party packages.

1. `resolve-library-id`
2. `query-docs`
3. Max 3 calls per question.

### Sequential Thinking — hard problems

Use proactively for: non-obvious bug diagnosis, architectural trade-offs, multi-layer features, EF Core query design (GroupJoin, TPH eager loading, translation constraints). Skip for simple/obvious changes. Rule of thumb: more than 2 decisions → run it first.

---

## Agent Strategies

| Agent | Use for | Don't use for |
|-------|---------|---------------|
| `Explore` (medium/very thorough) | Broad codebase exploration, pattern discovery, 3+ queries | Single known file or symbol — use `Read` / `grep` |
| `Plan` | Multi-file implementation strategy, architecture comparisons | Simple bug fixes, single-file changes, research-only |
| `feature-dev:code-explorer` | Trace execution paths, map architecture before building | — |
| `feature-dev:code-reviewer` | Bug / security / quality review | — |
| `feature-dev:code-architect` | Blueprint a feature with files & flows | — |
| `test-generator` | xUnit + Testcontainers / Jest / Playwright suites | — |

---

## Task Workflows

### New backend feature

1. Invoke `/feature-dev:feature-dev`.
2. Schema change → migration → update `backend/Database/ENTITIES_DOCUMENTATION.md`.
3. Commit code (feature only).
4. Commit docs separately (ENTITIES_DOCUMENTATION.md, CLAUDE.md updates).

### New frontend feature / UI change

1. Invoke `frontend-dev` plugin (or work directly per `frontend/CLAUDE.md`).
2. Commit component/style changes.
3. Update `frontend/CLAUDE.md` only if a new pattern emerged.
4. Commit docs separately.

### Database migration

```bash
cd backend
dotnet ef migrations add <Name> --project Database/Backend.Database.csproj
dotnet ef database update     --project Database/Backend.Database.csproj
```

Update `ENTITIES_DOCUMENTATION.md`. Commit migration + entity, then docs.

### New API endpoint

1. DTOs in `Dtos/` (record types).
2. Service method in `Services/`.
3. Controller action with `[Authorize(Roles = "...")]` or `[AllowAnonymous]`.
4. Verify via Swagger `http://localhost:8080/swagger`.
5. Update API endpoints table in `backend/CLAUDE.md`.
6. Commit code, then docs.

### Documentation maintenance

After multiple commits, run `/claude-md-improver`.

### Deployment

1. `docker compose up --build` locally.
2. Commit per `rules.md`.
3. `git push origin <branch>`.
4. Monitor GitHub Actions.
5. PR targeting `master`.

---

## Tool selection quick reference

| Task | Use |
|------|-----|
| New Angular component / UI feature | `frontend-dev` plugin |
| Restructure frontend layout | `frontend-dev` plugin |
| Backend feature (3+ files) | `/feature-dev:feature-dev` |
| Backend architecture decision | Sequential Thinking + Plan agent |
| Debug unclear root cause | Sequential Thinking |
| Find specific symbol / file | `Grep` / `Glob` directly |
| Broad codebase exploration | `Explore` agent (medium) |
| Trace execution flow | `feature-dev:code-explorer` |
| Security review | `feature-dev:code-reviewer` |
| Feature blueprint | `feature-dev:code-architect` |
| Library / framework docs | Context7 |
| Verify UI end-to-end after impl | Playwright plugin |
| Reproduce reported UI bug | Playwright plugin |
| Single-file edit | `Edit` directly |
| Generate tests | `test-generator` agent |
| Group changes & commit | `commit-changes` skill |
| Open PR | `create-pr` skill |
| Post-session doc audit | `/claude-md-improver` |
