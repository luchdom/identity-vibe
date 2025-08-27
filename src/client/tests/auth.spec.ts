import { test, expect } from '@playwright/test';

test.describe('Authentication', () => {
  test.beforeEach(async ({ page }) => {
    // Start from the login page
    await page.goto('/login');
  });

  test('should display login form', async ({ page }) => {
    await expect(page.locator('h1')).toContainText('Sign In');
    await expect(page.locator('input[type="email"]')).toBeVisible();
    await expect(page.locator('input[type="password"]')).toBeVisible();
    await expect(page.locator('button[type="submit"]')).toContainText('Sign In');
  });

  test('should navigate to register page', async ({ page }) => {
    await page.click('text=Sign up');
    await expect(page.url()).toContain('/register');
    await expect(page.locator('h1')).toContainText('Create Account');
  });

  test('should display register form', async ({ page }) => {
    await page.goto('/register');
    await expect(page.locator('h1')).toContainText('Create Account');
    await expect(page.locator('input[type="email"]')).toBeVisible();
    await expect(page.locator('input[type="password"]').first()).toBeVisible();
    await expect(page.locator('input[type="password"]').last()).toBeVisible();
    await expect(page.locator('button[type="submit"]')).toContainText('Create Account');
  });

  test('should navigate from register to login', async ({ page }) => {
    await page.goto('/register');
    await page.click('text=Sign in');
    await expect(page.url()).toContain('/login');
  });

  test('should show validation errors for empty login form', async ({ page }) => {
    await page.click('button[type="submit"]');
    await expect(page.locator('text=Invalid email address')).toBeVisible();
    await expect(page.locator('text=Password is required')).toBeVisible();
  });

  test('should show validation errors for invalid email', async ({ page }) => {
    await page.fill('input[type="email"]', 'invalid-email');
    await page.click('button[type="submit"]');
    await expect(page.locator('text=Invalid email address')).toBeVisible();
  });

  test('should redirect authenticated users away from login', async ({ page }) => {
    // This test assumes we can mock authentication state
    // For now, we'll skip it as it requires more complex setup
    test.skip(true, 'Requires authentication mocking setup');
  });
});