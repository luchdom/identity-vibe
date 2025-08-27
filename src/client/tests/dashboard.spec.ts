import { test, expect } from '@playwright/test';

test.describe('Dashboard', () => {
  test.beforeEach(async ({ page }) => {
    // Mock authentication state
    await page.addInitScript(() => {
      // Set up localStorage with mock auth data
      localStorage.setItem('accessToken', 'mock-access-token');
      localStorage.setItem('refreshToken', 'mock-refresh-token');
      localStorage.setItem('user', JSON.stringify({
        id: 'test-user-id',
        email: 'test@example.com',
        name: 'Test User'
      }));
    });
  });

  test('should redirect unauthenticated users to login', async ({ page }) => {
    // Clear auth data
    await page.addInitScript(() => {
      localStorage.clear();
    });
    
    await page.goto('/dashboard');
    await expect(page.url()).toContain('/login');
  });

  test('should display dashboard layout for authenticated users', async ({ page }) => {
    // Mock successful API responses
    await page.route('**/auth/user', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 'test-user-id',
          email: 'test@example.com',
          name: 'Test User'
        })
      });
    });

    await page.route('**/data/info', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          message: 'API connection successful',
          timestamp: new Date().toISOString(),
          userInfo: { id: 'test-user-id' }
        })
      });
    });

    await page.goto('/dashboard');
    
    // Check for dashboard elements
    await expect(page.locator('h1')).toContainText('Welcome back, Test User!');
    await expect(page.locator('text=Admin Dashboard')).toBeVisible();
    
    // Check for stats cards
    await expect(page.locator('text=Total Users')).toBeVisible();
    await expect(page.locator('text=Active Sessions')).toBeVisible();
    await expect(page.locator('text=System Status')).toBeVisible();
    
    // Check for user info section
    await expect(page.locator('text=User Information')).toBeVisible();
    await expect(page.locator('text=test@example.com')).toBeVisible();
  });

  test('should handle API errors gracefully', async ({ page }) => {
    // Mock auth user response
    await page.route('**/auth/user', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 'test-user-id',
          email: 'test@example.com',
          name: 'Test User'
        })
      });
    });

    // Mock API error
    await page.route('**/data/info', (route) => {
      route.fulfill({
        status: 500,
        contentType: 'application/json',
        body: JSON.stringify({ message: 'Internal Server Error' })
      });
    });

    await page.goto('/dashboard');
    
    // Should still show dashboard but handle API error
    await expect(page.locator('text=No data available')).toBeVisible();
  });

  test('should show user menu and allow logout', async ({ page }) => {
    // Mock auth response
    await page.route('**/auth/user', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 'test-user-id',
          email: 'test@example.com',
          name: 'Test User'
        })
      });
    });

    // Mock logout response
    await page.route('**/auth/logout', (route) => {
      route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ success: true })
      });
    });

    await page.goto('/dashboard');
    
    // Click on user avatar to open menu
    await page.click('button[role="button"] >> text=T'); // Avatar with "T" initial
    
    // Check if dropdown menu appears
    await expect(page.locator('text=Log out')).toBeVisible();
    
    // Click logout
    await page.click('text=Log out');
    
    // Should redirect to login
    await expect(page.url()).toContain('/login');
  });
});