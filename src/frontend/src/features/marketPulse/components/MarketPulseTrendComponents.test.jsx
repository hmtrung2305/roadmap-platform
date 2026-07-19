import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import MarketSnapshotSection from "./MarketSnapshotSection";
import ResponsiveTrendChart from "./ResponsiveTrendChart";
import TrendAnalysisSection from "./TrendAnalysisSection";

const period = {
  estimatedTotal: 8,
  exactCount: 6,
  relativeEstimate: 2,
  averagePerDay: 1.1,
};

const comparison = {
  currentTotal: 8,
  previousTotal: 6,
  currentAverage: 1.1,
  previousAverage: 0.9,
  delta: 2,
  growthPercent: 33.3,
  direction: "up",
  confidence: "medium",
};

function analyticsFixture() {
  return {
    hasHistory: true,
    historyMessage: "No trend",
    currentStart: "2026-07-01",
    currentEnd: "2026-07-07",
    previousStart: "2026-06-24",
    previousEnd: "2026-06-30",
    historyCoverageStart: "2026-06-01",
    historyCoverageEnd: "2026-07-07",
    confidence: "medium",
    currentPeriod: period,
    previousPeriod: { ...period, estimatedTotal: 6 },
    marketComparison: comparison,
    marketTrendPoints: [{ date: "2026-07-01", exactPostings: 6, relativeEstimate: 2, totalEstimate: 8, available: true }],
    skillTrendPoints: [{ date: "2026-07-01", skillName: "Kotlin", skillSlug: "kotlin", exactPostings: 2, relativeEstimate: 1, totalEstimate: 3, available: true }],
    skillComparisons: [{ skillName: "Kotlin", skillSlug: "kotlin", ...comparison }],
    postDateQuality: { reliablePercent: 80, exactPercent: 60, relativePercent: 20, averageIntervalWidthDays: 2, broadRangeSharePercent: 10 },
  };
}

describe("Market Pulse trend components", () => {
  it("defaults Compare skills from publication analytics rather than the overview ranking", () => {
    render(
      <TrendAnalysisSection
        overview={{ skills: [{ skillName: "React", skillSlug: "react", postingCount: 20 }] }}
        analytics={analyticsFixture()}
        tab="skill"
        onTabChange={vi.fn()}
        comparisonSkills={[]}
        onComparisonSkillsChange={vi.fn()}
      />,
    );

    const kotlinButtons = screen.getAllByRole("button", { name: "Kotlin" });
    expect(kotlinButtons.every((button) => button.getAttribute("aria-pressed") === "true")).toBe(true);
    expect(screen.getByRole("button", { name: "React" })).toHaveAttribute("aria-pressed", "false");
  });

  it("keeps the snapshot Top skill independent from comparison-series ordering", () => {
    render(
      <MarketSnapshotSection
        overview={{ activePostings: 12, skills: [{ skillName: "React", skillSlug: "react", postingCount: 20 }] }}
        analytics={analyticsFixture()}
      />,
    );

    expect(screen.getByText("React")).toBeInTheDocument();
    expect(screen.queryByText("Kotlin")).not.toBeInTheDocument();
  });

  it("renders Market demand as one postings line", () => {
    render(
      <TrendAnalysisSection
        overview={{ skills: [] }}
        analytics={analyticsFixture()}
        tab="market"
        onTabChange={vi.fn()}
        comparisonSkills={[]}
        onComparisonSkillsChange={vi.fn()}
      />,
    );

    expect(screen.getByRole("button", { name: "Postings" })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Exact date" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Relative-date estimate" })).not.toBeInTheDocument();
    expect(screen.getByRole("img", { name: "TopCV postings by publication date" })).toBeInTheDocument();
    expect(screen.queryByText(/Confidence:/)).not.toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "How certain is this trend?" })).not.toBeInTheDocument();
  });

  it("restores a series when a parent update leaves only a previously hidden key", async () => {
    const alpha = { key: "alpha", label: "Alpha", color: "#123456", points: [{ date: "2026-07-01", value: 2 }] };
    const beta = { key: "beta", label: "Beta", color: "#654321", points: [{ date: "2026-07-01", value: 3 }] };
    const { rerender } = render(<ResponsiveTrendChart series={[alpha, beta]} ariaLabel="Test demand" />);

    fireEvent.click(screen.getByRole("button", { name: "Alpha" }));
    expect(screen.getByRole("button", { name: "Alpha" })).toHaveAttribute("aria-pressed", "false");
    rerender(<ResponsiveTrendChart series={[alpha]} ariaLabel="Test demand" />);

    await waitFor(() => expect(screen.getByRole("button", { name: "Alpha" })).toHaveAttribute("aria-pressed", "true"));
    expect(screen.getByRole("img", { name: "Test demand" })).toBeInTheDocument();
  });
});
