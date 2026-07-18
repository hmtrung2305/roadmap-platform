import { beforeEach, describe, expect, it, vi } from "vitest";
import axiosClient from "./axiosClient";
import { learningModuleApi } from "./learningModuleApi";

vi.mock("./axiosClient", () => ({
  default: {
    get: vi.fn(),
  },
}));

describe("learningModuleApi skill modules", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("loads published modules by skill slug", async () => {
    axiosClient.get.mockResolvedValue({
      data: {
        skillId: "skill-1",
        skillName: "PostgreSQL",
        skillSlug: "postgresql",
        isActive: true,
        modules: [{ skillModuleId: "module-1" }],
      },
    });

    const result = await learningModuleApi.getPublishedModulesBySkillSlug("postgresql");

    expect(axiosClient.get).toHaveBeenCalledWith("/learning-modules/skills/postgresql");
    expect(result.modules).toEqual([{ skillModuleId: "module-1" }]);
  });

  it("normalizes a missing modules collection to an empty array", async () => {
    axiosClient.get.mockResolvedValue({
      data: {
        skillId: "skill-1",
        skillSlug: "postgresql",
      },
    });

    const result = await learningModuleApi.getPublishedModulesBySkillSlug("postgresql");

    expect(result.modules).toEqual([]);
  });
});
