import { AlertTriangle, BriefcaseBusiness, CalendarDays, Clock3, DatabaseZap, ShieldCheck, TriangleAlert } from "lucide-react";
import { formatDateTime, formatDecimal, formatDuration, formatInteger, statusTone } from "./adminViewModel";
import { StatusPill } from "./PipelineStepper";

export default function AdminDashboard({ dashboard, onAlertAction }) {
  const failureTotal = dashboard.openCrawlerFailures + dashboard.openImportFailures;
  const metrics = [
    { label: "Active jobs", value: formatInteger(dashboard.activeJobs), detail: "Canonical TopCV jobs", icon: BriefcaseBusiness },
    { label: "Posted in 7 days", value: `~${formatDecimal(dashboard.estimatedPostings7Days)}`, detail: "Publication-date estimate", icon: CalendarDays },
    { label: "Crawler freshness", value: dashboard.crawlerFreshnessHours === null ? "N/A" : formatDuration(dashboard.crawlerFreshnessHours * 60), detail: "Since TopCV crawl success", icon: Clock3 },
    { label: "Reliable dates", value: `${formatDecimal(dashboard.reliablePostDateCoveragePercent)}%`, detail: "Exact + relative bounds", icon: ShieldCheck },
    { label: "Import lag", value: formatDuration(dashboard.importLagMinutes), detail: "Crawler to .NET", icon: DatabaseZap },
    { label: "Open failures", value: formatInteger(failureTotal), detail: `${dashboard.openCrawlerFailures} crawler / ${dashboard.openImportFailures} import`, icon: TriangleAlert },
  ];
  const health = [dashboard.pipeline.crawler, dashboard.pipeline.importer, dashboard.pipeline.history, dashboard.pipeline.quality];

  return (
    <div className="space-y-5">
      <section aria-labelledby="operations-kpis-title">
        <h2 id="operations-kpis-title" className="text-base font-extrabold text-[#18332D]">Operational overview</h2>
        <div className="mt-3 grid grid-cols-2 gap-3 xl:grid-cols-3 2xl:grid-cols-6">
          {metrics.map(({ label, value, detail, icon: Icon }) => (
            <article key={label} className="min-w-0 rounded-2xl border border-[#B9D8CC] bg-white p-4">
              <div className="flex items-center gap-2 text-xs font-extrabold uppercase tracking-wide text-slate-500"><Icon size={15} className="shrink-0 text-[#1F6F5F]" /><span className="break-words">{label}</span></div>
              <div className="mt-3 break-words text-2xl font-extrabold text-[#18332D]">{value}</div>
              <div className="mt-1 text-xs font-semibold leading-5 text-slate-500">{detail}</div>
            </article>
          ))}
        </div>
      </section>

      <AnalyticsConfidence dashboard={dashboard} />

      <div className="grid gap-5 xl:grid-cols-[minmax(0,1.35fr)_minmax(340px,0.65fr)]">
        <section aria-labelledby="pipeline-health-title" className="rounded-2xl border border-[#B9D8CC] bg-white p-4 sm:p-5">
          <h2 id="pipeline-health-title" className="text-base font-extrabold text-[#18332D]">Pipeline health</h2>
          <p className="mt-1 text-xs font-semibold text-slate-500">Each stage is assessed independently so the source of degradation is visible.</p>
          <div className="mt-4 grid gap-3 sm:grid-cols-2">
            {health.map((item) => <HealthCard key={item.label} item={item} />)}
          </div>
        </section>

        <section aria-labelledby="operations-alerts-title" className="rounded-2xl border border-[#B9D8CC] bg-white p-4 sm:p-5">
          <h2 id="operations-alerts-title" className="text-base font-extrabold text-[#18332D]">Action centre</h2>
          {dashboard.alerts.length === 0 ? (
            <div className="mt-4 flex min-h-40 flex-col items-center justify-center rounded-xl bg-emerald-50 px-4 text-center text-sm font-bold text-emerald-800"><ShieldCheck size={24} /><span className="mt-2">No active operational alerts.</span></div>
          ) : (
            <div className="mt-4 space-y-3">{dashboard.alerts.map((alert) => <AlertCard key={alert.id} alert={alert} onAction={onAlertAction} />)}</div>
          )}
        </section>
      </div>

      <div className="grid gap-5 xl:grid-cols-[minmax(0,1fr)_minmax(420px,1fr)]">
        <PublicationMiniChart points={dashboard.demandPoints} />
        <RecentOperations operations={dashboard.recentOperations} />
      </div>
    </div>
  );
}

function AnalyticsConfidence({ dashboard }) {
  const quality = dashboard.postDateQuality;
  const metrics = [
    { label: "Dated sample", value: formatInteger(quality.sampleSize) },
    { label: "Exact dates", value: `${formatDecimal(quality.exactPercent)}% (${formatInteger(quality.exactCount)})` },
    { label: "Relative estimates", value: `${formatDecimal(quality.relativePercent)}% (${formatInteger(quality.relativeCount)})` },
    { label: "Unknown dates", value: `${formatDecimal(quality.unknownPercent)}% (${formatInteger(quality.unknownCount)})` },
    { label: "Average date range", value: `${formatDecimal(quality.averageIntervalWidthDays)} days` },
    { label: "Broad ranges (>7 days)", value: `${formatDecimal(quality.broadRangeSharePercent)}%` },
  ];
  return (
    <section aria-labelledby="analytics-confidence-title" className="rounded-2xl border border-[#B9D8CC] bg-white p-4 sm:p-5">
      <div className="flex flex-wrap items-start justify-between gap-3">
        <div>
          <h2 id="analytics-confidence-title" className="text-base font-extrabold text-[#18332D]">Analytics confidence</h2>
          <p className="mt-1 text-xs font-semibold text-slate-500">Publication-date certainty is operational metadata and is kept out of the public market view.</p>
        </div>
        <StatusPill status={dashboard.analyticsConfidence} />
      </div>
      <div className="mt-4 grid grid-cols-2 gap-3 lg:grid-cols-3 2xl:grid-cols-6">
        {metrics.map((item) => (
          <article key={item.label} className="rounded-xl bg-[#FCFAF6] p-3">
            <div className="text-[11px] font-extrabold uppercase tracking-wide text-slate-500">{item.label}</div>
            <div className="mt-2 text-base font-extrabold text-[#18332D]">{item.value}</div>
          </article>
        ))}
      </div>
    </section>
  );
}

