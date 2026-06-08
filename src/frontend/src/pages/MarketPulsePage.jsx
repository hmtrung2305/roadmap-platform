import { useEffect, useMemo, useState } from "react";
import { Activity, AlertTriangle, BarChart3, BriefcaseBusiness, Database, TrendingUp } from "lucide-react";
import { marketPulseApi } from "../api/marketPulseApi";

const chartColors = ["#2563eb", "#dc2626", "#ca8a04", "#16a34a", "#7c3aed", "#0891b2"];
const dayOptions = [14, 30, 60, 90];

export default function MarketPulsePage() {
  const [overview, setOverview] = useState(null);
  const [days, setDays] = useState(30);
  const [selectedSkills, setSelectedSkills] = useState([]);
  const [hoveredPoint, setHoveredPoint] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");

  useEffect(() => {
    let ignore = false;

    async function loadOverview() {
      setIsLoading(true);
      setError("");

      try {
        const data = await marketPulseApi.getOverview({
          days,
          skills: selectedSkills,
        });

        if (ignore) return;

        setOverview(data);

        if (selectedSkills.length === 0 && data?.skills?.length > 0) {
          setSelectedSkills(data.skills.slice(0, 4).map((skill) => skill.skillSlug));
        }
      } catch (err) {
        if (!ignore) {
          setError(err?.message || "Unable to load market pulse data.");
        }
      } finally {
        if (!ignore) {
          setIsLoading(false);
        }
      }
    }

    loadOverview();

    return () => {
      ignore = true;
    };
  }, [days, selectedSkills]);

  const selectedSkillSet = useMemo(() => new Set(selectedSkills), [selectedSkills]);
  const visibleSkills = overview?.skills ?? [];
  const trendPoints = overview?.trendPoints ?? [];

  const latestUpdated = overview?.lastUpdatedAt
    ? new Intl.DateTimeFormat("en", { dateStyle: "medium" }).format(new Date(overview.lastUpdatedAt))
    : "Waiting for first scrape";

  const toggleSkill = (skillSlug) => {
    setSelectedSkills((current) => {
      if (current.includes(skillSlug)) {
        return current.filter((slug) => slug !== skillSlug);
      }

      return [...current, skillSlug].slice(-6);
    });
  };

  return (
    <section className="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
      <div className="mb-6 flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
        <div>
          <div className="inline-flex items-center gap-2 rounded-md border border-cyan-200 bg-cyan-50 px-3 py-1 text-xs font-bold uppercase tracking-wide text-cyan-800">
            <Activity size={14} />
            Market Pulse
          </div>
          <h1 className="mt-3 text-3xl font-bold text-slate-950">IT skill demand trends</h1>
          <p className="mt-3 max-w-2xl text-sm leading-6 text-slate-600">
            Daily job-market snapshots from configured portals, analyzed by keyword frequency in job descriptions.
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          {dayOptions.map((option) => (
            <button
              key={option}
              type="button"
              onClick={() => setDays(option)}
              className={`h-10 rounded-md border px-4 text-sm font-bold transition ${
                days === option
                  ? "border-slate-900 bg-slate-900 text-white"
                  : "border-slate-200 bg-white text-slate-600 hover:border-slate-400"
              }`}
            >
              {option}d
            </button>
          ))}
        </div>
      </div>

      {error && (
        <div className="mb-6 rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm font-semibold text-red-700">
          {error}
        </div>
      )}

      <div className="grid gap-4 md:grid-cols-4">
        <MetricCard icon={<BriefcaseBusiness size={18} />} label="Active postings" value={overview?.activePostings ?? overview?.totalPostings ?? 0} />
        <MetricCard icon={<Database size={18} />} label="Active sources" value={overview?.sourceCount ?? 0} />
        <MetricCard icon={<AlertTriangle size={18} />} label="Stale / expired" value={`${overview?.stalePostings ?? 0} / ${overview?.expiredPostings ?? 0}`} />
        <MetricCard icon={<TrendingUp size={18} />} label="Last snapshot" value={latestUpdated} />
      </div>

      <div className="mt-6 grid gap-6 lg:grid-cols-[minmax(0,1fr)_360px]">
        <div className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
          <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <h2 className="text-lg font-bold text-slate-950">Demand movement</h2>
              <p className="mt-1 text-sm text-slate-500">Mention count by day across selected skills.</p>
            </div>
            <BarChart3 className="text-slate-400" size={22} />
          </div>

          <TrendChart
            points={trendPoints}
            selectedSkills={selectedSkills}
            hoveredPoint={hoveredPoint}
            onHoverPoint={setHoveredPoint}
          />
        </div>

        <aside className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
          <h2 className="text-lg font-bold text-slate-950">Trending skills</h2>
          <p className="mt-1 text-sm text-slate-500">Choose up to six skills to compare.</p>

          <div className="mt-4 flex flex-wrap gap-2">
            {visibleSkills.map((skill) => (
              <button
                key={skill.skillSlug}
                type="button"
                onClick={() => toggleSkill(skill.skillSlug)}
                className={`rounded-md border px-3 py-2 text-left text-sm font-bold transition ${
                  selectedSkillSet.has(skill.skillSlug)
                    ? "border-blue-500 bg-blue-50 text-blue-800"
                    : "border-slate-200 bg-slate-50 text-slate-600 hover:border-slate-400"
                }`}
              >
                {skill.skillName}
              </button>
            ))}
          </div>

          <div className="mt-5 overflow-hidden rounded-md border border-slate-200">
            <table className="w-full text-left text-sm">
              <thead className="bg-slate-50 text-xs uppercase text-slate-500">
                <tr>
                  <th className="px-3 py-3">Skill</th>
                  <th className="px-3 py-3">Mentions</th>
                  <th className="px-3 py-3">Growth</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {visibleSkills.slice(0, 10).map((skill) => (
                  <tr key={skill.skillSlug}>
                    <td className="px-3 py-3 font-semibold text-slate-800">{skill.skillName}</td>
                    <td className="px-3 py-3 text-slate-600">{skill.mentionCount}</td>
                    <td className={`px-3 py-3 font-bold ${skill.growthPercent >= 0 ? "text-green-700" : "text-red-700"}`}>
                      {skill.growthPercent > 0 ? "+" : ""}
                      {skill.growthPercent}%
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </aside>
      </div>

      {isLoading && (
        <div className="mt-6 rounded-md border border-slate-200 bg-white px-4 py-3 text-sm font-semibold text-slate-500">
          Loading market signals...
        </div>
      )}
    </section>
  );
}

function MetricCard({ icon, label, value }) {
  return (
    <div className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
      <div className="flex items-center gap-3 text-slate-500">
        <span className="inline-flex h-9 w-9 items-center justify-center rounded-md bg-slate-100 text-slate-700">
          {icon}
        </span>
        <span className="text-sm font-bold uppercase tracking-wide">{label}</span>
      </div>
      <div className="mt-4 min-h-8 text-2xl font-bold text-slate-950">{value}</div>
    </div>
  );
}

function TrendChart({ points, selectedSkills, hoveredPoint, onHoverPoint }) {
  const width = 860;
  const height = 360;
  const padding = { top: 24, right: 28, bottom: 44, left: 52 };

  const dates = [...new Set(points.map((point) => point.date.slice(0, 10)))].sort();
  const skills = selectedSkills.length > 0
    ? selectedSkills
    : [...new Set(points.map((point) => point.skillSlug))].slice(0, 6);
  const maxValue = Math.max(1, ...points.map((point) => point.mentionCount));
  const innerWidth = width - padding.left - padding.right;
  const innerHeight = height - padding.top - padding.bottom;

  const xForDate = (date) => {
    if (dates.length <= 1) return padding.left + innerWidth / 2;
    return padding.left + (dates.indexOf(date) / (dates.length - 1)) * innerWidth;
  };

  const yForValue = (value) => padding.top + innerHeight - (value / maxValue) * innerHeight;

  const pointsBySkill = skills.map((skillSlug, index) => {
    const skillPoints = points
      .filter((point) => point.skillSlug === skillSlug)
      .sort((a, b) => a.date.localeCompare(b.date));

    return {
      skillSlug,
      label: skillPoints[0]?.skillName || skillSlug,
      color: chartColors[index % chartColors.length],
      points: skillPoints,
    };
  });

  if (points.length === 0) {
    return (
      <div className="flex aspect-[16/7] min-h-72 items-center justify-center rounded-md border border-dashed border-slate-300 bg-slate-50 text-center text-sm font-semibold text-slate-500">
        No trend snapshots yet. The scheduled scraper will populate this chart after the first successful run.
      </div>
    );
  }

  return (
    <div className="relative overflow-x-auto">
      <svg viewBox={`0 0 ${width} ${height}`} className="min-h-72 w-full min-w-[720px]" role="img">
        {[0, 0.25, 0.5, 0.75, 1].map((ratio) => {
          const value = Math.round(maxValue * (1 - ratio));
          const y = padding.top + innerHeight * ratio;

          return (
            <g key={ratio}>
              <line x1={padding.left} x2={width - padding.right} y1={y} y2={y} stroke="#e2e8f0" />
              <text x={padding.left - 12} y={y + 4} textAnchor="end" className="fill-slate-400 text-[11px]">
                {value}
              </text>
            </g>
          );
        })}

        {dates.map((date, index) => {
          if (index % Math.ceil(Math.max(1, dates.length / 6)) !== 0 && index !== dates.length - 1) {
            return null;
          }

          return (
            <text key={date} x={xForDate(date)} y={height - 14} textAnchor="middle" className="fill-slate-400 text-[11px]">
              {date.slice(5)}
            </text>
          );
        })}

        {pointsBySkill.map((series) => {
          const path = series.points
            .map((point, index) => {
              const date = point.date.slice(0, 10);
              const command = index === 0 ? "M" : "L";
              return `${command} ${xForDate(date)} ${yForValue(point.mentionCount)}`;
            })
            .join(" ");

          return (
            <g key={series.skillSlug}>
              <path d={path} fill="none" stroke={series.color} strokeWidth="3" strokeLinecap="round" strokeLinejoin="round" />
              {series.points.map((point) => {
                const date = point.date.slice(0, 10);
                const x = xForDate(date);
                const y = yForValue(point.mentionCount);

                return (
                  <circle
                    key={`${series.skillSlug}-${date}`}
                    cx={x}
                    cy={y}
                    r={hoveredPoint === point ? 6 : 4}
                    fill={series.color}
                    className="cursor-pointer"
                    onMouseEnter={() => onHoverPoint(point)}
                    onMouseLeave={() => onHoverPoint(null)}
                  />
                );
              })}
            </g>
          );
        })}
      </svg>

      <div className="mt-3 flex flex-wrap gap-3">
        {pointsBySkill.map((series) => (
          <span key={series.skillSlug} className="inline-flex items-center gap-2 text-xs font-bold text-slate-600">
            <span className="h-2.5 w-2.5 rounded-full" style={{ background: series.color }} />
            {series.label}
          </span>
        ))}
      </div>

      {hoveredPoint && (
        <div className="mt-3 rounded-md border border-slate-200 bg-slate-50 px-3 py-2 text-sm font-semibold text-slate-700">
          {hoveredPoint.skillName}: {hoveredPoint.mentionCount} mentions in {hoveredPoint.postingCount} postings on{" "}
          {hoveredPoint.date.slice(0, 10)}
        </div>
      )}
    </div>
  );
}
