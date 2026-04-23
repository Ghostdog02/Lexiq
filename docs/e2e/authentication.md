# Authentication Helpers

## Overview

E2E tests bypass Google OAuth by directly injecting JWT cookies. This approach is:
- ✅ **Fast** - No external OAuth roundtrip
- ✅ **Reliable** - No dependency on Google services
- ✅ **Secure** - Only works in test environment

## JWT Cookie Flow

### Production Flow (What We're Bypassing)

```
1. User clicks "Sign in with Google"
2. Redirect to Google OAuth
3. User authorizes app
4. Google redirects back with ID token
5. Frontend sends token to /api/auth/google-login
6. Backend validates token, creates user, generates JWT
7. Backend sets AuthToken HttpOnly cookie
8. Frontend stores auth state
```

### Test Flow (Fast Path)

```
1. Test calls /api/auth/google-login with MOCK Google token
2. Backend creates user (skips Google validation in test)
3. Backend generates JWT, sets AuthToken cookie
4. Test extracts JWT from response headers
5. Test injects JWT cookie into browser context
6. Test proceeds as authenticated user
```

## Implementation

### Create Test User via API

**File**: `e2e/helpers/auth.helper.ts`

```typescript
import { Page, APIRequestContext, request } from '@playwright/test';

export interface TestUser {
  id: string;
  email: string;
  userName: string;
  token: string;
}

/**
 * Creates a test user via backend API and returns JWT token
 *
 * @param email - Unique email (use generateTestEmail())
 * @param userName - Display name
 * @returns TestUser with JWT token
 */
export async function createTestUser(
  email: string,
  userName: string
): Promise<TestUser> {
  const apiContext = await request.newContext({
    baseURL: 'http://localhost:8080'
  });

  // Mock Google OAuth payload
  const googleTokenPayload = {
    idToken: 'mock-google-id-token-' + Date.now(),
    email: email,
    name: userName,
    picture: 'https://example.com/avatar.jpg',
  };

  // Call backend auth endpoint
  const response = await apiContext.post('/api/auth/google-login', {
    data: googleTokenPayload,
  });

  if (!response.ok()) {
    const body = await response.text();
    throw new Error(
      `Failed to create test user: ${response.status()} - ${body}`
    );
  }

  // Extract JWT from Set-Cookie header
  const cookies = response.headers()['set-cookie'];
  const authTokenMatch = cookies?.match(/AuthToken=([^;]+)/);

  if (!authTokenMatch) {
    throw new Error('No AuthToken cookie in response headers');
  }

  const token = authTokenMatch[1];
  const userData = await response.json();

  await apiContext.dispose();

  return {
    id: userData.id,
    email: userData.email,
    userName: userData.userName,
    token: token,
  };
}

/**
 * Logs in by injecting JWT cookie into browser context
 *
 * @param page - Playwright page object
 * @param user - TestUser with JWT token
 */
export async function loginAsUser(page: Page, user: TestUser): Promise<void> {
  // Must navigate to domain first (cookies can only be set for loaded domain)
  await page.goto('/');

  // Inject JWT cookie
  await page.context().addCookies([
    {
      name: 'AuthToken',
      value: user.token,
      domain: 'localhost',
      path: '/',
      httpOnly: true,
      sameSite: 'Lax',
      expires: Math.floor(Date.now() / 1000) + 24 * 60 * 60, // 24h from now
    },
  ]);

  // Reload to activate authentication
  // This triggers APP_INITIALIZER → AuthService.initializeAuthState()
  await page.reload();

  // Wait for auth state to initialize
  await page.waitForLoadState('networkidle');
}

/**
 * Logs out by clearing JWT cookie
 *
 * @param page - Playwright page object
 */
export async function logout(page: Page): Promise<void> {
  // Clear auth cookie
  await page.context().clearCookies({ name: 'AuthToken' });

  // Reload to clear auth state
  await page.reload();
  await page.waitForLoadState('networkidle');
}

/**
 * Generates unique test email for each test run
 *
 * @param testName - Test name for uniqueness
 * @returns Unique email like "test-auth-flow-1234567890@example.com"
 */
export function generateTestEmail(testName: string): string {
  const timestamp = Date.now();
  const sanitized = testName.toLowerCase().replace(/[^a-z0-9]+/g, '-');
  return `test-${sanitized}-${timestamp}@example.com`;
}

/**
 * Generates unique test username
 *
 * @param prefix - Username prefix
 * @returns Username like "TestUser_1234567890"
 */
export function generateTestUsername(prefix: string = 'TestUser'): string {
  return `${prefix}_${Date.now()}`;
}
```

## Usage Examples

### Basic Authentication Test

```typescript
import { test, expect } from '@playwright/test';
import { createTestUser, loginAsUser, generateTestEmail } from '../helpers/auth.helper';

test('should login successfully', async ({ page }) => {
  // Create unique test user
  const testUser = await createTestUser(
    generateTestEmail('login-test'),
    'Test User'
  );

  // Login by injecting JWT cookie
  await loginAsUser(page, testUser);

  // Verify we're on authenticated home page
  await expect(page).toHaveURL('/');
  await expect(page.locator('[data-testid="user-menu"]')).toBeVisible();
});
```

