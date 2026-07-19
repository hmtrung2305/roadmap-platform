import { ArrowDownRight, ArrowRight, ArrowUpRight, BriefcaseBusiness, CalendarDays, Sparkles } from "lucide-react";
import { formatDecimal, formatEstimate, formatNumber } from "../marketPulseViewModel";

export default function MarketSnapshotSection({ overview, analytics }) {
  // The snapshot describes the unmodified overview response. Skill comparison
  // selections are an explorer concern and must not rewrite this KPI.
  const topSkill = overview?.skills?.[0];
  const current = analytics?.currentPeriod ?? {};
  const comparison = analytics?.marketComparison ?? {};
  const hasEstimate = Number(current.relativeEstimate || 0) > 0;
  const change = changePresentation(comparison);
  const metrics = [
    {
      label: "Active jobs",
      value: formatNumber(overview?.activePostings),
      detail: "Currently visible on TopCV",
      icon: BriefcaseBusiness,
    },
    {
      label: "Posted in period",
      value: analytics?.hasHistory ? formatEstimate(current.estimatedTotal, hasEstimate) : "Not ready",
      detail: analytics?.hasHistory
        ? `${formatNumber(current.exactCount)} exact + ${formatDecimal(current.relativeEstimate)} estimated`
        : "Reliable publication history is required",
      icon: CalendarDays,
    },
    {
      label: "Change vs previous",
      value: change.value,
      detail: change.detail,
      icon: change.icon,
      tone: change.tone,
    },
    {
      label: "Top skill",
      value: topSkill?.skillName || "No signal",
      detail: topSkill ? skillDetail(topSkill) : "No reliable dated postings",
      icon: Sparkles,
    },
  ];

  return (
    <section aria-labelledby="market-snapshot-title" className="mt-8">
      <div>
        <h2 id="market-snapshot-title" className="text-xl font-extrabold text-[#18332D]">Market snapshot</h2>
        <p className="mt-1 text-sm text-slate-600">A concise view of TopCV demand in the selected publication period.</p>
      </div>
      <div className="mt-4 grid grid-cols-2 gap-3 lg:grid-cols-4">
        {metrics.map(({ label, value, detail, icon: Icon, tone }) => (
          <article key={label} className="min-w-0 rounded-2xl border border-[#B9D8CC] bg-white p-4 sm:p-5">
            <div className="flex items-center gap-2 text-xs font-extrabold uppercase tracking-wide text-slate-500">
              <span className={`grid h-8 w-8 shrink-0 place-items-center rounded-lg ${tone || "bg-[#EAF8F1] text-[#1F6F5F]"}`}>
                <Icon size={16} aria-hidden="true" />
              </span>
              <span className="min-w-0 break-words">{label}</span>
            </div>
            <div className="mt-3 break-words text-xl font-extrabold text-[#18332D] sm:text-2xl">{value}</div>
            <div className="mt-1 text-xs font-semibold leading-5 text-slate-500">{detail}</div>
          </article>
        ))}
      </div>
    </section>
  );
}

function changePresentation(comparison) {
  if (!comparison || comparison.direction === "insufficient") {
    return { value: "Not ready", detail: "Previous period is not fully covered", icon: ArrowRight, tone: "bg-slate-100 text-slate-600" };
  }
  if (comparison.direction === "new") {
    return { value: "New", detail: `+${formatDecimal(comparison.delta)} postings`, icon: ArrowUpRight, tone: "bg-emerald-50 text-emerald-700" };
  }
  const growth = Number.isFinite(Number(comparison.growthPercent))
    ? `${Number(comparison.growthPercent) > 0 ? "+" : ""}${formatDecimal(comparison.growthPercent)}%`
    : `${Number(comparison.delta) > 0 ? "+" : ""}${formatDecimal(comparison.delta)}`;
  if (comparison.direction === "down") {
    return { value: growth, detail: `${formatDecimal(comparison.previousTotal)} in previous period`, icon: ArrowDownRight, tone: "bg-amber-50 text-amber-800" };
  }
  if (comparison.direction === "flat") {
    return { value: "0%", detail: "No material change", icon: ArrowRight, tone: "bg-slate-100 text-slate-600" };
  }
  return { value: growth, detail: `${formatDecimal(comparison.previousTotal)} in previous period`, icon: ArrowUpRight, tone: "bg-emerald-50 text-emerald-700" };
}

function skillDetail(skill) {
  const value = skill.currentTotal ?? skill.postingCount ?? skill.currentAverage;
  return `${formatDecimal(value)} postings in selected period`;
}
