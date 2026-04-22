# Lexiq Documentation

This directory contains comprehensive documentation for the Lexiq language learning platform.

## Documentation Structure

```
docs/
├── README.md (this file)
├── backend/          # Backend-specific documentation
│   ├── tests.md                    # Backend test coverage (xUnit, Testcontainers)
│   └── e2e-backend-changes.md      # Backend modifications needed for E2E tests
├── frontend/         # Frontend-specific documentation
│   └── (placeholder for future Angular/frontend docs)
└── e2e/             # End-to-end testing documentation
    ├── README.md                   # E2E testing overview
    ├── setup.md                    # Playwright installation and configuration
    ├── authentication.md           # JWT cookie injection and test user creation
    ├── test-data.md               # Test data strategy and seed data reference
    ├── page-objects.md            # Page Object Model patterns
    ├── data-testid-guide.md       # Guidelines for adding test selectors
    ├── test-suites.md             # Complete test specifications
    └── implementation-plan.md     # Step-by-step implementation guide
```

## Quick Links

### Backend Documentation
- **[Backend Tests](./backend/tests.md)** - Complete coverage of xUnit tests, Testcontainers setup, test infrastructure, and coverage gaps
- **[E2E Backend Changes](./backend/e2e-backend-changes.md)** - Required backend modifications to support E2E testing (test endpoints, mock OAuth)

### E2E Testing Documentation
- **[E2E Overview](./e2e/README.md)** - Introduction to E2E testing with Playwright
- **[Setup Guide](./e2e/setup.md)** - Installation, configuration, and running tests
- **[Authentication](./e2e/authentication.md)** - How to handle JWT cookies and create test users
- **[Test Data](./e2e/test-data.md)** - Seed data reference and data management strategies
- **[Page Objects](./e2e/page-objects.md)** - Reusable page object patterns for Playwright
- **[data-testid Guide](./e2e/data-testid-guide.md)** - How to add test selectors to Angular components
- **[Test Suites](./e2e/test-suites.md)** - Complete test specifications for all user journeys
- **[Implementation Plan](./e2e/implementation-plan.md)** - Step-by-step guide to implementing E2E tests

### Frontend Documentation
_(Placeholder for future Angular-specific documentation)_

## Testing Documentation Overview

### Backend Testing
The backend uses **xUnit v3** with **Testcontainers** for integration testing. Tests cover:
- Authentication flow (JWT cookies, Google OAuth)
- Authorization policies (role-based access control)
- Leaderboard queries (AllTime, Weekly, Monthly)
- Streak calculation
- Level calculation
- JWT generation

See [backend/tests.md](./backend/tests.md) for complete details.

### E2E Testing
End-to-end tests use **Playwright** to validate complete user journeys across the full stack. Priority flows:
- **Authentication Flow** - Login, logout, session persistence, protected routes
- **Complete Lesson Flow** - Browse courses → select lesson → solve exercises → see results

See [e2e/README.md](./e2e/README.md) for complete details.

## Related Documentation

- **[Root CLAUDE.md](../CLAUDE.md)** - Project overview, architecture, quick reference
- **[Backend CLAUDE.md](../backend/CLAUDE.md)** - Backend structure, patterns, debugging
- **[Frontend CLAUDE.md](../frontend/CLAUDE.md)** - Frontend structure, Angular patterns, debugging
- **[Backend Tests CLAUDE.md](../backend/Tests/CLAUDE.md)** - Test infrastructure patterns
- **[Database Entities](../backend/Database/ENTITIES_DOCUMENTATION.md)** - Complete entity reference

## Contributing to Documentation

When adding new documentation:

1. **Backend docs** → `docs/backend/`
2. **Frontend docs** → `docs/frontend/`
3. **E2E/integration docs** → `docs/e2e/`
4. **General architecture/workflow docs** → Root `CLAUDE.md` or `.claude/` directory

Keep documentation:
- ✅ Up-to-date with code changes
- ✅ Clear and concise
- ✅ Includes examples where helpful
- ✅ Cross-referenced appropriately