### Authenticated User Journey

```typescript
test('should complete lesson as authenticated user', async ({ page }) => {
  // Setup: Create and login
  const testUser = await createTestUser(
    generateTestEmail('lesson-flow'),
    'Learner User'
  );
  await loginAsUser(page, testUser);

  // Navigate to lesson
  await page.goto('/');
  // ... rest of test
});
```

### Logout Test

```typescript
import { logout } from '../helpers/auth.helper';

test('should logout successfully', async ({ page }) => {
  // Login first
  const testUser = await createTestUser(
    generateTestEmail('logout-test'),
    'Logout User'
  );
  await loginAsUser(page, testUser);

  // Verify logged in
  await expect(page.locator('[data-testid="user-menu"]')).toBeVisible();

  // Logout
  await logout(page);

  // Should redirect to login page
  await expect(page).toHaveURL('/google-login');
});
```

## Protected Route Testing

### Testing Auth Guard

```typescript
test('should protect authenticated routes', async ({ page }) => {
  // Try to access protected route without login
  await page.goto('/profile');

  // Should redirect to login
  await expect(page).toHaveURL(/\/google-login/);
});

test('should allow access after authentication', async ({ page }) => {
  // Login first
  const testUser = await createTestUser(
    generateTestEmail('protected-route'),
    'Auth User'
  );
  await loginAsUser(page, testUser);

  // Now can access protected route
  await page.goto('/profile');
  await expect(page).toHaveURL('/profile');
  await expect(page.locator('[data-testid="profile-username"]'))
    .toContainText('Auth User');
});
```

## Session Persistence

### Testing Cookie Persistence

```typescript
test('should persist session across page reloads', async ({ page }) => {
  // Login
  const testUser = await createTestUser(
    generateTestEmail('persist-session'),
    'Persistent User'
  );
  await loginAsUser(page, testUser);

  // Navigate to different page
  await page.goto('/leaderboard');
  await expect(page).toHaveURL('/leaderboard');

  // Reload page
  await page.reload();

  // Should still be authenticated (no redirect to login)
  await expect(page).toHaveURL('/leaderboard');
  await expect(page.locator('[data-testid="user-menu"]')).toBeVisible();
});
```

## Role-Based Testing

### Creating Admin User

```typescript
// NOTE: Current implementation only supports Student role
// Admin/ContentCreator roles require manual database insertion

test.skip('should create admin user', async ({ page }) => {
  // TODO: Implement admin user creation
  // Requires either:
  // 1. Backend test endpoint: POST /api/test/users with role parameter
  // 2. Direct database insertion (requires DbContext access)
  // 3. Manual role assignment via UserManager after user creation
});
```

**Future Enhancement**: Add test-only endpoint to create users with specific roles.

## Advanced Patterns

### Reusing Authenticated Context

```typescript
// Create authenticated context once, reuse across tests
test.describe('Authenticated user tests', () => {
  let authenticatedPage: Page;
  let testUser: TestUser;

  test.beforeAll(async ({ browser }) => {
    // Create user once
    testUser = await createTestUser(
      generateTestEmail('shared-user'),
      'Shared User'
    );

    // Create context with cookie
    const context = await browser.newContext();
    authenticatedPage = await context.newPage();
    await loginAsUser(authenticatedPage, testUser);
  });

  test.afterAll(async () => {
    await authenticatedPage.close();
  });

  test('test 1', async () => {
    await authenticatedPage.goto('/profile');
    // ... assertions
  });

  test('test 2', async () => {
    await authenticatedPage.goto('/leaderboard');
    // ... assertions
  });
});
```

## Troubleshooting

### Cookie Not Set

**Problem**: JWT cookie not appearing in browser context

**Solutions**:
1. Ensure `await page.goto('/')` is called **before** `addCookies()`
2. Verify cookie domain matches page domain (`localhost`)
3. Check cookie expiration is in the future

### 401 Unauthorized After Login

**Problem**: Requests fail with 401 even after injecting cookie

**Solutions**:
1. Verify JWT token is valid (check `createTestUser` response)
2. Ensure `page.reload()` is called after injecting cookie
3. Check backend logs for JWT validation errors
4. Verify `withCredentials: true` in HTTP interceptor

### User Already Exists Error

**Problem**: Duplicate email error when creating test user

**Solutions**:
1. Use `generateTestEmail()` to ensure unique emails
2. Include timestamp in email generation
3. Clear database: `docker compose down -v && docker compose up`

## Backend Requirements

### Mock Google Auth in Tests

Backend must accept mock Google tokens in test environment.

**Current Implementation**: `GoogleAuthService.ValidateGoogleTokenAsync()` validates real Google tokens.

**Required Change**: Skip Google validation if `ASPNETCORE_ENVIRONMENT=Development` and token starts with `"mock-"`.

**See**: [backend-changes.md](./backend-changes.md#mock-google-auth)
