import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { describe, expect, it } from "vitest";
import { PERMISSIONS } from "../../constants/permissions";
import { buildTopNavItems } from "./topNavItems";

describe("Market Pulse permission gates", () => {
  it("shows the top-nav link only with the catalog-view permission", () => {
    const withoutPermission = buildTopNavItems({ permissions: [] }, "/portfolio");
    const withPermission = buildTopNavItems({ permissions: [PERMISSIONS.MARKET_PULSE_VIEW_CATALOG] }, "/portfolio");
    expect(withoutPermission.some((item) => item.path === "/market-pulse")).toBe(false);
    expect(withPermission.some((item) => item.path === "/market-pulse")).toBe(true);
  });

  it("keeps public and admin routes aligned with their backend permissions", () => {
    const appSource = readFileSync(resolve("src/App.jsx"), "utf8");
    const adminLayoutSource = readFileSync(resolve("src/layouts/AdminLayout.jsx"), "utf8");
    expect(appSource).toContain("anyPermissions={[PERMISSIONS.MARKET_PULSE_VIEW_CATALOG]}");
    expect(appSource).toContain("anyPermissions={[PERMISSIONS.MARKET_PULSE_MANAGE_ANY]}");
    expect(adminLayoutSource).toContain("requiredPermissions: [PERMISSIONS.MARKET_PULSE_MANAGE_ANY]");
  });
});
