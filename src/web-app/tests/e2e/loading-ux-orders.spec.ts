import { expect, test } from '@playwright/test';

import { delay, setupAuthenticatedSession } from './helpers/e2e-auth';

type MockOrdersResponseParams = {
  orderNumber: string;
  totalCount?: number;
};

function buildOrdersListResponse({ orderNumber, totalCount = 1 }: MockOrdersResponseParams) {
  const now = new Date().toISOString();

  return {
    orders: [
      {
        id: Number(orderNumber.replace(/\D/g, '')) || 1001,
        orderNumber,
        status: 1,
        statusDisplay: 'Confirmed',
        customerName: 'Ada Lovelace',
        customerEmail: 'ada@example.com',
        totalAmount: 129.5,
        currency: 'USD',
        itemCount: 2,
        createdAt: now,
        updatedAt: now,
        canBeCancelled: true,
        canBeModified: true,
      },
    ],
    totalCount,
    pageNumber: 1,
    pageSize: 10,
    totalPages: 1,
    hasPreviousPage: false,
    hasNextPage: false,
  };
}

test.describe('Orders loading UX', () => {
  test('shows delayed loading hint for slow first load', async ({ page }) => {
    await setupAuthenticatedSession(page, 'orders.manage orders.read profile openid');

    await page.route('**/orders/all**', async (route) => {
      const requestUrl = new URL(route.request().url());
      const pageSize = requestUrl.searchParams.get('pageSize');
      const status = requestUrl.searchParams.get('status');

      if (pageSize === '10') {
        await delay(1400);
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(buildOrdersListResponse({ orderNumber: 'ORD-1001' })),
        });
        return;
      }

      const totalCount = status && status !== '1' ? 0 : 1;
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(buildOrdersListResponse({ orderNumber: 'ORD-1001', totalCount })),
      });
    });

    await page.goto('/orders');

    await expect(page.getByText('Loading orders...')).toBeVisible();
    await expect(page.getByText('#ORD-1001')).toBeVisible();
  });

  test('keeps current table visible while refetching', async ({ page }) => {
    await setupAuthenticatedSession(page, 'orders.manage orders.read profile openid');

    let listRequestCount = 0;

    await page.route('**/orders/all**', async (route) => {
      const requestUrl = new URL(route.request().url());
      const pageSize = requestUrl.searchParams.get('pageSize');

      if (pageSize !== '10') {
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(buildOrdersListResponse({ orderNumber: 'ORD-1001', totalCount: 1 })),
        });
        return;
      }

      if (listRequestCount === 0) {
        listRequestCount += 1;
        await route.fulfill({
          status: 200,
          contentType: 'application/json',
          body: JSON.stringify(buildOrdersListResponse({ orderNumber: 'ORD-1001' })),
        });
        return;
      }

      await delay(1450);
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(buildOrdersListResponse({ orderNumber: 'ORD-2002' })),
      });
    });

    await page.goto('/orders');
    await expect(page.getByText('#ORD-1001')).toBeVisible();

    await page.getByRole('button', { name: 'Refresh' }).click();

    await expect(page.getByText('#ORD-1001')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Refreshing...' })).toBeVisible();
    await expect(page.locator('.MuiLinearProgress-root').first()).toBeVisible();
    await expect(page.getByText('#ORD-2002')).toBeVisible();
  });
});
