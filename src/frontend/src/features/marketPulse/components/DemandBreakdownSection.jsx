import { BriefcaseBusiness, MapPin, ShieldCheck, Sparkles } from "lucide-react";
import { useMemo, useState } from "react";
import { formatDecimal, formatNumber, segmentPercent } from "../marketPulseViewModel";

const tabs = [
  { key: "skills", label: "Skills", icon: Sparkles },
  { key: "roles", label: "Roles", icon: BriefcaseBusiness },
  { key: "locations", label: "Locations", icon: MapPin },
  { key: "seniority", label: "Seniority", icon: ShieldCheck },
];

export default function DemandBreakdownSection({ overview }) {
  const [tab, setTab] = useState("skills");
  const [expanded, setExpanded] = useState(false);
  const sampleSize = Number(overview?.insightMeta?.sampleSize ?? overview?.dataQuality?.sampleSize ?? 0);
  const datasets = useMemo(() => ({
    skills: (overview?.skills ?? []).map((item) => ({
      name: item.skillName,
      count: Number(item.postingCount || 0),
      percent: segmentPercent(item, sampleSize, "postingCount"),
    })),
    roles: normalizeSegments(overview?.categorySummaries),
    locations: normalizeSegments(overview?.locationSummaries),
    seniority: normalizeSegments(overview?.experienceSummaries),
  }), [overview, sampleSize]);
  const data = datasets[tab] ?? [];
  const visible = expanded ? data : data.slice(0, 8);

  return (
    <section aria-labelledby="demand-breakdown-title" className="mt-8">
      <div>
        <h2 id="demand-breakdown-title" className="text-xl font-extrabold text-[#18332D]">Demand breakdown</h2>
        <p className="mt-1 text-sm text-slate-600">See how the analyzed sample is distributed across the market.</p>
      </div>
      <div className="mt-4 rounded-2xl border border-[#B9D8CC] bg-white p-4 sm:p-5">
        <div className="flex gap-1 overflow-x-auto rounded-xl bg-[#F7F1E8] p-1" role="tablist" aria-label="Demand dimension">
          {tabs.map(({ key, label, icon: Icon }) => (
            <button
              key={key}
              type="button"
              role="tab"
              aria-selected={tab === key}
              onClick={() => { setTab(key); setExpanded(false); }}
              className={`inline-flex min-h-11 shrink-0 items-center gap-1.5 rounded-lg px-3 text-xs font-extrabold focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#2FA084] ${tab === key ? "bg-white text-[#1F6F5F] shadow-sm" : "text-slate-500"}`}
            >
              <Icon size={15} aria-hidden="true" />
              {label}
            </button>
          ))}
        </div>

        {visible.length === 0 ? (
          <div className="mt-4 rounded-xl border border-dashed border-[#B9D8CC] bg-[#FCFAF6] px-4 py-8 text-center text-sm font-semibold text-slate-500">
            No distribution data is available for this view.
          </div>
        ) : (
          <div className="mt-5 grid gap-x-8 gap-y-4 lg:grid-cols-2">
            {visible.map((item) => <ShareBar key={item.name} item={item} />)}
          </div>
        )}

        {data.length > 8 && (
          <button
            type="button"
            aria-expanded={expanded}
            onClick={() => setExpanded((current) => !current)}
            className="mt-4 min-h-11 rounded-xl px-3 text-sm font-extrabold text-[#1F6F5F] hover:bg-[#EAF8F1] focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#2FA084]"
          >
            {expanded ? "Show less" : `Show all ${formatNumber(data.length)}`}
          </button>
        )}
      </div>
    </section>
  );
}

function ShareBar({ item }) {
  const percent = Math.max(0, Math.min(100, Number(item.percent || 0)));
  return (
    <div>
      <div className="mb-1.5 flex items-end justify-between gap-3 text-xs">
        <span className="min-w-0 truncate font-extrabold text-[#18332D]">{item.name || "Unspecified"}</span>
        <span className="shrink-0 font-bold text-slate-500">{formatNumber(item.count)} jobs / {formatDecimal(percent)}%</span>
      </div>
      <div className="h-2.5 overflow-hidden rounded-full bg-[#E8EEE9]" aria-label={`${item.name}: ${formatDecimal(percent)} percent`}>
        <div className="h-full rounded-full bg-[#2FA084]" style={{ width: `${percent}%` }} />
      </div>
    </div>
  );
}

function normalizeSegments(segments = []) {
  return segments.map((item) => ({
    name: item.name,
    count: Number(item.count || 0),
    percent: Number(item.percent || 0),
  }));
}
