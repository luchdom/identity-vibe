import type { Page, Route } from '@playwright/test';

export const TEST_USER = {
  id: 'test-user-1',
  sub: 'test-user-1',
  email: 'test.user@example.com',
  firstName: 'Test',
  lastName: 'User',
};

export async function fulfillJsonWithCors(route: Route, body: unknown, status = 200) {
  const requestOrigin = route.request().headers().origin;
  const allowOrigin = requestOrigin || 'http://localhost:3101';

  if (route.request().method() === 'OPTIONS') {
    await route.fulfill({
      status: 204,
      headers: {
        'access-control-allow-origin': allowOrigin,
        'access-control-allow-methods': 'GET,POST,PUT,DELETE,OPTIONS',
        'access-control-allow-headers': '*',
        'access-control-allow-credentials': 'true',
      },
    });
    return;
  }

  await route.fulfill({
    status,
    contentType: 'application/json',
    headers: {
      'access-control-allow-origin': allowOrigin,
      'access-control-allow-credentials': 'true',
    },
    body: JSON.stringify(body),
  });
}

export async function setupAuthenticatedSession(page: Page, grantedScopes: string) {
  await page.addInitScript((payload) => {
    localStorage.setItem('accessToken', 'fake-access-token');
    localStorage.setItem('refreshToken', 'fake-refresh-token');
    localStorage.setItem('grantedScopes', payload.grantedScopes);
    localStorage.setItem('user', JSON.stringify(payload.user));
    localStorage.setItem('__e2e_bypass_auth', '1');
  }, {
    user: TEST_USER,
    grantedScopes,
  });

  await page.route('**/connect/userinfo', async (route) => {
    await fulfillJsonWithCors(route, TEST_USER);
  });

  await page.route('**/connect/token', async (route) => {
    await fulfillJsonWithCors(route, {
      access_token: 'fake-access-token',
      refresh_token: 'fake-refresh-token',
      scope: grantedScopes,
    });
  });
}

export async function delay(ms: number) {
  await new Promise((resolve) => {
    setTimeout(resolve, ms);
  });
}
