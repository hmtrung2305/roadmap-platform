import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { marketPulseApi } from "../../api/marketPulseApi";
import AdminMarketPulsePage from "./AdminMarketPulsePage";

vi.mock("../../api/marketPulseApi", () => ({
  marketPulseApi: {
    getAdminDashboard: vi.fn(),
    getImportRuns: vi.fn(),
    getOperationsFailures: vi.fn(),
    getClassifierCategories: vi.fn(),
    getClassifierMappings: vi.fn(),
    createRefreshOperation: vi.fn(),
    getRefreshOperation: vi.fn(),
    retryOperationsFailures: vi.fn(),
    ignoreOperationsFailures: vi.fn(),
    importLatest: vi.fn(),
    syncPublicationHistory: vi.fn(),
  },
}));

const dashboard = {
  overallStatus: "healthy",
  latestSuccessfulRefreshAt: "2026-07-18T02:00:00Z",
  activeJobs: 480,
  estimatedPostings7Days: 62.5,
  crawlerFreshnessHours: 1,
  reliablePostDateCoveragePercent: 88,
  analyticsConfidence: "medium",
  postDateQuality: {
    sampleSize: 480,
    exactCount: 120,
    relativeCount: 302,
    unknownCount: 58,
    exactPercent: 25,
    relativePercent: 62.9,
    unknownPercent: 12.1,
    reliablePercent: 87.9,
    averageIntervalWidthDays: 4.2,
    broadRangeSharePercent: 18,
  },
  importLagMinutes: 4,
  openCrawlerFailures: 0,
  openImportFailures: 0,
  pipelineHealth: {
    crawler: { status: "healthy", summary: "TopCV listing crawl is fresh." },
    import: { status: "healthy", summary: "Latest crawler batch imported." },
    publicationHistory: { status: "ready", summary: "400-day history is covered." },
    dataQuality: { status: "healthy", summary: "Detail fields meet thresholds." },
  },
  publicationDemandPoints: [{ date: "2026-07-18", totalEstimate: 10, relativeEstimate: 2 }],
  alerts: [],
  recentOperations: [],
};

