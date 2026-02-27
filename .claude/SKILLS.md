# Skills & Tool Usage

Guidelines for when and how to use specific MCP tools, agents, and workflows in the Lexiq project.

---

## Skills (Slash Commands)

### `frontend-dev` — Frontend Design & Angular Work

> **Plugin status**: `frontend-dev` is a plugin skill — install it via Claude Code settings before use. Once installed it is invoked via the Skill tool (`skill: "frontend-dev"`). Until installed, handle frontend tasks directly with Edit/Write/Bash tools guided by `frontend/CLAUDE.md`.

**Invoke for ANY of these once installed:**
- Adding or restructuring Angular components, modules, or routes
- Changing layout, visual design, or SCSS/styling
- Refactoring component architecture (splitting, merging, lifting state)
- Working with Angular signals, forms, animations, or routing
- Implementing new UI features end-to-end (component + service + template)
- Debugging Angular-specific issues (change detection, lifecycle hooks, template errors)
- Updating the Angular design system or shared styles

**Examples that MUST trigger `frontend-dev`:**
```
"Add a progress bar to the lesson viewer"
"Restructure the home page layout"
"Create a modal component for exercise feedback"
"Fix the SCSS animation on the leaderboard cards"
"Refactor the exercise-viewer to use signals"
```

**After completing frontend work:**
1. Commit the component changes
2. Update `frontend/CLAUDE.md` with any new patterns or conventions discovered
3. Commit the doc update separately

---

### `/feature-dev:feature-dev` — New Backend Feature Development

**Invoke with the Skill tool when building anything new on the backend:**
- New API endpoint (controller + service + DTOs)
- New entity or schema change (entity + migration + mapping)
- New service or significant service expansion
- Backend integration with external systems (OAuth, storage, etc.)
- Any work touching 3+ backend files or requiring architectural decisions

**Examples that MUST trigger `feature-dev:feature-dev`:**
```
"Add a notifications system"
"Implement course enrollment with access control"
"Add an achievements/badges entity and endpoint"
"Create a streak snapshot table for performance"
"Build an admin analytics endpoint"
```

**After completing backend feature work:**
1. Commit the feature code
2. If schema changed: update `backend/Database/ENTITIES_DOCUMENTATION.md` and commit
3. Update `backend/CLAUDE.md` with new patterns, endpoints, or conventions
4. Commit the doc update separately

---

### `/claude-md-improver` — Documentation Audit

**Run after completing a chain of logical changes (multiple commits).** See Documentation Maintenance section below.

---

### Playwright — Browser Automation & Live Verification

> **Plugin status**: Playwright is a plugin — install via `/plugin` in Claude Code. Once installed it gives Claude direct browser control (navigate, click, fill, screenshot) during the session.

**Invoke for ANY of these:**
- Verifying a UI feature works end-to-end after implementation (navigate → interact → assert)
- Debugging visual or layout issues by taking screenshots of the live app
- Testing authentication flows (Google OAuth redirect, JWT cookie set, protected route access)
- Reproducing a reported UI bug to confirm root cause before fixing
- Checking that an Angular route guard redirects unauthenticated users correctly
- Validating exercise submission flow (answer → feedback → unlock next exercise)
- Cross-checking API responses against what the UI actually renders

**Examples that SHOULD use Playwright:**
```
"Check that the leaderboard shows the correct user rank after completing an exercise"
"Verify the login redirect works after the auth-guard changes"
"Screenshot the lesson page — the layout looks broken on mobile"
"Confirm the avatar shows up correctly after the binary storage migration"
"Make sure the fill-in-blank exercise unlocks the next one on correct answer"
```

**When NOT to use Playwright:**
- Writing Playwright `.spec.ts` test files (use Edit/Write tools directly)
- Backend-only changes with no UI surface to verify
- Tasks fully verifiable via Swagger or API logs alone

**Typical verification workflow:**
1. Start the app: `docker compose up` or `npm start` + `dotnet run`
2. Use Playwright to navigate to the relevant page
3. Interact and screenshot to confirm behaviour
4. If broken, fix and re-verify in the same session

---

## MCP Tools

### Context7 — Library Documentation Lookup

**When to use:**
- Need up-to-date docs for ASP.NET Core, Entity Framework, Angular, or any third-party package
- Checking API signatures or behaviour that may have changed across versions
- Finding best practices before implementing a pattern

