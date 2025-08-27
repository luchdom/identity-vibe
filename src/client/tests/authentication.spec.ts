import { test, expect } from '@playwright/test';

// Test the authentication flow
test.describe('Authentication Flow', () => {
  test('should login with pre-filled admin credentials', async ({ page }) => {
    // Navigate to login page
    await page.goto('/login');
    
    // Check if form fields are pre-filled
    await expect(page.getByLabel('Email')).toHaveValue('admin@example.com');
    await expect(page.getByLabel('Password')).toHaveValue('Admin123!');
    
    // Check if theme toggle is visible
    await expect(page.getByLabel('Toggle theme')).toBeVisible();
    
    // Submit login form
    await page.getByRole('button', { name: 'Sign In' }).click();
    
    // Should redirect to dashboard
    await expect(page).toHaveURL('/dashboard');
    
    // Should display welcome message
    await expect(page.getByText('Welcome back,')).toBeVisible();
  });

  test('should redirect unauthenticated users to login', async ({ page }) => {
    // Try to access protected route directly
    await page.goto('/dashboard');
    
    // Should redirect to login page
    await expect(page).toHaveURL('/login');
  });

  test('should redirect unauthenticated users from profile page', async ({ page }) => {
    // Try to access profile page directly
    await page.goto('/profile');
    
    // Should redirect to login page
    await expect(page).toHaveURL('/login');
  });

  test('should logout successfully', async ({ page }) => {
    // Login first
    await page.goto('/login');
    await page.getByRole('button', { name: 'Sign In' }).click();
    await expect(page).toHaveURL('/dashboard');
    
    // Click logout button in header
    await page.getByRole('button', { name: 'Logout' }).click();
    
    // Should redirect to login page
    await expect(page).toHaveURL('/login');
    
    // Should show login form again
    await expect(page.getByRole('button', { name: 'Sign In' })).toBeVisible();
  });

  test('should logout from user dropdown menu', async ({ page }) => {
    // Login first
    await page.goto('/login');
    await page.getByRole('button', { name: 'Sign In' }).click();
    await expect(page).toHaveURL('/dashboard');
    
    // Click on user avatar to open dropdown
    await page.getByRole('button').filter({ hasText: 'A' }).click();
    
    // Click logout from dropdown
    await page.getByRole('menuitem', { name: 'Log out' }).click();
    
    // Should redirect to login page
    await expect(page).toHaveURL('/login');
  });

  test('should maintain authentication across page reloads', async ({ page }) => {
    // Login first
    await page.goto('/login');
    await page.getByRole('button', { name: 'Sign In' }).click();
    await expect(page).toHaveURL('/dashboard');
    
    // Reload the page
    await page.reload();
    
    // Should still be on dashboard (authenticated)
    await expect(page).toHaveURL('/dashboard');
    await expect(page.getByText('Welcome back,')).toBeVisible();
  });

  test('should switch between light and dark themes', async ({ page }) => {
    // Navigate to login page
    await page.goto('/login');
    
    // Click theme toggle
    await page.getByLabel('Toggle theme').click();
    
    // Should show theme options
    await expect(page.getByRole('menuitem', { name: 'Light' })).toBeVisible();
    await expect(page.getByRole('menuitem', { name: 'Dark' })).toBeVisible();
    await expect(page.getByRole('menuitem', { name: 'System' })).toBeVisible();
    
    // Select dark theme
    await page.getByRole('menuitem', { name: 'Dark' }).click();
    
    // Theme should change (we can check for dark class or CSS variables)
    await expect(page.locator('html')).toHaveClass(/dark/);
    
    // Login and check theme persists
    await page.getByRole('button', { name: 'Sign In' }).click();
    await expect(page).toHaveURL('/dashboard');
    
    // Theme should still be dark
    await expect(page.locator('html')).toHaveClass(/dark/);
  });
});