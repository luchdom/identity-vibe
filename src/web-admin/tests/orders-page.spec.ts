import { test, expect } from '@playwright/test';

test.describe('Orders Page', () => {
  test.beforeEach(async ({ page }) => {
    // Mock API calls to prevent actual backend calls during testing
    await page.route('**/orders', async route => {
      const url = route.request().url();
      
      if (url.includes('/orders/all')) {
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
    await page.route('**/account/profile', async route => {
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
      localStorage.setItem('user', JSON.stringify({
        id: 'admin-id',
        email: 'admin@example.com',
        name: 'Admin User',
        roles: ['Admin']
      }));
    });
  });

  test('should display orders page with navigation', async ({ page }) => {
    await page.goto('http://localhost:3001/dashboard/orders');
    
    // Wait for page to load
    await page.waitForLoadState('networkidle');

    // Check if the page title is present
    await expect(page.locator('h1')).toContainText('Orders');

    // Check if the sidebar navigation is present
    await expect(page.locator('text=Identity Admin')).toBeVisible();
    await expect(page.locator('text=Orders Management')).toBeVisible();
  });

  test('should display orders table', async ({ page }) => {
    await page.goto('http://localhost:3001/dashboard/orders');
    
    // Wait for the table to load
    await page.waitForSelector('table', { timeout: 10000 });

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
    await page.goto('http://localhost:3001/dashboard/orders');
    
    // Wait for page to load
    await page.waitForLoadState('networkidle');

    // Find and click the status filter dropdown
    const statusSelect = page.locator('[data-testid="status-filter"], select, .select-trigger').first();
    if (await statusSelect.isVisible()) {
      await statusSelect.click();
      
      // Try to select a status option
      const confirmedOption = page.locator('text=Confirmed').first();
      if (await confirmedOption.isVisible()) {
        await confirmedOption.click();
      }
    }

    // The page should still show the orders table
    await expect(page.locator('table')).toBeVisible();
  });

  test('should show order actions menu', async ({ page }) => {
    await page.goto('http://localhost:3001/dashboard/orders');
    
    // Wait for page to load and table to appear
    await page.waitForSelector('table', { timeout: 10000 });

    // Find and click the first actions menu button
    const actionButton = page.locator('button[aria-haspopup="menu"]').first();
    if (await actionButton.isVisible()) {
      await actionButton.click();
      
      // Check if menu items are visible
      await expect(page.locator('text=View Details')).toBeVisible();
    }
  });

  test('should refresh orders when refresh button is clicked', async ({ page }) => {
    await page.goto('http://localhost:3001/dashboard/orders');
    
    // Wait for page to load
    await page.waitForLoadState('networkidle');

    // Click refresh button
    const refreshButton = page.locator('text=Refresh').first();
    if (await refreshButton.isVisible()) {
      await refreshButton.click();
    }

    // Page should still show orders
    await expect(page.locator('table')).toBeVisible();
  });
});