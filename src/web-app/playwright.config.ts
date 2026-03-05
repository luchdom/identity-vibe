import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: './tests/e2e',
  timeout: 45000,
  expect: {
    timeout: 8000,
  },
  fullyParallel: false,
  retries: 0,
  reporter: 'list',
  use: {
    baseURL: 'http://localhost:3101',
    trace: 'on-first-retry',
    viewport: { width: 1440, height: 900 },
  },
  webServer: {
    command: 'npx next dev --port 3101 --hostname localhost',
    url: 'http://localhost:3101/login',
    reuseExistingServer: false,
    timeout: 120000,
    env: {
      NEXT_PUBLIC_E2E_BYPASS_AUTH: 'true',
    },
  },
});
