import { test, expect } from "@playwright/test";

test.describe("Authentication Flow", () => {
  test.beforeEach(async ({ page }) => {
    // Clear localStorage and cookies before each test
    await page.goto("/");
    await page.evaluate(() => localStorage.clear());
    await page.context().clearCookies();
  });

  test("should redirect to login when not authenticated", async ({ page }) => {
    await page.goto("/dashboard");
    await expect(page).toHaveURL(/.*login/);
  });

  test("should show login page", async ({ page }) => {
    await page.goto("/login");

    // Verify that the login page has loaded
    await expect(page).toHaveURL(/.*login/);

    // Check for main elements (using more flexible selectors)
    const emailInput = page.locator('input[type="email"], input[name="email"]').first();
    const passwordInput = page.locator('input[type="password"], input[name="password"]').first();

    await expect(emailInput).toBeVisible({ timeout: 10000 });
    await expect(passwordInput).toBeVisible({ timeout: 10000 });
  });

  test("should have login button", async ({ page }) => {
    await page.goto("/login");

    // Search for login button by various text options
    const loginButton = page.getByRole("button").filter({
      hasText: /login|sign in/i
    }).first();

    await expect(loginButton).toBeVisible({ timeout: 10000 });
  });

  test("should navigate to forgot password and submit reset request", async ({ page }) => {
    await page.goto("/login");

    await page.getByRole("link", { name: /forgot password/i }).click();
    await expect(page).toHaveURL(/.*forgot-password/);

    await page.getByLabel(/email/i).fill("admin@lastmile.com");
    await page.getByRole("button", { name: /send reset link/i }).click();

    await expect(page.getByText(/if an account exists for/i)).toBeVisible({
      timeout: 5000,
    });
  });

  test("should redirect to dashboard after successful login", async ({ page }) => {
    await page.goto("/login");

    // Fill in valid credentials
    await page.fill('input[type="email"]', "admin@lastmile.com");
    await page.fill('input[type="password"]', "Admin@12345");

    // Click login button
    await page.click('button[type="submit"]');

    // Should redirect to dashboard
    await expect(page).toHaveURL(/.*dashboard/, { timeout: 10000 });
  });

  test("should show error message for invalid credentials", async ({ page }) => {
    await page.goto("/login");

    // Fill in invalid credentials
    await page.fill('input[type="email"]', "invalid@example.com");
    await page.fill('input[type="password"]', "wrongpassword");

    // Click login button
    await page.click('button[type="submit"]');

    // Should show error toast/message
    await expect(page.getByText(/invalid email or password/i)).toBeVisible({ timeout: 5000 });

    // Should remain on login page
    await expect(page).toHaveURL(/.*login/);
  });

  test("should logout and redirect to login", async ({ page }) => {
    // First login with valid credentials
    await page.goto("/login");
    await page.fill('input[type="email"]', "admin@lastmile.com");
    await page.fill('input[type="password"]', "Admin@12345");
    await page.click('button[type="submit"]');
    await expect(page).toHaveURL(/.*dashboard/, { timeout: 10000 });

    // Click logout button
    await page.getByRole("button", { name: /^logout$/i }).click({ timeout: 5000 });

    // Should redirect to login page
    await expect(page).toHaveURL(/.*login/, { timeout: 5000 });
  });
});
