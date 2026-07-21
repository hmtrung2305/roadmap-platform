import { ChevronRight, FlaskConical, LoaderCircle, RotateCcw, Trash2, X } from "lucide-react";
import { useEffect, useRef, useState } from "react";
import { marketPulseApi } from "../../api/marketPulseApi";
import { errorMessage, formatDateTime, formatInteger } from "./adminViewModel";
import { StatusPill } from "./PipelineStepper";

const tabs = [
  { key: "overview", label: "Overview" },
  { key: "imports", label: "Import runs" },
  { key: "failures", label: "Failures" },
  { key: "classifier", label: "Classifier" },
];

export default function AdminDetailTabs({ requestedTab, onTabChange }) {
  const activeTab = requestedTab ?? "overview";
  return (
    <section className="rounded-2xl border border-[#B9D8CC] bg-white">
      <div className="overflow-x-auto border-b border-[#DCEBE5] px-2" role="tablist" aria-label="TopCV operations details">
        <div className="flex min-w-max gap-1">
          {tabs.map((tab) => (
            <button key={tab.key} type="button" role="tab" aria-selected={activeTab === tab.key} onClick={() => onTabChange(tab.key)} className={`min-h-12 border-b-2 px-4 text-sm font-extrabold focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#2FA084] ${activeTab === tab.key ? "border-[#1F6F5F] text-[#1F6F5F]" : "border-transparent text-slate-500"}`}>{tab.label}</button>
          ))}
        </div>
      </div>
      <div className="p-4 sm:p-5">
        {activeTab === "overview" && <p className="text-sm font-semibold leading-6 text-slate-500">Operational details are loaded only when you open a tab, keeping the initial dashboard fast and focused.</p>}
        {activeTab === "imports" && <ImportRunsPanel />}
        {activeTab === "failures" && <FailuresPanel />}
        {activeTab === "classifier" && <ClassifierPanel />}
      </div>
    </section>
  );
}

function ImportRunsPanel() {
  const { data, loading, error, reload } = useLazyResource(() => marketPulseApi.getImportRuns({ limit: 50 }));
  const runs = listValue(data, "items", "runs", "importRuns");
  const [selected, setSelected] = useState(null);
  if (loading && !data) return <Loading label="Loading import runs" />;
  if (error && !data) return <ErrorState message={error} onRetry={reload} />;
  return (
    <div>
      <PanelHeader title=".NET import runs" description="TopCV batches accepted by the canonical PostgreSQL store." onReload={reload} loading={loading} />
      {runs.length === 0 ? <Empty message="No import run has been recorded." /> : (
        <div className="mt-4 overflow-x-auto rounded-xl border border-[#DCEBE5]">
          <table className="w-full min-w-[760px] text-left text-xs">
            <thead className="bg-[#EAF8F1] text-[#18332D]"><tr><th className="px-3 py-3">Started</th><th className="px-3 py-3">Status</th><th className="px-3 py-3">Mode</th><th className="px-3 py-3 text-right">Fetched</th><th className="px-3 py-3 text-right">Imported</th><th className="px-3 py-3 text-right">Failed</th><th className="px-3 py-3"><span className="sr-only">Details</span></th></tr></thead>
            <tbody>{runs.map((run) => (
              <tr key={run.id ?? run.runId} className="border-t border-[#DCEBE5] bg-white">
                <td className="px-3 py-3 font-semibold text-slate-600">{formatDateTime(run.startedAt)}</td><td className="px-3 py-3"><StatusPill status={run.status} /></td><td className="px-3 py-3 font-semibold text-slate-600">{run.mode ?? run.trigger ?? "latest"}</td><td className="px-3 py-3 text-right font-bold">{formatInteger(run.fetchedCount)}</td><td className="px-3 py-3 text-right font-bold">{formatInteger(run.importedCount)}</td><td className="px-3 py-3 text-right font-bold">{formatInteger(run.failedCount)}</td><td className="px-3 py-3 text-right"><button type="button" onClick={() => setSelected(run)} aria-label="Open import run details" className="inline-grid h-11 w-11 place-items-center rounded-lg text-[#1F6F5F] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#2FA084]"><ChevronRight size={18} /></button></td>
              </tr>
            ))}</tbody>
          </table>
        </div>
      )}
      {selected && <RunDrawer run={selected} onClose={() => setSelected(null)} />}
    </div>
  );
}