**How to use:**
1. Call `resolve-library-id` with the library name first
2. Call `query-docs` with the returned library ID
3. Maximum 3 calls per question

**Example scenarios:**
- "How to configure JWT in ASP.NET Core 10?" → Context7
- "Angular 21 standalone component patterns" → Context7
- "EF Core eager loading with TPH subtypes" → Context7

---

### Sequential Thinking — Complex Problem Solving

**Use this proactively — don't skip it to save time on hard problems.**

**ALWAYS use for:**
- Debugging non-obvious issues where the root cause is unclear (hypothesis → test → revise cycle)
- Architectural decisions with trade-offs (e.g. "should this be a new entity or a column?")
- Planning a feature that touches multiple layers (entity + service + controller + frontend)
- Any task where the first obvious approach might be wrong
- EF Core query design — especially GroupJoin, TPH eager loading, or translation constraints

**Use for:**
- Multi-step problem solving with uncertainties mid-way
- When you've tried an approach and hit an unexpected blocker
- Evaluating whether to add a new service vs. extend an existing one

**When NOT to use:**
- Simple, well-understood changes with clear requirements
- Single-file edits following an already-established pattern
- Documentation updates

**Rule of thumb**: If the task requires more than 2 decisions, run sequential thinking first before writing code.

---

## Agent Strategies

### Explore Agent

**Use for:**
- Codebase exploration ("find all authentication middleware")
- Pattern discovery ("how does file upload work?")
- Broad searches requiring 3+ queries

**Thoroughness levels:**
- `quick`: Basic searches, single location
- `medium`: Multiple locations, moderate depth
- `very thorough`: Comprehensive analysis

**Example:**
```
Task: "Find all places where JWT claims are used"
→ Use Explore agent with medium thoroughness
```

---

### Plan Agent

**Use for:**
- Multi-file feature implementation where you need to research before planning
- Architectural changes spanning backend + frontend
- Tasks with multiple valid approaches that need comparison

**Don't use for:**
- Simple bug fixes
- Single-file changes
- Research-only tasks (use Explore instead)

---

### Feature-Dev Agents (Subagents)

| Agent | Use for |
|-------|---------|
| `feature-dev:code-explorer` | Trace execution paths, map architecture layers before building |
| `feature-dev:code-reviewer` | Review for bugs, security issues, code quality |
| `feature-dev:code-architect` | Design feature implementation blueprints with specific files/flows |

**When to use:**
- New backend features (see `/feature-dev:feature-dev` skill above — prefer the skill)
- Security audits before merging sensitive code
- Architecture redesign spanning many files
- When you need a blueprint before writing any code

---

## Task Workflows

### New Backend Feature

1. **Invoke** `/feature-dev:feature-dev` skill for guided implementation
2. **Schema changes**: create migration, apply, update `ENTITIES_DOCUMENTATION.md`
3. **Commit code** (feature files only)
4. **Commit docs** (ENTITIES_DOCUMENTATION.md, CLAUDE.md updates — separate commit)

### New Frontend Feature or UI Change

1. **Invoke** `/frontend-dev` skill
2. **Commit component/style changes**
3. **Update** `frontend/CLAUDE.md` if new patterns emerged
4. **Commit docs** separately

### Database Migration Workflow

1. **Create migration**:
   ```bash
   cd backend
   dotnet ef migrations add <DescriptiveName> --project Database/Backend.Database.csproj
   ```
2. **Review generated files**: Check `Database/Migrations/`
3. **Apply migration**:
   ```bash
   dotnet ef database update --project Database/Backend.Database.csproj
   ```
4. **Update `ENTITIES_DOCUMENTATION.md`** to reflect schema change
5. **Commit** migration and entity files, then commit docs separately

### Adding New API Endpoint

1. Create/update DTO in `backend/Dtos/`
2. Update service in `backend/Services/`
3. Add controller action in `backend/Controllers/`
4. Apply `[Authorize(Roles = "...")]` or `[AllowAnonymous]` as needed
5. Test via Swagger at `http://localhost:8080/swagger`
6. Update the API endpoints table in `backend/CLAUDE.md`
7. Commit code, then commit the CLAUDE.md update separately

### Documentation Maintenance

**After completing a chain of multiple commits**, run:
```bash
/claude-md-improver
```

