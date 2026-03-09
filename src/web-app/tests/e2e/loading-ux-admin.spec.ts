import { expect, test } from '@playwright/test';

import { delay, fulfillJsonWithCors, setupAuthenticatedSession } from './helpers/e2e-auth';

function buildUsersListResponse(email: string) {
  const now = new Date().toISOString();

  return {
    users: [
      {
        id: `${email}-id`,
        email,
        userName: email,
        phoneNumber: null,
        isActive: true,
        roles: ['Admin'],
        createdAt: now,
        updatedAt: now,
      },
    ],
    totalCount: 1,
    pageNumber: 1,
    pageSize: 5,
    totalPages: 1,
    hasPreviousPage: false,
    hasNextPage: false,
  };
}

function buildRoleSummary(roleName: string) {
  return {
    id: 'role-1',
    name: roleName,
    description: `${roleName} role`,
    isActive: true,
    assignedScopeCount: 2,
    assignedScopes: ['orders.read', 'users.read'],
  };
}

function buildRoleDetail(roleName: string) {
  const now = new Date().toISOString();

  return {
    id: 'role-1',
    name: roleName,
    description: `${roleName} role`,
    isActive: true,
    assignedScopes: ['orders.read', 'users.read'],
    createdAt: now,
    updatedAt: now,
  };
}

const AVAILABLE_ROLES = [
  {
    id: 'role-admin',
    name: 'Admin',
    description: 'Admin role',
    isActive: true,
    assignedScopes: ['admin.users', 'admin.roles'],
  },
];

const AVAILABLE_SCOPES = [
  {
    name: 'orders.read',
    displayName: 'Orders Read',
    description: 'Read orders',
    group: 'orders',
    resources: ['orders'],
  },
  {
    name: 'users.read',
    displayName: 'Users Read',
    description: 'Read users',
    group: 'users',
    resources: ['users'],
  },
];

test.describe('Users loading UX', () => {
  test('shows first-load loader and resolves list', async ({ page }) => {
    await setupAuthenticatedSession(page, 'admin.users admin.full profile openid');

    await page.route('**/*', async (route) => {
      const pathname = new URL(route.request().url()).pathname;

      if (pathname.includes('/connect/')) {
        await route.fallback();
        return;
      }

      if (pathname.includes('/api/admin/users/available-roles')) {
        await fulfillJsonWithCors(route, AVAILABLE_ROLES);
        return;
      }

      if (pathname.includes('/api/admin/users')) {
        await delay(1400);
        await fulfillJsonWithCors(route, buildUsersListResponse('alpha@example.com'));
        return;
      }

      await route.fallback();
    });

    await page.goto('/users');

    await expect(page.getByText('Loading users...')).toBeVisible();
    await expect(page.getByRole('link', { name: 'alpha@example.com' })).toBeVisible();
  });

  test('keeps current table visible while users refetch', async ({ page }) => {
    await setupAuthenticatedSession(page, 'admin.users admin.full profile openid');

    let listRequestCount = 0;

    await page.route('**/*', async (route) => {
      const pathname = new URL(route.request().url()).pathname;

      if (pathname.includes('/connect/')) {
        await route.fallback();
        return;
      }

      if (pathname.includes('/api/admin/users/available-roles')) {
        await fulfillJsonWithCors(route, AVAILABLE_ROLES);
        return;
      }

      if (pathname.includes('/api/admin/users')) {
        listRequestCount += 1;
        if (listRequestCount === 1) {
          await fulfillJsonWithCors(route, buildUsersListResponse('alpha@example.com'));
          return;
        }

        await delay(1450);
        await fulfillJsonWithCors(route, buildUsersListResponse('bravo@example.com'));
        return;
      }

      await route.fallback();
    });

    await page.goto('/users');
    await expect(page.getByRole('link', { name: 'alpha@example.com' })).toBeVisible();

    await page.getByRole('tab', { name: /Active/i }).click();

    await expect(page.getByRole('link', { name: 'alpha@example.com' })).toBeVisible();
    await expect(page.locator('.MuiLinearProgress-root').first()).toBeVisible();
    await expect(page.getByRole('link', { name: 'bravo@example.com' })).toBeVisible();
  });
});

test.describe('Roles loading UX', () => {
  test('shows first-load loader and resolves role list', async ({ page }) => {
    await setupAuthenticatedSession(page, 'admin.roles admin.full profile openid');

    await page.route('**/*', async (route) => {
      const pathname = new URL(route.request().url()).pathname;

      if (pathname.includes('/connect/')) {
        await route.fallback();
        return;
      }

      if (pathname.includes('/api/admin/roles/available-scopes')) {
        await fulfillJsonWithCors(route, AVAILABLE_SCOPES);
        return;
      }

      if (pathname.includes('/api/admin/roles/role-1')) {
        await fulfillJsonWithCors(route, buildRoleDetail('Operator'));
        return;
      }

      if (pathname.includes('/api/admin/roles')) {
        await delay(1400);
        await fulfillJsonWithCors(route, [buildRoleSummary('Operator')]);
        return;
      }

      await route.fallback();
    });

    await page.goto('/users/roles');

    await expect(page.getByText('Loading roles...')).toBeVisible();
    await expect(page.getByTestId('role-item-Operator')).toBeVisible();
  });

  test('keeps current role list visible while refresh is in progress', async ({ page }) => {
    await setupAuthenticatedSession(page, 'admin.roles admin.full profile openid');

    let listRequestCount = 0;
    let detailRequestCount = 0;

    await page.route('**/*', async (route) => {
      const pathname = new URL(route.request().url()).pathname;

      if (pathname.includes('/connect/')) {
        await route.fallback();
        return;
      }

      if (pathname.includes('/api/admin/roles/available-scopes')) {
        await fulfillJsonWithCors(route, AVAILABLE_SCOPES);
        return;
      }

      if (pathname.includes('/api/admin/roles/role-1')) {
        detailRequestCount += 1;
        const roleName = detailRequestCount > 1 ? 'Manager' : 'Operator';
        await fulfillJsonWithCors(route, buildRoleDetail(roleName));
        return;
      }

      if (pathname.includes('/api/admin/roles')) {
        listRequestCount += 1;
        if (listRequestCount === 1) {
          await fulfillJsonWithCors(route, [buildRoleSummary('Operator')]);
          return;
        }

        await delay(1450);
        await fulfillJsonWithCors(route, [buildRoleSummary('Manager')]);
        return;
      }

      await route.fallback();
    });

    await page.goto('/users/roles');
    await expect(page.getByTestId('role-item-Operator')).toBeVisible();

    await page.getByRole('button', { name: 'Refresh' }).click();

    await expect(page.getByTestId('role-item-Operator')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Refreshing...' })).toBeVisible();
    await expect(page.locator('.MuiLinearProgress-root').first()).toBeVisible();
    await expect(page.getByTestId('role-item-Manager')).toBeVisible();
  });
});
