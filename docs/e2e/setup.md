# E2E Testing Setup

## Installation

### 1. Install Playwright

```bash
cd frontend/
npm install -D @playwright/test
npx playwright install chromium
```

### 2. Verify Docker Compose

```bash
cd ..
docker compose up --build

# Verify services are healthy
curl http://localhost:4200  # Frontend
curl http://localhost:8080/api/courses  # Backend
```

## Configuration

### Playwright Config

**File**: `frontend/playwright.config.ts`

```typescript
import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './e2e',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 1 : undefined,
  reporter: 'html',

  use: {
    baseURL: 'http://localhost:4200',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],

  // Docker Compose is already running - just verify it's up
  webServer: {
    command: 'echo "Docker Compose should be running"',
    url: 'http://localhost:4200',
    reuseExistingServer: true,
    timeout: 5000,
  },
});
```

### Test Directory Structure

```
frontend/
├── e2e/
│   ├── fixtures/
│   │   └── test-avatar.png       # 1x1 PNG for avatar upload tests
│   ├── helpers/
│   │   ├── auth.helper.ts        # JWT cookie injection
│   │   └── test-data.helper.ts   # Seed data ID discovery
│   ├── pages/
│   │   ├── base.page.ts          # Base page object
│   │   ├── login.page.ts
│   │   ├── home.page.ts
│   │   ├── exercise.page.ts
│   │   ├── leaderboard.page.ts
│   │   └── profile.page.ts
│   └── tests/
│       ├── auth.spec.ts          # Authentication flows
│       └── lesson-flow.spec.ts   # Complete lesson journey
├── playwright.config.ts
└── package.json
```

### Package.json Scripts

**Add to `frontend/package.json`**:

```json
{
  "scripts": {
    "test:e2e": "playwright test",
    "test:e2e:ui": "playwright test --ui",
    "test:e2e:debug": "playwright test --debug",
    "test:e2e:headed": "playwright test --headed",
    "test:e2e:report": "playwright show-report"
  }
}
```

## Running Tests

### Local Development

```bash
# Ensure Docker Compose is running first
docker compose up --build

# In another terminal
cd frontend/

# Run all tests
npm run test:e2e

# Run specific test file
npx playwright test e2e/tests/auth.spec.ts

# Run in UI mode (interactive)
npm run test:e2e:ui

# Run in headed mode (see browser)
npm run test:e2e:headed

# Debug mode (step through)
npm run test:e2e:debug
```

### View Results

```bash
# Open HTML report
npm run test:e2e:report

# View screenshots/videos in test-results/
ls test-results/
```

## CI/CD Integration

### GitHub Actions Workflow

**File**: `.github/workflows/e2e-tests.yml`

```yaml
name: E2E Tests

on:
  pull_request:
    branches: [master, develop]
  push:
    branches: [master, develop]

jobs:
  e2e:
    runs-on: ubuntu-latest

    steps:
      - uses: actions/checkout@v4

      - name: Set up Docker Compose
        run: docker compose up -d --build

      - name: Wait for services
        run: |
          timeout 120 bash -c 'until curl -f http://localhost:4200; do sleep 2; done'
          timeout 120 bash -c 'until curl -f http://localhost:8080/api/courses; do sleep 2; done'

      - name: Setup Node.js
        uses: actions/setup-node@v4
        with:
          node-version: '20'
          cache: 'npm'
          cache-dependency-path: frontend/package-lock.json

      - name: Install dependencies
        working-directory: frontend
        run: npm ci

      - name: Install Playwright browsers
        working-directory: frontend
        run: npx playwright install chromium --with-deps

      - name: Run E2E tests
        working-directory: frontend
        run: npm run test:e2e

      - name: Upload test results
        if: always()
        uses: actions/upload-artifact@v4
        with:
          name: playwright-report
          path: frontend/playwright-report/
          retention-days: 7

      - name: Docker logs on failure
        if: failure()
        run: docker compose logs

      - name: Cleanup
        if: always()
        run: docker compose down -v
```

## Troubleshooting

### Docker Compose Not Running

```bash
# Check service status
docker compose ps

# View logs
docker compose logs frontend
docker compose logs backend

# Restart services
docker compose restart
```

### Port Already in Use

```bash
# Find process using port 4200 or 8080
lsof -i :4200
lsof -i :8080

# Kill process or use different ports in docker-compose.yml
```

### Tests Timing Out

```bash
# Increase timeout in playwright.config.ts
timeout: 30000  # 30 seconds per test

# Or per-test
test('my test', async ({ page }) => {
  test.setTimeout(60000);  // 60 seconds
  // ...
});
```

### Seed Data Not Found

```bash
# Check backend logs to verify seed data was created
docker compose logs backend | grep "Seed"

# Manually verify courses exist
curl http://localhost:8080/api/courses
```

## Environment Variables

Tests inherit from Docker Compose environment:

- `JWT_SECRET` - JWT signing key (auto-generated in tests)
- `JWT_EXPIRATION_HOURS` - Token expiry (24h default)
- `GOOGLE_CLIENT_ID` - Mock value for tests

No additional env vars needed for E2E tests.