function FailuresPanel() {
  const { data, loading, error, reload } = useLazyResource(() => marketPulseApi.getOperationsFailures({ status: "open", limit: 100 }));
  const groupedCrawlerFailures = listValue(data, "crawlerFailures").map((item) => ({ ...item, origin: "crawler" }));
  const groupedImportFailures = listValue(data, "importFailures").map((item) => ({ ...item, origin: "import" }));
  const failures = [...listValue(data, "items", "failures"), ...groupedCrawlerFailures, ...groupedImportFailures];
  const [selectedIds, setSelectedIds] = useState([]);
  const [actionState, setActionState] = useState({ loading: false, error: "", message: "" });
  const crawlerFailures = failures.filter((item) => String(item.type ?? item.origin ?? "import").toLowerCase().includes("crawler"));
  const importFailures = failures.filter((item) => !crawlerFailures.includes(item));
  const runBulk = async (action) => {
    if (selectedIds.length === 0) return;
    setActionState({ loading: true, error: "", message: "" });
    try {
      if (action === "retry") await marketPulseApi.retryOperationsFailures(selectedIds);
      else await marketPulseApi.ignoreOperationsFailures(selectedIds);
      setSelectedIds([]);
      setActionState({
        loading: false,
        error: "",
        message: action === "retry"
          ? `Retry requested for ${selectedIds.length} failure(s). Check the refreshed statuses for the outcome.`
          : `${selectedIds.length} failure(s) ignored.`,
      });
      reload();
    } catch (requestError) {
      setActionState({ loading: false, error: errorMessage(requestError), message: "" });
    }
  };
  if (loading && !data) return <Loading label="Loading failures" />;
  if (error && !data) return <ErrorState message={error} onRetry={reload} />;
  return (
    <div>
      <PanelHeader title="Failure queues" description="Crawler and import failures stay separate so ownership and recovery are clear." onReload={reload} loading={loading} />
      <div className="mt-4 flex flex-wrap items-center gap-2">
        <button type="button" disabled={!selectedIds.length || actionState.loading} onClick={() => runBulk("retry")} className="min-h-11 rounded-xl bg-[#1F6F5F] px-4 text-sm font-extrabold text-white disabled:opacity-40">Retry selected</button>
        <button type="button" disabled={!selectedIds.length || actionState.loading} onClick={() => runBulk("ignore")} className="min-h-11 rounded-xl border border-[#B9D8CC] px-4 text-sm font-extrabold text-slate-600 disabled:opacity-40">Ignore selected</button>
        <span className="text-xs font-bold text-slate-500">{selectedIds.length} selected</span>
      </div>
      <div aria-live="polite">{actionState.error && <InlineMessage tone="error">{actionState.error}</InlineMessage>}{actionState.message && <InlineMessage tone="success">{actionState.message}</InlineMessage>}</div>
      <div className="mt-4 grid gap-4 xl:grid-cols-2"><FailureGroup title="Crawler failures" items={crawlerFailures} selectedIds={selectedIds} onSelection={setSelectedIds} /><FailureGroup title="Import failures" items={importFailures} selectedIds={selectedIds} onSelection={setSelectedIds} /></div>
    </div>
  );
}

