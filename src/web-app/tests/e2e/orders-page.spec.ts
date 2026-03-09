import { expect, test } from '@playwright/test';

import { setupAuthenticatedSession } from './helpers/e2e-auth';

function buildOrdersListResponse(status?: string | null) {
  const now = new Date().toISOString();
  const isShipped = status === '3';
  const isDraft = status === '0';

  return {
    orders: [
      {
        id: isShipped ? 3003 : isDraft ? 1000 : 1001,
        orderNumber: isShipped ? 'ORD-3003' : isDraft ? 'ORD-1000' : 'ORD-1001',
        status: isShipped ? 3 : isDraft ? 0 : 1,
        statusDisplay: isShipped ? 'Shipped' : isDraft ? 'Draft' : 'Confirmed',
        customerName: isShipped ? 'Grace Hopper' : 'Ada Lovelace',
        customerEmail: isShipped ? 'grace@example.com' : 'ada@example.com',
        totalAmount: isShipped ? 249.99 : 129.5,
        currency: 'USD',
        itemCount: isShipped ? 4 : 2,
        createdAt: now,
        updatedAt: now,
        canBeCancelled: !isShipped,
        canBeModified: !isShipped,
      },
    ],
    totalCount: 1,
    pageNumber: 1,
    pageSize: 10,
    totalPages: 1,
    hasPreviousPage: false,
    hasNextPage: false,
  };
}

test.describe('Orders page structure and filters', () => {
  test.beforeEach(async ({ page }) => {
    await setupAuthenticatedSession(page, 'orders.manage orders.read profile openid');
  });

  test('renders toolbar controls and overflow-only row actions', async ({ page }) => {
    await page.route('**/orders/all**', async (route) => {
      const requestUrl = new URL(route.request().url());
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(buildOrdersListResponse(requestUrl.searchParams.get('status'))),
      });
    });

    await page.goto('/orders');

    await expect(page.getByTestId('orders-table-toolbar')).toBeVisible();
    await expect(page.getByTestId('orders-status-filter-trigger')).toBeVisible();
    await expect(page.getByTestId('orders-clear-filters')).toBeVisible();
    await expect(page.getByTestId('orders-refresh-button')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Refresh' })).toHaveCount(1);
    await expect(page.getByRole('tab')).toHaveCount(0);
    await expect(page.getByRole('button', { name: /Open order /i })).toHaveCount(0);

    const actionButton = page.getByRole('button', { name: /Open actions for order/i }).first();
    await actionButton.click();

    await expect(page.getByRole('menuitem', { name: 'View details' })).toBeVisible();
  });

  test('updates status from the toolbar and clears back to the default state without count requests', async ({ page }) => {
    let countRequestCount = 0;

    await page.route('**/orders/all**', async (route) => {
      const requestUrl = new URL(route.request().url());
      if (requestUrl.searchParams.get('pageSize') === '1') {
        countRequestCount += 1;
      }

      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(buildOrdersListResponse(requestUrl.searchParams.get('status'))),
      });
    });

    await page.goto('/orders');

    await expect(page.getByText('#ORD-1001')).toBeVisible();
    await expect(page.getByTestId('orders-clear-filters')).toBeDisabled();

    await page.getByTestId('orders-status-filter-trigger').click();
    await page.getByRole('option', { name: 'Shipped' }).click();

    await expect(page).toHaveURL(/status=3/);
    await expect(page.getByText('#ORD-3003')).toBeVisible();
    await expect(page.getByTestId('orders-clear-filters')).toBeEnabled();

    await page.getByTestId('orders-clear-filters').click();

    await expect(page).not.toHaveURL(/status=/);
    await expect(page.getByText('#ORD-1001')).toBeVisible();
    expect(countRequestCount).toBe(0);
  });
});
