import { expect, test, type Locator } from "@playwright/test";

const backendUrl = "http://127.0.0.1:5100";
const supportKey = process.env.TEST_SUPPORT_KEY ?? "e2e-test-support-key";

interface UserManagementFixture {
  adminEmail: string;
  adminPassword: string;
  depotName: string;
  zoneName: string;
}

async function typeIntoField(locator: Locator, value: string) {
  await locator.click();
  await locator.press("Control+A");
  await locator.press("Delete");
  await locator.pressSequentially(value);
}

test.describe("User Management CRUD", () => {
  let fixture: UserManagementFixture;

  test.beforeEach(async ({ page, request }) => {
    const response = await request.post(
      `${backendUrl}/api/test-support/user-management/reset-and-seed`,
      {
        headers: {
          "X-Test-Support-Key": supportKey,
        },
      }
    );

    expect(response.ok()).toBeTruthy();
    fixture = (await response.json()) as UserManagementFixture;

    await page.goto("/login");
    await page.getByLabel(/email/i).fill(fixture.adminEmail);
    await page.getByLabel(/password/i).fill(fixture.adminPassword);
    await page.getByRole("button", { name: /^login$/i }).click();
    await expect(page).toHaveURL(/.*dashboard/, { timeout: 15_000 });

    await page.getByRole("link", { name: /^users$/i }).click();
    await expect(page).toHaveURL(/.*\/users$/, { timeout: 10_000 });
    await expect(page.getByRole("heading", { name: /user management/i })).toBeVisible();
    await expect(page.locator("select").nth(0)).toContainText("Dispatcher", {
      timeout: 15_000,
    });
    await expect(page.locator("select").nth(2)).toContainText(fixture.depotName, {
      timeout: 15_000,
    });
  });

  test("admin can create, edit, and deactivate a user", async ({ page }) => {
    const createdFirstName = "E2E";
    const createdLastName = "Dispatcher";
    const updatedFirstName = "E2E Updated";
    const updatedLastName = "Manager";
    const email = `e2e-user-${Date.now()}@lastmile.test`;

    await page.getByRole("button", { name: /new user/i }).click();
    const modal = page.getByTestId("user-form-modal");
    await expect(modal.getByRole("heading", { name: /create user/i })).toBeVisible();

    await typeIntoField(modal.getByLabel(/first name/i), createdFirstName);
    await typeIntoField(modal.getByLabel(/last name/i), createdLastName);
    await typeIntoField(modal.getByLabel(/^email$/i), email);
    await typeIntoField(modal.getByLabel(/phone/i), "+10000000077");
    const roleSelect = modal.getByLabel(/^role$/i);
    const depotSelect = modal.getByLabel(/^depot$/i);
    const zoneSelect = modal.getByLabel(/^zone$/i);

    await expect(roleSelect).toContainText("Dispatcher");
    await expect(depotSelect).toContainText(fixture.depotName);
    await roleSelect.selectOption({ label: "Dispatcher" });
    await depotSelect.selectOption({ label: fixture.depotName });
    await expect(zoneSelect).toContainText(fixture.zoneName);
    await zoneSelect.selectOption({ label: fixture.zoneName });
    await modal.getByRole("button", { name: /create user/i }).click();

    const createdRow = page.locator("tbody tr", { hasText: email });
    await expect(createdRow).toContainText(`${createdFirstName} ${createdLastName}`, {
      timeout: 10_000,
    });
    await expect(createdRow).toContainText("Dispatcher");
    await expect(createdRow).toContainText(fixture.depotName);
    await expect(createdRow).toContainText(fixture.zoneName);
    await expect(createdRow).toContainText("Active");

    await createdRow.getByRole("button", { name: /edit/i }).click();
    await expect(modal.getByRole("heading", { name: /edit user/i })).toBeVisible();

    await typeIntoField(modal.getByLabel(/first name/i), updatedFirstName);
    await typeIntoField(modal.getByLabel(/last name/i), updatedLastName);
    await typeIntoField(modal.getByLabel(/phone/i), "+10000000088");
    await expect(roleSelect).toContainText("Operations Manager");
    await roleSelect.selectOption({ label: "Operations Manager" });
    await modal.getByRole("button", { name: /save changes/i }).click();

    const updatedRow = page.locator("tbody tr", { hasText: email });
    await expect(updatedRow).toContainText(`${updatedFirstName} ${updatedLastName}`);
    await expect(updatedRow).toContainText("Operations Manager");
    await expect(updatedRow).toContainText("Active");

    page.once("dialog", (dialog) => {
      void dialog.accept();
    });
    await updatedRow.getByRole("button", { name: /deactivate/i }).click();

    await expect(updatedRow).toContainText("Inactive");
    await expect(
      updatedRow.getByRole("button", { name: /deactivate/i })
    ).toBeDisabled();
  });

  test("seeded system admin is visible but read-only", async ({ page }) => {
    const protectedRow = page.locator("tbody tr", { hasText: fixture.adminEmail });

    await expect(protectedRow).toContainText("System admin");
    await expect(protectedRow.getByRole("button", { name: /edit/i })).toBeDisabled();
    await expect(
      protectedRow.getByRole("button", { name: /reset email/i })
    ).toBeDisabled();
    await expect(
      protectedRow.getByRole("button", { name: /deactivate/i })
    ).toBeDisabled();
  });
});