function FailureGroup({ title, items, selectedIds, onSelection }) {
  return (
    <section className="rounded-xl border border-[#DCEBE5] bg-[#FCFAF6] p-3">
      <div className="flex items-center justify-between gap-2"><h3 className="text-sm font-extrabold text-[#18332D]">{title}</h3><span className="rounded-full bg-white px-2 py-1 text-xs font-extrabold text-slate-600">{items.length}</span></div>
      {items.length === 0 ? <p className="mt-3 text-sm font-semibold text-slate-500">No open failures.</p> : <div className="mt-3 space-y-2">{items.map((item) => {
        const id = item.id ?? item.failureId ?? item.failedItemId;
        const checked = selectedIds.includes(id);
        const actionable = item.actionable !== false;
        const failureLabel = item.title ?? (item.stage ? `${String(item.stage).replaceAll("_", " ")} failure` : "Pipeline failure");
        return <label key={id} className={`flex min-h-16 items-start gap-3 rounded-lg border border-[#DCEBE5] bg-white p-3 ${actionable ? "cursor-pointer" : "cursor-not-allowed opacity-70"}`}><input type="checkbox" checked={checked} disabled={!actionable} onChange={() => { if (actionable) onSelection(checked ? selectedIds.filter((value) => value !== id) : [...selectedIds, id]); }} className="mt-1 h-5 w-5 accent-[#1F6F5F]" /><span className="min-w-0"><span className="block truncate text-xs font-extrabold capitalize text-[#18332D]">{failureLabel}</span><span className="mt-1 block text-xs font-semibold leading-5 text-slate-500">{errorMessage(item.errorMessage ?? item.error ?? item.errorSummary ?? item.message, "No error summary")}</span>{!actionable && <span className="mt-1 block text-[11px] font-bold text-amber-700">Read-only health signal</span>}</span></label>;
      })}</div>}
    </section>
  );
}

function ClassifierPanel() {
  const loader = async () => {
    const [categories, mappings] = await Promise.all([marketPulseApi.getClassifierCategories(), marketPulseApi.getClassifierMappings()]);
    return { categories: listValue(categories, "items", "categories"), mappings: listValue(mappings, "items", "mappings") };
  };
  const { data, loading, error, reload } = useLazyResource(loader);
  const [testText, setTestText] = useState("");
  const [testResult, setTestResult] = useState(null);
  const [keyword, setKeyword] = useState("");
  const [category, setCategory] = useState("");
  const [working, setWorking] = useState(false);
  const [actionError, setActionError] = useState("");
  const categories = data?.categories ?? [];
  const mappings = data?.mappings ?? [];
  const runTest = async () => {
    if (!testText.trim()) return;
    setWorking(true); setActionError("");
    try { setTestResult(await marketPulseApi.testClassifier(testText.trim())); } catch (requestError) { setActionError(errorMessage(requestError)); } finally { setWorking(false); }
  };
  const addMapping = async () => {
    if (!keyword.trim() || !category) return;
    setWorking(true); setActionError("");
    try { await marketPulseApi.createClassifierMapping({ keyword: keyword.trim(), category }); setKeyword(""); reload(); } catch (requestError) { setActionError(errorMessage(requestError)); } finally { setWorking(false); }
  };
  const removeMapping = async (id) => {
    setWorking(true); setActionError("");
    try { await marketPulseApi.deleteClassifierMapping(id); reload(); } catch (requestError) { setActionError(errorMessage(requestError)); } finally { setWorking(false); }
  };
  if (loading && !data) return <Loading label="Loading classifier" />;
  if (error && !data) return <ErrorState message={error} onRetry={reload} />;
  return (
    <div>
      <PanelHeader title="Classifier laboratory" description="Test categorization and maintain keyword mappings without mixing them with pipeline health." onReload={reload} loading={loading} />
      {actionError && <InlineMessage tone="error">{actionError}</InlineMessage>}
      <div className="mt-4 grid gap-4 xl:grid-cols-2">
        <section className="rounded-xl border border-[#DCEBE5] bg-[#FCFAF6] p-4"><h3 className="text-sm font-extrabold text-[#18332D]">Test a job title or description</h3><textarea value={testText} onChange={(event) => setTestText(event.target.value)} rows="4" className="mt-3 w-full rounded-xl border border-[#B9D8CC] bg-white p-3 text-sm outline-none focus:ring-2 focus:ring-[#2FA084]" placeholder="Example: Senior .NET Backend Developer" /><button type="button" disabled={working || !testText.trim()} onClick={runTest} className="mt-3 inline-flex min-h-11 items-center gap-2 rounded-xl bg-[#1F6F5F] px-4 text-sm font-extrabold text-white disabled:opacity-40"><FlaskConical size={16} />Run classifier</button>{testResult && <pre className="mt-3 max-h-52 overflow-auto whitespace-pre-wrap rounded-lg bg-[#18332D] p-3 text-xs text-white">{JSON.stringify(testResult, null, 2)}</pre>}</section>
        <section className="rounded-xl border border-[#DCEBE5] bg-[#FCFAF6] p-4"><h3 className="text-sm font-extrabold text-[#18332D]">Add keyword mapping</h3><div className="mt-3 grid gap-2 sm:grid-cols-[minmax(0,1fr)_minmax(160px,0.7fr)]"><input value={keyword} onChange={(event) => setKeyword(event.target.value)} className="min-h-11 rounded-xl border border-[#B9D8CC] bg-white px-3 text-sm outline-none focus:ring-2 focus:ring-[#2FA084]" placeholder="Keyword" /><select value={category} onChange={(event) => setCategory(event.target.value)} className="min-h-11 rounded-xl border border-[#B9D8CC] bg-white px-3 text-sm font-bold"><option value="">Choose category</option>{categories.map((item) => { const value = typeof item === "string" ? item : item.name ?? item.label ?? item.category; return <option key={typeof item === "string" ? item : item.id ?? item.categoryId ?? value} value={value}>{value}</option>; })}</select></div><button type="button" disabled={working || !keyword.trim() || !category} onClick={addMapping} className="mt-3 min-h-11 rounded-xl bg-[#1F6F5F] px-4 text-sm font-extrabold text-white disabled:opacity-40">Add mapping</button></section>
      </div>
      <section className="mt-4 rounded-xl border border-[#DCEBE5] p-4"><h3 className="text-sm font-extrabold text-[#18332D]">Keyword mappings ({mappings.length})</h3>{mappings.length === 0 ? <p className="mt-3 text-sm text-slate-500">No custom mappings.</p> : <div className="mt-3 grid gap-2 sm:grid-cols-2 xl:grid-cols-3">{mappings.map((mapping) => <article key={mapping.id ?? mapping.mappingId} className="flex items-center justify-between gap-3 rounded-lg bg-[#FCFAF6] p-3"><div className="min-w-0"><div className="truncate text-sm font-extrabold text-[#18332D]">{mapping.keyword}</div><div className="mt-1 truncate text-xs text-slate-500">{mapping.categoryName ?? mapping.category ?? "Uncategorized"}</div></div><button type="button" disabled={working} onClick={() => removeMapping(mapping.id ?? mapping.mappingId)} aria-label={`Delete ${mapping.keyword} mapping`} className="grid h-11 w-11 shrink-0 place-items-center rounded-lg text-red-700 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-red-500"><Trash2 size={16} /></button></article>)}</div>}</section>
    </div>
  );
}

