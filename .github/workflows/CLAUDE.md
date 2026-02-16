# CI/CD & Deployment CLAUDE.md

GitHub Actions CI/CD pipeline with Docker Compose and Hetzner deployment.

## Docker Commands

From repository root:

```bash
# Start all services locally (development)
docker compose up

# Start in detached mode
docker compose up -d

# Build images before starting
docker compose up --build

# Stop all services
docker compose down

# View container logs
docker compose logs

# View specific service logs
docker compose logs backend
docker compose logs frontend
docker compose logs db

# Build for production (uses docker-compose.prod.yml)
TAG=latest docker compose -f docker-compose.prod.yml up --build
```

## CI/CD Pipeline

Four-stage pipeline in `.github/workflows/`:

1. **build-and-push-docker.yml** — Builds and pushes Docker images to GHCR
2. **development.yml** — Orchestrates the full CI/CD workflow
3. **continuous-delivery.yml** — Deploys to Hetzner production server
4. **codeql.yml** — Security scanning with GitHub Advanced Security (runs on push/PR/schedule)

### Deployment Flow

- Triggered on push to `master` or `fix/refactor`
- Builds both frontend and backend Docker images
- Pushes to GitHub Container Registry (ghcr.io)
- SSHs into Hetzner server and runs `scripts/deploy.sh`

### Deployment Script (`scripts/deploy.sh`)

- Loads environment variables from `/tmp/.deploy.env` (passed by CD workflow)
- Authenticates to GHCR using GitHub token
- Pulls latest images, stops old containers, starts new ones
- **Security**: Masks IPv4 addresses in logs via `mask_ips()` function to prevent infrastructure details from leaking
- Logs to `/var/log/lexiq/deployment/` with GitHub Actions annotations
- Exit codes: 1 (system/file error), 3 (auth/pull failed), 4 (container start failed)

## Testing Deployment Locally

1. Ensure secrets files exist:
   - `backend/Database/password.txt`   # DB password (Docker secret: `db_password`)
   - `backend/.env`                    # Backend env vars (Docker secret: `backend_env`)
2. Run: `docker compose up --build`
3. Access frontend at http://localhost:4200
4. Access backend API at http://localhost:8080
5. Access Swagger docs at http://localhost:8080/swagger

## Infrastructure Details

### Docker Services

- **db**: SQL Server 2022 with health checks
- **backend**: ASP.NET Core 10.0 API (port 8080)
- **frontend**: Angular 21 + nginx (port 4200)

### Health Checks

- Backend health: `curl http://localhost:8080/health`
- Frontend health: `curl http://localhost:4200` (should return HTML)
- Database health: `docker compose exec db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P <password> -Q "SELECT 1"`

### Docker Secrets

- `db_password` — from `backend/Database/password.txt`
- `backend_env` — from `backend/.env`

Backend loads secrets from `/run/secrets/backend_env` in production.

### Production Deployment

- Docker health checks configured for all services
- Production deployment uses nginx in frontend container
- Images pushed to GitHub Container Registry (ghcr.io)
- SSH deployment to Hetzner server via `scripts/deploy.sh`
- Production HTTPS auto-provisioned via **LettuceEncrypt** (Let's Encrypt)

## Known Limitations

- The `pull-and-test` CI job does not actually run tests — it only authenticates to GHCR
- `continuous-delivery` job references `@feature/ci-cd` — may need updating to match current branch naming

## Common Debugging Scenarios

### Docker Container Issues

**Container fails to start:**
1. Check container logs: `docker compose logs <service-name>`
2. Verify secrets files exist: `backend/Database/password.txt`, `backend/.env`
3. Check port conflicts: `sudo lsof -i :8080` (backend), `sudo lsof -i :4200` (frontend)
4. Ensure database is ready: Backend retries 10 times (3s delay) waiting for SQL Server

**Health check failures:**
- Backend health: `curl http://localhost:8080/health`
- Frontend health: `curl http://localhost:4200` (should return HTML)
- Database health: `docker compose exec db /opt/mssql-tools/bin/sqlcmd -S localhost -U sa -P <password> -Q "SELECT 1"`

**Volume/permission issues:**
- SQL Server data: Ensure `~/mssql-data` directory has correct permissions
- Upload directory: Check `backend/static/uploads` is writable by container user
- Log directory: Verify `/var/log/lexiq` exists on production server
