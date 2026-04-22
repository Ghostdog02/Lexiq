 # E2E Testing Documentation

**Last Updated**: 2026-03-03
**Framework**: Playwright
**Test Environment**: Local Docker Compose

## Overview

This directory contains comprehensive documentation for Lexiq's end-to-end testing implementation using Playwright. E2E tests validate complete user journeys across multiple pages, ensuring all integration points work correctly.

## Documentation Structure

- **[setup.md](./setup.md)** - Playwright installation, configuration, and Docker Compose integration
- **[test-data.md](./test-data.md)** - Test data strategy, seed data reference, user creation helpers
- **[authentication.md](./authentication.md)** - JWT cookie injection, test user creation, auth helpers
- **[page-objects.md](./page-objects.md)** - Page Object Model patterns, selectors, component interactions
- **[test-suites.md](./test-suites.md)** - Test specifications for each user journey
- **[e2e-backend-changes.md](../backend/e2e-backend-changes.md)** - Required backend modifications (test endpoint)
- **[data-testid-guide.md](./data-testid-guide.md)** - Guidelines for adding data-testid attributes to Angular components

## Quick Start

```bash
# 1. Install Playwright (from frontend/)
npm install -D @playwright/test
npx playwright install chromium

# 2. Start Docker Compose (from root)
docker compose up --build

# 3. Run tests (from frontend/)
npx playwright test

# 4. View report
npx playwright show-report
```

## Priority Test Flows

1. **Authentication Flow** - Login, logout, session persistence, protected routes
2. **Complete Lesson Flow** - Browse courses → select lesson → solve exercises → see results

## Key Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| **E2E Framework** | Playwright | TypeScript native, excellent Docker support, parallel execution, auto-wait |
| **Auth Strategy** | Mock OAuth + inject JWT cookie | Fast, reliable, no external dependencies |
| **Test Data** | Use seed data from DatabaseFixture | Consistent, no per-test setup required |
| **Answer Lookup** | Add test endpoint `/api/exercises/{id}/correct-answer` | Clean separation, reusable across tests |
| **Cleanup** | Unique emails per run, no deletion | Fast, simple, independent tests |
| **Selectors** | Add `data-testid` attributes | Resilient to UI changes, clear intent |

## See Also

- [Frontend CLAUDE.md](../../frontend/CLAUDE.md) - Angular patterns and conventions
- [Backend Tests CLAUDE.md](../../backend/Tests/CLAUDE.md) - Backend test infrastructure
- [Root CLAUDE.md](../../CLAUDE.md) - Architecture overview
