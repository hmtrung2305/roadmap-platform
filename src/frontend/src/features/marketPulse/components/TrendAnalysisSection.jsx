import { ArrowDownRight, ArrowRight, ArrowUpRight, BarChart3, CalendarRange, LineChart, TrendingDown, TrendingUp } from "lucide-react";
import { useMemo, useState } from "react";
import {
  CHART_COLORS,
  buildSkillSeries,
  comparisonCopy,
  formatDecimal,
  formatEstimate,
  formatRange,
  signedDecimal,
} from "../marketPulseViewModel";
import ResponsiveTrendChart from "./ResponsiveTrendChart";

export default function TrendAnalysisSection({
  overview,
  analytics,
  tab,
  onTabChange,
  comparisonSkills,
  onComparisonSkillsChange,
}) {
  const [movement, setMovement] = useState("rising");
  const skills = useMemo(
    () => buildTrendSkillCatalog(overview?.skills, analytics.skillComparisons, analytics.skillTrendPoints),
    [analytics.skillComparisons, analytics.skillTrendPoints, overview?.skills],
  );
  const defaultSkillSlugs = useMemo(
    () => uniqueSkillSlugs(analytics.skillComparisons, analytics.skillTrendPoints).slice(0, 3),
    [analytics.skillComparisons, analytics.skillTrendPoints],
  );
  const selectedSkillSlugs = comparisonSkills.length > 0 ? comparisonSkills : defaultSkillSlugs;
  const skillSeries = useMemo(
    () => buildSkillSeries(analytics.skillTrendPoints, selectedSkillSlugs),
    [analytics.skillTrendPoints, selectedSkillSlugs],
  );
  const marketSeries = useMemo(() => buildMarketSeries(analytics.marketTrendPoints), [analytics.marketTrendPoints]);
  const movementItems = (analytics.skillComparisons ?? [])
    .filter((item) => movement === "rising" ? ["up", "new"].includes(item.direction) : item.direction === "down")
    .sort((left, right) => movement === "rising" ? right.delta - left.delta : left.delta - right.delta)
    .slice(0, 6);

  const toggleSkill = (slug) => {
    const current = comparisonSkills.length > 0 ? comparisonSkills : defaultSkillSlugs;
    if (current.includes(slug)) {
      if (current.length === 1) return;
      onComparisonSkillsChange(current.filter((item) => item !== slug));
      return;
    }
    if (current.length < 3) onComparisonSkillsChange([...current, slug]);
  };

  return (
    <section id="market-trend-analysis" aria-labelledby="trend-analysis-title" className="mt-8 scroll-mt-24">
      <div>
        <h2 id="trend-analysis-title" tabIndex="-1" className="text-xl font-extrabold text-[#18332D] outline-none">Trend analysis</h2>
        <p className="mt-1 text-sm text-slate-600">Demand is placed on the date a role was posted, not the date it was crawled.</p>
      </div>

      <div className="mt-4 rounded-2xl border border-[#B9D8CC] bg-white p-4 sm:p-5">
        <div className="flex flex-col gap-4 xl:flex-row xl:items-start xl:justify-between">
          <div>
            <div className="inline-flex rounded-xl bg-[#F7F1E8] p-1" role="tablist" aria-label="Trend view">
              <TabButton selected={tab === "market"} onClick={() => onTabChange("market")} icon={BarChart3}>Market demand</TabButton>
              <TabButton selected={tab === "skill"} onClick={() => onTabChange("skill")} icon={LineChart}>Skill demand</TabButton>
            </div>
            <p className="mt-3 text-xs font-bold leading-5 text-slate-500">
              Current: {formatRange(analytics.currentStart, analytics.currentEnd)}
              {analytics.previousStart && analytics.previousEnd && <> / Previous: {formatRange(analytics.previousStart, analytics.previousEnd)}</>}
            </p>
          </div>
          <HistoryCoverage analytics={analytics} />
        </div>

        {tab === "market" ? (
          <div className="mt-5 grid gap-5 lg:grid-cols-[minmax(0,1fr)_300px]">
            <div className="min-w-0">
              <ResponsiveTrendChart
                series={marketSeries}
                ariaLabel="TopCV postings by publication date"
                emptyMessage={analytics.historyMessage}
              />
              <p className="mt-2 text-xs font-semibold leading-5 text-slate-500">
                <strong>~</strong> means the value includes relative dates. Week and month ranges contribute a total weight of one posting, spread evenly across possible dates.
              </p>
            </div>
            <PeriodComparison analytics={analytics} />
          </div>
        ) : (
          <div className="mt-5">
            <div className="mb-4">
              <div className="text-sm font-extrabold text-[#18332D]">Compare skills</div>
              <p className="mt-1 text-xs text-slate-500">Choose up to 3 skills. Each series follows publication-date demand.</p>
              <div className="mt-3 flex max-h-32 flex-wrap gap-2 overflow-y-auto">
                {skills.slice(0, 18).map((skill) => {
                  const selected = selectedSkillSlugs.includes(skill.skillSlug);
                  const disabled = !selected && selectedSkillSlugs.length >= 3;
                  return (
                    <button
                      key={skill.skillSlug}
                      type="button"
                      aria-pressed={selected}
                      disabled={disabled}
                      onClick={() => toggleSkill(skill.skillSlug)}
                      className={`min-h-11 rounded-full border px-3 text-xs font-extrabold focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#2FA084] disabled:cursor-not-allowed disabled:opacity-40 ${selected ? "border-[#1F6F5F] bg-[#EAF8F1] text-[#1F6F5F]" : "border-[#DCEBE5] bg-white text-slate-600"}`}
                    >
                      {skill.skillName}
                    </button>
                  );
                })}
              </div>
              {selectedSkillSlugs.length >= 3 && <p className="mt-2 text-xs font-semibold text-amber-700">Three skills selected. Remove one to choose another.</p>}
            </div>
            <ResponsiveTrendChart
              series={skillSeries}
              ariaLabel="TopCV postings mentioning selected skills by publication date"
              emptyMessage={analytics.historyMessage}
            />
          </div>
        )}
      </div>

      <div className="mt-4">
        <div className="rounded-2xl border border-[#B9D8CC] bg-white p-4 sm:p-5">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div>
              <h3 className="text-base font-extrabold text-[#18332D]">Skill movement</h3>
              <p className="mt-1 text-xs text-slate-500">Estimated demand across equal publication periods.</p>
            </div>
            <div className="inline-flex rounded-xl bg-[#F7F1E8] p-1" role="tablist" aria-label="Skill movement">
              <TabButton selected={movement === "rising"} onClick={() => setMovement("rising")} icon={TrendingUp}>Rising</TabButton>
              <TabButton selected={movement === "falling"} onClick={() => setMovement("falling")} icon={TrendingDown}>Falling</TabButton>
            </div>
          </div>
          {!analytics.hasHistory ? (
            <NoHistory message={analytics.historyMessage} />
          ) : movementItems.length === 0 ? (
            <NoHistory message="No meaningful skill movement was detected for this filter set." />
          ) : (
            <div className="mt-4 divide-y divide-[#DCEBE5]">
              {movementItems.map((item) => <MovementRow key={item.skillSlug} item={item} />)}
            </div>
          )}
        </div>
      </div>
    </section>
  );
}

