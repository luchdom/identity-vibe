import { test, expect } from '@playwright/test';
import type { Page } from '@playwright/test';

const BASE_URL = process.env.PLAYWRIGHT_BASE_URL ?? 'http://localhost:3000';
const ORDERS_ROUTE = '/orders';

const dismissNextOverlay = async (page: Page) => {
  await page.addStyleTag({
    content: '[data-nextjs-dev-overlay], nextjs-portal { display: none !important; pointer-events: none !important; }',
  }).catch(() => {});

  await page
    .evaluate(() => {
      document
        .querySelectorAll('[data-nextjs-dev-overlay], nextjs-portal')
        .forEach((el) => {
          el.remove();
        });
    })
    .catch(() => {});
};

test.describe('Orders Page', () => {
  test.beforeEach(async ({ page }) => {
    // Mock API calls to prevent actual backend calls during testing
    await page.route((url) => {
      const requestUrl = typeof url === 'string' ? url : url.href;
      return requestUrl.includes('/orders');
    }, async route => {
      if (route.request().resourceType() === 'document') {
        return route.continue();
      }
      const requestUrl = route.request().url();

      if (requestUrl.includes('/orders/all')) {
        // Mock admin orders response
        await route.fulfill({
          contentType: 'application/json',
          body: JSON.stringify({
            orders: [
              {
                id: 1,
                orderNumber: 'ORD-001',
                status: 1,
                statusDisplay: 'Confirmed',
                customerName: 'John Doe',
                customerEmail: 'john@example.com',
                totalAmount: 99.99,
                currency: 'USD',
                itemCount: 2,
                createdAt: new Date().toISOString(),
                updatedAt: new Date().toISOString(),
                canBeCancelled: true,
                canBeModified: true
              },
              {
                id: 2,
                orderNumber: 'ORD-002',
                status: 3,
                statusDisplay: 'Shipped',
                customerName: 'Jane Smith',
                customerEmail: 'jane@example.com',
                totalAmount: 149.99,
                currency: 'USD',
                itemCount: 1,
                createdAt: new Date().toISOString(),
                updatedAt: new Date().toISOString(),
                shippedAt: new Date().toISOString(),
                canBeCancelled: false,
                canBeModified: false
              }
            ],
            totalCount: 2,
            pageNumber: 1,
            pageSize: 10,
            totalPages: 1,
            hasPreviousPage: false,
            hasNextPage: false
          })
        });
      } else {
        // Mock user orders response
        await route.fulfill({
          contentType: 'application/json',
          body: JSON.stringify({
            orders: [],
            totalCount: 0,
            pageNumber: 1,
            pageSize: 10,
            totalPages: 0,
            hasPreviousPage: false,
            hasNextPage: false
          })
        });
      }
    });

    // Mock auth profile endpoint
    await page.route('**/connect/userinfo', async route => {
      if (route.request().resourceType() === 'document') {
        return route.continue();
      }
      await route.fulfill({
        contentType: 'application/json',
        body: JSON.stringify({
          id: 'admin-id',
          email: 'admin@example.com',
          firstName: 'Admin',
          lastName: 'User',
          roles: ['Admin'],
          isActive: true,
          createdAt: '2024-01-01T00:00:00Z'
        })
      });
    });
    // Mock localStorage for authentication
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

  test('should display orders page with navigation', async ({ page }) => {
    await page.goto(`${BASE_URL}${ORDERS_ROUTE}`);
    
    // Wait for page to load
    await page.waitForLoadState('networkidle');
    await dismissNextOverlay(page);

    // Check if the page title is present
    await expect(page.getByRole('heading', { level: 1, name: /Orders/i })).toBeVisible();

    // Check if the sidebar navigation is present
    await expect(page.getByText('Identity Admin', { exact: false })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Orders Management' })).toBeVisible();
    await expect(page.locator('[data-sidebar=\"menu-sub-button\"][data-active=\"true\"]', { hasText: 'All Orders' })).toBeVisible();
  });

  test('should display orders table', async ({ page }) => {
    await page.goto(`${BASE_URL}${ORDERS_ROUTE}`);
    
    // Wait for the table to load
    await page.waitForSelector('table', { timeout: 10000 });
    await dismissNextOverlay(page);

    // Check table headers
    await expect(page.locator('text=Order #')).toBeVisible();
    await expect(page.locator('text=Status')).toBeVisible();
    await expect(page.locator('text=Customer')).toBeVisible();
    await expect(page.locator('text=Total')).toBeVisible();
    await expect(page.locator('text=Items')).toBeVisible();
    await expect(page.locator('text=Date')).toBeVisible();

    // Check if orders are displayed
    await expect(page.locator('text=ORD-001')).toBeVisible();
    await expect(page.locator('text=John Doe')).toBeVisible();
    await expect(page.locator('text=ORD-002')).toBeVisible();
    await expect(page.locator('text=Jane Smith')).toBeVisible();
  });

  test('should filter orders by status', async ({ page }) => {
    await page.goto(`${BASE_URL}${ORDERS_ROUTE}`);
    
    // Wait for page to load
    await page.waitForLoadState('networkidle');
    await dismissNextOverlay(page);

    await expect(page.getByTestId('status-filter-group')).toBeVisible();
    await page.getByTestId('status-filter-shipped').click();
    await page.waitForLoadState('networkidle');

    // The page should still show the orders table
    await expect(page.locator('table')).toBeVisible();
    await expect(page).toHaveURL(/status=3/);
    await expect(page.locator('[data-sidebar=\"menu-sub-button\"][data-active=\"true\"]', { hasText: 'All Orders' })).toBeVisible();
  });

  test('should show order actions menu', async ({ page }) => {
    await page.goto(`${BASE_URL}${ORDERS_ROUTE}`);
    
    // Wait for page to load and table to appear
    await page.waitForSelector('table', { timeout: 10000 });
    await dismissNextOverlay(page);

    // Find and click the first actions menu button
    const actionButton = page.locator('table tbody button[aria-haspopup="menu"]').first();
    await expect(actionButton).toBeVisible();
    await actionButton.click();

    // Check if menu items are visible
    await expect(page.getByRole('menuitem', { name: 'View Details' })).toBeVisible();
  });

  test('should refresh orders when refresh button is clicked', async ({ page }) => {
    await page.goto(`${BASE_URL}${ORDERS_ROUTE}`);
    
    // Wait for page to load
    await page.waitForLoadState('networkidle');
    await dismissNextOverlay(page);

    // Click refresh button
    const refreshButton = page.locator('text=Refresh').first();
    if (await refreshButton.isVisible()) {
      await refreshButton.click();
    }

    // Page should still show orders
    await expect(page.locator('table')).toBeVisible();
  });

  test('should show enabled new order button when user has create scope', async ({ page }) => {
    await page.goto(`${BASE_URL}${ORDERS_ROUTE}`);
    await page.waitForLoadState('networkidle');
    await dismissNextOverlay(page);

    const newOrderButton = page.getByTestId('new-order-button');
    await expect(newOrderButton).toBeVisible();
    await expect(newOrderButton).toBeEnabled();
  });

  test('should show disabled new order button when user lacks create scopes', async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem('grantedScopes', 'openid profile email orders.read');
    });

    await page.goto(`${BASE_URL}${ORDERS_ROUTE}`);
    await page.waitForLoadState('networkidle');
    await dismissNextOverlay(page);

    const newOrderButton = page.getByTestId('new-order-button');
    await expect(newOrderButton).toBeVisible();
    await expect(newOrderButton).toBeDisabled();
  });
});
