import { expect, test, type Page } from "@playwright/test";

async function loginAsAdmin(page: Page) {
  await page.goto("/login");
  await page.getByLabel(/email/i).fill("admin@lastmile.com");
  await page.getByLabel(/password/i).fill("Admin@12345");
  await page.getByRole("button", { name: /^login$/i }).click();
  await expect(page).toHaveURL(/\/dashboard$/, { timeout: 15_000 });
}

test.describe("Depots smoke", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("depots list page renders", async ({ page }) => {
    await page.goto("/depots");

    await expect(page.getByText("Depots", { exact: true }).first()).toBeVisible({
      timeout: 15_000,
    });
    await expect(page.getByRole("button", { name: /add depot/i })).toBeVisible({
      timeout: 15_000,
    });
    await expect(page.getByRole("columnheader", { name: /depot/i })).toBeVisible();
    await expect(page.getByRole("columnheader", { name: /status/i })).toBeVisible();
  });

  test("can create a depot", async ({ page }) => {
    const depotName = `E2E Depot ${Date.now()}`;

    await page.goto("/depots");
    await page.getByRole("button", { name: /add depot/i }).click();

    await expect(page.getByRole("heading", { name: /new depot/i })).toBeVisible();

    await page.getByLabel(/depot name/i).fill(depotName);
    await page.getByLabel(/street address/i).fill("100 E2E Logistics Ave");
    await page.getByLabel(/^city$/i).fill("Melbourne");
    await page.getByLabel(/^state$/i).fill("VIC");
    await page.getByLabel(/postal code/i).fill("3000");
    await page.getByLabel(/country code/i).fill("AU");
    await page.getByRole("button", { name: /create depot/i }).click();

    const createdRow = page.locator("tbody tr", { hasText: depotName }).first();
    await expect(createdRow).toBeVisible({ timeout: 15_000 });
    await expect(createdRow).toContainText("Melbourne");
    await expect(createdRow).toContainText("Active");
  });
});