describe("AdminMarketPulsePage", () => {
  beforeEach(() => {
    Object.values(marketPulseApi).forEach((mock) => mock.mockReset());
    marketPulseApi.getAdminDashboard.mockResolvedValue(dashboard);
    marketPulseApi.getImportRuns.mockResolvedValue({ items: [] });
  });

  it("loads only the dashboard on first paint and lazy-loads detail tabs", async () => {
    render(<MemoryRouter><AdminMarketPulsePage /></MemoryRouter>);
    expect(await screen.findByRole("heading", { name: "Market Pulse operations" })).toBeInTheDocument();
    expect(marketPulseApi.getAdminDashboard).toHaveBeenCalledTimes(1);
    expect(marketPulseApi.getImportRuns).not.toHaveBeenCalled();
    expect(screen.getByText("TopCV listing crawl is fresh.")).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Analytics confidence" })).toBeInTheDocument();
    expect(screen.getByText("Relative estimates")).toBeInTheDocument();
    expect(screen.getByText("62.9% (302)")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("tab", { name: "Import runs" }));
    await waitFor(() => expect(marketPulseApi.getImportRuns).toHaveBeenCalledTimes(1));
    expect(await screen.findByText("No import run has been recorded.")).toBeInTheDocument();
  });

  it("starts one durable end-to-end refresh from the primary action", async () => {
    marketPulseApi.createRefreshOperation.mockResolvedValue({ id: "op-1", status: "queued", createdAt: "2026-07-18T03:00:00Z" });
    marketPulseApi.getRefreshOperation.mockResolvedValue({ id: "op-1", status: "success" });
    render(<MemoryRouter><AdminMarketPulsePage /></MemoryRouter>);
    const button = await screen.findByRole("button", { name: "Refresh TopCV market data" });
    fireEvent.click(button);
    await waitFor(() => expect(marketPulseApi.createRefreshOperation).toHaveBeenCalledTimes(1));
    expect(await screen.findByText("Refresh queued. The page can be safely reloaded while it runs.")).toBeInTheDocument();
  });

  it("renders grouped crawler/import failures and submits their stable IDs", async () => {
    marketPulseApi.getOperationsFailures.mockResolvedValue({
      crawlerFailures: [{ failureId: "crawler:12", errorMessage: "TopCV detail failed" }],
      importFailures: [{ failedItemId: "05ef5845-5827-49dc-8f79-a5165ad86c7c", errorMessage: "Import validation failed" }],
    });
    marketPulseApi.retryOperationsFailures.mockResolvedValue({ crawlerFailures: [], importFailures: [] });

    render(<MemoryRouter><AdminMarketPulsePage /></MemoryRouter>);
    await screen.findByRole("heading", { name: "Market Pulse operations" });
    fireEvent.click(screen.getByRole("tab", { name: "Failures" }));

    const checkboxes = await screen.findAllByRole("checkbox");
    fireEvent.click(checkboxes[0]);
    fireEvent.click(checkboxes[1]);
    fireEvent.click(screen.getByRole("button", { name: "Retry selected" }));

    await waitFor(() => expect(marketPulseApi.retryOperationsFailures).toHaveBeenCalledWith([
      "crawler:12",
      "05ef5845-5827-49dc-8f79-a5165ad86c7c",
    ]));
  });

  it("opens Import runs from an incomplete-import alert", async () => {
    marketPulseApi.getAdminDashboard.mockResolvedValueOnce({
      ...dashboard,
      alerts: [{ code: "TOPCV_IMPORT_INCOMPLETE", severity: "warning", message: "Latest import was partial.", action: "Review import run details" }],
    });
    render(<MemoryRouter><AdminMarketPulsePage /></MemoryRouter>);

    fireEvent.click(await screen.findByRole("button", { name: "Review import run details" }));
    await waitFor(() => expect(marketPulseApi.getImportRuns).toHaveBeenCalledTimes(1));
    expect(screen.getByRole("tab", { name: "Import runs" })).toHaveAttribute("aria-selected", "true");
  });

  it("responds to low post-date quality with explicit host-CLI guidance and focus", async () => {
    marketPulseApi.getAdminDashboard.mockResolvedValueOnce({
      ...dashboard,
      alerts: [{ code: "POST_DATE_QUALITY_LOW", severity: "warning", message: "Dates need repair.", action: "Run Python post-date backfill" }],
    });
    render(<MemoryRouter><AdminMarketPulsePage /></MemoryRouter>);

    fireEvent.click(await screen.findByRole("button", { name: "Run Python post-date backfill" }));
    expect(await screen.findByText(/python -m crawler\.post_date_backfill --apply/)).toBeInTheDocument();
    expect(document.activeElement).toHaveAttribute("id", "advanced-market-pulse-actions");
  });

  it("uses the lifecycle-safe 50,000 item default for historical sync", async () => {
    marketPulseApi.syncPublicationHistory.mockResolvedValueOnce({ status: "success" });
    render(<MemoryRouter><AdminMarketPulsePage /></MemoryRouter>);
    await screen.findByRole("heading", { name: "Market Pulse operations" });

    fireEvent.click(screen.getByRole("button", { name: /Advanced actions/ }));
    expect(screen.getByLabelText("Maximum items")).toHaveValue(50000);
    fireEvent.click(screen.getByRole("button", { name: "Historical sync" }));

    await waitFor(() => expect(marketPulseApi.syncPublicationHistory).toHaveBeenCalledWith({
      lookbackDays: 400,
      pageSize: 100,
      maxItems: 50000,
    }));
  });

  it("focuses, traps and closes the import-run drawer with focus restoration", async () => {
    marketPulseApi.getImportRuns.mockResolvedValueOnce({
      items: [{ id: "run-1", startedAt: "2026-07-18T01:00:00Z", status: "success", fetchedCount: 10, importedCount: 10, failedCount: 0 }],
    });
    render(<MemoryRouter><AdminMarketPulsePage /></MemoryRouter>);
    await screen.findByRole("heading", { name: "Market Pulse operations" });
    fireEvent.click(screen.getByRole("tab", { name: "Import runs" }));
    const trigger = await screen.findByRole("button", { name: "Open import run details" });
    trigger.focus();
    fireEvent.click(trigger);

    const close = await screen.findByRole("button", { name: "Close details" });
    expect(close).toHaveFocus();
    fireEvent.keyDown(document, { key: "Tab" });
    expect(close).toHaveFocus();
    fireEvent.keyDown(document, { key: "Escape" });
    await waitFor(() => expect(screen.queryByRole("dialog")).not.toBeInTheDocument());
    expect(trigger).toHaveFocus();
  });
});
