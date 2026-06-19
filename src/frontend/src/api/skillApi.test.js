import { beforeEach, describe, expect, it, vi } from "vitest";

import axiosClient from "./axiosClient";
import { skillApi } from "./skillApi";

vi.mock("./axiosClient", () => ({
  default: {
    get: vi.fn(),
  },
}));

describe("skillApi", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("passes ranking, category, pagination, and cancellation options", async () => {
    const controller = new AbortController();

    axiosClient.get.mockResolvedValue({
      data: {
        items: [],
        totalCount: 0,
        limit: 20,
        offset: 0,
        hasMore: false,
      },
    });

    await skillApi.searchSkills({
      search: "sql",
      category: "Database",
      sort: "popular",
      limit: 20,
      offset: 0,
      signal: controller.signal,
    });

    expect(axiosClient.get).toHaveBeenCalledWith("/skills", {
      params: {
        search: "sql",
        category: "Database",
        sort: "popular",
        limit: 20,
        offset: 0,
      },
      signal: controller.signal,
    });
  });
});
