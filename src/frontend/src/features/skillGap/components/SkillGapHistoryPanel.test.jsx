import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";

import { skillGapApi } from "../../../api/skillGapApi";
import SkillGapHistoryPanel from "./SkillGapHistoryPanel";

vi.mock("../../../api/skillGapApi", () => ({
  skillGapApi: {
    getHistory: vi.fn(),
    getHistoryDetail: vi.fn(),
    deleteHistory: vi.fn(),
  },
}));

function createHistoryItem(historyId, roadmapTitle) {
  return {
    skillGapAnalysisHistoryId: historyId,
    roadmapId: `roadmap-${historyId}`,
    roadmapTitle,
    careerRoleName: "Backend Developer",
    authorName: "Roadmap Author",
    matchedSkills: 3,
    missingSkills: 2,
    totalSkills: 5,
    createdAt: "2026-07-18T08:00:00Z",
  };
}

describe("SkillGapHistoryPanel", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.spyOn(window, "confirm").mockReturnValue(true);
  });

  it("loads the next cursor page and appends its items", async () => {
    const user = userEvent.setup();

    skillGapApi.getHistory
      .mockResolvedValueOnce({
        items: [createHistoryItem("history-1", "Backend roadmap")],
        nextCursor: "next-page-cursor",
        hasMore: true,
      })
      .mockResolvedValueOnce({
        items: [createHistoryItem("history-2", "Cloud roadmap")],
        nextCursor: null,
        hasMore: false,
      });

    render(<SkillGapHistoryPanel onViewResult={vi.fn()} />);

    expect(await screen.findByText("Backend roadmap")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Load more" }));

    expect(await screen.findByText("Cloud roadmap")).toBeInTheDocument();
    expect(skillGapApi.getHistory).toHaveBeenNthCalledWith(2, {
      limit: 20,
      cursor: "next-page-cursor",
    });
    expect(
      screen.queryByRole("button", { name: "Load more" }),
    ).not.toBeInTheDocument();
  });

  it("hard deletes an item and removes it from the visible list", async () => {
    const user = userEvent.setup();

    skillGapApi.getHistory.mockResolvedValue({
      items: [createHistoryItem("history-1", "Backend roadmap")],
      nextCursor: null,
      hasMore: false,
    });
    skillGapApi.deleteHistory.mockResolvedValue();

    render(<SkillGapHistoryPanel onViewResult={vi.fn()} />);

    expect(await screen.findByText("Backend roadmap")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Delete" }));

    await waitFor(() => {
      expect(skillGapApi.deleteHistory).toHaveBeenCalledWith("history-1");
      expect(screen.queryByText("Backend roadmap")).not.toBeInTheDocument();
    });
    expect(screen.getByText("No history yet")).toBeInTheDocument();
  });

  it("hands the selected history id to the route owner", async () => {
    const user = userEvent.setup();
    const onViewResult = vi.fn();

    skillGapApi.getHistory.mockResolvedValue({
      items: [createHistoryItem("history-1", "Backend roadmap")],
      nextCursor: null,
      hasMore: false,
    });

    render(<SkillGapHistoryPanel onViewResult={onViewResult} />);

    expect(await screen.findByText("Backend roadmap")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "View" }));

    expect(onViewResult).toHaveBeenCalledWith("history-1");
    expect(skillGapApi.getHistoryDetail).not.toHaveBeenCalled();
  });
});
