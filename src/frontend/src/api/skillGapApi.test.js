import { beforeEach, describe, expect, it, vi } from "vitest";

import axiosClient from "./axiosClient";
import { skillGapApi } from "./skillGapApi";

vi.mock("./axiosClient", () => ({
  default: {
    get: vi.fn(),
    post: vi.fn(),
    delete: vi.fn(),
    put: vi.fn(),
  },
}));

describe("skillGapApi", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("passes the history limit and opaque cursor to the backend", async () => {
    axiosClient.get.mockResolvedValue({
      data: {
        items: [{ skillGapAnalysisHistoryId: "history-1" }],
        nextCursor: "opaque-cursor",
        hasMore: true,
      },
    });

    const page = await skillGapApi.getHistory({
      limit: 10,
      cursor: "current-cursor",
    });

    expect(axiosClient.get).toHaveBeenCalledWith("/me/skill-gap/history", {
      params: {
        limit: 10,
        cursor: "current-cursor",
      },
    });
    expect(page).toEqual({
      items: [{ skillGapAnalysisHistoryId: "history-1" }],
      nextCursor: "opaque-cursor",
      hasMore: true,
    });
  });

  it("omits an empty cursor and normalizes an empty history page", async () => {
    axiosClient.get.mockResolvedValue({ data: {} });

    const page = await skillGapApi.getHistory();

    expect(axiosClient.get).toHaveBeenCalledWith("/me/skill-gap/history", {
      params: { limit: 20 },
    });
    expect(page).toEqual({
      items: [],
      nextCursor: null,
      hasMore: false,
    });
  });
});