function uniqueSkillSlugs(comparisons = [], points = []) {
  return [...new Set([...comparisons, ...points].map((item) => item?.skillSlug).filter(Boolean))];
}

function buildTrendSkillCatalog(overviewSkills = [], comparisons = [], points = []) {
  const items = [...comparisons, ...points, ...(overviewSkills ?? [])];
  const seen = new Set();
  return items.filter((item) => {
    const slug = item?.skillSlug;
    if (!slug || seen.has(slug)) return false;
    seen.add(slug);
    return true;
  });
}

function buildMarketSeries(points) {
  return [
    {
      key: "total",
      label: "Postings",
      color: CHART_COLORS[0],
      points: points.map((point) => ({
        date: point.date,
        value: point.available ? point.totalEstimate : null,
        exactValue: point.available ? point.exactPostings : null,
        estimatedValue: point.available ? point.relativeEstimate : null,
        approximate: point.available && Number(point.relativeEstimate || 0) > 0,
      })),
    },
  ];
}

function TabButton({ selected, onClick, icon: Icon, children }) {
  return (
    <button
      type="button"
      role="tab"
      aria-selected={selected}
      onClick={onClick}
      className={`inline-flex min-h-11 items-center gap-1.5 rounded-lg px-3 text-xs font-extrabold focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#2FA084] ${selected ? "bg-white text-[#1F6F5F] shadow-sm" : "text-slate-500"}`}
    >
      <Icon size={15} aria-hidden="true" />
      {children}
    </button>
  );
}