function useLazyResource(loader) {
  const [version, setVersion] = useState(0);
  const [state, setState] = useState({ data: null, loading: true, error: "" });
  useEffect(() => {
    let active = true;
    setState((current) => ({ ...current, loading: true, error: "" }));
    loader().then((data) => { if (active) setState({ data, loading: false, error: "" }); }).catch((error) => { if (active) setState((current) => ({ ...current, loading: false, error: errorMessage(error) })); });
    return () => { active = false; };
  // The loader is intentionally captured once per mounted panel; `version` controls reloads.
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [version]);
  return { ...state, reload: () => setVersion((value) => value + 1) };
}

function PanelHeader({ title, description, onReload, loading }) {
  return <div className="flex flex-col gap-3 sm:flex-row sm:items-start sm:justify-between"><div><h2 className="text-base font-extrabold text-[#18332D]">{title}</h2><p className="mt-1 text-xs font-semibold leading-5 text-slate-500">{description}</p></div><button type="button" onClick={onReload} disabled={loading} className="inline-flex min-h-11 items-center gap-2 self-start rounded-xl border border-[#B9D8CC] px-3 text-xs font-extrabold text-[#1F6F5F] disabled:opacity-50"><RotateCcw size={15} className={loading ? "animate-spin" : ""} />Reload</button></div>;
}

function RunDrawer({ run, onClose }) {
  const entries = Object.entries(run).filter(([key, value]) => value !== null && value !== undefined && value !== "" && !/(^id$|id$)/i.test(key));
  const drawerRef = useRef(null);
  const closeButtonRef = useRef(null);
  const onCloseRef = useRef(onClose);

  useEffect(() => {
    onCloseRef.current = onClose;
  }, [onClose]);

  useEffect(() => {
    const previouslyFocused = document.activeElement instanceof HTMLElement ? document.activeElement : null;
    closeButtonRef.current?.focus();

    const onKeyDown = (event) => {
      if (event.key === "Escape") {
        event.preventDefault();
        onCloseRef.current();
        return;
      }
      if (event.key !== "Tab") return;

      const focusable = [...(drawerRef.current?.querySelectorAll(
        'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])',
      ) ?? [])].filter((element) => !element.hasAttribute("hidden"));
      if (focusable.length === 0) {
        event.preventDefault();
        return;
      }
      const first = focusable[0];
      const last = focusable[focusable.length - 1];
      if (event.shiftKey && (document.activeElement === first || !drawerRef.current?.contains(document.activeElement))) {
        event.preventDefault();
        last.focus();
      } else if (!event.shiftKey && (document.activeElement === last || !drawerRef.current?.contains(document.activeElement))) {
        event.preventDefault();
        first.focus();
      }
    };

    document.addEventListener("keydown", onKeyDown);
    return () => {
      document.removeEventListener("keydown", onKeyDown);
      previouslyFocused?.focus();
    };
  }, []);

  return (
    <div className="fixed inset-0 z-50 flex justify-end bg-slate-950/30" role="dialog" aria-modal="true" aria-labelledby="run-drawer-title" onMouseDown={(event) => { if (event.currentTarget === event.target) onClose(); }}>
      <div ref={drawerRef} className="h-full w-full max-w-lg overflow-y-auto bg-white p-5 shadow-2xl">
        <div className="flex items-start justify-between gap-3">
          <div><h2 id="run-drawer-title" className="text-xl font-extrabold text-[#18332D]">Import run detail</h2><p className="mt-1 text-xs font-semibold text-slate-500">TopCV import execution</p></div>
          <button ref={closeButtonRef} type="button" onClick={onClose} aria-label="Close details" className="grid h-11 w-11 place-items-center rounded-lg text-slate-600 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#2FA084]"><X /></button>
        </div>
        <dl className="mt-5 divide-y divide-[#DCEBE5]">{entries.map(([key, value]) => <div key={key} className="grid grid-cols-[140px_minmax(0,1fr)] gap-3 py-3 text-xs"><dt className="font-bold text-slate-500">{key}</dt><dd className="break-words font-semibold text-[#18332D]">{typeof value === "object" ? JSON.stringify(value) : String(value)}</dd></div>)}</dl>
      </div>
    </div>
  );
}

function Loading({ label }) { return <div role="status" className="flex min-h-48 items-center justify-center gap-2 text-sm font-bold text-slate-500"><LoaderCircle className="animate-spin" size={20} />{label}...</div>; }
function ErrorState({ message, onRetry }) { return <div role="alert" className="flex min-h-48 flex-col items-center justify-center gap-3 rounded-xl border border-red-200 bg-red-50 p-5 text-center text-sm font-bold text-red-800"><span>{message}</span><button type="button" onClick={onRetry} className="min-h-11 rounded-xl border border-current px-4">Try again</button></div>; }
function Empty({ message }) { return <div className="mt-4 grid min-h-40 place-items-center rounded-xl border border-dashed border-[#B9D8CC] px-4 text-center text-sm font-semibold text-slate-500">{message}</div>; }
function InlineMessage({ tone, children }) { return <div role={tone === "error" ? "alert" : "status"} className={`mt-3 rounded-xl px-3 py-2 text-sm font-bold ${tone === "error" ? "bg-red-50 text-red-800" : "bg-emerald-50 text-emerald-800"}`}>{children}</div>; }

function listValue(value, ...keys) {
  if (Array.isArray(value)) return value;
  for (const key of keys) if (Array.isArray(value?.[key])) return value[key];
  return [];
}
