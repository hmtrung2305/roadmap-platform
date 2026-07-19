import { act, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { marketPulseApi } from "../api/marketPulseApi";
import MarketPulsePage from "./MarketPulsePage";

vi.mock("../api/marketPulseApi", () => ({ marketPulseApi: { getOverview: vi.fn() } }));

const overviewFixture = {
  lastUpdatedAt: "2026-07-07T02:00:00Z",
  activePostings: 120,
  totalPostings: 120,
  skills: [
    { skillName: "React", skillSlug: "react", postingCount: 30 },
    { skillName: ".NET", skillSlug: "dotnet", postingCount: 25 },
    { skillName: "Python", skillSlug: "python", postingCount: 20 },
  ],
  categorySummaries: [{ name: "Frontend", count: 60, percent: 50 }, { name: "Backend", count: 48, percent: 40 }],
  locationSummaries: [{ name: "Ho Chi Minh City", count: 72, percent: 60 }],
  experienceSummaries: [{ name: "Mid-level", count: 54, percent: 45 }],
  insightMeta: { sampleSize: 120, confidence: "medium" },
  dataQuality: { level: "medium", sampleSize: 120, freshnessHours: 2, warnings: [] },
  salaryInsight: { coveragePercent: 20, sampleSize: 24, medianMinMonthlyVnd: 20_000_000, medianMaxMonthlyVnd: 35_000_000 },
  learningRecommendations: [{ title: "Focus on React", detail: "Demand is rising.", actionLabel: "Explore trend", skillSlug: "react", priority: "high" }],
  skillCoOccurrences: [{ skillA: "React", skillASlug: "react", skillB: "TypeScript", skillBSlug: "typescript", postingCount: 18, percentOfSample: 15 }],
  publicationAnalytics: {
    basis: "published_date",
    dateModel: "interval_weighted",
    availability: "available",
    anchorDate: "2026-07-07",
    sourceDataAt: "2026-07-07T02:00:00Z",
    historyCoverageStart: "2026-06-01",
    historyCoverageEnd: "2026-07-07",
    currentPeriod: { startDate: "2026-07-01", endDate: "2026-07-07", estimatedTotal: 42.5, exactCount: 30, relativeEstimate: 12.5, averagePerDay: 6.1 },
    previousPeriod: { startDate: "2026-06-24", endDate: "2026-06-30", estimatedTotal: 34, exactCount: 25, relativeEstimate: 9, averagePerDay: 4.9 },
    marketTrendPoints: [
      { date: "2026-07-01", exactPostings: 4, relativeEstimate: 1.5, totalEstimate: 5.5, available: true },
      { date: "2026-07-07", exactPostings: 5, relativeEstimate: 2, totalEstimate: 7, available: true },
    ],
    marketComparison: { currentTotal: 42.5, previousTotal: 34, currentAverage: 6.1, previousAverage: 4.9, delta: 8.5, growthPercent: 25, direction: "up", confidence: "medium" },
    skillTrendPoints: [
      { date: "2026-07-01", skillName: "React", skillSlug: "react", exactPostings: 3, relativeEstimate: 1, totalEstimate: 4, available: true },
      { date: "2026-07-07", skillName: "React", skillSlug: "react", exactPostings: 4, relativeEstimate: 1, totalEstimate: 5, available: true },
      { date: "2026-07-01", skillName: ".NET", skillSlug: "dotnet", exactPostings: 2, relativeEstimate: 1, totalEstimate: 3, available: true },
      { date: "2026-07-07", skillName: ".NET", skillSlug: "dotnet", exactPostings: 3, relativeEstimate: 1, totalEstimate: 4, available: true },
      { date: "2026-07-01", skillName: "Python", skillSlug: "python", exactPostings: 2, relativeEstimate: 0.5, totalEstimate: 2.5, available: true },
      { date: "2026-07-07", skillName: "Python", skillSlug: "python", exactPostings: 2, relativeEstimate: 1, totalEstimate: 3, available: true },
    ],
    skillComparisons: [{ skillName: "React", skillSlug: "react", currentTotal: 18, previousTotal: 12, currentAverage: 2.6, previousAverage: 1.7, delta: 6, growthPercent: 50, direction: "up", confidence: "medium" }],
    postDateQuality: { sampleSize: 120, exactCount: 72, relativeCount: 36, unknownCount: 12, reliableCoveragePercent: 90, averageIntervalWidthDays: 3, broadRangeSharePercent: 10, confidence: "medium" },
  },
};

describe("MarketPulsePage", () => {
  beforeEach(() => {
    marketPulseApi.getOverview.mockReset();
    marketPulseApi.getOverview.mockResolvedValue(overviewFixture);
    Element.prototype.scrollIntoView = vi.fn();
  });

  it("renders publication-based market sections without source KPIs", async () => {
    render(<MarketPulsePage />);
    expect(screen.getByLabelText("Loading market insights")).toBeInTheDocument();
    expect(await screen.findByRole("heading", { name: "Market snapshot" })).toBeInTheDocument();
    expect(screen.getByText("Posted in period")).toBeInTheDocument();
    expect(screen.getByText("Change vs previous")).toBeInTheDocument();
    expect(screen.queryByText("New in latest crawl")).not.toBeInTheDocument();
    expect(screen.queryByText("Sources")).not.toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Trend analysis" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Demand breakdown" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Learning signals" })).toBeInTheDocument();
    expect(screen.queryByText(/confidence/i)).not.toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "How certain is this trend?" })).not.toBeInTheDocument();
  });

  it("uses one GET for a period change and applies seniority only after Apply", async () => {
    render(<MarketPulsePage />);
    await screen.findByRole("heading", { name: "Market snapshot" });
    expect(marketPulseApi.getOverview).toHaveBeenCalledTimes(1);
    fireEvent.click(screen.getByRole("button", { name: "7 days" }));
    await waitFor(() => expect(marketPulseApi.getOverview).toHaveBeenCalledTimes(2));
    expect(marketPulseApi.getOverview.mock.calls[1][0]).toMatchObject({ days: 7, experience: "" });
    fireEvent.click(screen.getByRole("button", { name: /^Filters/ }));
    fireEvent.click(screen.getByRole("button", { name: "Seniority" }));
    fireEvent.click(screen.getByRole("option", { name: "Mid-level" }));
    expect(marketPulseApi.getOverview).toHaveBeenCalledTimes(2);
    fireEvent.click(screen.getByRole("button", { name: "Apply filters" }));
    await waitFor(() => expect(marketPulseApi.getOverview).toHaveBeenCalledTimes(3));
    expect(marketPulseApi.getOverview.mock.calls[2][0]).toMatchObject({ experience: "Mid-level" });
    expect(marketPulseApi.getOverview.mock.calls[2][0]).not.toHaveProperty("source");
  });

  it("keeps the prior view when a reload fails", async () => {
    render(<MarketPulsePage />);
    await screen.findByRole("heading", { name: "Market snapshot" });
    marketPulseApi.getOverview.mockRejectedValueOnce(new Error("Network down"));
    fireEvent.click(screen.getByRole("button", { name: "Reload market insights" }));
    expect(await screen.findByText("Couldn't update insights. Showing the last loaded view.")).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Market snapshot" })).toBeInTheDocument();
  });

  it("discards unapplied advanced-filter drafts when Clear all is used", async () => {
    render(<MarketPulsePage />);
    await screen.findByRole("heading", { name: "Market snapshot" });
    fireEvent.click(screen.getByRole("button", { name: "7 days" }));
    await waitFor(() => expect(marketPulseApi.getOverview).toHaveBeenCalledTimes(2));
    fireEvent.click(screen.getByRole("button", { name: /^Filters/ }));
    fireEvent.click(screen.getByRole("button", { name: "Seniority" }));
    fireEvent.click(screen.getByRole("option", { name: "Mid-level" }));
    fireEvent.click(screen.getByRole("button", { name: "Clear all" }));
    fireEvent.click(screen.getByRole("button", { name: /^Filters/ }));
    expect(screen.getByRole("button", { name: "Seniority" })).toHaveTextContent("All");
  });

  it("switches to skill demand when a recommendation is explored", async () => {
    render(<MarketPulsePage />);
    await screen.findByRole("heading", { name: "Learning signals" });
    await act(async () => fireEvent.click(screen.getByRole("button", { name: "Explore trend" })));
    expect(screen.getByRole("tab", { name: "Skill demand" })).toHaveAttribute("aria-selected", "true");
    await waitFor(() => expect(marketPulseApi.getOverview).toHaveBeenLastCalledWith(expect.objectContaining({ skills: ["react"] })));
  });

  it("shows data-quality guidance when publication history is absent", async () => {
    marketPulseApi.getOverview.mockResolvedValueOnce({ ...overviewFixture, publicationAnalytics: undefined });
    render(<MarketPulsePage />);
    expect((await screen.findAllByText("No reliable publication-date history is available for these filters yet.")).length).toBeGreaterThan(0);
  });

  it("aborts the in-flight overview request when unmounted", () => {
    let requestSignal;
    marketPulseApi.getOverview.mockImplementationOnce(({ signal }) => { requestSignal = signal; return new Promise(() => {}); });
    const { unmount } = render(<MarketPulsePage />);
    expect(requestSignal.aborted).toBe(false);
    unmount();
    expect(requestSignal.aborted).toBe(true);
  });
});
