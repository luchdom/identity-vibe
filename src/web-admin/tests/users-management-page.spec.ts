import { expect, test, type Page } from '@playwright/test';

const BASE_URL = process.env.PLAYWRIGHT_BASE_URL ?? 'http://localhost:3000';

const dismissNextOverlay = async (page: Page) => {
  await page.addStyleTag({
    content: '[data-nextjs-dev-overlay], nextjs-portal { display: none !important; pointer-events: none !important; }',
  }).catch(() => {});

  await page.evaluate(() => {
    document.querySelectorAll('[data-nextjs-dev-overlay], nextjs-portal').forEach((el) => el.remove());
  }).catch(() => {});
};

test.describe('Users Management Page', () => {
  test.beforeEach(async ({ page }) => {
    type MockUserSummary = {
      id: string;
      email: string;
      userName: string;
      phoneNumber: string | null;
      isActive: boolean;
      roles: string[];
      createdAt: string;
      updatedAt: string | null;
    };

    const roles = [
      {
        id: 'role-admin',
        name: 'Admin',
        description: 'System administrator',
        isActive: true,
        assignedScopes: ['admin.full', 'admin.users', 'admin.roles'],
      },
      {
        id: 'role-user',
        name: 'User',
        description: 'Default user',
        isActive: true,
        assignedScopes: ['orders.read', 'orders.write', 'profile.read'],
      },
      {
        id: 'role-support',
        name: 'Support',
        description: 'Support agent',
        isActive: true,
        assignedScopes: ['orders.read', 'profile.read'],
      },
    ];

    let users: MockUserSummary[] = [
      {
        id: 'admin-id',
        email: 'admin@example.com',
        userName: 'admin@example.com',
        phoneNumber: '+1-555-0001',
        isActive: true,
        roles: ['Admin'],
        createdAt: '2024-01-01T00:00:00Z',
        updatedAt: null,
      },
      {
        id: 'user-1',
        email: 'user1@example.com',
        userName: 'user1@example.com',
        phoneNumber: '+1-555-0002',
        isActive: true,
        roles: ['User'],
        createdAt: '2024-01-02T00:00:00Z',
        updatedAt: null,
      },
    ];

    const userDetails = new Map<string, any>([
      ['admin-id', { ...users[0], emailConfirmed: true }],
      ['user-1', { ...users[1], emailConfirmed: true }],
    ]);

    await page.route('**/connect/userinfo', async (route) => {
      await route.fulfill({
        contentType: 'application/json',
        body: JSON.stringify({
          id: 'admin-id',
          email: 'admin@example.com',
          firstName: 'Admin',
          lastName: 'User',
          roles: ['Admin'],
          isActive: true,
          createdAt: '2024-01-01T00:00:00Z',
        }),
      });
    });

    await page.route((url) => {
      const requestUrl = typeof url === 'string' ? url : url.href;
      return requestUrl.includes('/api/admin/users');
    }, async (route) => {
      const request = route.request();
      const url = new URL(request.url());
      const pathname = url.pathname.replace(/\/+$/, '');
      const parts = pathname.split('/').filter(Boolean);
      const userId = parts[3];
      const action = parts[4];

      if (url.pathname.includes('available-roles')) {
        await route.fulfill({
          contentType: 'application/json',
          body: JSON.stringify(roles),
        });
        return;
      }

      if (!userId && request.method() === 'GET') {
        const search = (url.searchParams.get('search') ?? '').toLowerCase();
        const status = url.searchParams.get('isActive');
        const role = url.searchParams.get('role');
        const pageNumber = Number(url.searchParams.get('pageNumber') ?? '1');
        const pageSize = Number(url.searchParams.get('pageSize') ?? '20');

        let filtered = [...users];
        if (search) {
          filtered = filtered.filter((entry) =>
            entry.email.toLowerCase().includes(search) ||
            entry.userName.toLowerCase().includes(search) ||
            (entry.phoneNumber ?? '').toLowerCase().includes(search)
          );
        }

        if (status === 'true' || status === 'false') {
          filtered = filtered.filter((entry) => entry.isActive === (status === 'true'));
        }

        if (role) {
          filtered = filtered.filter((entry) => entry.roles.includes(role));
        }

        const start = (pageNumber - 1) * pageSize;
        const pageUsers = filtered.slice(start, start + pageSize);
        const totalPages = filtered.length === 0 ? 0 : Math.ceil(filtered.length / pageSize);

        await route.fulfill({
          contentType: 'application/json',
          body: JSON.stringify({
            users: pageUsers,
            totalCount: filtered.length,
            pageNumber,
            pageSize,
            totalPages,
            hasPreviousPage: pageNumber > 1,
            hasNextPage: pageNumber < totalPages,
          }),
        });
        return;
      }

      if (!userId && request.method() === 'POST') {
        const body = request.postDataJSON() as {
          email: string;
          phoneNumber?: string;
          isActive?: boolean;
          roleNames: string[];
        };
        const id = `user-${Date.now()}`;
        const detail = {
          id,
          email: body.email,
          userName: body.email,
          phoneNumber: body.phoneNumber ?? null,
          isActive: body.isActive ?? true,
          emailConfirmed: true,
          roles: body.roleNames,
          createdAt: new Date().toISOString(),
          updatedAt: null,
        };

        users = [{ ...detail }, ...users];
        userDetails.set(id, detail);

        await route.fulfill({
          status: 201,
          contentType: 'application/json',
          body: JSON.stringify(detail),
        });
        return;
      }

      if (!userId || !userDetails.has(userId)) {
        await route.fulfill({
          status: 404,
          contentType: 'application/json',
          body: JSON.stringify({ detail: 'User not found' }),
        });
        return;
      }

      const current = userDetails.get(userId)!;

      if (request.method() === 'GET' && !action) {
        await route.fulfill({
          contentType: 'application/json',
          body: JSON.stringify(current),
        });
        return;
      }

      if (request.method() === 'PUT' && !action) {
        const body = request.postDataJSON() as { email?: string; phoneNumber?: string };
        const updated = {
          ...current,
          email: body.email ?? current.email,
          userName: body.email ?? current.userName,
          phoneNumber: body.phoneNumber ?? null,
          updatedAt: new Date().toISOString(),
        };
        userDetails.set(userId, updated);
        users = users.map((entry) => (entry.id === userId ? { ...entry, ...updated } : entry));

        await route.fulfill({
          contentType: 'application/json',
          body: JSON.stringify(updated),
        });
        return;
      }

      if (request.method() === 'PUT' && action === 'roles') {
        const body = request.postDataJSON() as { roleNames: string[] };
        const updated = {
          ...current,
          roles: body.roleNames,
          updatedAt: new Date().toISOString(),
        };
        userDetails.set(userId, updated);
        users = users.map((entry) => (entry.id === userId ? { ...entry, roles: body.roleNames } : entry));

        await route.fulfill({
          contentType: 'application/json',
          body: JSON.stringify(updated),
        });
        return;
      }

      if (request.method() === 'PUT' && action === 'status') {
        const body = request.postDataJSON() as { isActive: boolean };
        const updated = {
          ...current,
          isActive: body.isActive,
          updatedAt: new Date().toISOString(),
        };
        userDetails.set(userId, updated);
        users = users.map((entry) => (entry.id === userId ? { ...entry, isActive: body.isActive } : entry));

        await route.fulfill({
          contentType: 'application/json',
          body: JSON.stringify(updated),
        });
        return;
      }

      if (request.method() === 'PUT' && action === 'password') {
        await route.fulfill({
          contentType: 'application/json',
          body: JSON.stringify({
            ...current,
            updatedAt: new Date().toISOString(),
          }),
        });
        return;
      }

      await route.continue();
    });

    await page.addInitScript(() => {
      localStorage.setItem('accessToken', 'mock-token');
      localStorage.setItem('refreshToken', 'mock-refresh-token');
      localStorage.setItem('grantedScopes', 'openid profile email offline_access admin.users');
      localStorage.setItem('user', JSON.stringify({
        id: 'admin-id',
        email: 'admin@example.com',
        firstName: 'Admin',
        lastName: 'User',
        roles: ['Admin'],
        isActive: true,
        createdAt: '2024-01-01T00:00:00Z',
      }));
    });
  });

  test('renders list and applies search filter', async ({ page }) => {
    await page.goto(`${BASE_URL}/users`);
    await page.waitForLoadState('networkidle');
    await dismissNextOverlay(page);

    await expect(page.getByTestId('users-management-page')).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Users' })).toBeVisible();
    await expect(page.getByTestId('users-table')).toBeVisible();
    await expect(page.locator('tr', { hasText: 'user1@example.com' })).toBeVisible();

    await page.getByTestId('users-search-input').fill('user1');
    await page.getByRole('button', { name: 'Apply' }).click();
    await page.waitForLoadState('networkidle');

    await expect(page.locator('tr', { hasText: 'user1@example.com' })).toBeVisible();
    await expect(page.locator('tr', { hasText: 'admin@example.com' })).not.toBeVisible();
  });

  test('creates a user and updates status', async ({ page }) => {
    await page.goto(`${BASE_URL}/users`);
    await page.waitForLoadState('networkidle');
    await dismissNextOverlay(page);

    await page.getByTestId('new-user-button').click();
    await page.getByTestId('create-user-email').fill('new.user@example.com');
    await page.getByTestId('create-user-password').fill('Temp123!');
    await page.locator('label:has-text("User") input[type="checkbox"]').first().check();
    await page.getByTestId('create-user-submit').click();

    await expect(page.getByText('User created')).toBeVisible();
    await expect(page.locator('tr', { hasText: 'new.user@example.com' })).toBeVisible();

    const disableButton = page
      .locator('tr', { hasText: 'new.user@example.com' })
      .getByRole('button', { name: 'Disable' });
    await disableButton.click();

    await expect(page.getByText('User disabled')).toBeVisible();
    await expect(
      page.locator('tr', { hasText: 'new.user@example.com' }).getByRole('button', { name: 'Enable' })
    ).toBeVisible();
  });
});
