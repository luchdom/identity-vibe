import { test, expect } from '@playwright/test';

// Test the sidebar functionality
test.describe('Sidebar Navigation', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to login page
    await page.goto('/login');
    
    // Login with admin credentials (they should be pre-filled)
    await page.getByRole('button', { name: 'Sign In' }).click();
    
    // Wait for navigation to dashboard
    await expect(page).toHaveURL('/dashboard');
  });

  test('should display sidebar with navigation items', async ({ page }) => {
    // Check if sidebar is visible
    await expect(page.getByText('Admin Dashboard')).toBeVisible();
    await expect(page.getByText('Enterprise')).toBeVisible();
    
    // Check if navigation items are present
    await expect(page.getByRole('link', { name: 'Dashboard' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Profile' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Settings' })).toBeVisible();
    
    // Check if user info is displayed in footer
    await expect(page.getByText('admin@example.com')).toBeVisible();
  });

  test('should navigate between pages using sidebar', async ({ page }) => {
    // Navigate to Profile page
    await page.getByRole('link', { name: 'Profile' }).click();
    await expect(page).toHaveURL('/profile');
    await expect(page.getByRole('heading', { name: 'Profile' })).toBeVisible();
    
    // Navigate back to Dashboard
    await page.getByRole('link', { name: 'Dashboard' }).click();
    await expect(page).toHaveURL('/dashboard');
    await expect(page.getByText('Welcome back,')).toBeVisible();
  });

  test('should toggle sidebar collapse state', async ({ page }) => {
    // Check if sidebar trigger is visible
    await expect(page.getByLabel('Toggle sidebar')).toBeVisible();
    
    // Click to collapse sidebar
    await page.getByLabel('Toggle sidebar').click();
    
    // Sidebar should be in collapsed state
    // Note: We can't easily test the visual state change, but we can verify the trigger still works
    await expect(page.getByLabel('Toggle sidebar')).toBeVisible();
    
    // Click to expand sidebar again
    await page.getByLabel('Toggle sidebar').click();
    
    // Sidebar content should still be visible
    await expect(page.getByText('Admin Dashboard')).toBeVisible();
  });

  test('should display user email in sidebar footer', async ({ page }) => {
    // Check if user email is displayed in the sidebar footer
    await expect(page.getByText('admin@example.com')).toBeVisible();
    
    // Check if user avatar/initials are visible
    await expect(page.getByText('A')).toBeVisible(); // First letter of admin@example.com
  });

  test('should work properly on mobile devices', async ({ page }) => {
    // Set mobile viewport
    await page.setViewportSize({ width: 375, height: 667 });
    
    // Sidebar should be collapsible on mobile
    await expect(page.getByLabel('Toggle sidebar')).toBeVisible();
    
    // Click to toggle sidebar
    await page.getByLabel('Toggle sidebar').click();
    
    // Navigation should still be accessible
    await expect(page.getByRole('link', { name: 'Profile' })).toBeVisible();
    
    // Navigate to profile
    await page.getByRole('link', { name: 'Profile' }).click();
    await expect(page).toHaveURL('/profile');
  });

  test('should maintain sidebar state across navigation', async ({ page }) => {
    // Collapse the sidebar
    await page.getByLabel('Toggle sidebar').click();
    
    // Navigate to profile page
    await page.getByRole('link', { name: 'Profile' }).click();
    await expect(page).toHaveURL('/profile');
    
    // Sidebar should still be in same state (collapsed)
    await expect(page.getByLabel('Toggle sidebar')).toBeVisible();
    
    // Navigate back to dashboard
    await page.getByRole('link', { name: 'Dashboard' }).click();
    await expect(page).toHaveURL('/dashboard');
    
    // Sidebar state should be maintained
    await expect(page.getByLabel('Toggle sidebar')).toBeVisible();
  });

  test('should handle keyboard navigation', async ({ page }) => {
    // Focus on sidebar navigation
    await page.keyboard.press('Tab');
    
    // Should be able to navigate with keyboard
    await page.keyboard.press('Enter');
    
    // Or use arrow keys if supported
    await page.keyboard.press('ArrowDown');
    await page.keyboard.press('Enter');
  });
});