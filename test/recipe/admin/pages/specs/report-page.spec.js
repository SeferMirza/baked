import { expect, test } from "@nuxt/test-utils/playwright";
import primevue from "~/utils/locators/primevue";

test.beforeEach(async({goto}) => {
  await goto("/specs/report-page", { waitUntil: "hydration" });
});

test.describe("Base", () => {
  const id = "Base";

  test("title", async({page}) => {
    const component = page.getByTestId(id);

    await expect(component.locator("h1")).toHaveText("TITLE");
  });

  test("description", async({page}) => {
    const component = page.getByTestId(id);

    await expect(component.getByTestId("description")).toHaveText("DESCRIPTION");
  });

  test("tabs", async({page}) => {
    const component = page.getByTestId(id);

    const tabs = component.locator(primevue.tab.base);
    await expect(tabs).toHaveCount(2);
    await expect(tabs.nth(0).locator("span").last()).toHaveText("TAB 1");
    await expect(tabs.nth(1).locator("span").last()).toHaveText("TAB 2");
  });

  test("tab icons", async({page}) => {
    const component = page.getByTestId(id);

    await expect(component.getByTestId("icon 1")).toHaveText("I.");
    await expect(component.getByTestId("icon 2")).toHaveText("II.");
  });

  test("tab content", async({page}) => {
    const component = page.getByTestId(id);

    await expect(component.getByTestId("content 1.1")).toHaveText("CONTENT 1.1");
    await expect(component.getByTestId("content 1.2")).toHaveText("CONTENT 1.2");

    await component.locator(primevue.tab.base).nth(1).click(); // switch to tab 2

    await expect(component.getByTestId("content 2.1")).toHaveText("CONTENT 2.1");
    await expect(component.getByTestId("content 2.2")).toHaveText("CONTENT 2.2");
  });

  test("tab content is deferred", async({page}) => {
    const component = page.getByTestId(id);

    await expect(component.getByTestId("content 2.1")).not.toBeAttached();
    await expect(component.getByTestId("content 2.2")).not.toBeAttached();
  });

  test("grid added", async({page}) => {
    const component = page.getByTestId(id);

    await expect(component.locator(".grid")).toBeAttached();
  });

  test("visual", { tag: "@visual" }, async({page}) => {
    const component = page.getByTestId(id);

    await expect(component).toHaveScreenshot();
  });
});

test.describe("Full Page", () => {
  const id = "Full Page";

  test("grid not added", async({page}) => {
    const component = page.getByTestId(id);

    await expect(component.locator(".grid")).not.toBeAttached();
  });

  test("visual", { tag: "@visual" }, async({page}) => {
    const component = page.getByTestId(id);

    await expect(component).toHaveScreenshot();
  });
});

test.describe("Narrow", () => {
  const id = "Narrow";

  test("visual", { tag: "@visual" }, async({page}) => {
    const component = page.getByTestId(id);

    await expect(component).toHaveScreenshot();
  });
});
