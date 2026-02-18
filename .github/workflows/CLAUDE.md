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

Five workflows in `.github/workflows/`:

1. **build-and-push-docker.yml** — Builds and pushes Docker images to GHCR
2. **development.yml** — Orchestrates the full CI/CD workflow
3. **continuous-delivery.yml** — Deploys to Hetzner production server
4. **codeql.yml** — Security scanning with GitHub Advanced Security (runs on push/PR/schedule)
5. **infrastructure-update.yml** — Weekly OS + infrastructure image updates (Sundays 02:00 UTC)

### Deployment Flow

- Triggered on push to `master` or `fix/refactor`
- Builds both frontend and backend Docker images
- Pushes to GitHub Container Registry (ghcr.io)
- SSHs into Hetzner server and runs `scripts/deploy.sh`

### Deployment Script (`scripts/deploy.sh`)

- Loads environment variables from `/tmp/.deploy.env` (passed by CD workflow)
- Authenticates to GHCR using GitHub token
- Pulls latest images then does an **in-place `docker compose up -d --wait`** — no `docker compose down`. This keeps the DB container running across deploys, avoiding SQL Server's 30s cold-start and the full health-check cascade (db → backend → frontend) on every push.
- **Do NOT add `apt update`/`apt upgrade`** to this script. OS package management has no place in a container deployment and was historically the biggest time sink (60-120s/run). Schedule host updates out-of-band.
- **Security**: Masks IPv4 addresses in logs via `mask_ips()` function to prevent infrastructure details from leaking
- Logs to `/var/log/lexiq/deployment/` with GitHub Actions annotations
- Exit codes: 1 (file not found), 3 (auth/pull failed), 4 (container start failed)

### `continuous-delivery.yml` SSH Efficiency

The CD job minimises SSH handshakes to 3 per run:
1. **SCP** — transfers `scripts/deploy.sh`, `scripts/verify-deployment.sh`, and `docker-compose.prod.yml` in one connection to `/tmp/deploy_assets_<run_id>/`
2. **SSH deploy+verify** — runs `deploy.sh` then `verify-deployment.sh` in a single connection; the compose file is `cp`'d to the production directory inside this step
3. **SSH cleanup** (`always`) — removes temp assets and prunes dangling images

### `build-and-push-docker.yml` Layer Cache

Both build jobs use `docker/setup-buildx-action` (required for BuildKit) and `cache-from/cache-to` with `type=gha`:
- **`scope=frontend`** and **`scope=backend`** keep the two caches isolated in the GHA store
- On routine source-only pushes, `npm ci` and `dotnet restore` layers are served from cache — the most expensive steps are skipped entirely
- When `no-cache: true` triggers (Dockerfile or dependency manifest changed), the build runs from scratch and then refreshes the cache for subsequent runs
- **Do NOT remove `setup-buildx-action`** — without it the GHA cache driver is unavailable and `cache-from/cache-to` silently does nothing

### `infrastructure-update.yml`

Runs every Sunday at 02:00 UTC (also `workflow_dispatch`). Two SSH steps:
1. **OS update** — `apt-get update && apt-get upgrade -y` on the host
2. **Image update** — `docker compose pull db certbot` then `docker compose up -d --wait db certbot` (no-op if images unchanged) + `docker image prune -f`

App images (`backend`, `frontend`) are intentionally excluded — those are managed by the main CD pipeline.

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

- **db**: SQL Server 2022 with health checks (`mcr.microsoft.com/mssql/server:2022-latest`)
- **backend**: ASP.NET Core 10.0 API (port 8080) — Alpine-based image (`aspnet:10.0-alpine`, ~120 MB runtime)
- **frontend**: Angular 21 + nginx (port 4200) — Alpine-based image (`nginx-unprivileged:stable-alpine`)
- **certbot**: Let's Encrypt renewal sidecar

### Backend Alpine Image Notes

The backend uses `dotnet/sdk:10.0-alpine` (build) and `dotnet/aspnet:10.0-alpine` (runtime). Alpine uses musl libc which does not bundle ICU. Two things are required for correct behaviour with Bulgarian and Italian text:
- `apk add icu-libs` installed in the final image
- `ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false` set in the Dockerfile

**Do NOT remove either of these.** Without `icu-libs`, .NET string operations on non-ASCII characters silently fall back to invariant culture and produce incorrect sort/compare results.

### Health Checks

