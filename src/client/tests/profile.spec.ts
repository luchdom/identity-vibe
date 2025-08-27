import { test, expect } from '@playwright/test';

// Test the profile page functionality
test.describe('Profile Page', () => {
  test.beforeEach(async ({ page }) => {
    // Navigate to login page
    await page.goto('/login');
    
    // Login with admin credentials (they should be pre-filled)
    await page.getByRole('button', { name: 'Sign In' }).click();
    
    // Wait for navigation to dashboard
    await expect(page).toHaveURL('/dashboard');
  });

  test('should display profile page with user information', async ({ page }) => {
    // Navigate to profile page via sidebar
    await page.getByRole('link', { name: 'Profile' }).click();
    
    // Wait for navigation
    await expect(page).toHaveURL('/profile');
    
    // Check if profile page header is visible
    await expect(page.getByRole('heading', { name: 'Profile' })).toBeVisible();
    await expect(page.getByText('Manage your account settings and personal information')).toBeVisible();
    
    // Check if user information card is present
    await expect(page.getByText('Personal Information')).toBeVisible();
    await expect(page.getByText('Your personal details and contact information')).toBeVisible();
    
    // Check if account status card is present
    await expect(page.getByText('Account Status')).toBeVisible();
    await expect(page.getByText('Account Details')).toBeVisible();
    
    // Check if form fields are present
    await expect(page.getByLabel('First Name')).toBeVisible();
    await expect(page.getByLabel('Last Name')).toBeVisible();
    await expect(page.getByLabel('Email Address')).toBeVisible();
    
    // Verify admin user data is loaded
    await expect(page.getByLabel('First Name')).toHaveValue('John');
    await expect(page.getByLabel('Last Name')).toHaveValue('Administrator');
    await expect(page.getByLabel('Email Address')).toHaveValue('admin@example.com');
  });

  test('should toggle edit mode', async ({ page }) => {
    // Navigate to profile page
    await page.getByRole('link', { name: 'Profile' }).click();
    await expect(page).toHaveURL('/profile');
    
    // Initially, fields should be disabled
    await expect(page.getByLabel('First Name')).toBeDisabled();
    await expect(page.getByLabel('Last Name')).toBeDisabled();
    
    // Click edit profile button
    await page.getByRole('button', { name: 'Edit Profile' }).click();
    
    // Fields should now be enabled
    await expect(page.getByLabel('First Name')).toBeEnabled();
    await expect(page.getByLabel('Last Name')).toBeEnabled();
    
    // Save changes button should be visible
    await expect(page.getByRole('button', { name: 'Save Changes' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Cancel' })).toBeVisible();
    
    // Cancel edit mode
    await page.getByRole('button', { name: 'Cancel' }).click();
    
    // Fields should be disabled again
    await expect(page.getByLabel('First Name')).toBeDisabled();
    await expect(page.getByLabel('Last Name')).toBeDisabled();
  });

  test('should display profile page on mobile', async ({ page }) => {
    // Set mobile viewport
    await page.setViewportSize({ width: 375, height: 667 });
    
    // Navigate to profile page
    await page.getByRole('link', { name: 'Profile' }).click();
    await expect(page).toHaveURL('/profile');
    
    // Check if sidebar trigger is visible on mobile
    await expect(page.getByLabel('Toggle sidebar')).toBeVisible();
    
    // Check if profile content is properly displayed
    await expect(page.getByRole('heading', { name: 'Profile' })).toBeVisible();
    await expect(page.getByText('Personal Information')).toBeVisible();
  });
});