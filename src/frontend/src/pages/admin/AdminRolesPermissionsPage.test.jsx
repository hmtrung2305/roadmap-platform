import { describe, expect, it } from "vitest";

import { describePermission } from "./permissionLabels";

describe("AdminRolesPermissionsPage permission labels", () => {
  it("turns technical permission codes into useful access descriptions", () => {
    expect(describePermission("account.update.self")).toBe("Update account for own account");
    expect(describePermission("role_permission.view.any")).toBe("View role permissions platform-wide");
  });
});
