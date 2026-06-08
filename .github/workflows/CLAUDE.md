# CI/CD & Deployment CLAUDE.md

GitHub Actions + Docker Compose + Hetzner deployment.

> Cross-cutting bug patterns: [`/.claude/rules/common-gotchas.md`](../../.claude/rules/common-gotchas.md).
> Debugging playbooks: [`/.claude/rules/troubleshooting.md`](../../.claude/rules/troubleshooting.md).

## Local commands (from repo root)

```bash
docker compose up                        # dev
docker compose up --build
docker compose up -d
docker compose down
docker compose logs <service>            # backend | frontend | db

# Production-mode local build
TAG=latest docker compose -f docker-compose.prod.yml up --build
```

Local secrets required:

- `backend/Database/password.txt` — `db_password` Docker secret
- `backend/.env` — `backend_env` Docker secret

## Workflows

| File | Purpose |
|------|---------|
| `build-and-push-docker.yml` | Builds & pushes frontend / backend images to GHCR |
| `test.yml` | Runs backend tests (unit → integration → E2E, fail-fast) |
| `release.yml` | Orchestrates production release: build → verify → deploy |
| `continuous-delivery.yml` | SSHes to Hetzner and runs `scripts/deploy.sh` |
| `pr-validation.yml` | Build checks on PRs |
| `codeql.yml` | GitHub Advanced Security scans (push / PR / schedule) |

All support `workflow_dispatch`. Trigger via UI or:

```bash
gh workflow run release.yml --ref master
gh run list --workflow=release.yml
gh run watch
```

## Deploy flow (push to `master`)

1. **build-and-push-docker** — frontend + backend → GHCR.
2. **pull-and-test** — verifies images exist (pulls both).
3. **test-backend** — sequential: unit (~10–30s) → integration (~2–4min) → E2E (~1–2min). Fail-fast.
4. **continuous-delivery** — SSH → Hetzner → `scripts/deploy.sh`.

`.trx` test artifacts uploaded with 30-day retention.

## `scripts/deploy.sh`

- Reads env from `/tmp/.deploy.env` (passed by CD).
- `docker login` to GHCR with the GitHub token.
- Pulls latest images → **in-place** `docker compose up -d --wait`. **No** `docker compose down` — keeps DB warm, avoids the 30s SQL Server cold-start cascade.
- **Do NOT add `apt update` / `apt upgrade`** — host package management has no place here. It used to cost 60–120s/run.
- `mask_ips()` redacts IPv4 from logs.
- Logs to `/var/log/lexiq/deployment/`.
- Exit codes: `1` file not found · `3` auth/pull failed · `4` container start failed.

## SSH via Cloudflare Tunnel

SSH goes through Cloudflare Access (`cloudflared access ssh`). The CD workflow installs `cloudflared` on the runner, adds a `ProxyCommand` entry to `~/.ssh/config`, and authenticates using a Cloudflare service token (`CF_ACCESS_CLIENT_ID` + `CF_ACCESS_CLIENT_SECRET`). `appleboy/scp-action` and `appleboy/ssh-action` are **not used** — they have no `ProxyCommand` support.

Required GitHub Actions secrets:

| Secret | Purpose |
|--------|---------|
| `HETZNER_PROD_HOST` | Cloudflare Access hostname for SSH (e.g. `ssh.relexiq.com`) |
| `HETZNER_PROD_USERNAME` | Server user |
| `HETZNER_PROD_PRIVATE_SSH_KEY` | Private key (PEM, no passphrase) |
| `CF_ACCESS_CLIENT_ID` | Cloudflare Access service token ID |
| `CF_ACCESS_CLIENT_SECRET` | Cloudflare Access service token secret |

Four steps per run:

1. **Setup** — install cloudflared, start ssh-agent, add key, write SSH config with ProxyCommand.
2. **SCP** — copy `scripts/deploy.sh`, `scripts/verify-deployment.sh`, `docker-compose.prod.yml`, `.deploy.env` to `/tmp/deploy_assets_<run_id>/`.
3. **SSH deploy + verify** — runs both scripts in one connection; compose file `cp`'d into production dir.
4. **SSH cleanup** (`always`) — removes temp assets, prunes dangling images.