function HistoryCoverage({ analytics }) {
  return (
    <div className="rounded-xl border border-[#DCEBE5] bg-[#FCFAF6] px-3 py-2 text-xs font-bold leading-5 text-slate-600">
      <span className="inline-flex items-center gap-1 text-[#18332D]"><CalendarRange size={14} /> Publication history</span>
      <span className="block">{analytics.historyCoverageStart && analytics.historyCoverageEnd
        ? formatRange(analytics.historyCoverageStart, analytics.historyCoverageEnd)
        : "Historical sync not complete"}</span>
    </div>
  );
}

function PeriodComparison({ analytics }) {
  const comparison = analytics.marketComparison;
  const current = analytics.currentPeriod;
  const previous = analytics.previousPeriod;
  const approximate = Number(current.relativeEstimate || 0) > 0;
  return (
    <aside className="rounded-xl border border-[#DCEBE5] bg-[#FCFAF6] p-4">
      <div className="text-xs font-extrabold uppercase tracking-wide text-slate-500">Period comparison</div>
      {analytics.hasHistory && comparison.direction !== "insufficient" ? (
        <>
          <div className="mt-3 text-3xl font-extrabold text-[#18332D]">{formatEstimate(current.estimatedTotal, approximate)}</div>
          <div className="mt-1 text-xs font-semibold text-slate-500">postings / {formatDecimal(current.averagePerDay)} per day</div>
          <p className="mt-4 text-sm font-bold leading-6 text-slate-700">{comparisonCopy(comparison)}</p>
          <div className="mt-4 grid grid-cols-2 gap-2 text-xs">
            <MetricBox label="Current total" value={formatDecimal(comparison.currentTotal)} />
            <MetricBox label="Previous total" value={formatDecimal(comparison.previousTotal)} />
            <MetricBox label="Current avg/day" value={formatDecimal(comparison.currentAverage)} />
            <MetricBox label="Previous avg/day" value={formatDecimal(comparison.previousAverage || previous.averagePerDay)} />
          </div>
        </>
      ) : <NoHistory message={analytics.historyMessage} />}
    </aside>
  );
}

function MetricBox({ label, value }) {
  return <div className="rounded-lg bg-white p-3"><div className="text-slate-500">{label}</div><div className="mt-1 text-base font-extrabold text-[#18332D]">{value}</div></div>;
}

function NoHistory({ message }) {
  return <div className="mt-4 rounded-xl border border-dashed border-[#B9D8CC] bg-[#FCFAF6] px-4 py-6 text-center text-sm font-semibold leading-6 text-slate-500">{message}</div>;
}

function MovementRow({ item }) {
  const Icon = item.direction === "down" ? ArrowDownRight : item.direction === "flat" ? ArrowRight : ArrowUpRight;
  const tone = item.direction === "down" ? "text-amber-800 bg-amber-50" : item.direction === "flat" ? "text-slate-600 bg-slate-100" : "text-emerald-700 bg-emerald-50";
  return (
    <div className="flex flex-col gap-3 py-3 sm:flex-row sm:items-center sm:justify-between">
      <div className="min-w-0">
        <div className="truncate text-sm font-extrabold text-[#18332D]">{item.skillName}</div>
        <div className="mt-1 text-xs font-semibold text-slate-500">Current {formatDecimal(item.currentTotal)} / Previous {formatDecimal(item.previousTotal)}</div>
      </div>
      <div className={`inline-flex min-h-9 shrink-0 items-center gap-1 self-start rounded-full px-2.5 text-xs font-extrabold ${tone}`}>
        <Icon size={14} aria-hidden="true" />
        {item.direction === "new" ? "New" : signedDecimal(item.delta)}
        {item.growthPercent !== null && item.direction !== "new" && <> ({formatDecimal(Math.abs(item.growthPercent))}%)</>}
        <span className="sr-only"> {item.direction}</span>
      </div>
    </div>
  );
}
