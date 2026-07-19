import { describe, expect, it } from "vitest";
import {
  buildSkillSeries,
  comparisonCopy,
  mergeOptionCatalog,
  normalizePublicationAnalytics,
  segmentPercent,
} from "./marketPulseViewModel";

describe("marketPulseViewModel", () => {
  it("normalizes publication analytics and preserves dates outside history as gaps", () => {
    const analytics = normalizePublicationAnalytics({
      publicationAnalytics: {
        basis: "published_date",
        dateModel: "interval_weighted",
        availability: "available",
        anchorDate: "2026-07-07",
        sourceDataAt: "2026-07-07T02:00:00Z",
        historyCoverageStart: "2026-06-01",
        historyCoverageEnd: "2026-07-07",
        currentPeriod: { startDate: "2026-07-01", endDate: "2026-07-07", estimatedTotal: 30.5, exactCount: 20, relativeEstimate: 10.5, averagePerDay: 4.36 },
        previousPeriod: { startDate: "2026-06-24", endDate: "2026-06-30", estimatedTotal: 20, exactCount: 15, relativeEstimate: 5, averagePerDay: 2.86 },
        marketTrendPoints: [
          { date: "2026-07-01", exactPostings: 3, relativeEstimate: 1.5, totalEstimate: 4.5, available: true },
          { date: "2026-07-02", exactPostings: null, relativeEstimate: null, totalEstimate: null, available: false },
        ],
        marketComparison: { currentTotal: 30.5, previousTotal: 20, currentAverage: 4.36, previousAverage: 2.86, delta: 10.5, growthPercent: 52.5, direction: "up", confidence: "medium" },
        skillTrendPoints: [
          { date: "2026-07-01", skillName: "React", skillSlug: "react", exactPostings: 2, relativeEstimate: 0.5, totalEstimate: 2.5, available: true },
          { date: "2026-07-02", skillName: "React", skillSlug: "react", available: false },
        ],
        skillComparisons: [{ skillName: "React", skillSlug: "react", currentTotal: 12, previousTotal: 9, delta: 3, growthPercent: 33.3, direction: "up", confidence: "medium" }],
        postDateQuality: { sampleSize: 100, exactCount: 60, relativeCount: 30, unknownCount: 10, reliableCoveragePercent: 90, averageIntervalWidthDays: 3.2, broadRangeSharePercent: 12, confidence: "high" },
      },
    }, 7);

    expect(analytics.hasHistory).toBe(true);
    expect(analytics.basis).toBe("published_date");
    expect(analytics.currentStart).toBe("2026-07-01");
    expect(analytics.marketTrendPoints[0].totalEstimate).toBe(4.5);
    expect(analytics.marketTrendPoints[1].totalEstimate).toBeNull();
    expect(analytics.marketComparison.currentTotal).toBe(30.5);
    expect(analytics.skillTrendPoints[1].available).toBe(false);
    expect(analytics.postDateQuality.relativePercent).toBe(30);
  });

  it("does not invent publication history from legacy trend points", () => {
    const analytics = normalizePublicationAnalytics({ lastUpdatedAt: "2026-07-07T02:00:00Z", trendPoints: [{ date: "2026-07-07", postingCount: 99 }] }, 30);
    expect(analytics.hasContract).toBe(false);
    expect(analytics.hasHistory).toBe(false);
    expect(analytics.marketTrendPoints).toEqual([]);
  });

  it("uses explicit new, flat and insufficient comparison states", () => {
    expect(comparisonCopy({ delta: 4, direction: "new" })).toContain("fully covered previous period");
    expect(comparisonCopy({ delta: 0, direction: "flat" })).toContain("unchanged");
    expect(comparisonCopy({ direction: "insufficient" })).toContain("not fully covered");
  });

  it("keeps skill publication gaps and true facet shares", () => {
    const series = buildSkillSeries([
      { date: "2026-07-01", skillName: "React", skillSlug: "react", exactPostings: 1, relativeEstimate: 1, totalEstimate: 2, available: true },
      { date: "2026-07-02", skillName: "React", skillSlug: "react", totalEstimate: null, available: false },
    ], ["react"]);
    expect(series[0].points.map((point) => point.value)).toEqual([2, null]);
    expect(series[0].points[0].approximate).toBe(true);
    expect(segmentPercent({ postingCount: 25 }, 100, "postingCount")).toBe(25);
    expect(mergeOptionCatalog(["Backend"], [{ name: "Frontend" }, { name: "Unspecified" }])).toEqual(["Backend", "Frontend"]);
  });

  it.each([
    ["history_sync_required", "has not been synchronized"],
    ["insufficient_history", "does not yet cover both comparison periods"],
  ])("gives an actionable message for %s", (availability, expected) => {
    const analytics = normalizePublicationAnalytics({
      publicationAnalytics: { availability, marketTrendPoints: [], postDateQuality: {} },
    });
    expect(analytics.historyMessage).toContain(expected);
  });
});
