import { expect, test, type Page } from "@playwright/test";

declare global {
  interface Window {
    __zoneMap?: {
      fire: (event: string, payload?: unknown) => void;
      project: (lngLat: [number, number]) => { x: number; y: number };
      queryRenderedFeatures: (
        geometry?: unknown,
        options?: { layers?: string[] },
      ) => Array<{
        geometry: { coordinates: number[][][] };
        properties?: Record<string, string>;
      }>;
    };
    __zoneMapDraw?: {
      deleteAll: () => void;
      add: (feature: unknown) => void;
    } | null;
  }
}

async function loginAsAdmin(page: Page) {
  await page.goto("/login");
  await page.getByLabel(/email/i).fill("admin@lastmile.com");
  await page.getByLabel(/password/i).fill("Admin@12345");
  await page.getByRole("button", { name: /^login$/i }).click();
  await expect(page).toHaveURL(/\/dashboard$/, { timeout: 15_000 });
}

async function injectDraftPolygon(page: Page) {
  await page.waitForFunction(() => Boolean(window.__zoneMap && window.__zoneMapDraw), {
    timeout: 15_000,
  });

  await page.evaluate(() => {
    const draw = window.__zoneMapDraw;
    const map = window.__zoneMap;

    if (!draw || !map) {
      throw new Error("Zone map is not ready");
    }

    draw.deleteAll();
    draw.add({
      type: "Feature",
      properties: {},
      geometry: {
        type: "Polygon",
        coordinates: [[
          [144.95, -37.82],
          [144.99, -37.82],
          [144.99, -37.78],
          [144.95, -37.78],
          [144.95, -37.82],
        ]],
      },
    });

    map.fire("draw.create", {});
  });
}

async function clickZoneOverlay(page: Page, zoneName: string) {
  const zoneLabel = page.locator(
    `[aria-label="Select zone ${zoneName}"]`,
  );

  await expect(zoneLabel).toBeVisible({ timeout: 15_000 });
  await zoneLabel.click();
}

test.describe("Zones map flows", () => {
  test.beforeEach(async ({ page }) => {
    await loginAsAdmin(page);
  });

  test("zones page renders the map-first editor", async ({ page }) => {
    await page.goto("/zones");

    await expect(page.getByRole("heading", { name: /^zones$/i })).toBeVisible();
    await expect(page.getByRole("button", { name: /add zone/i })).toBeEnabled({
      timeout: 15_000,
    });
    await expect(page.getByTestId("zones-map")).toBeVisible();
  });

  test("can create, select from the map, and delete a zone", async ({ page }) => {
    const zoneName = `E2E Zone ${Date.now()}`;

    await page.goto("/zones");
    await expect(page.getByRole("button", { name: /add zone/i })).toBeEnabled({
      timeout: 15_000,
    });

    await page.getByRole("button", { name: /add zone/i }).click();
    await page.getByLabel(/zone name/i).fill(zoneName);

    const depotSelect = page.getByLabel(/^depot$/i);
    await expect
      .poll(
        async () =>
          await depotSelect.evaluate(
            (element) => (element as HTMLSelectElement).options.length,
          ),
        { timeout: 15_000 },
      )
      .toBeGreaterThan(1);

    const selectedDepotLabel = await depotSelect.evaluate((element) => {
      const options = Array.from((element as HTMLSelectElement).options)
        .map((option) => option.text.trim())
        .filter((label, index) => index > 0 && label.length > 0);

      return options[0] ?? "";
    });

    expect(selectedDepotLabel).not.toBe("");
    await depotSelect.selectOption({ label: selectedDepotLabel });

    await injectDraftPolygon(page);
    await expect(page.getByText(/polygon ready/i)).toBeVisible();

    await page.getByRole("button", { name: /create zone/i }).click();

    const createdRow = page.locator("tbody tr", { hasText: zoneName }).first();
    await expect(createdRow).toBeVisible({ timeout: 15_000 });
    await expect(createdRow).toContainText(selectedDepotLabel);

    await clickZoneOverlay(page, zoneName);
    await expect(page.getByRole("heading", { name: new RegExp(`editing ${zoneName}`, "i") })).toBeVisible();
    await expect(page.getByLabel(/zone name/i)).toHaveValue(zoneName);

    await page.getByRole("button", { name: /^delete zone$/i }).click();
    await expect(page.getByRole("heading", { name: /delete zone/i })).toBeVisible();
    await page.getByRole("button", { name: /^delete$/i }).first().click();

    await expect(createdRow).toBeHidden({ timeout: 15_000 });
  });
});
