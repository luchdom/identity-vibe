import { test, expect } from '@playwright/test';
import type { Page, Route } from '@playwright/test';

const BASE_URL = process.env.PLAYWRIGHT_BASE_URL ?? 'http://localhost:3000';

const dismissNextOverlay = async (page: Page) => {
  await page.addStyleTag({
    content: '[data-nextjs-dev-overlay], nextjs-portal { display: none !important; pointer-events: none !important; }',
  }).catch(() => {});

  await page.evaluate(() => {
    document.querySelectorAll('[data-nextjs-dev-overlay], nextjs-portal').forEach((el) => el.remove());
  }).catch(() => {});
};

test.describe('New Order Page', () => {
  test.beforeEach(async ({ page }) => {
    await page.route('**/connect/userinfo', async (route) => {
      await route.fulfill({
        contentType: 'application/json',
        body: JSON.stringify({
          sub: 'user-1',
          name: 'Admin User',
          email: 'admin@example.com',
          roles: ['Admin'],
        }),
      });
    });

    await page.route((url) => {
      const href = typeof url === 'string' ? url : url.href;
      return href.includes('/orders');
    }, async (route: Route) => {
      const request = route.request();
      const url = request.url();

      if (request.resourceType() === 'document') {
        return route.continue();
      }

      if (request.method() === 'POST' && /\/orders$/.test(new URL(url).pathname)) {
        await route.fulfill({
          status: 201,
          contentType: 'application/json',
          body: JSON.stringify({
            order: {
              id: 101,
              orderNumber: 'ORD-101',
              status: 0,
              statusDisplay: 'Draft',
              customer: { name: 'John Doe', email: 'john@example.com' },
              items: [],
              subtotalAmount: 20,
              taxAmount: 0,
              shippingAmount: 0,
              discountAmount: 0,
              totalAmount: 20,
              currency: 'USD',
              createdAt: new Date().toISOString(),
              updatedAt: new Date().toISOString(),
              itemCount: 1,
              canBeCancelled: true,
              canBeModified: true,
              statusHistory: [],
            },
          }),
        });
        return;
      }

      if (request.method() === 'GET' && /\/orders$/.test(new URL(url).pathname)) {
        await route.fulfill({
          contentType: 'application/json',
          body: JSON.stringify({
            orders: [],
            totalCount: 0,
            pageNumber: 1,
            pageSize: 20,
            totalPages: 0,
            hasPreviousPage: false,
            hasNextPage: false,
          }),
        });
        return;
      }

      await route.continue();
    });

    await page.addInitScript(() => {
      localStorage.setItem('accessToken', 'mock-token');
      localStorage.setItem('refreshToken', 'mock-refresh');
      localStorage.setItem('grantedScopes', 'openid profile email orders.read orders.write');
      localStorage.setItem('user', JSON.stringify({
        id: 'admin-id',
        email: 'admin@example.com',
        firstName: 'Admin',
        lastName: 'User',
        roles: ['Admin'],
        isActive: true,
        createdAt: '2024-01-01T00:00:00Z'
      }));
    });
  });

  test('blocks submit when required fields are missing', async ({ page }) => {
    await page.goto(`${BASE_URL}/orders/new`);
    await page.waitForLoadState('networkidle');
    await dismissNextOverlay(page);

    await expect(page.getByTestId('new-order-form')).toBeVisible();
    await expect(page.locator('[data-sidebar=\"menu-sub-button\"][data-active=\"true\"]', { hasText: 'All Orders' })).toBeVisible();
    await page.getByTestId('submit-new-order-button').click();

    await expect(page.getByText('Customer name is required.')).toBeVisible();
    await expect(page.getByText('Customer email is required.')).toBeVisible();
  });

  test('creates order and redirects to orders list', async ({ page }) => {
    await page.goto(`${BASE_URL}/orders/new`);
    await page.waitForLoadState('networkidle');
    await dismissNextOverlay(page);

    await page.getByTestId('customer-name').fill('John Doe');
    await page.getByTestId('customer-email').fill('john@example.com');
    await page.getByTestId('item-name-0').fill('Widget');
    await page.getByTestId('item-quantity-0').fill('2');
    await page.getByTestId('item-unitprice-0').fill('10');

    await page.getByTestId('submit-new-order-button').click();

    await expect(page).toHaveURL(/\/orders(\?|$)/);
  });

  test('redirects or blocks when user lacks create scope', async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem('grantedScopes', 'openid profile email orders.read');
    });

    await page.goto(`${BASE_URL}/orders/new`);
    await page.waitForLoadState('networkidle');
    await dismissNextOverlay(page);

    await expect(page.getByText(/Access Denied|Redirecting/i)).toBeVisible();
  });
});
