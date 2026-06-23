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

export default function AdminMarketPulsePage() {
  const [runs, setRuns] = useState([]);
  const [failedItems, setFailedItems] = useState([]);
  const [mappings, setMappings] = useState([]);
  const [categories, setCategories] = useState([]);
  const [sourceHealth, setSourceHealth] = useState([]);
  const [runFilters, setRunFilters] = useState({ status: "", source: "", from: "", to: "" });
  const [failedFilters, setFailedFilters] = useState({ status: "open", source: "", from: "", to: "" });
  const [selectedFailedIds, setSelectedFailedIds] = useState([]);
  const [mappingForm, setMappingForm] = useState(emptyMappingForm);
  const [editingMappingId, setEditingMappingId] = useState("");
  const [classifierText, setClassifierText] = useState("");
  const [classifierResult, setClassifierResult] = useState(null);
  const [refreshResult, setRefreshResult] = useState(null);
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

    try {
      const [runData, failedData, mappingData, categoryData, healthData] = await Promise.all([
        marketPulseApi.getCrawlRuns({ ...normalizeDateFilters(runFilters), limit: 50 }),
        marketPulseApi.getFailedItems({ ...normalizeDateFilters(failedFilters), limit: 50 }),
        marketPulseApi.getClassifierMappings(),
        marketPulseApi.getClassifierCategories(),
        marketPulseApi.getSourceHealth(),
      ]);

      setRuns(runData || []);
      setFailedItems(failedData || []);
      setMappings(mappingData || []);
      setCategories(categoryData || []);
      setSourceHealth(healthData || []);
    } catch (error) {
      setActionError(error?.message || "Unable to load Market Pulse admin data.");
    } finally {
      setIsLoading(false);
    }
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
      const result = await marketPulseApi.refresh();
      setRefreshResult(result);
      setRefreshCooldownUntil(Date.now() + 30_000);
      await loadAdminData();
    } catch (error) {
      setActionError(error?.message || "Manual refresh failed.");
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
          Operate crawl refreshes, monitor data health, review failed items, and tune category classification.
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
              <h2 className="text-lg font-black text-[#18332D]">Crawl status overview</h2>
              <p className="mt-1 text-sm font-semibold text-slate-500">
                Latest runs, source freshness, and failed item queue.
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

          <div className="mt-5 grid gap-3 sm:grid-cols-2 xl:grid-cols-4">
            <AdminMetric icon={History} label="Latest status" value={runs[0]?.status || "n/a"} />
            <AdminMetric icon={Database} label="Fetched" value={formatNumber(runs[0]?.fetchedCount)} />
            <AdminMetric icon={CheckCircle2} label="Saved" value={formatNumber(runs[0]?.savedCount)} />
            <AdminMetric icon={XCircle} label="Open failed" value={formatNumber(failedItems.filter((item) => item.status === "open").length)} />
          </div>

          {refreshResult && (
            <div className="mt-5 rounded-md border border-emerald-200 bg-emerald-50 p-4 text-sm font-semibold text-emerald-800">
              <div className="font-extrabold">Manual refresh result</div>
              <div className="mt-2 grid gap-2 md:grid-cols-2 xl:grid-cols-4">
                <MetricLine label="Started" value={formatDate(refreshResult.startedAt)} />
                <MetricLine label="Finished" value={formatDate(refreshResult.finishedAt)} />
                <MetricLine label="Status" value={refreshResult.status} />
                <MetricLine label="Fetched" value={formatNumber(refreshResult.totalFetched || refreshResult.postingsScraped)} />
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
          <h2 className="text-lg font-black text-[#18332D]">Source health</h2>
          <div className="mt-4 space-y-3">
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
                  {source.lastErrorSummary && (
                    <div className="mt-2 text-xs font-semibold text-red-700">{source.lastErrorSummary}</div>
                  )}
                </div>
              ))
            )}
          </div>
        </div>
      </section>

      <section className="rounded-lg border border-[#B9D8CC] bg-white p-5 shadow-sm">
        <SectionHeader icon={History} title="Crawl Run History" />
        <FilterRow
          filters={runFilters}
          onChange={setRunFilters}
          onApply={loadRuns}
          statusOptions={["", "success", "empty", "failed", "partial_success", "blocked"]}
        />
        <DataTable
          headers={["runId", "source", "status", "mode", "startedAt", "finishedAt", "duration", "fetched", "saved", "duplicated", "failed", "stoppedReason", "errorSummary"]}
          rows={runs.map((run) => [
            shortId(run.runId),
            run.source,
            <StatusBadge key="status" status={run.status} />,
            run.mode,
            formatDate(run.startedAt),
            formatDate(run.finishedAt),
            formatDuration(run.durationMs),
            formatNumber(run.fetchedCount),
            formatNumber(run.savedCount),
            formatNumber(run.duplicateCount),
            formatNumber(run.failedCount),
            run.stoppedReason || "-",
            run.errorSummary || "-",
          ])}
        />
      </section>

      <section className="rounded-lg border border-[#B9D8CC] bg-white p-5 shadow-sm">
        <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
          <SectionHeader icon={AlertTriangle} title="Failed Jobs" compact />
          <div className="flex flex-wrap gap-2">
            <button
              type="button"
              onClick={() => handleRetry(selectedFailedIds)}
              disabled={selectedFailedIds.length === 0}
              className="inline-flex h-9 items-center gap-2 rounded-md border border-blue-200 bg-blue-50 px-3 text-xs font-extrabold text-blue-700 disabled:cursor-not-allowed disabled:opacity-50"
            >
              <RotateCcw size={14} />
              Retry selected
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
            <EmptyState message="No failed items for the current filter." />
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
                          Retry
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
  const tone = value === "success"
    ? "bg-emerald-100 text-emerald-700"
    : value === "failed" || value === "blocked"
      ? "bg-red-100 text-red-700"
      : value === "open" || value === "retry_queued"
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

function formatNumber(value) {
  return numberFormatter.format(Number(value || 0));
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

function shortId(value) {
  return value ? String(value).slice(0, 8) : "-";
}