function HealthCard({ item }) {
  return (
    <article className="rounded-xl border border-[#DCEBE5] bg-[#FCFAF6] p-3">
      <div className="flex items-start justify-between gap-3"><h3 className="text-sm font-extrabold text-[#18332D]">{item.label}</h3><StatusPill status={item.status} /></div>
      <p className="mt-2 text-xs font-semibold leading-5 text-slate-600">{item.summary}</p>
      {item.metricLabel && <div className="mt-2 text-xs text-slate-500">{item.metricLabel}: <strong className="text-[#18332D]">{item.metricValue}</strong></div>}
      {item.updatedAt && <div className="mt-1 text-[11px] text-slate-400">Updated {formatDateTime(item.updatedAt)}</div>}
    </article>
  );
}

function AlertCard({ alert, onAction }) {
  const tone = statusTone(alert.severity);
  const className = tone === "danger" ? "border-red-200 bg-red-50 text-red-900" : tone === "warning" ? "border-amber-200 bg-amber-50 text-amber-900" : "border-slate-200 bg-slate-50 text-slate-700";
  return (
    <article className={`rounded-xl border p-3 ${className}`}>
      <div className="flex items-start gap-2"><AlertTriangle size={16} className="mt-0.5 shrink-0" /><div><h3 className="text-sm font-extrabold">{alert.title}</h3>{alert.message && <p className="mt-1 text-xs font-semibold leading-5 opacity-80">{alert.message}</p>}</div></div>
      {alert.action && <button type="button" onClick={() => onAction(alert.actionType)} className="mt-2 min-h-11 rounded-lg border border-current px-3 text-xs font-extrabold focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#2FA084]">{alert.action}</button>}
    </article>
  );
}

function PublicationMiniChart({ points }) {
  const values = points.map((point) => point.value).filter(Number.isFinite);
  const max = Math.max(1, ...values);
  const path = points.map((point, index) => {
    if (!Number.isFinite(point.value)) return null;
    const x = points.length <= 1 ? 50 : (index / (points.length - 1)) * 100;
    const y = 94 - (point.value / max) * 82;
    return { x, y, point };
  }).filter(Boolean);
  const pathValue = path.map((point, index) => `${index === 0 ? "M" : "L"} ${point.x} ${point.y}`).join(" ");
  return (
    <section aria-labelledby="mini-demand-title" className="rounded-2xl border border-[#B9D8CC] bg-white p-4 sm:p-5">
      <h2 id="mini-demand-title" className="text-base font-extrabold text-[#18332D]">Recent publication demand</h2>
      <p className="mt-1 text-xs font-semibold text-slate-500">A quick operational signal; use the public page for full analysis.</p>
      {path.length === 0 ? <div className="mt-4 grid min-h-40 place-items-center rounded-xl border border-dashed border-[#DCEBE5] text-sm font-semibold text-slate-500">No reliable publication dates yet.</div> : (
        <svg viewBox="0 0 100 100" preserveAspectRatio="none" className="mt-4 h-44 w-full rounded-xl bg-[#FCFAF6] p-2" role="img" aria-label="Recent estimated postings by publication date">
          <path d={pathValue} fill="none" stroke="#1F6F5F" strokeWidth="2" vectorEffect="non-scaling-stroke" />
          {path.map(({ x, y, point }) => <circle key={point.date} cx={x} cy={y} r="1.5" fill="#1F6F5F"><title>{point.date}: {point.approximate ? "approximately " : ""}{formatDecimal(point.value)}</title></circle>)}
        </svg>
      )}
    </section>
  );
}

function RecentOperations({ operations }) {
  return (
    <section aria-labelledby="recent-operations-title" className="rounded-2xl border border-[#B9D8CC] bg-white p-4 sm:p-5">
      <h2 id="recent-operations-title" className="text-base font-extrabold text-[#18332D]">Latest refresh operations</h2>
      {operations.length === 0 ? <p className="mt-4 rounded-xl bg-[#FCFAF6] p-4 text-sm font-semibold text-slate-500">No end-to-end refresh has been requested yet.</p> : (
        <div className="mt-3 space-y-2">{operations.map((operation) => (
          <article key={operation.id} className="flex flex-col gap-2 rounded-xl border border-[#DCEBE5] p-3 sm:flex-row sm:items-center sm:justify-between">
            <div className="min-w-0"><div className="truncate text-sm font-extrabold text-[#18332D]">TopCV market data refresh</div><div className="mt-1 text-xs text-slate-500">{formatDateTime(operation.createdAt)}</div></div>
            <StatusPill status={operation.status} />
          </article>
        ))}</div>
      )}
    </section>
  );
}
