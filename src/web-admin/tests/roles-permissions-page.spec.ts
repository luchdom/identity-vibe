import { test, expect, type Page, type Route } from '@playwright/test';

const BASE_URL = process.env.PLAYWRIGHT_BASE_URL ?? 'http://localhost:3000';

const dismissNextOverlay = async (page: Page) => {
  await page.addStyleTag({
    content: '[data-nextjs-dev-overlay], nextjs-portal { display: none !important; pointer-events: none !important; }',
  }).catch(() => {});

  await page.evaluate(() => {
    document.querySelectorAll('[data-nextjs-dev-overlay], nextjs-portal').forEach((el) => el.remove());
  }).catch(() => {});
};

test.describe('Roles & Permissions Page', () => {
  test.beforeEach(async ({ page }) => {
    let roles: Array<{
      id: string;
      name: string;
      description: string | null;
      isActive: boolean;
      assignedScopeCount: number;
      assignedScopes: string[];
    }> = [
      {
        id: 'role-admin',
        name: 'Admin',
        description: 'Administrators',
        isActive: true,
        assignedScopeCount: 3,
        assignedScopes: ['orders.read', 'orders.manage', 'admin.roles'],
      },
      {
        id: 'role-user',
        name: 'User',
        description: 'Standard users',
        isActive: true,
        assignedScopeCount: 2,
        assignedScopes: ['orders.read', 'orders.write'],
      },
    ];

    const roleDetails = new Map<string, any>([
      ['role-admin', {
        id: 'role-admin',
        name: 'Admin',
        description: 'Administrators',
        isActive: true,
        assignedScopes: ['orders.read', 'orders.manage', 'admin.roles'],
        createdAt: '2024-01-01T00:00:00Z',
        updatedAt: '2024-01-02T00:00:00Z',
      }],
      ['role-user', {
        id: 'role-user',
        name: 'User',
        description: 'Standard users',
        isActive: true,
        assignedScopes: ['orders.read', 'orders.write'],
        createdAt: '2024-01-01T00:00:00Z',
        updatedAt: '2024-01-03T00:00:00Z',
      }],
    ]);

    const availableScopes = [
      { name: 'orders.read', displayName: 'Read Orders', description: 'View orders', group: 'orders', resources: [] },
      { name: 'orders.write', displayName: 'Write Orders', description: 'Modify orders', group: 'orders', resources: [] },
      { name: 'orders.manage', displayName: 'Manage Orders', description: 'Admin orders', group: 'orders', resources: [] },
      { name: 'admin.roles', displayName: 'Role Management', description: 'Manage roles', group: 'admin', resources: [] },
      { name: 'admin.full', displayName: 'Admin Full', description: 'Super admin', group: 'admin', resources: [] },
    ];

    await page.route('**/connect/userinfo', async (route) => {
      await route.fulfill({
        contentType: 'application/json',
        body: JSON.stringify({
          sub: 'admin-id',
          name: 'Admin User',
          email: 'admin@example.com',
          roles: ['Admin'],
        }),
      });
    });

    await page.route('**/api/admin/roles/available-scopes', async (route) => {
      await route.fulfill({ contentType: 'application/json', body: JSON.stringify(availableScopes) });
    });

    await page.route('**/api/admin/roles', async (route: Route) => {
      if (route.request().method() === 'GET') {
        await route.fulfill({ contentType: 'application/json', body: JSON.stringify(roles) });
        return;
      }

      if (route.request().method() === 'POST') {
        const body = route.request().postDataJSON() as { name: string; description?: string };
        const newRole = {
          id: `role-${body.name.toLowerCase()}`,
          name: body.name,
          description: body.description ?? null,
          isActive: true,
          assignedScopes: [],
          createdAt: '2024-01-04T00:00:00Z',
          updatedAt: null,
        };

        roleDetails.set(newRole.id, newRole);
        roles = [{ ...newRole, assignedScopeCount: 0 }, ...roles];

        await route.fulfill({ status: 201, contentType: 'application/json', body: JSON.stringify(newRole) });
        return;
      }

      await route.continue();
    });

    await page.route('**/api/admin/roles/*', async (route) => {
      const url = new URL(route.request().url());
      const pathParts = url.pathname.split('/').filter(Boolean);
      const roleId = pathParts[3];
      const isScopesUpdate = pathParts[4] === 'scopes';

      if (!roleId || !roleDetails.has(roleId)) {
        await route.fulfill({ status: 404, contentType: 'application/json', body: '{}' });
        return;
      }

      if (route.request().method() === 'GET') {
        await route.fulfill({ contentType: 'application/json', body: JSON.stringify(roleDetails.get(roleId)) });
        return;
      }

      if (route.request().method() === 'PUT' && isScopesUpdate) {
        const body = route.request().postDataJSON() as { scopeNames: string[] };
        const detail = { ...roleDetails.get(roleId), assignedScopes: [...body.scopeNames].sort(), updatedAt: '2024-01-05T00:00:00Z' };
        roleDetails.set(roleId, detail);
        roles = roles.map((r) => r.id === roleId ? { ...r, assignedScopes: detail.assignedScopes, assignedScopeCount: detail.assignedScopes.length } : r);
        await route.fulfill({ contentType: 'application/json', body: JSON.stringify(detail) });
        return;
      }

      if (route.request().method() === 'PUT') {
        const body = route.request().postDataJSON() as { description?: string; isActive: boolean };
        const detail = {
          ...roleDetails.get(roleId),
          description: body.description ?? null,
          isActive: body.isActive,
          updatedAt: '2024-01-05T00:00:00Z',
        };
        roleDetails.set(roleId, detail);
        roles = roles.map((r) => r.id === roleId ? { ...r, description: detail.description, isActive: detail.isActive } : r);
        await route.fulfill({ contentType: 'application/json', body: JSON.stringify(detail) });
        return;
      }

      await route.continue();
    });

    await page.addInitScript(() => {
      localStorage.setItem('accessToken', 'mock-token');
      localStorage.setItem('refreshToken', 'mock-refresh-token');
      localStorage.setItem('grantedScopes', 'openid profile email offline_access roles admin.roles');
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

  test('renders roles page and loads role details', async ({ page }) => {
    await page.goto(`${BASE_URL}/users/roles`);
    await page.waitForLoadState('networkidle');
    await dismissNextOverlay(page);

    await expect(page.getByTestId('roles-permissions-page')).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Roles & Permissions' })).toBeVisible();
    await expect(page.getByTestId('role-item-Admin')).toBeVisible();
    await expect(page.getByTestId('role-details-panel')).toBeVisible();
    await expect(page.getByTestId('role-scope-editor')).toBeVisible();
    await expect(page.getByText('Read Orders')).toBeVisible();
  });

  test('updates role permissions via save action', async ({ page }) => {
    await page.goto(`${BASE_URL}/users/roles`);
    await page.waitForLoadState('networkidle');
    await dismissNextOverlay(page);

    await page.getByTestId('role-item-User').click();
    await page.waitForTimeout(300);

    const manageOrdersCheckbox = page.locator('label:has-text("Manage Orders") input[type="checkbox"]').first();
    await manageOrdersCheckbox.check();

    const saveButton = page.getByTestId('save-role-scopes-button');
    await expect(saveButton).toBeEnabled();
    await saveButton.click();

    await expect(page.getByText('Role permissions saved')).toBeVisible();
  });
});
