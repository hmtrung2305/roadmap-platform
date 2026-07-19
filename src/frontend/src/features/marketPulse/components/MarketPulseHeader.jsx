import { Activity, RefreshCw } from "lucide-react";
import { formatDateTime } from "../marketPulseViewModel";

export default function MarketPulseHeader({ overview, analytics, isRefreshing, onReload }) {
  return (
    <header className="flex flex-col gap-5 lg:flex-row lg:items-end lg:justify-between">
      <div>
        <div className="inline-flex items-center gap-2 rounded-full border border-[#B9D8CC] bg-white px-3 py-1 text-xs font-extrabold tracking-[0.12em] text-[#1F6F5F]">
          <Activity size={14} aria-hidden="true" />
          JOB MARKET PULSE
        </div>
        <h1 className="mt-3 text-3xl font-extrabold text-[#18332D] sm:text-4xl">
          See where IT demand is moving
        </h1>
        <p className="mt-3 max-w-2xl text-sm leading-6 text-slate-600 sm:text-base">
          Understand when IT roles were posted on TopCV, with relative dates distributed transparently across their possible range.
        </p>

        {overview && (
          <p className="mt-3 text-xs font-bold text-slate-600">
            TopCV data at {formatDateTime(analytics?.sourceDataAt || overview.lastUpdatedAt)}
          </p>
        )}
      </div>

      <button
        type="button"
        onClick={onReload}
        disabled={isRefreshing}
        aria-label="Reload market insights"
        className="inline-flex min-h-11 items-center justify-center gap-2 self-start rounded-xl border border-[#B9D8CC] bg-white px-4 text-sm font-extrabold text-[#1F6F5F] transition hover:border-[#2FA084] hover:bg-[#EAF8F1] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#2FA084] disabled:cursor-not-allowed disabled:opacity-60 lg:self-auto"
      >
        <RefreshCw size={16} className={isRefreshing ? "animate-spin" : ""} aria-hidden="true" />
        Reload market insights
      </button>
    </header>
  );
}
