import { test, expect } from '@playwright/test';
import type { Page } from '@playwright/test';

const BASE_URL = process.env.PLAYWRIGHT_BASE_URL ?? 'http://localhost:3000';

const dismissNextOverlay = async (page: Page) => {
  await page.addStyleTag({
    content: '[data-nextjs-dev-overlay], nextjs-portal { display: none !important; pointer-events: none !important; }',
  }).catch(() => {});

  await page
    .evaluate(() => {
      document
        .querySelectorAll('[data-nextjs-dev-overlay], nextjs-portal')
        .forEach((el) => el.remove());
    })
    .catch(() => {});
};

test.describe('Order Detail Page', () => {
  test.beforeEach(async ({ page }) => {
    await page.addInitScript(() => {
      localStorage.setItem('accessToken', 'mock-token');
      localStorage.setItem('refreshToken', 'mock-refresh');
      localStorage.setItem('grantedScopes', 'openid profile email orders.read orders.write orders.manage');
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

    // Mock order detail using backend-real field names (lineTotal/productSku/fullName/etc.)
    await page.route('**/orders/1', async route => {
      if (route.request().resourceType() === 'document') {
        return route.continue();
      }

      await route.fulfill({
        contentType: 'application/json',
        body: JSON.stringify({
          id: 1,
          orderNumber: 'ORD-001',
          status: 1,
          statusDisplay: 'Confirmed',
          customer: {
            id: 10,
            firstName: 'John',
            lastName: 'Doe',
            fullName: 'John Doe',
            email: 'john@example.com',
            phone: '555-0100',
            address: '123 Main St',
            city: 'Austin',
            state: 'TX',
            postalCode: '78701',
            country: 'USA',
            fullAddress: '123 Main St, Austin, TX 78701, USA'
          },
          items: [
            {
              id: 101,
              productSku: 'SKU-101',
              productName: 'Wireless Keyboard',
              displayName: 'Wireless Keyboard',
              productDescription: 'Compact keyboard',
              quantity: 2,
              unitPrice: 50.0,
              discountAmount: 0,
              lineTotal: 100.0
            }
          ],
          subtotalAmount: 100.0,
          taxAmount: 8.0,
          shippingAmount: 5.0,
          discountAmount: 0,
          totalAmount: 113.0,
          currency: 'USD',
          shipping: {
            address: '123 Main St',
            city: 'Austin',
            state: 'TX',
            postalCode: '78701',
            country: 'USA',
            shippingMethod: null
          },
          createdAt: '2024-01-01T00:00:00Z',
          updatedAt: '2024-01-02T00:00:00Z',
          itemCount: 1,
          canBeCancelled: true,
          canBeModified: true,
          statusHistory: [
            {
              id: 500,
              fromStatus: 0,
              toStatus: 1,
              statusChange: 'Draft → Confirmed',
              notes: 'Confirmed by admin',
              changedByUserId: 'admin-id',
              changedByUserName: 'Admin User',
              createdAt: '2024-01-02T00:00:00Z'
            }
          ]
        })
      });
    });
  });

  test('renders backend order detail payload without crashing and maps fields correctly', async ({ page }) => {
    await page.goto(`${BASE_URL}/orders/1`);
    await page.waitForLoadState('networkidle');
    await dismissNextOverlay(page);

    await expect(page.getByText('Order #ORD-001')).toBeVisible();
    await expect(page.getByText('John Doe')).toBeVisible();
    await expect(page.getByText('SKU: SKU-101')).toBeVisible();
    await expect(page.getByText('$100.00')).toBeVisible();
    await expect(page.getByText('2 × $50.00')).toBeVisible();
    await expect(page.getByText('Method: N/A')).toBeVisible();
    await expect(page.getByText('Draft → Confirmed')).toBeVisible();
    await expect(page.getByText('by Admin User')).toBeVisible();
  });
});
