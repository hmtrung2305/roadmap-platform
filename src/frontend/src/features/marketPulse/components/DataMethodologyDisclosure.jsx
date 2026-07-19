import { ChevronDown, Database, WalletCards } from "lucide-react";
import { useState } from "react";
import { formatDate, formatDateTime, formatDecimal, formatMoneyVnd, formatNumber } from "../marketPulseViewModel";

export default function DataMethodologyDisclosure({ overview, analytics }) {
  const [open, setOpen] = useState(false);
  const salary = overview?.salaryInsight ?? {};

  return (
    <section aria-labelledby="about-data-title" className="mt-8 rounded-2xl border border-[#B9D8CC] bg-white">
      <button
        type="button"
        aria-expanded={open}
        aria-controls="market-pulse-data-details"
        onClick={() => setOpen((current) => !current)}
        className="flex min-h-16 w-full items-center justify-between gap-3 px-4 text-left focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-inset focus-visible:ring-[#2FA084] sm:px-5"
      >
        <div>
          <h2 id="about-data-title" className="text-base font-extrabold text-[#18332D]">About this data</h2>
          <p className="mt-1 text-xs text-slate-500">TopCV provenance, salary coverage, and publication-date methodology</p>
        </div>
        <ChevronDown size={18} className={`shrink-0 text-[#1F6F5F] transition ${open ? "rotate-180" : ""}`} aria-hidden="true" />
      </button>

      {open && (
        <div id="market-pulse-data-details" className="grid gap-4 border-t border-[#DCEBE5] p-4 md:grid-cols-2 sm:p-5">
          <DetailCard icon={Database} title="TopCV data source">
            <Metric label="Provider" value="TopCV only" />
            <Metric label="Latest successful crawl" value={formatDateTime(analytics.sourceDataAt)} />
            <Metric label="Analytics anchor" value={formatDate(analytics.anchorDate)} />
            <p className="mt-3 text-xs font-semibold leading-5 text-slate-500">The provider name is shown for provenance but is not stored repeatedly or exposed as a market filter.</p>
          </DetailCard>

          <DetailCard icon={WalletCards} title="Salary signals">
            {Number(salary.coveragePercent || 0) < 30 && <p className="mb-3 rounded-lg bg-amber-50 p-2 text-xs font-bold leading-5 text-amber-800">Salary coverage is below 30%; treat salary-based filtering and summaries as directional.</p>}
            <Metric label="Coverage" value={`${formatDecimal(salary.coveragePercent)}%`} />
            <Metric label="Sample" value={formatNumber(salary.sampleSize)} />
            <Metric label="Median minimum" value={formatMoneyVnd(salary.medianMinMonthlyVnd)} />
            <Metric label="Median maximum" value={formatMoneyVnd(salary.medianMaxMonthlyVnd)} />
          </DetailCard>

          <div className="rounded-xl bg-[#FCFAF6] p-4 md:col-span-2">
            <h3 className="text-sm font-extrabold text-[#18332D]">How publication demand is calculated</h3>
            <p className="mt-2 text-sm font-semibold leading-6 text-slate-600">
              Exact dates count once on their date. Relative day values are resolved against the crawler date in Vietnam. Week and month values distribute one posting evenly across their possible date range. When the same job is crawled again, overlapping ranges are narrowed; a one-day intersection becomes an exact publication date. Unknown dates stay in quality coverage but never increase demand. Empty covered dates are zero in the retained TopCV dataset; dates outside that history watermark are unavailable.
            </p>
          </div>
        </div>
      )}
    </section>
  );
}

function DetailCard({ icon: Icon, title, children }) {
  return <article className="rounded-xl bg-[#FCFAF6] p-4"><div className="mb-3 flex items-center gap-2"><Icon size={16} className="text-[#1F6F5F]" aria-hidden="true" /><h3 className="text-sm font-extrabold text-[#18332D]">{title}</h3></div><div className="space-y-2">{children}</div></article>;
}

function Metric({ label, value }) {
  return <div className="flex items-start justify-between gap-3 text-xs font-semibold text-slate-600"><span>{label}</span><strong className="max-w-[60%] text-right text-[#18332D]">{value}</strong></div>;
}
