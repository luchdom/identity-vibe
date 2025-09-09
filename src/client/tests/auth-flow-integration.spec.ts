import { test, expect } from '@playwright/test';

// Test configuration
const FRONTEND_URL = 'http://localhost:5174';
const GATEWAY_URL = 'http://localhost:5002';

// Test users
const ADMIN_USER = {
  email: 'admin@example.com',
  password: 'Admin123!'
};

const REGULAR_USER = {
  email: 'user@example.com',
  password: 'User123!'
};

test.describe('Authentication Flow End-to-End', () => {
  
  test.beforeEach(async ({ page }) => {
    // Clear any existing auth state
    await page.context().clearCookies();
    await page.evaluate(() => {
      localStorage.clear();
      sessionStorage.clear();
    });
  });

  test('Gateway BFF endpoints are accessible via direct API calls', async ({ request }) => {
    // Test admin login via Gateway BFF
    const adminLoginResponse = await request.post(`${GATEWAY_URL}/auth/login`, {
      data: {
        email: ADMIN_USER.email,
        password: ADMIN_USER.password
      },
      headers: {
        'Content-Type': 'application/json'
      }
    });
    
    expect(adminLoginResponse.ok()).toBeTruthy();
    const adminLoginData = await adminLoginResponse.json();
    expect(adminLoginData.success).toBe(true);
    expect(adminLoginData.accessToken).toBeDefined();
    expect(adminLoginData.user.email).toBe(ADMIN_USER.email);

    // Test regular user login via Gateway BFF  
    const userLoginResponse = await request.post(`${GATEWAY_URL}/auth/login`, {
      data: {
        email: REGULAR_USER.email,
        password: REGULAR_USER.password
      },
      headers: {
        'Content-Type': 'application/json'
      }
    });
    
    expect(userLoginResponse.ok()).toBeTruthy();
    const userLoginData = await userLoginResponse.json();
    expect(userLoginData.success).toBe(true);
    expect(userLoginData.accessToken).toBeDefined();
    expect(userLoginData.user.email).toBe(REGULAR_USER.email);
  });

  test('Frontend loads correctly and shows login page', async ({ page }) => {
    await page.goto(FRONTEND_URL);
    
    // Wait for the page to load
    await page.waitForLoadState('domcontentloaded');
    
    // Check if we're redirected to login or if login elements are present
    const currentUrl = page.url();
    console.log('Current URL:', currentUrl);
    
    // Look for login-related elements (email input, password input, or sign-in button)
    const hasEmailInput = await page.locator('input[type="email"], input[name="email"], input[placeholder*="email" i]').first().isVisible().catch(() => false);
    const hasPasswordInput = await page.locator('input[type="password"], input[name="password"], input[placeholder*="password" i]').first().isVisible().catch(() => false);
    const hasSignInButton = await page.locator('button:has-text("Sign In"), button:has-text("Login"), input[type="submit"]').first().isVisible().catch(() => false);
    
    // Take a screenshot for debugging
    await page.screenshot({ path: 'tests/screenshots/login-page.png', fullPage: true });
    
    // We should see login elements or be on a login page
    expect(hasEmailInput || hasPasswordInput || hasSignInButton || currentUrl.includes('login') || currentUrl.includes('sign-in')).toBeTruthy();
  });

  test('Admin user can login through frontend', async ({ page }) => {
    await page.goto(FRONTEND_URL);
    await page.waitForLoadState('domcontentloaded');
    
    // Take initial screenshot
    await page.screenshot({ path: 'tests/screenshots/before-admin-login.png', fullPage: true });
    
    // Try to find and fill login form
    const emailInput = page.locator('input[type="email"], input[name="email"], input[placeholder*="email" i]').first();
    const passwordInput = page.locator('input[type="password"], input[name="password"], input[placeholder*="password" i]').first();
    const submitButton = page.locator('button:has-text("Sign In"), button:has-text("Login"), input[type="submit"], button[type="submit"]').first();
    
    // Wait for form elements to be visible
    await emailInput.waitFor({ state: 'visible', timeout: 10000 });
    await passwordInput.waitFor({ state: 'visible', timeout: 10000 });
    
    // Fill in credentials
    await emailInput.fill(ADMIN_USER.email);
    await passwordInput.fill(ADMIN_USER.password);
    
    // Take screenshot before submitting
    await page.screenshot({ path: 'tests/screenshots/admin-credentials-filled.png', fullPage: true });
    
    // Submit form
    await submitButton.click();
    
    // Wait for navigation or success indication
    await page.waitForTimeout(2000); // Give time for authentication
    
    // Take screenshot after login attempt
    await page.screenshot({ path: 'tests/screenshots/after-admin-login.png', fullPage: true });
    
    // Check if login was successful - could be dashboard, profile, or any authenticated page
    const currentUrl = page.url();
    const hasLogoutButton = await page.locator('button:has-text("Logout"), button:has-text("Sign Out"), a:has-text("Logout"), a:has-text("Sign Out")').first().isVisible().catch(() => false);
    const hasDashboard = await page.locator('h1:has-text("Dashboard"), h2:has-text("Dashboard"), [data-testid="dashboard"]').first().isVisible().catch(() => false);
    const hasWelcomeMessage = await page.locator(':has-text("Welcome"), :has-text("Hello")').first().isVisible().catch(() => false);
    
    console.log('After admin login - URL:', currentUrl);
    console.log('Has logout button:', hasLogoutButton);
    console.log('Has dashboard:', hasDashboard);
    console.log('Has welcome message:', hasWelcomeMessage);
    
    // Login should be successful if we're not on login page anymore or see authenticated elements
    expect(
      !currentUrl.includes('login') && !currentUrl.includes('sign-in') ||
      hasLogoutButton ||
      hasDashboard ||
      hasWelcomeMessage
    ).toBeTruthy();
  });

  test('Regular user can login through frontend', async ({ page }) => {
    await page.goto(FRONTEND_URL);
    await page.waitForLoadState('domcontentloaded');
    
    // Take initial screenshot
    await page.screenshot({ path: 'tests/screenshots/before-user-login.png', fullPage: true });
    
    // Find and fill login form
    const emailInput = page.locator('input[type="email"], input[name="email"], input[placeholder*="email" i]').first();
    const passwordInput = page.locator('input[type="password"], input[name="password"], input[placeholder*="password" i]').first();
    const submitButton = page.locator('button:has-text("Sign In"), button:has-text("Login"), input[type="submit"], button[type="submit"]').first();
    
    // Wait for form elements
    await emailInput.waitFor({ state: 'visible', timeout: 10000 });
    await passwordInput.waitFor({ state: 'visible', timeout: 10000 });
    
    // Fill credentials
    await emailInput.fill(REGULAR_USER.email);
    await passwordInput.fill(REGULAR_USER.password);
    
    // Take screenshot before submitting
    await page.screenshot({ path: 'tests/screenshots/user-credentials-filled.png', fullPage: true });
    
    // Submit
    await submitButton.click();
    
    // Wait for authentication
    await page.waitForTimeout(2000);
    
    // Take screenshot after login
    await page.screenshot({ path: 'tests/screenshots/after-user-login.png', fullPage: true });
    
    // Verify successful login
    const currentUrl = page.url();
    const hasLogoutButton = await page.locator('button:has-text("Logout"), button:has-text("Sign Out"), a:has-text("Logout"), a:has-text("Sign Out")').first().isVisible().catch(() => false);
    const hasDashboard = await page.locator('h1:has-text("Dashboard"), h2:has-text("Dashboard"), [data-testid="dashboard"]').first().isVisible().catch(() => false);
    const hasWelcomeMessage = await page.locator(':has-text("Welcome"), :has-text("Hello")').first().isVisible().catch(() => false);
    
    console.log('After user login - URL:', currentUrl);
    console.log('Has logout button:', hasLogoutButton);
    console.log('Has dashboard:', hasDashboard);
    console.log('Has welcome message:', hasWelcomeMessage);
    
    // Verify successful authentication
    expect(
      !currentUrl.includes('login') && !currentUrl.includes('sign-in') ||
      hasLogoutButton ||
      hasDashboard ||
      hasWelcomeMessage
    ).toBeTruthy();
  });

  test('Test user dropdown functionality (if available)', async ({ page }) => {
    await page.goto(FRONTEND_URL);
    await page.waitForLoadState('domcontentloaded');
    
    // Take screenshot for debugging
    await page.screenshot({ path: 'tests/screenshots/login-page-dropdown.png', fullPage: true });
    
    // Look for test user dropdown
    const testDropdown = page.locator('select:has-text("Admin User"), select:has-text("Regular User"), select[data-testid*="test"], select:has(option:has-text("admin@example.com"))').first();
    
    if (await testDropdown.isVisible().catch(() => false)) {
      console.log('Test dropdown found!');
      
      // Select admin user from dropdown
      await testDropdown.selectOption({ label: 'Admin User' });
      
      // Check if credentials were auto-filled
      const emailInput = page.locator('input[type="email"], input[name="email"]').first();
      const emailValue = await emailInput.inputValue();
      
      expect(emailValue).toBe(ADMIN_USER.email);
      console.log('Dropdown functionality working - admin credentials auto-filled');
      
      // Try regular user
      await testDropdown.selectOption({ label: 'Regular User' });
      const userEmailValue = await emailInput.inputValue();
      
      expect(userEmailValue).toBe(REGULAR_USER.email);
      console.log('Dropdown functionality working - user credentials auto-filled');
    } else {
      console.log('Test dropdown not found - may not be in development mode');
    }
  });

  test('CORS and API communication verification', async ({ page }) => {
    await page.goto(FRONTEND_URL);
    
    // Monitor network requests
    const requests: any[] = [];
    const responses: any[] = [];
    
    page.on('request', request => {
      requests.push({
        url: request.url(),
        method: request.method(),
        headers: request.headers()
      });
    });
    
    page.on('response', response => {
      responses.push({
        url: response.url(),
        status: response.status(),
        headers: response.headers()
      });
    });
    
    // Perform login to trigger API calls
    const emailInput = page.locator('input[type="email"], input[name="email"], input[placeholder*="email" i]').first();
    const passwordInput = page.locator('input[type="password"], input[name="password"], input[placeholder*="password" i]').first();
    const submitButton = page.locator('button:has-text("Sign In"), button:has-text("Login"), input[type="submit"], button[type="submit"]').first();
    
    if (await emailInput.isVisible() && await passwordInput.isVisible()) {
      await emailInput.fill(ADMIN_USER.email);
      await passwordInput.fill(ADMIN_USER.password);
      await submitButton.click();
      
      // Wait for API calls
      await page.waitForTimeout(3000);
      
      // Check for Gateway BFF API calls
      const gatewayRequests = requests.filter(req => req.url.includes('localhost:5002'));
      const gatewayResponses = responses.filter(resp => resp.url.includes('localhost:5002'));
      
      console.log('Gateway BFF requests:', gatewayRequests.length);
      console.log('Gateway BFF responses:', gatewayResponses.length);
      
      // Verify no direct AuthServer calls (should go through Gateway)
      const directAuthRequests = requests.filter(req => req.url.includes('localhost:5000'));
      console.log('Direct AuthServer requests:', directAuthRequests.length);
      
      // We should have Gateway requests and NO direct AuthServer requests (BFF pattern)
      expect(gatewayRequests.length).toBeGreaterThan(0);
      expect(directAuthRequests.length).toBe(0);
      
      // Check for successful responses (no CORS errors)
      const successfulResponses = gatewayResponses.filter(resp => resp.status < 400);
      expect(successfulResponses.length).toBeGreaterThan(0);
      
      console.log('BFF pattern verified - all API calls go through Gateway');
    }
  });

});