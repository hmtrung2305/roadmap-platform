import { describe, expect, it } from "vitest";
import { errorMessage, normalizeDashboard, normalizeOperation, operationStepState } from "./adminViewModel";

describe("adminViewModel", () => {
  it("normalizes a dashboard without exposing source abstractions", () => {
    const dashboard = normalizeDashboard({
      status: "degraded",
      activePostings: 480,
      postingsLast7Days: 62.5,
      pipelineHealth: {
        crawler: { status: "healthy", freshnessHours: 1.5 },
        import: { status: "stale", lagMinutes: 42 },
        publicationHistory: { status: "ready" },
      },
      failures: { crawler: 2, import: 1 },
      marketTrendPoints: [{ date: "2026-07-18", totalEstimate: 7.5, relativeEstimate: 2 }],
    });
    expect(dashboard.overallStatus).toBe("degraded");
    expect(dashboard.activeJobs).toBe(480);
    expect(dashboard.openCrawlerFailures).toBe(2);
    expect(dashboard.pipeline.crawler.label).toBe("Python TopCV crawler");
    expect(dashboard.demandPoints[0]).toMatchObject({ value: 7.5, approximate: true });
  });

  it("maps persisted refresh states to the three pipeline steps", () => {
    const importing = normalizeOperation({ id: "op-1", status: "importing", crawlerCompletedAt: "2026-07-18T01:00:00Z" });
    expect(operationStepState(importing, "crawler")).toBe("complete");
    expect(operationStepState(importing, "import")).toBe("active");
    expect(operationStepState(importing, "analytics")).toBe("pending");
  });

  it("matches the TopCV operations API contract exactly", () => {
    const dashboard = normalizeDashboard({
      overallStatus: "healthy",
      reliablePostDateCoverage: 86.5,
      analyticsConfidence: "medium",
      postDateQuality: {
        sampleSize: 100,
        exactCount: 30,
        relativeCount: 55,
        unknownCount: 15,
        averageIntervalWidthDays: 4.5,
        broadRangeSharePercent: 12,
      },
      pipelineHealth: [
        { key: "crawler", label: "Python TopCV crawler", status: "healthy", detail: "Fresh" },
        { key: "import", label: ".NET TopCV import", status: "success", detail: "Imported" },
        { key: "history", label: "Publication history", status: "healthy", detail: "Covered" },
        { key: "quality", label: "Data quality", status: "medium", detail: "Directional" },
      ],
      demandTrend: [{ date: "2026-07-18", exactPostings: 4, relativeEstimate: 1.5, totalEstimate: 5.5 }],
      currentOperation: {
        operationId: "op-2",
        status: "importing",
        currentStep: "import",
        baselineCrawlerSuccessAt: "2026-07-18T00:00:00Z",
        crawlerSuccessAt: "2026-07-18T01:00:00Z",
        requestedAt: "2026-07-18T00:30:00Z",
      },
    });

    expect(dashboard.reliablePostDateCoveragePercent).toBe(86.5);
    expect(dashboard.analyticsConfidence).toBe("medium");
    expect(dashboard.postDateQuality).toMatchObject({
      sampleSize: 100,
      exactPercent: 30,
      relativePercent: 55,
      unknownPercent: 15,
      averageIntervalWidthDays: 4.5,
    });
    expect(dashboard.pipeline.importer.summary).toBe("Imported");
    expect(dashboard.demandPoints[0].value).toBe(5.5);
    expect(dashboard.currentOperation).toMatchObject({
      id: "op-2",
      currentStep: "import",
      crawlerCompletedAt: "2026-07-18T01:00:00Z",
    });
  });

  it("renders structured API errors as text instead of React children", () => {
    expect(errorMessage({ code: "conflict", message: "Refresh already active", details: { id: 3 } })).toBe("Refresh already active");
  });

  it("maps import and post-date alerts to actions the console handles", () => {
    const normalized = normalizeDashboard({
      alerts: [
        { code: "TOPCV_IMPORT_INCOMPLETE", action: "Review import run details" },
        { code: "POST_DATE_QUALITY_LOW", action: "Run Python post-date backfill" },
      ],
    });
    expect(normalized.alerts).toEqual(expect.arrayContaining([
      expect.objectContaining({ id: "TOPCV_IMPORT_INCOMPLETE", actionType: "view_imports" }),
      expect.objectContaining({ id: "POST_DATE_QUALITY_LOW", actionType: "post_date_backfill" }),
    ]));
  });
});