- Backend health: `wget -q -O /dev/null -T 10 http://localhost:8080/health`
- Frontend health: `wget -q -O /dev/null -T 10 http://localhost:80/`
- Database health: `docker compose exec db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P <password> -Q "SELECT 1" -C`

**BusyBox wget gotcha**: Alpine images use BusyBox wget, NOT GNU wget. Only these flags work: `-q`, `-O`, `-T`, `-c`, `-S`, `-P`, `-U`, `-Y`. GNU-only flags (`--spider`, `--no-verbose`, `--tries`) silently fail with exit 1, making containers permanently unhealthy.

**Health check output not in logs**: Docker health checks run in a separate exec — output does NOT appear in `docker compose logs`. Use `docker inspect --format='{{json .State.Health}}' <container>` to see health check results and error messages.

### Docker Secrets

- `db_password` — from `backend/Database/password.txt`
- `backend_env` — from `backend/.env`

Backend loads secrets from `/run/secrets/backend_env` in production.

### Production Deployment

- Docker health checks configured for all services
- Production deployment uses nginx in frontend container
- Images pushed to GitHub Container Registry (ghcr.io)
- SSH deployment to Hetzner server via `scripts/deploy.sh`
- Production HTTPS terminated at **nginx** (certbot manages Let's Encrypt certs)

### SSL / Let's Encrypt Bootstrap

- **Named volumes vs host paths**: `letsencrypt-certs` is a Docker named volume — unrelated to the host's `/etc/letsencrypt/`. Never write SSL files to host paths; always use `docker compose run` so writes land in the correct volume.
- **`certbot --webroot` never writes `options-ssl-nginx.conf`** — only the `--nginx` plugin does. `init-letsencrypt.sh` must explicitly download this file into the volume via `docker compose run certbot`.
- **HTTP-first bootstrap**: On fresh deploy, `docker-entrypoint.sh` detects missing certs and starts nginx with `nginx.acme-only.conf` (HTTP-only, port 80, serves ACME challenges). After `init-letsencrypt.sh` issues certs it switches to `nginx.prod.conf` via `nginx -s reload` — no container restart needed.
- **`init-letsencrypt.sh` is a one-time manual script** — run once on a new server. Subsequent CD deployments start HTTPS directly because certs persist in the named volume across `docker compose down/up`.
- **Staging mode**: `STAGING=1 scripts/init-letsencrypt.sh` to test cert issuance without hitting Let's Encrypt rate limits.
- **CRITICAL — separate certs per domain group**: `init-letsencrypt.sh` must issue **two separate** certbot certs:
  - Frontend: `-d lexiqlanguage.eu -d www.lexiqlanguage.eu` → stored at `live/lexiqlanguage.eu/`
  - API: `-d api.lexiqlanguage.eu` alone → stored at `live/api.lexiqlanguage.eu/`
  Running all three in one command creates a SAN cert under `live/lexiqlanguage.eu/` only. `live/api.lexiqlanguage.eu/` never gets created, so `docker-entrypoint.sh` falls back to acme-only mode and nginx.prod.conf fails to reload.

## Known Limitations

- The `pull-and-test` CI job does not actually run tests — it only authenticates to GHCR
- `continuous-delivery` job in `development.yml` references `@fix/refactor` — update the `uses:` pin when the main branch changes

## Common Debugging Scenarios

### Docker Container Issues

**Container fails to start:**
1. Check container logs: `docker compose logs <service-name>`
2. Verify secrets files exist: `backend/Database/password.txt`, `backend/.env`
3. Check port conflicts: `sudo lsof -i :8080` (backend), `sudo lsof -i :4200` (frontend)
4. Ensure database is ready: Backend retries 10 times (3s delay) waiting for SQL Server

**Health check failures:**
- Backend health: `wget -q -O /dev/null -T 10 http://localhost:8080/health`
- Frontend health: `wget -q -O /dev/null -T 10 http://localhost:80/`
- Database health: `docker compose exec db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P <password> -Q "SELECT 1" -C`
- **Debug output**: `docker inspect --format='{{json .State.Health}}' <container>` (health check output is NOT in `docker compose logs`)

**Volume/permission issues:**
- SQL Server data: Ensure `~/mssql-data` directory has correct permissions
- Upload directory: Check `backend/static/uploads` is writable by container user
- Log directory: Verify `/var/log/lexiq` exists on production server
