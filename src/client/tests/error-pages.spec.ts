import { test, expect } from '@playwright/test';

test.describe('Error Pages', () => {
  test('should display 404 page for unknown routes', async ({ page }) => {
    await page.goto('/unknown-route');
    
    await expect(page.locator('h2')).toContainText('Page Not Found');
    await expect(page.locator('text=Go Back')).toBeVisible();
    await expect(page.locator('text=Go to Dashboard')).toBeVisible();
  });

  test('should navigate back from error page', async ({ page }) => {
    await page.goto('/login');
    await page.goto('/unknown-route');
    
    await expect(page.locator('h2')).toContainText('Page Not Found');
    await page.click('text=Go Back');
    
    // Should go back to login page
    await expect(page.url()).toContain('/login');
  });

  test('should navigate to dashboard from error page', async ({ page }) => {
    await page.goto('/unknown-route');
    
    await page.click('text=Go to Dashboard');
    
    // Should redirect to login (since not authenticated) or dashboard (if authenticated)
    await expect(page.url()).toMatch(/(login|dashboard)/);
  });
});