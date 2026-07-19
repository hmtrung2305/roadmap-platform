import { Activity, AlertTriangle, ChevronDown, ExternalLink, LoaderCircle, Play, RefreshCw, RotateCcw } from "lucide-react";
import { useCallback, useEffect, useRef, useState } from "react";
import { Link } from "react-router-dom";
import { marketPulseApi } from "../../api/marketPulseApi";
import AdminDashboard from "../../features/marketPulseAdmin/AdminDashboard";
import AdminDetailTabs from "../../features/marketPulseAdmin/AdminDetailTabs";
import PipelineStepper, { StatusPill } from "../../features/marketPulseAdmin/PipelineStepper";
import {
  ACTIVE_OPERATION_STATUSES,
  errorMessage,
  formatDateTime,
  normalizeDashboard,
  normalizeOperation,
} from "../../features/marketPulseAdmin/adminViewModel";

export default function AdminMarketPulsePage() {
  const [dashboard, setDashboard] = useState(null);
  const [operation, setOperation] = useState(null);
  const [activeTab, setActiveTab] = useState("overview");
  const [loading, setLoading] = useState(true);
  const [refreshingDashboard, setRefreshingDashboard] = useState(false);
  const [startingRefresh, setStartingRefresh] = useState(false);
  const [error, setError] = useState("");
  const [actionMessage, setActionMessage] = useState("");
  const [actionGuidance, setActionGuidance] = useState("");
  const pollTimerRef = useRef(null);

  const loadDashboard = useCallback(async ({ quiet = false } = {}) => {
    if (quiet) setRefreshingDashboard(true);
    else setLoading(true);
    setError("");
    try {
      const raw = await marketPulseApi.getAdminDashboard();
      const normalized = normalizeDashboard(raw);
      setDashboard(normalized);
      if (normalized.currentOperation) setOperation(normalized.currentOperation);
    } catch (requestError) {
      setError(errorMessage(requestError, "Unable to load the TopCV operations dashboard."));
    } finally {
      setLoading(false);
      setRefreshingDashboard(false);
    }
  }, []);

  useEffect(() => { loadDashboard(); }, [loadDashboard]);

  useEffect(() => {
    if (pollTimerRef.current) clearTimeout(pollTimerRef.current);
    if (!operation?.id || !ACTIVE_OPERATION_STATUSES.has(operation.status)) return undefined;
    let active = true;
    const poll = async () => {
      try {
        const latest = normalizeOperation(await marketPulseApi.getRefreshOperation(operation.id));
        if (!active) return;
        setOperation(latest);
        if (ACTIVE_OPERATION_STATUSES.has(latest.status)) pollTimerRef.current = setTimeout(poll, 2000);
        else {
          setActionMessage(latest.status === "success" ? "TopCV market data is ready." : "The refresh stopped before analytics were published.");
          loadDashboard({ quiet: true });
        }
      } catch (requestError) {
        if (!active) return;
        setError(errorMessage(requestError, "Refresh status polling failed. The operation is still persisted on the server."));
        pollTimerRef.current = setTimeout(poll, 5000);
      }
    };
    pollTimerRef.current = setTimeout(poll, 1000);
    return () => { active = false; if (pollTimerRef.current) clearTimeout(pollTimerRef.current); };
  }, [loadDashboard, operation?.id, operation?.status]);

  const startRefresh = async () => {
    setStartingRefresh(true);
    setError("");
    setActionMessage("");
    try {
      const next = normalizeOperation(await marketPulseApi.createRefreshOperation());
      setOperation(next);
      setActionMessage("Refresh queued. The page can be safely reloaded while it runs.");
    } catch (requestError) {
      const details = requestError?.details ?? requestError?.response?.data?.error?.details ?? requestError?.response?.data?.details;
      const existing = details?.operation ?? details?.currentOperation ?? (details?.operationId ? details : null);
      const code = requestError?.code ?? requestError?.response?.data?.error?.code;
      if (String(code ?? "").toLowerCase().includes("refresh_running") || requestError?.response?.status === 409) {
        if (existing) setOperation(normalizeOperation(existing));
        setError("A TopCV refresh is already active. Its current progress is shown below.");
      } else setError(errorMessage(requestError));
    } finally {
      setStartingRefresh(false);
    }
  };

  const focusAdvancedActions = () => {
    const target = document.getElementById("advanced-market-pulse-actions");
    target?.scrollIntoView?.({ behavior: "smooth", block: "start" });
    target?.focus({ preventScroll: true });
  };

  const handleAlertAction = (actionType) => {
    if (actionType === "history_sync") focusAdvancedActions();
    else if (actionType === "view_failures") setActiveTab("failures");
    else if (actionType === "view_imports") setActiveTab("imports");
    else if (actionType === "post_date_backfill") {
      setActionGuidance("Run `python -m crawler.post_date_backfill --apply` in the Python Jobs API project, then run Historical sync below. The browser cannot execute this host CLI safely.");
      focusAdvancedActions();
    }
    else if (actionType === "refresh") startRefresh();
  };

  if (loading && !dashboard) return <PageLoading />;
  if (error && !dashboard) return <PageError message={error} onRetry={() => loadDashboard()} />;

  return (
    <main className="tm-page mx-auto min-h-[calc(100vh-4rem)] max-w-[1500px] px-4 py-7 sm:px-6 lg:px-8">
      <header className="flex flex-col gap-5 xl:flex-row xl:items-end xl:justify-between">
        <div>
          <div className="inline-flex items-center gap-2 rounded-full border border-[#B9D8CC] bg-white px-3 py-1 text-xs font-extrabold tracking-[0.1em] text-[#1F6F5F]"><Activity size={14} />TOPCV OPERATIONS CONSOLE</div>
          <div className="mt-3 flex flex-wrap items-center gap-3"><h1 className="text-3xl font-extrabold text-[#18332D] sm:text-4xl">Market Pulse operations</h1>{dashboard && <StatusPill status={dashboard.overallStatus} />}</div>
          <p className="mt-2 max-w-3xl text-sm leading-6 text-slate-600">Operate one pipeline from TopCV crawl through canonical import to publication-date analytics.</p>
          <p className="mt-2 text-xs font-bold text-slate-500">Latest successful end-to-end refresh: {formatDateTime(dashboard?.latestSuccessfulRefreshAt)}</p>
        </div>
        <div className="flex flex-col gap-2 sm:flex-row">
          <Link to="/market-pulse" className="inline-flex min-h-11 items-center justify-center gap-2 rounded-xl border border-[#B9D8CC] bg-white px-4 text-sm font-extrabold text-[#1F6F5F] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#2FA084]">View public Market Pulse<ExternalLink size={15} /></Link>
          <button type="button" disabled={startingRefresh || ACTIVE_OPERATION_STATUSES.has(operation?.status)} onClick={startRefresh} className="inline-flex min-h-11 items-center justify-center gap-2 rounded-xl bg-[#1F6F5F] px-4 text-sm font-extrabold text-white hover:bg-[#18594D] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#2FA084] focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50">{startingRefresh ? <LoaderCircle size={16} className="animate-spin" /> : <RefreshCw size={16} />}Refresh TopCV market data</button>
        </div>
      </header>

      <div aria-live="polite" className="mt-5 space-y-2">
        {error && dashboard && <Message tone="error" onDismiss={() => setError("")}>{error}</Message>}
        {actionMessage && <Message tone="success" onDismiss={() => setActionMessage("")}>{actionMessage}</Message>}
        {actionGuidance && <Message tone="info" onDismiss={() => setActionGuidance("")}>{actionGuidance}</Message>}
        {refreshingDashboard && <div role="status" className="inline-flex items-center gap-2 text-xs font-bold text-[#1F6F5F]"><LoaderCircle size={14} className="animate-spin" />Updating dashboard...</div>}
      </div>

      <div className="mt-5"><PipelineStepper operation={operation ?? dashboard?.currentOperation} /></div>
      {dashboard && <div className="mt-5"><AdminDashboard dashboard={dashboard} onAlertAction={handleAlertAction} /></div>}
      <div className="mt-5"><AdminDetailTabs requestedTab={activeTab} onTabChange={setActiveTab} /></div>
      <div className="mt-5"><AdvancedActions onComplete={(message) => { setActionMessage(message); loadDashboard({ quiet: true }); }} onError={setError} /></div>
    </main>
  );
}