## Build cache

`docker/setup-buildx-action` is **required** — without it the GHA cache driver silently does nothing. `cache-from` / `cache-to` use `type=gha` with `scope=frontend` and `scope=backend` (isolated). BuildKit auto-invalidates the install layer when `package.json` / `package-lock.json` / `*.csproj` change. `no-cache` is `false` by default.

## Services

| Service | Image | Notes |
|---------|-------|-------|
| `db` | `mssql/server:2022-latest` | Health-checked |
| `backend` | `aspnet:10.0-alpine` (~120 MB) | Plain HTTP :8080 internally |
| `frontend` | `nginx-unprivileged:stable-alpine` | Plain HTTP :80 → cloudflared on host; runs as non-root |

### Backend Alpine ICU requirement

```dockerfile
RUN apk add icu-libs
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
```

**Do NOT remove either.** Without `icu-libs`, .NET silently falls back to invariant culture for non-ASCII strings — Bulgarian and Italian sort/compare results become incorrect.

### Log suppression (production)

```yaml
Logging__LogLevel__Default: Warning
Logging__LogLevel__Microsoft.AspNetCore: Warning
Logging__LogLevel__Microsoft.EntityFrameworkCore: Warning
```

JSON-file driver, `max-size: "10m"`, `max-file: "3"`. Set a namespace to `Debug` temporarily for troubleshooting.

### Health checks

```bash
# Backend
wget -q -O /dev/null -T 10 http://localhost:8080/health
# Frontend
wget -q -O /dev/null -T 10 http://localhost:80/
# Database
docker compose exec db /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P <pass> -Q "SELECT 1" -C
```

BusyBox `wget` flags only — see [`common-gotchas.md`](../../.claude/rules/common-gotchas.md). Healthcheck output is NOT in `docker compose logs` — use `docker inspect --format='{{json .State.Health}}' <container>`.

### Docker secrets

- `db_password` ← `backend/Database/password.txt`
- `backend_env` ← `backend/.env` (loaded from `/run/secrets/backend_env` in production)

## Production TLS — Cloudflare Tunnel

TLS is terminated by Cloudflare at their edge. The server does **not** need inbound port 80/443 open to the internet.

- `cloudflared` runs as a host service on the Hetzner box, tunnelling to `localhost:3000` (the `frontend` container's host port).
- nginx serves plain HTTP on `:80` inside the container. No certs, no ACME, no certbot.
- Cloudflare forwards `CF-Connecting-IP` with the real client IP and `X-Forwarded-Proto: https`. nginx passes these to the backend.
- Domain: `relexiq.com` / `www.relexiq.com`.

### Compose-file naming

CI copies `docker-compose.prod.yml` → `docker-compose.yml` on the server (`continuous-delivery.yml` line 67). All server scripts reference `docker-compose.yml`, NOT `docker-compose.prod.yml`.

## Backend test pipeline (`test.yml`)

Sequential, fail-fast.

- **Unit** (`Tests/Unit/`) — pure logic, no DB. `--filter "FullyQualifiedName~Unit"`.
- **Integration** (`Tests/Integration/Services/` + `Integration/Controllers/`) — Testcontainers + SQL Server. `--filter "FullyQualifiedName~Integration.Services|FullyQualifiedName~Integration.Controllers"`.
- **E2E** (`Tests/Integration/E2E/`) — `WebApplicationFactory`. `--filter "FullyQualifiedName~Integration.E2E"`.

```bash
gh workflow run test.yml
gh run list --workflow=test.yml
gh run view <run-id>
```

## Maintenance: workflow branch pins

When merging a feature branch back to `master`, update `uses:` references:

```yaml
# release.yml
uses: ./.github/workflows/continuous-delivery.yml@<BRANCH>

# infrastructure-update.yml
uses: ./.github/workflows/build-and-push-docker.yml@<BRANCH>
uses: ./.github/workflows/continuous-delivery.yml@<BRANCH>
```

After merge: change all `@fix/refactor` → `@master`. Stale references either fail CI or deploy outdated workflow logic.
