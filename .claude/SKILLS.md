# Skills & Tool Usage

Guidelines for when and how to use specific MCP tools, agents, and workflows in the Lexiq project.

## MCP Tools Usage

### Context7 (Documentation Lookup)

**When to use:**
- Need up-to-date library/framework documentation
- Checking API changes in ASP.NET Core, Entity Framework, Angular
- Finding best practices for third-party packages

**How to use:**
1. Call `resolve-library-id` with library name first
2. Then call `query-docs` with the returned library ID
3. Limit to 3 calls per question maximum

**Example scenarios:**
- "How to configure JWT in ASP.NET Core 10.0?" → Use Context7
- "Angular 21 standalone component patterns" → Use Context7
- "Entity Framework Core eager loading best practices" → Use Context7

### Sequential Thinking

**When to use:**
- Complex architectural decisions
- Multi-step problem solving with uncertainties
- Debugging issues requiring hypothesis testing
- Planning implementation strategies

**When NOT to use:**
- Simple, straightforward tasks
- Direct code changes with clear requirements

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

### Plan Agent

**Use for:**
- Multi-file feature implementation
- Architectural changes
- Tasks with multiple valid approaches
- When EnterPlanMode is appropriate but you need research first

**Don't use for:**
- Simple bug fixes
- Single-file changes
- Research-only tasks (use Explore instead)

### Feature-Dev Agents

**code-explorer**: Trace execution paths, understand architecture
**code-reviewer**: Review for bugs, security issues, quality
**code-architect**: Design feature implementations with blueprints

**When to use:**
- Major feature development
- Security audits
- Architecture redesign

## Task Workflows

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

4. **Update ENTITIES_DOCUMENTATION.md** if schema changed

5. **Commit**:
   ```
   Add migration for <feature>

   - Create <TableName> table with <columns>
   - Add relationship between <Entity1> and <Entity2>
   ```

### Deployment Workflow

1. **Verify changes locally**: `docker compose up --build`

2. **Run tests**: Backend tests + Frontend tests

3. **Commit changes**: Follow commit message format

4. **Push to branch**: `git push origin <branch-name>`

5. **CI/CD runs automatically**: Monitor GitHub Actions

6. **Create PR if ready**: Target `master` branch

### Adding New API Endpoint

1. **Create/update DTO** in `backend/DTOs/`

2. **Update service** in `backend/Services/`

3. **Add controller action** in `backend/Controllers/`

4. **Add authorization** if needed (`[Authorize]`, `[AllowAnonymous]`)

5. **Test endpoint**: Use Swagger at `http://localhost:8080/swagger`

6. **Update `backend/CLAUDE.md`** API endpoints section

7. **Commit**:
   ```
   Add endpoint for <feature>

   - POST /api/<resource>/<action> - <description>
   - Add <DtoName> DTO with validation
   - Implement <ServiceMethod> in <ServiceName>
   ```

### Documentation Maintenance

**After completing a chain of logical changes**, use the claude-md-management skill to update documentation:

```bash
/claude-md-improver
```

**This will:**
- Audit CLAUDE.md and related files for quality
- Update with learnings from the current session
- Ensure documentation reflects recent changes
- Maintain consistency across all CLAUDE.md files

**When to trigger:**
- After major feature implementation
- After significant refactoring
- After fixing bugs that reveal documentation gaps
- After adding new patterns or conventions
- End of development session with multiple commits

## Debugging Playbooks

### 401 Unauthorized Errors

1. **Check JWT cookie exists**:
   - Browser DevTools → Application → Cookies
   - Look for `AuthToken` cookie

2. **Verify JWT claims**:
   - Backend logs: Search for `[JWT] OnTokenValidated: Claims =`
   - Check for `nameidentifier` claim (not `sub`)

3. **Check UserContextMiddleware**:
   - Logs: `🔍 UserContextMiddleware: UserId from JWT =`
   - Verify user exists in database

4. **Quick fix**: Clear cookies and re-login

5. **If persists**: Check CORS configuration and `withCredentials: true`

### Docker Container Failures

1. **Check logs**:
   ```bash
   docker compose logs <service-name>
   docker compose logs --tail=50 <service-name>
   ```

2. **Verify health**:
   ```bash
   docker compose ps
   curl http://localhost:8080/health  # backend
   curl http://localhost:4200         # frontend
   ```

3. **Common issues**:
   - Missing secrets: Check `backend/Database/password.txt`, `backend/.env`
   - Port conflicts: `sudo lsof -i :8080` or `sudo lsof -i :4200`
   - Database not ready: Backend retries 10x, check timing

4. **Reset**:
   ```bash
   docker compose down -v  # removes volumes
   docker compose up --build
   ```

### CI/CD Pipeline Failures

1. **Check workflow logs**: Click red X in GitHub

2. **Common failures**:
   - Docker build: Check Dockerfile syntax
   - Docker push: Verify GHCR permissions
   - SSH/SCP: Check Hetzner secrets in repo settings
   - Deployment script: Check `scripts/deploy.sh` exit codes

3. **Local reproduction**:
   ```bash
   docker compose -f docker-compose.prod.yml build
   ```

4. **Check deployment logs on server**:
   ```bash
   ssh <server>
   tail -100 /var/log/lexiq/deployment/deploy-*.log
   ```

### Migration Errors

1. **Check migration file**: `Database/Migrations/<timestamp>_<Name>.cs`

2. **Common issues**:
   - Conflicting migrations: Delete and recreate
   - Constraint violations: Check existing data
   - Missing dependencies: Ensure related tables exist

3. **Rollback**:
   ```bash
   dotnet ef database update <PreviousMigrationName> --project Database/Backend.Database.csproj
   ```

4. **Remove bad migration**:
   ```bash
   dotnet ef migrations remove --project Database/Backend.Database.csproj
   ```

## Tool Selection Quick Reference

| Task | Preferred Tool/Agent |
|------|---------------------|
| Find specific file/class | Glob, Grep directly |
| Broad codebase exploration | Explore agent (medium) |
| Understand execution flow | feature-dev:code-explorer |
| Security review | feature-dev:code-reviewer |
| Design new feature | Plan agent or feature-dev:code-architect |
| Library documentation | Context7 MCP |
| Complex decision | Sequential-thinking MCP |
| Simple code change | Direct tool use (Edit, Write) |
| Multi-step workflow | Task agent with specific type |
| Update documentation | /claude-md-improver skill |