function AdvancedActions({ onComplete, onError }) {
  const [open, setOpen] = useState(false);
  const [working, setWorking] = useState("");
  const [pageSize, setPageSize] = useState(100);
  const [maxItems, setMaxItems] = useState(50000);
  const [lookbackDays, setLookbackDays] = useState(400);
  const execute = async (action) => {
    setWorking(action); onError("");
    try {
      if (action === "import") await marketPulseApi.importLatest({ pageSize, maxItems });
      else await marketPulseApi.syncPublicationHistory({ lookbackDays, pageSize, maxItems });
      onComplete(action === "import" ? "Latest completed TopCV crawl imported." : "Publication history sync completed and the coverage watermark was updated.");
    } catch (requestError) { onError(errorMessage(requestError)); } finally { setWorking(""); }
  };
  return (
    <section id="advanced-market-pulse-actions" tabIndex="-1" className="scroll-mt-24 rounded-2xl border border-[#B9D8CC] bg-white outline-none focus-visible:ring-2 focus-visible:ring-[#2FA084]">
      <button type="button" aria-expanded={open} onClick={() => setOpen((value) => !value)} className="flex min-h-16 w-full items-center justify-between gap-3 px-4 text-left focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-[#2FA084] sm:px-5"><div><h2 className="text-base font-extrabold text-[#18332D]">Advanced actions</h2><p className="mt-1 text-xs font-semibold text-slate-500">Run import-only or full historical synchronization with explicit limits.</p></div><ChevronDown size={18} className={`text-[#1F6F5F] transition ${open ? "rotate-180" : ""}`} /></button>
      {open && <div className="border-t border-[#DCEBE5] p-4 sm:p-5"><div className="grid gap-3 sm:grid-cols-3"><NumberField label="Page size" value={pageSize} min={10} max={500} onChange={setPageSize} /><NumberField label="Maximum items" value={maxItems} min={100} max={50000} onChange={setMaxItems} /><NumberField label="History lookback (days)" value={lookbackDays} min={1} max={400} onChange={setLookbackDays} /></div><div className="mt-4 flex flex-col gap-2 sm:flex-row"><button type="button" disabled={Boolean(working)} onClick={() => execute("import")} className="inline-flex min-h-11 items-center justify-center gap-2 rounded-xl border border-[#1F6F5F] px-4 text-sm font-extrabold text-[#1F6F5F] disabled:opacity-50"><Play size={15} />Import latest crawler data</button><button type="button" disabled={Boolean(working)} onClick={() => execute("history")} className="inline-flex min-h-11 items-center justify-center gap-2 rounded-xl bg-[#1F6F5F] px-4 text-sm font-extrabold text-white disabled:opacity-50">{working === "history" ? <LoaderCircle size={15} className="animate-spin" /> : <RotateCcw size={15} />}Historical sync</button></div><p className="mt-3 text-xs font-semibold leading-5 text-amber-800">Historical sync fetches active and inactive TopCV jobs. It does not run missing-item lifecycle and updates the watermark only after a complete transaction.</p></div>}
    </section>
  );
}

