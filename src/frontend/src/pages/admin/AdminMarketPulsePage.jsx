/* eslint-disable react-hooks/exhaustive-deps, react-hooks/set-state-in-effect */
import { useEffect, useMemo, useState } from "react";
import {
  AlertTriangle,
  CheckCircle2,
  Database,
  History,
  Play,
  RefreshCw,
  RotateCcw,
  Search,
  Settings2,
  ShieldCheck,
  SlidersHorizontal,
  Trash2,
  XCircle,
} from "lucide-react";
import { marketPulseApi } from "../../api/marketPulseApi";

const numberFormatter = new Intl.NumberFormat("vi-VN");
const dateFormatter = new Intl.DateTimeFormat("vi-VN", {
  day: "2-digit",
  month: "2-digit",
  year: "numeric",
  hour: "2-digit",
  minute: "2-digit",
  second: "2-digit",
});

const emptyMappingForm = {
  keyword: "",
  category: "Other",
  weight: 1,
  isEnabled: true,
};

const defaultRefreshOptions = {
  jobsApiPageSize: 100,
  jobsApiMaxPages: 10,
  maxPostingsPerSource: 500,
  maxPagesPerSource: 4,
};

export default function AdminMarketPulsePage() {
  const [runs, setRuns] = useState([]);
  const [failedItems, setFailedItems] = useState([]);
  const [mappings, setMappings] = useState([]);
  const [categories, setCategories] = useState([]);
  const [sourceHealth, setSourceHealth] = useState([]);
  const [externalHealth, setExternalHealth] = useState(null);
  const [runFilters, setRunFilters] = useState({ status: "", source: "", from: "", to: "" });
  const [failedFilters, setFailedFilters] = useState({ status: "open", source: "", from: "", to: "" });
  const [selectedFailedIds, setSelectedFailedIds] = useState([]);
  const [mappingForm, setMappingForm] = useState(emptyMappingForm);
  const [editingMappingId, setEditingMappingId] = useState("");
  const [classifierText, setClassifierText] = useState("");
  const [classifierResult, setClassifierResult] = useState(null);
  const [refreshResult, setRefreshResult] = useState(null);
  const [refreshOptions, setRefreshOptions] = useState(defaultRefreshOptions);
  const [isLoading, setIsLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [isSavingMapping, setIsSavingMapping] = useState(false);
  const [actionError, setActionError] = useState("");
  const [refreshCooldownUntil, setRefreshCooldownUntil] = useState(0);
  const [nowTick, setNowTick] = useState(0);

  const cooldownRemaining = Math.max(0, Math.ceil((refreshCooldownUntil - nowTick) / 1000));
  const selectedFailedSet = useMemo(() => new Set(selectedFailedIds), [selectedFailedIds]);

  useEffect(() => {
    loadAdminData();
  }, []);

  useEffect(() => {
    setNowTick(Date.now());
  }, []);

  useEffect(() => {
    if (!refreshCooldownUntil) return undefined;

    const intervalId = window.setInterval(() => setNowTick(Date.now()), 1000);
    return () => window.clearInterval(intervalId);
  }, [refreshCooldownUntil]);

  async function loadAdminData() {
    setIsLoading(true);
    setActionError("");

    const requests = [
      ["import runs", marketPulseApi.getCrawlRuns({ ...normalizeDateFilters(runFilters), limit: 50 }), (data) => setRuns(data || [])],
      ["import failures", marketPulseApi.getFailedItems({ ...normalizeDateFilters(failedFilters), limit: 50 }), (data) => setFailedItems(data || [])],
      ["classifier mappings", marketPulseApi.getClassifierMappings(), (data) => setMappings(data || [])],
      ["classifier categories", marketPulseApi.getClassifierCategories(), (data) => setCategories(data || [])],
      [".NET import health", marketPulseApi.getSourceHealth(), (data) => setSourceHealth(data || [])],
      ["Python crawler health", marketPulseApi.getExternalSourceHealth(), setExternalHealth],
    ];
    const results = await Promise.allSettled(requests.map(([, request]) => request));
    const failures = [];

    results.forEach((result, index) => {
      const [label, , applyResult] = requests[index];
      if (result.status === "fulfilled") {
        applyResult(result.value);
      } else {
        failures.push(`${label}: ${result.reason?.message || "request failed"}`);
      }
    });

    if (failures.length) {
      setActionError(`Some admin sections could not be loaded. ${failures.join(" | ")}`);
    }
    setIsLoading(false);
  }

  async function loadRuns() {
    const data = await marketPulseApi.getCrawlRuns({ ...normalizeDateFilters(runFilters), limit: 50 });
    setRuns(data || []);
  }

  async function loadFailedItems() {
    const data = await marketPulseApi.getFailedItems({ ...normalizeDateFilters(failedFilters), limit: 50 });
    setFailedItems(data || []);
    setSelectedFailedIds([]);
  }

  const handleManualRefresh = async () => {
    if (isRefreshing || cooldownRemaining > 0) return;

    setIsRefreshing(true);
    setActionError("");
    setRefreshResult(null);

    try {
      const result = await marketPulseApi.refresh(normalizeRefreshOptions(refreshOptions));
      setRefreshResult(result);
      setRefreshCooldownUntil(Date.now() + 30_000);
      await loadAdminData();
    } catch (error) {
      const message = typeof error?.message === "string"
        ? error.message
        : "Manual refresh failed.";
      const detailMessage = typeof error?.details?.message === "string"
        ? error.details.message
        : "";

      setActionError(
        detailMessage && detailMessage !== message
          ? `${message} ${detailMessage}`
          : message,
      );
      setRefreshCooldownUntil(Date.now() + 30_000);
      await Promise.allSettled([loadRuns(), loadFailedItems()]);
    } finally {
      setIsRefreshing(false);
    }
  };

  const handleRetry = async (ids) => {
    if (!ids.length) return;
    await marketPulseApi.retryFailedItems(ids);
    await loadFailedItems();
  };

  const handleIgnore = async (ids) => {
    if (!ids.length) return;
    await marketPulseApi.ignoreFailedItems(ids);
    await loadFailedItems();
  };

  const updateRefreshOption = (key, value) => {
    setRefreshOptions((current) => ({ ...current, [key]: value }));
  };

  const toggleFailedSelection = (id) => {
    setSelectedFailedIds((current) => (
      current.includes(id)
        ? current.filter((item) => item !== id)
        : [...current, id]
    ));
  };

  const editMapping = (mapping) => {
    setEditingMappingId(mapping.mappingId);
    setMappingForm({
      keyword: mapping.keyword,
      category: mapping.category,
      weight: mapping.weight,
      isEnabled: mapping.isEnabled,
    });
  };

  const resetMappingForm = () => {
    setEditingMappingId("");
    setMappingForm(emptyMappingForm);
  };

  const saveMapping = async (event) => {
    event.preventDefault();
    setIsSavingMapping(true);
    setActionError("");

    try {
      if (editingMappingId) {
        await marketPulseApi.updateClassifierMapping(editingMappingId, mappingForm);
      } else {
        await marketPulseApi.createClassifierMapping(mappingForm);
      }

      resetMappingForm();
      const [mappingData, categoryData] = await Promise.all([
        marketPulseApi.getClassifierMappings(),
        marketPulseApi.getClassifierCategories(),
      ]);
      setMappings(mappingData || []);
      setCategories(categoryData || []);
    } catch (error) {
      setActionError(error?.message || "Unable to save classifier mapping.");
    } finally {
      setIsSavingMapping(false);
    }
  };

  const deleteMapping = async (mappingId) => {
    await marketPulseApi.deleteClassifierMapping(mappingId);
    setMappings((current) => current.filter((item) => item.mappingId !== mappingId));
  };

  const testClassifier = async () => {
    if (!classifierText.trim()) {
      setClassifierResult(null);
      return;
    }

    const result = await marketPulseApi.testClassifier(classifierText);
    setClassifierResult(result);
  };

  if (isLoading) {
    return (
      <div className="mx-auto max-w-7xl px-6 py-8">
        <div className="rounded-lg border border-[#B9D8CC] bg-white p-6 text-sm font-semibold text-slate-600 shadow-sm">
          Loading Market Pulse admin...
        </div>
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-7xl space-y-6 px-6 py-8">
      <div>
        <p className="text-xs font-extrabold uppercase tracking-[0.18em] text-[#1F6F5F]">
          Job Market Pulse
        </p>
        <h1 className="mt-1 text-3xl font-black text-[#18332D]">
          Market Pulse Admin
        </h1>
        <p className="mt-2 max-w-3xl text-sm font-semibold leading-6 text-slate-600">
          Run .NET imports from the Python Jobs API, monitor import health, review import failures, and tune category classification.
        </p>
      </div>

      {actionError && (
        <div className="flex items-start gap-2 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm font-bold text-red-700">
          <AlertTriangle size={17} />
          {actionError}
        </div>
      )}

      <section className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_380px]">
        <div className="rounded-lg border border-[#B9D8CC] bg-white p-5 shadow-sm">
          <div className="flex flex-col gap-4 lg:flex-row lg:items-center lg:justify-between">
            <div>
              <h2 className="text-lg font-black text-[#18332D]">Import status overview</h2>
              <p className="mt-1 text-sm font-semibold text-slate-500">
                Latest .NET import runs, source freshness, and import failure queue.
              </p>
            </div>
            <button
              type="button"
              onClick={handleManualRefresh}
              disabled={isRefreshing || cooldownRemaining > 0}
              className="inline-flex h-10 items-center justify-center gap-2 rounded-md bg-[#1F6F5F] px-4 text-sm font-extrabold text-white transition hover:bg-[#18584c] disabled:cursor-not-allowed disabled:opacity-60"
            >
              <Play size={16} />
              {isRefreshing ? "Refreshing..." : cooldownRemaining > 0 ? `Wait ${cooldownRemaining}s` : "Manual Refresh"}
            </button>
          </div>

          <div className="mt-5 grid gap-3 sm:grid-cols-2 xl:grid-cols-6">
            <AdminMetric icon={History} label="Latest status" value={runs[0]?.status || "n/a"} />
            <AdminMetric icon={Database} label="Fetched" value={formatNumber(runs[0]?.fetchedCount)} />
            <AdminMetric icon={CheckCircle2} label="Imported" value={formatNumber(runs[0]?.importedCount ?? runs[0]?.savedCount)} />
            <AdminMetric icon={RefreshCw} label="Sync type" value={formatSyncType(runs[0])} />
            <AdminMetric icon={ShieldCheck} label="Missing lifecycle" value={formatLifecycleOutcome(runs[0])} />
            <AdminMetric icon={XCircle} label="Open import failures" value={formatNumber(failedItems.filter((item) => item.status === "open").length)} />
          </div>

          {runs[0] && !runs[0].isCompleteSync && (
            <div className="mt-4 rounded-md border border-amber-200 bg-amber-50 px-4 py-3 text-sm font-bold text-amber-800">
              Partial sync: fetched {formatNumber(runs[0].fetchedCount)} of {formatOptionalNumber(runs[0].sourceTotalCount)} jobs.
              {runs[0].lifecycleSkippedReason && ` Missing-job lifecycle was skipped (${formatLifecycleReason(runs[0].lifecycleSkippedReason)}).`}
            </div>
          )}

          <div className="mt-5 rounded-md border border-slate-200 bg-slate-50 p-4">
            <div className="flex items-center gap-2 text-sm font-black text-slate-800">
              <SlidersHorizontal size={16} />
              Refresh options
            </div>
            <div className="mt-3 grid gap-3 sm:grid-cols-2 xl:grid-cols-3">
              <RefreshOptionInput
                label="Jobs API page size"
                value={refreshOptions.jobsApiPageSize}
                min={1}
                max={500}
                onChange={(value) => updateRefreshOption("jobsApiPageSize", value)}
              />
              <RefreshOptionInput
                label="Jobs API max pages"
                value={refreshOptions.jobsApiMaxPages}
                min={1}
                max={100}
                onChange={(value) => updateRefreshOption("jobsApiMaxPages", value)}
              />
              <RefreshOptionInput
                label="Max postings"
                value={refreshOptions.maxPostingsPerSource}
                min={1}
                max={5000}
                onChange={(value) => updateRefreshOption("maxPostingsPerSource", value)}
              />
            </div>
          </div>

          {refreshResult && (
            <div className="mt-5 rounded-md border border-emerald-200 bg-emerald-50 p-4 text-sm font-semibold text-emerald-800">
              <div className="font-extrabold">Manual refresh result</div>
              <div className="mt-2 grid gap-2 md:grid-cols-2 xl:grid-cols-4">
                <MetricLine label="Started" value={formatDate(refreshResult.startedAt)} />
                <MetricLine label="Finished" value={formatDate(refreshResult.finishedAt)} />
                <MetricLine label="Status" value={refreshResult.status} />
                <MetricLine label="Fetched" value={formatNumber(refreshResult.totalFetched || refreshResult.postingsScraped)} />
                <MetricLine label="Source total" value={formatOptionalNumber(refreshResult.sourceTotal)} />
                <MetricLine label="Sync type" value={formatSyncType(refreshResult)} />
                <MetricLine label="Missing lifecycle" value={formatLifecycleOutcome(refreshResult)} />
                <MetricLine label="Saved" value={formatNumber(refreshResult.totalSaved || refreshResult.postingsSaved)} />
                <MetricLine label="Duplicated" value={formatNumber(refreshResult.totalSkippedDuplicated || refreshResult.postingsDuplicated)} />
                <MetricLine label="Failed" value={formatNumber(refreshResult.totalFailed || refreshResult.postingsFailed)} />
                <MetricLine label="Run" value={shortId(refreshResult.runId)} />
              </div>
              {refreshResult.errorSummary && <div className="mt-2 text-red-700">{refreshResult.errorSummary}</div>}
            </div>
          )}
        </div>

        <div className="rounded-lg border border-[#B9D8CC] bg-white p-5 shadow-sm">
          <div className="flex items-center justify-between gap-3">
            <h2 className="text-lg font-black text-[#18332D]">Python crawler health</h2>
            <StatusBadge status={externalHealth?.status || "unavailable"} />
          </div>
          <p className="mt-1 text-xs font-semibold leading-5 text-slate-500">
            Direct health from the Python crawler, independent of the latest .NET import.
          </p>
          <div className="mt-4 space-y-2 text-sm font-semibold text-slate-700">
            <MetricLine label="API available" value={externalHealth?.isAvailable ? "yes" : "no"} />
            <MetricLine label="Data freshness" value={externalHealth?.isStale ? "stale" : "fresh"} />
            <MetricLine label="Last successful crawl" value={formatDate(externalHealth?.latestSuccessfulCrawlAt)} />
            <MetricLine label="Crawl age" value={formatHours(externalHealth?.hoursSinceSuccessfulCrawl)} />
            <MetricLine label="Latest crawler run" value={externalHealth?.latestListingStatus || "-"} />
            <MetricLine label="Blocked / failed pages" value={`${formatNumber(externalHealth?.pagesBlocked)} / ${formatNumber(externalHealth?.pagesFailed)}`} />
            <MetricLine label="Active jobs" value={formatNumber(externalHealth?.activeJobs)} />
            <MetricLine label="New jobs today" value={formatNumber(externalHealth?.newJobsToday)} />
            <MetricLine label="Detail completion" value={formatPercent(externalHealth?.detailCompletionRate)} />
          </div>
          {(externalHealth?.errorMessage || externalHealth?.warnings?.length > 0) && (
            <div className="mt-4 rounded-md border border-amber-200 bg-amber-50 p-3 text-xs font-semibold leading-5 text-amber-800">
              {externalHealth?.errorMessage || externalHealth.warnings.join(" ")}
            </div>
          )}
        </div>
      </section>

      <section className="rounded-lg border border-[#B9D8CC] bg-white p-5 shadow-sm">
        <h2 className="text-lg font-black text-[#18332D]">.NET import health</h2>
        <p className="mt-1 text-xs font-semibold leading-5 text-slate-500">
          Import status persisted by Roadmap Platform. Retry actions below only queue .NET import failures; they do not retry Python crawler failures.
        </p>
        <div className="mt-4 grid gap-3 lg:grid-cols-2">
            {sourceHealth.length === 0 ? (
              <EmptyState message="No source health records yet." />
            ) : (
              sourceHealth.map((source) => (
                <div key={source.source} className="rounded-md border border-slate-200 bg-slate-50 p-3">
                  <div className="flex items-center justify-between gap-3">
                    <div className="font-extrabold text-slate-900">{source.source}</div>
                    <StatusBadge status={source.status} />
                  </div>
                  <div className="mt-2 text-xs font-semibold leading-5 text-slate-500">
                    Last success {formatDate(source.lastSuccessAt)} | Failures {formatNumber(source.consecutiveFailures)}
                  </div>
                  <div className="mt-1 text-xs font-semibold leading-5 text-slate-500">
                    Source freshness captured by last import {formatDate(source.sourceLatestSuccessAt)}
                  </div>
                  {source.lastErrorSummary && (
                    <div className="mt-2 text-xs font-semibold text-red-700">{source.lastErrorSummary}</div>
                  )}
                </div>
              ))
            )}
        </div>
      </section>

      <section className="rounded-lg border border-[#B9D8CC] bg-white p-5 shadow-sm">
        <SectionHeader icon={History} title="Import Run History" />
        <FilterRow
          filters={runFilters}
          onChange={setRunFilters}
          onApply={loadRuns}
          statusOptions={["", "success", "empty", "failed", "partial_success", "blocked"]}
        />
        <DataTable
          headers={["runId", "source", "status", "mode", "trigger", "startedAt", "finishedAt", "duration", "fetched", "sourceTotal", "syncType", "lifecycle", "lifecycleSkippedReason", "crawlerFreshAt", "imported", "updated", "skipped", "failed", "stoppedReason", "errorSummary"]}
          rows={runs.map((run) => [
            shortId(run.runId),
            run.source,
            <StatusBadge key="status" status={run.status} />,
            run.mode,
            run.triggerType,
            formatDate(run.startedAt),
            formatDate(run.finishedAt),
            formatDuration(run.durationMs),
            formatNumber(run.fetchedCount),
            formatOptionalNumber(run.sourceTotalCount),
            formatSyncType(run),
            formatLifecycleOutcome(run),
            formatLifecycleReason(run.lifecycleSkippedReason),
            formatDate(run.sourceLatestSuccessAt),
            formatNumber(run.importedCount ?? run.savedCount),
            formatNumber(run.updatedCount),
            formatNumber(run.skippedCount ?? run.duplicateCount),
            formatNumber(run.failedCount),
            run.stoppedReason || "-",
            run.errorSummary || "-",
          ])}
        />
      </section>

      <section className="rounded-lg border border-[#B9D8CC] bg-white p-5 shadow-sm">
        <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
          <SectionHeader icon={AlertTriangle} title=".NET Import Failures" compact />
          <div className="flex flex-wrap gap-2">
            <button
              type="button"
              onClick={() => handleRetry(selectedFailedIds)}
              disabled={selectedFailedIds.length === 0}
              className="inline-flex h-9 items-center gap-2 rounded-md border border-blue-200 bg-blue-50 px-3 text-xs font-extrabold text-blue-700 disabled:cursor-not-allowed disabled:opacity-50"
            >
              <RotateCcw size={14} />
              Queue import retry
            </button>
            <button
              type="button"
              onClick={() => handleIgnore(selectedFailedIds)}
              disabled={selectedFailedIds.length === 0}
              className="inline-flex h-9 items-center gap-2 rounded-md border border-slate-200 bg-slate-50 px-3 text-xs font-extrabold text-slate-700 disabled:cursor-not-allowed disabled:opacity-50"
            >
              <ShieldCheck size={14} />
              Ignore selected
            </button>
          </div>
        </div>
        <FilterRow
          filters={failedFilters}
          onChange={setFailedFilters}
          onApply={loadFailedItems}
          statusOptions={["", "open", "retry_queued", "ignored"]}
        />
        <div className="mt-4 overflow-x-auto">
          {failedItems.length === 0 ? (
            <EmptyState message="No .NET import failures for the current filter." />
          ) : (
            <table className="min-w-[1100px] w-full border-collapse text-left text-xs">
              <thead>
                <tr className="border-b border-slate-200 text-slate-500">
                  <th className="py-2 pr-3">Select</th>
                  <th className="py-2 pr-3">failed item id</th>
                  <th className="py-2 pr-3">source</th>
                  <th className="py-2 pr-3">stage</th>
                  <th className="py-2 pr-3">errorCode</th>
                  <th className="py-2 pr-3">errorMessage</th>
                  <th className="py-2 pr-3">retryCount</th>
                  <th className="py-2 pr-3">createdAt</th>
                  <th className="py-2 pr-3">lastRetryAt</th>
                  <th className="py-2 pr-3">status</th>
                  <th className="py-2 pr-3">actions</th>
                </tr>
              </thead>
              <tbody>
                {failedItems.map((item) => (
                  <tr key={item.failedItemId} className="border-b border-slate-100 align-top text-slate-700">
                    <td className="py-3 pr-3">
                      <input
                        type="checkbox"
                        checked={selectedFailedSet.has(item.failedItemId)}
                        onChange={() => toggleFailedSelection(item.failedItemId)}
                      />
                    </td>
                    <td className="py-3 pr-3 font-bold">{shortId(item.failedItemId)}</td>
                    <td className="py-3 pr-3">{item.source}</td>
                    <td className="py-3 pr-3">{item.stage}</td>
                    <td className="py-3 pr-3">{item.errorCode}</td>
                    <td className="max-w-[260px] py-3 pr-3">
                      <div className="font-semibold">{item.errorMessage}</div>
                      {item.url && <div className="mt-1 break-all text-slate-500">{item.url}</div>}
                      {item.errorDetail && (
                        <details className="mt-1">
                          <summary className="cursor-pointer font-bold text-[#1F6F5F]">Error detail</summary>
                          <pre className="mt-2 max-h-40 overflow-auto whitespace-pre-wrap rounded-md bg-slate-900 p-2 text-[11px] text-white">
                            {item.errorDetail}
                          </pre>
                        </details>
                      )}
                    </td>
                    <td className="py-3 pr-3">{formatNumber(item.retryCount)}</td>
                    <td className="py-3 pr-3">{formatDate(item.createdAt)}</td>
                    <td className="py-3 pr-3">{formatDate(item.lastRetryAt)}</td>
                    <td className="py-3 pr-3"><StatusBadge status={item.status} /></td>
                    <td className="py-3 pr-3">
                      <div className="flex flex-wrap gap-2">
                        <button type="button" onClick={() => handleRetry([item.failedItemId])} className="rounded-md bg-blue-50 px-2 py-1 font-extrabold text-blue-700">
                          Queue retry
                        </button>
                        <button type="button" onClick={() => handleIgnore([item.failedItemId])} className="rounded-md bg-slate-100 px-2 py-1 font-extrabold text-slate-700">
                          Ignore
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </section>

      <section className="grid gap-6 xl:grid-cols-[420px_minmax(0,1fr)]">
        <div className="rounded-lg border border-[#B9D8CC] bg-white p-5 shadow-sm">
          <SectionHeader icon={Settings2} title="Category Classifier Tuning" />
          <form className="mt-4 space-y-3" onSubmit={saveMapping}>
            <FormInput label="Keyword" value={mappingForm.keyword} onChange={(value) => setMappingForm((current) => ({ ...current, keyword: value }))} />
            <label className="block">
              <span className="text-xs font-extrabold uppercase text-slate-500">Category</span>
              <select
                value={mappingForm.category}
                onChange={(event) => setMappingForm((current) => ({ ...current, category: event.target.value }))}
                className="mt-1 h-10 w-full rounded-md border border-slate-200 bg-white px-3 text-sm font-bold text-slate-800 outline-none"
              >
                {categories.map((category) => (
                  <option key={category} value={category}>{category}</option>
                ))}
              </select>
            </label>
            <FormInput type="number" label="Weight" value={mappingForm.weight} onChange={(value) => setMappingForm((current) => ({ ...current, weight: Number(value) }))} />
            <label className="flex items-center gap-2 text-sm font-bold text-slate-700">
              <input
                type="checkbox"
                checked={mappingForm.isEnabled}
                onChange={(event) => setMappingForm((current) => ({ ...current, isEnabled: event.target.checked }))}
              />
              Enabled
            </label>
            <div className="flex flex-wrap gap-2">
              <button type="submit" disabled={isSavingMapping} className="inline-flex h-10 items-center gap-2 rounded-md bg-[#1F6F5F] px-4 text-sm font-extrabold text-white disabled:opacity-60">
                <SlidersHorizontal size={16} />
                {editingMappingId ? "Update mapping" : "Add mapping"}
              </button>
              {editingMappingId && (
                <button type="button" onClick={resetMappingForm} className="h-10 rounded-md border border-slate-200 bg-white px-4 text-sm font-extrabold text-slate-700">
                  Cancel
                </button>
              )}
            </div>
          </form>

          <div className="mt-6">
            <label className="block">
              <span className="text-xs font-extrabold uppercase text-slate-500">Test classifier</span>
              <textarea
                value={classifierText}
                onChange={(event) => setClassifierText(event.target.value)}
                rows={5}
                className="mt-1 w-full rounded-md border border-slate-200 bg-white px-3 py-2 text-sm font-semibold text-slate-800 outline-none"
                placeholder="Paste sample job description or market text"
              />
            </label>
            <button type="button" onClick={testClassifier} className="mt-2 inline-flex h-9 items-center gap-2 rounded-md border border-blue-200 bg-blue-50 px-3 text-xs font-extrabold text-blue-700">
              <Search size={14} />
              Test
            </button>
            {classifierResult && (
              <div className="mt-3 rounded-md border border-slate-200 bg-slate-50 p-3 text-sm font-semibold text-slate-700">
                <MetricLine label="Category" value={classifierResult.category} />
                <MetricLine label="Confidence" value={`${Math.round(Number(classifierResult.confidence || 0) * 100)}%`} />
                <MetricLine label="Fallback" value={classifierResult.fallbackCategory} />
                <div className="mt-2 flex flex-wrap gap-2">
                  {(classifierResult.matches || []).map((match) => (
                    <span key={`${match.keyword}-${match.category}`} className="rounded-md bg-white px-2 py-1 text-xs font-bold text-slate-600">
                      {match.keyword} {"->"} {match.category}
                    </span>
                  ))}
                </div>
              </div>
            )}
          </div>
        </div>

        <div className="rounded-lg border border-[#B9D8CC] bg-white p-5 shadow-sm">
          <SectionHeader icon={SlidersHorizontal} title="Keyword -> Category Mappings" />
          <div className="mt-4 overflow-x-auto">
            <table className="min-w-[760px] w-full border-collapse text-left text-xs">
              <thead>
                <tr className="border-b border-slate-200 text-slate-500">
                  <th className="py-2 pr-3">keyword</th>
                  <th className="py-2 pr-3">category</th>
                  <th className="py-2 pr-3">weight</th>
                  <th className="py-2 pr-3">enabled</th>
                  <th className="py-2 pr-3">updatedAt</th>
                  <th className="py-2 pr-3">actions</th>
                </tr>
              </thead>
              <tbody>
                {mappings.map((mapping) => (
                  <tr key={mapping.mappingId} className="border-b border-slate-100 text-slate-700">
                    <td className="py-3 pr-3 font-bold">{mapping.keyword}</td>
                    <td className="py-3 pr-3">{mapping.category}</td>
                    <td className="py-3 pr-3">{mapping.weight}</td>
                    <td className="py-3 pr-3">{mapping.isEnabled ? "on" : "off"}</td>
                    <td className="py-3 pr-3">{formatDate(mapping.updatedAt)}</td>
                    <td className="py-3 pr-3">
                      <div className="flex gap-2">
                        <button type="button" onClick={() => editMapping(mapping)} className="rounded-md bg-blue-50 px-2 py-1 font-extrabold text-blue-700">
                          Edit
                        </button>
                        <button type="button" onClick={() => deleteMapping(mapping.mappingId)} className="rounded-md bg-red-50 px-2 py-1 font-extrabold text-red-700" title="Delete mapping">
                          <Trash2 size={13} />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </section>
    </div>
  );
}

function SectionHeader({ icon: Icon, title, compact = false }) {
  return (
    <div className={`flex items-center gap-2 ${compact ? "" : "mb-1"}`}>
      <span className="inline-flex h-9 w-9 items-center justify-center rounded-md bg-[#6FCF97]/20 text-[#1F6F5F]">
        <Icon size={17} />
      </span>
      <h2 className="text-lg font-black text-[#18332D]">{title}</h2>
    </div>
  );
}

function AdminMetric({ icon: Icon, label, value }) {
  return (
    <div className="rounded-md border border-slate-200 bg-slate-50 p-3">
      <div className="flex items-center gap-2 text-xs font-extrabold uppercase text-slate-500">
        <Icon size={15} />
        {label}
      </div>
      <div className="mt-2 text-xl font-black text-slate-950">{value}</div>
    </div>
  );
}

function FilterRow({ filters, onChange, onApply, statusOptions }) {
  return (
    <div className="mt-4 grid gap-3 md:grid-cols-5">
      <select value={filters.status} onChange={(event) => onChange({ ...filters, status: event.target.value })} className="h-10 rounded-md border border-slate-200 bg-white px-3 text-sm font-bold text-slate-800">
        {statusOptions.map((status) => <option key={status || "all"} value={status}>{status || "all status"}</option>)}
      </select>
      <input value={filters.source} onChange={(event) => onChange({ ...filters, source: event.target.value })} className="h-10 rounded-md border border-slate-200 bg-white px-3 text-sm font-bold text-slate-800" placeholder="source" />
      <input type="date" value={filters.from} onChange={(event) => onChange({ ...filters, from: event.target.value })} className="h-10 rounded-md border border-slate-200 bg-white px-3 text-sm font-bold text-slate-800" />
      <input type="date" value={filters.to} onChange={(event) => onChange({ ...filters, to: event.target.value })} className="h-10 rounded-md border border-slate-200 bg-white px-3 text-sm font-bold text-slate-800" />
      <button type="button" onClick={onApply} className="inline-flex h-10 items-center justify-center gap-2 rounded-md border border-[#B9D8CC] bg-[#F7F1E8] px-3 text-sm font-extrabold text-[#1F6F5F]">
        <RefreshCw size={15} />
        Apply
      </button>
    </div>
  );
}

function RefreshOptionInput({ label, value, min, max, onChange }) {
  return (
    <label className="block">
      <span className="text-xs font-extrabold uppercase text-slate-500">{label}</span>
      <input
        type="number"
        value={value}
        min={min}
        max={max}
        step="1"
        onChange={(event) => onChange(event.target.value)}
        className="mt-1 h-10 w-full rounded-md border border-slate-200 bg-white px-3 text-sm font-bold text-slate-800 outline-none"
      />
    </label>
  );
}

function DataTable({ headers, rows }) {
  if (!rows.length) {
    return <EmptyState message="No rows for the current filter." />;
  }

  return (
    <div className="mt-4 overflow-x-auto">
      <table className="min-w-[1180px] w-full border-collapse text-left text-xs">
        <thead>
          <tr className="border-b border-slate-200 text-slate-500">
            {headers.map((header) => <th key={header} className="py-2 pr-3">{header}</th>)}
          </tr>
        </thead>
        <tbody>
          {rows.map((row, rowIndex) => (
            <tr key={rowIndex} className="border-b border-slate-100 text-slate-700">
              {row.map((cell, cellIndex) => (
                <td key={`${rowIndex}-${cellIndex}`} className="max-w-[240px] py-3 pr-3 align-top">
                  {cell}
                </td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function FormInput({ label, value, onChange, type = "text" }) {
  return (
    <label className="block">
      <span className="text-xs font-extrabold uppercase text-slate-500">{label}</span>
      <input
        type={type}
        value={value}
        min={type === "number" ? "0.1" : undefined}
        step={type === "number" ? "0.1" : undefined}
        onChange={(event) => onChange(event.target.value)}
        className="mt-1 h-10 w-full rounded-md border border-slate-200 bg-white px-3 text-sm font-bold text-slate-800 outline-none"
      />
    </label>
  );
}

function StatusBadge({ status }) {
  const value = String(status || "unknown");
  const tone = value === "success" || value === "healthy"
    ? "bg-emerald-100 text-emerald-700"
    : value === "failed" || value === "blocked" || value === "critical" || value === "unavailable" || value === "unauthorized" || value === "rate_limited"
      ? "bg-red-100 text-red-700"
      : value === "open" || value === "retry_queued" || value === "warning" || value === "stale"
        ? "bg-amber-100 text-amber-700"
        : "bg-slate-100 text-slate-700";

  return <span className={`rounded-md px-2 py-1 text-xs font-extrabold ${tone}`}>{value}</span>;
}

function MetricLine({ label, value }) {
  return (
    <div className="flex items-center justify-between gap-3 rounded-md bg-white/70 px-2 py-1">
      <span>{label}</span>
      <span className="text-right font-extrabold">{value || "-"}</span>
    </div>
  );
}

function EmptyState({ message }) {
  return (
    <div className="mt-4 rounded-md border border-dashed border-slate-300 bg-slate-50 px-4 py-8 text-center text-sm font-bold text-slate-500">
      {message}
    </div>
  );
}

function normalizeDateFilters(filters) {
  return {
    ...filters,
    from: filters.from ? new Date(`${filters.from}T00:00:00`).toISOString() : "",
    to: filters.to ? new Date(`${filters.to}T23:59:59`).toISOString() : "",
  };
}

function normalizeRefreshOptions(options) {
  return Object.fromEntries(
    Object.entries(options)
      .map(([key, value]) => [key, Math.trunc(Number(value))])
      .filter(([, value]) => Number.isFinite(value) && value > 0)
  );
}

function formatNumber(value) {
  return numberFormatter.format(Number(value || 0));
}

function formatOptionalNumber(value) {
  return value === null || value === undefined ? "unknown" : formatNumber(value);
}

function formatSyncType(run) {
  if (!run) return "n/a";
  return run.isCompleteSync ? "Full sync" : "Partial sync";
}

function formatLifecycleOutcome(run) {
  if (!run) return "n/a";
  return run.missingLifecycleApplied ? "Applied" : "Skipped";
}

function formatLifecycleReason(reason) {
  if (!reason) return "-";

  const labels = {
    partial_sync: "partial sync protection",
    source_freshness_invalid: "source freshness invalid",
    fetch_status_not_eligible: "fetch status not eligible",
    below_minimum_posting_threshold: "below minimum posting threshold",
    manual_ingest_without_complete_sync_metadata: "manual ingest has no complete-sync metadata",
  };

  return labels[reason] || String(reason).replaceAll("_", " ");
}

function formatDate(value) {
  return value ? dateFormatter.format(new Date(value)) : "-";
}

function formatDuration(ms) {
  const normalized = Number(ms || 0);
  if (!normalized) return "-";
  if (normalized < 1000) return `${normalized}ms`;
  return `${Math.round(normalized / 1000)}s`;
}

function formatHours(value) {
  const normalized = Number(value);
  return Number.isFinite(normalized) ? `${normalized.toFixed(1)}h` : "unknown";
}

function formatPercent(value) {
  const normalized = Number(value);
  return Number.isFinite(normalized) ? `${Math.round(normalized * 100)}%` : "-";
}

function shortId(value) {
  return value ? String(value).slice(0, 8) : "-";
}
