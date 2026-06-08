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
docker compose logs <service>            # backend | frontend | db | certbot

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
| `infrastructure-update.yml` | Weekly OS + image refresh — Sundays 02:00 UTC |

All support `workflow_dispatch`. Trigger via UI or:

```bash
gh workflow run infrastructure-update.yml --ref master
gh run list --workflow=infrastructure-update.yml
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

## SSH efficiency in CD

Three connections per run, no more:

1. **SCP** — `scripts/deploy.sh`, `scripts/verify-deployment.sh`, `docker-compose.prod.yml` to `/tmp/deploy_assets_<run_id>/`.
2. **SSH deploy + verify** — runs both scripts in one connection; compose file `cp`'d into the production directory inside the same step.
3. **SSH cleanup** (`always`) — removes temp assets, prunes dangling images.

## Build cache

`docker/setup-buildx-action` is **required** — without it the GHA cache driver silently does nothing. `cache-from` / `cache-to` use `type=gha` with `scope=frontend` and `scope=backend` (isolated). BuildKit auto-invalidates the install layer when `package.json` / `package-lock.json` / `*.csproj` change. `no-cache` is `false` everywhere except `infrastructure-update.yml`.

## `infrastructure-update.yml`

Sundays 02:00 UTC (also `workflow_dispatch`). Three sequential jobs:

1. **`update-infrastructure`** — `apt-get upgrade` + `docker compose pull db certbot` + recreate + prune.
2. **`rebuild-app-images`** — calls `build-and-push-docker.yml` with `no-cache: true` to refresh base images (`node:*-alpine`, `nginx-unprivileged`, `dotnet/aspnet`).
3. **`deploy-fresh-images`** — calls `continuous-delivery.yml`.

Only place `no-cache: true` is used. Base-image security patches land here weekly, not on every push.

## Services

| Service | Image | Notes |
|---------|-------|-------|
| `db` | `mssql/server:2022-latest` | Health-checked |
| `backend` | `aspnet:10.0-alpine` (~120 MB) | Plain HTTP :8080 internally |
| `frontend` | `nginx-unprivileged:stable-alpine` | TLS terminator, runs as non-root |
| `certbot` | LE renewal sidecar | One-shot permission fix on startup |

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

## Production HTTPS / Let's Encrypt

- `letsencrypt-certs` is a Docker named volume — unrelated to host `/etc/letsencrypt/`. Always write via `docker compose run`.
- `certbot --webroot` does NOT write `options-ssl-nginx.conf` (only `--nginx` plugin does). `init-letsencrypt.sh` downloads it explicitly into the volume.
- HTTP-first bootstrap: missing certs → frontend `docker-entrypoint.sh` starts nginx with `nginx.acme-only.conf` (HTTP-only, ACME challenges). After `init-letsencrypt.sh` issues certs, switches to `nginx.prod.conf` via `nginx -s reload` — no container restart.
- `init-letsencrypt.sh` is **one-time**, manual. Subsequent CD runs use HTTPS directly because certs persist in the named volume across `down/up`.
- Re-running burns LE quota (5 issuances per domain per 7 days). Don't.
- Staging mode: `STAGING=1 scripts/init-letsencrypt.sh`.
- **Two separate cert issuances** are mandatory:
  - Frontend: `-d lexiqlanguage.eu -d www.lexiqlanguage.eu` → `live/lexiqlanguage.eu/`
  - API: `-d api.lexiqlanguage.eu` (alone) → `live/api.lexiqlanguage.eu/`
  Combining all three into one command creates a single SAN cert under `live/lexiqlanguage.eu/`; the API cert directory is never created and `nginx.prod.conf` reload fails.

### Compose-file naming

- CI copies `docker-compose.prod.yml` → `docker-compose.yml` on the server (`continuous-delivery.yml` line 67).
- All server scripts (`deploy.sh`, `init-letsencrypt.sh`) reference `docker-compose.yml`, NOT `docker-compose.prod.yml`.
- `init-letsencrypt.sh` resolves its own dir: `SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"`.

### Cert permissions for nginx-unprivileged

- `nginxinc/nginx-unprivileged` runs as non-root.
- Certbot creates `archive/` with `0700` — nginx can't follow `live/` → `archive/` symlinks.
- `init-letsencrypt.sh` step 3.5 `chmod 755` on archive dirs and `644` on cert files after issuance.
- `infrastructure-update.yml` renewal uses `--deploy-hook` to re-apply permissions after each renewal.
- Symptom of breakage: `BIO_new_file() failed (Permission denied)` on `nginx -s reload`.

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