**This will:**
- Audit all CLAUDE.md files for quality and consistency
- Update with learnings from the current session
- Catch gaps between code reality and documented conventions

**When to trigger:**
- After major feature implementation
- After significant refactoring
- After fixing bugs that reveal documentation gaps
- After adding new patterns or conventions
- End of development session with multiple commits

### Deployment Workflow

1. Verify changes locally: `docker compose up --build`
2. Commit changes following [RULES.md](RULES.md)
3. Push to branch: `git push origin <branch-name>`
4. CI/CD runs automatically — monitor GitHub Actions
5. Create PR targeting `master` when ready

---

## Debugging Playbooks

### 401 Unauthorized Errors

1. **Check JWT cookie exists**: Browser DevTools → Application → Cookies → `AuthToken`
2. **Verify JWT claims**: Backend logs — look for `nameidentifier` claim (not `sub`)
3. **Check UserContextMiddleware logs**: `🔍 UserContextMiddleware: UserId from JWT =`
4. **Quick fix**: Clear cookies and re-login
5. **If persists**: Check CORS config and `withCredentials: true` on frontend

### Docker Container Failures

1. **Check logs**:
   ```bash
   docker compose logs <service-name>
   docker compose logs --tail=50 <service-name>
   ```
2. **Verify health**:
   ```bash
   docker compose ps
   curl http://localhost:8080/health
   curl http://localhost:4200
   ```
3. **Common issues**:
   - Missing secrets: Check `backend/Database/password.txt`, `backend/.env`
   - Port conflicts: `sudo lsof -i :8080` or `sudo lsof -i :4200`
   - Database not ready: Backend retries 10x, check timing
4. **Reset**:
   ```bash
   docker compose down -v
   docker compose up --build
   ```

### CI/CD Pipeline Failures

1. **Check workflow logs**: Click red X in GitHub Actions
2. **Common failures**:
   - Docker build: Check Dockerfile syntax
   - Docker push: Verify GHCR permissions
   - SSH/SCP: Check Hetzner secrets in repo settings
   - Deployment script: Check `scripts/deploy.sh` exit codes
3. **Local reproduction**: `docker compose -f docker-compose.prod.yml build`
4. **Server logs**: `tail -100 /var/log/lexiq/deployment/deploy-*.log`

### Migration Errors

1. **Check migration file**: `Database/Migrations/<timestamp>_<Name>.cs`
2. **Common issues**:
   - Conflicting migrations: Delete and recreate
   - Constraint violations: Check existing data
   - Multiple cascade paths: Use `DeleteBehavior.NoAction` on secondary FK
3. **Rollback**: `dotnet ef database update <PreviousMigrationName> --project Database/Backend.Database.csproj`
4. **Remove bad migration**: `dotnet ef migrations remove --project Database/Backend.Database.csproj`

### EF Core LINQ Translation Failures

- **Anonymous types required** for intermediate `Join`/`GroupBy` steps — EF Core maps `new { }` directly to SQL columns
- **Named records fail** in `Join`/`GroupBy` intermediate steps — use them only in terminal `.Select()` before `.ToListAsync()`
- **Rule**: Anonymous `new { }` for joins/groupby → named `private record` for terminal projections only

---

## Tool Selection Quick Reference

| Task | Use |
|------|-----|
| New Angular component / UI feature | `frontend-dev` plugin skill (install first) |
| Restructure frontend layout or design | `frontend-dev` plugin skill (install first) |
| New backend feature (3+ files) | `/feature-dev:feature-dev` skill |
| Backend architecture decision | Sequential Thinking MCP + Plan agent |
| Debugging unclear root cause | Sequential Thinking MCP |
| Find specific file/class/function | Glob, Grep directly |
| Broad codebase exploration | Explore agent (medium) |
| Trace execution flow | `feature-dev:code-explorer` |
| Security review | `feature-dev:code-reviewer` |
| Design feature blueprint | `feature-dev:code-architect` |
| Library / framework docs | Context7 MCP |
| Verify UI works end-to-end after implementation | Playwright plugin |
| Debug visual/layout issues (screenshot live app) | Playwright plugin |
| Reproduce a reported UI bug before fixing | Playwright plugin |
| Simple single-file edit | Edit tool directly |
| Post-session doc audit | `/claude-md-improver` skill |