function NumberField({ label, value, min, max, onChange }) { return <label className="text-xs font-bold text-slate-600"><span>{label}</span><input type="number" min={min} max={max} value={value} onChange={(event) => onChange(Math.max(min, Math.min(max, Number(event.target.value) || min)))} className="mt-1.5 min-h-11 w-full rounded-xl border border-[#B9D8CC] bg-white px-3 text-sm font-bold text-[#18332D] outline-none focus:ring-2 focus:ring-[#2FA084]" /></label>; }

function Message({ tone, children, onDismiss }) {
  const style = tone === "error"
    ? "border-red-200 bg-red-50 text-red-800"
    : tone === "info"
      ? "border-sky-200 bg-sky-50 text-sky-900"
      : "border-emerald-200 bg-emerald-50 text-emerald-800";
  return <div role={tone === "error" ? "alert" : "status"} className={`flex items-start justify-between gap-3 rounded-xl border px-4 py-3 text-sm font-bold ${style}`}><span className="inline-flex items-start gap-2">{tone === "error" && <AlertTriangle size={16} className="mt-0.5 shrink-0" />}{children}</span><button type="button" onClick={onDismiss} className="min-h-8 shrink-0 rounded px-2 text-xs">Dismiss</button></div>;
}
function PageLoading() { return <main className="tm-page grid min-h-[60vh] place-items-center"><div role="status" className="flex items-center gap-3 text-sm font-bold text-[#1F6F5F]"><LoaderCircle className="animate-spin" />Loading TopCV operations...</div></main>; }
function PageError({ message, onRetry }) { return <main className="tm-page mx-auto grid min-h-[60vh] max-w-3xl place-items-center px-4"><div role="alert" className="w-full rounded-2xl border border-red-200 bg-red-50 p-6 text-center text-sm font-bold text-red-800"><AlertTriangle className="mx-auto" /><p className="mt-3">{message}</p><button type="button" onClick={onRetry} className="mt-4 min-h-11 rounded-xl border border-current px-4">Try again</button></div></main>; }
