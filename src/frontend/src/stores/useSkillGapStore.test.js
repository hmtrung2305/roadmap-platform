import { beforeEach, describe, expect, it, vi } from "vitest";

import { skillGapApi } from "../api/skillGapApi";
import { useSkillGapStore } from "./useSkillGapStore";

vi.mock("../api/skillGapApi", () => ({
  skillGapApi: {
    getCareerRoles: vi.fn(),
    getPublishedRoadmapsByRole: vi.fn(),
    getAssessmentByRoadmap: vi.fn(),
    analyzeSkillGap: vi.fn(),
  },
}));

function createAnalysisResponse(historyId) {
  return {
    skillGapAnalysisHistoryId: historyId,
    roadmapId: "roadmap-1",
    roadmapName: "Backend roadmap",
    matchedSkills: 1,
    totalSkills: 2,
    missingSkills: 1,
    categories: [],
  };
}

describe("useSkillGapStore analysis", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    useSkillGapStore.getState().resetSkillGap();
    useSkillGapStore.setState({
      selectedRoadmap: {
        roadmapId: "roadmap-1",
        title: "Backend roadmap",
      },
      selectedSkillIds: ["skill-1"],
      isAssessmentLoading: false,
      isAnalyzing: false,
      error: "",
    });
  });

  it("creates a new backend analysis for the same selection each time", async () => {
    skillGapApi.analyzeSkillGap
      .mockResolvedValueOnce(createAnalysisResponse("history-1"))
      .mockResolvedValueOnce(createAnalysisResponse("history-2"));

    await useSkillGapStore.getState().analyze();
    await useSkillGapStore.getState().analyze();

    expect(skillGapApi.analyzeSkillGap).toHaveBeenCalledTimes(2);
    expect(useSkillGapStore.getState().result.skillGapAnalysisHistoryId).toBe(
      "history-2",
    );
  });

  it("does not submit a second request while analysis is pending", async () => {
    let resolveAnalysis;
    const pendingResponse = new Promise((resolve) => {
      resolveAnalysis = resolve;
    });

    skillGapApi.analyzeSkillGap.mockReturnValue(pendingResponse);

    const firstRequest = useSkillGapStore.getState().analyze();
    const secondRequest = useSkillGapStore.getState().analyze();

    await expect(secondRequest).resolves.toBeNull();
    await vi.waitFor(() => {
      expect(skillGapApi.analyzeSkillGap).toHaveBeenCalledTimes(1);
    });

    resolveAnalysis(createAnalysisResponse("history-1"));
    await firstRequest;

    expect(useSkillGapStore.getState().isAnalyzing).toBe(false);
  });
});
