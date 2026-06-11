import { useEffect, useMemo, useState } from "react";
import {
  Activity,
  AlertTriangle,
  BarChart3,
  BriefcaseBusiness,
  CalendarDays,
  Database,
  ExternalLink,
  MapPin,
  RefreshCw,
  TrendingUp,
} from "lucide-react";
import { marketPulseApi } from "../api/marketPulseApi";

const chartColors = ["#2563eb", "#dc2626", "#ca8a04", "#16a34a", "#7c3aed", "#0891b2"];
const dayOptions = [7, 14, 30, 60];
const numberFormatter = new Intl.NumberFormat("vi-VN");
const dateFormatter = new Intl.DateTimeFormat("vi-VN", {
  day: "2-digit",
  month: "2-digit",
  year: "numeric",
});
const dateTimeFormatter = new Intl.DateTimeFormat("vi-VN", {
  day: "2-digit",
  month: "2-digit",
  year: "numeric",
  hour: "2-digit",
  minute: "2-digit",
});

export default function MarketPulsePage() {
  const [overview, setOverview] = useState(null);
  const [days, setDays] = useState(14);
  const [selectedSkills, setSelectedSkills] = useState([]);
  const [hoveredPoint, setHoveredPoint] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");
  const [refreshKey, setRefreshKey] = useState(0);

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
  }, [days, selectedSkills, refreshKey]);

  const selectedSkillSet = useMemo(() => new Set(selectedSkills), [selectedSkills]);
  const skills = overview?.skills ?? [];
  const todaySkills = overview?.todaySkills ?? [];
  const trendPoints = overview?.trendPoints ?? [];
  const categorySummaries = overview?.categorySummaries ?? [];
  const locationSummaries = overview?.locationSummaries ?? [];
  const todayJobs = overview?.todayJobs ?? [];
  const recentJobs = overview?.recentJobs ?? [];

  const latestUpdated = overview?.lastUpdatedAt
    ? dateTimeFormatter.format(new Date(overview.lastUpdatedAt))
    : "No sync yet";

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
          <div className="inline-flex items-center gap-2 rounded-md border border-cyan-200 bg-cyan-50 px-3 py-1 text-xs font-bold uppercase text-cyan-800">
            <Activity size={14} />
            Market Pulse
          </div>
          <h1 className="mt-3 text-3xl font-bold text-slate-950">IT hiring demand</h1>
          <p className="mt-3 max-w-3xl text-sm leading-6 text-slate-600">
            Live TopCV job signals from active openings and postings published today.
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
          <button
            type="button"
            onClick={() => setRefreshKey((value) => value + 1)}
            className="inline-flex h-10 items-center gap-2 rounded-md border border-slate-200 bg-white px-4 text-sm font-bold text-slate-700 transition hover:border-slate-400"
          >
            <RefreshCw size={16} />
            Refresh
          </button>
        </div>
      </div>

      {error && (
        <div className="mb-6 flex items-center gap-2 rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm font-semibold text-red-700">
          <AlertTriangle size={17} />
          {error}
        </div>
      )}

      <div className="grid gap-4 md:grid-cols-4">
        <MetricCard icon={<BriefcaseBusiness size={18} />} label="Active jobs" value={formatNumber(overview?.activePostings)} />
        <MetricCard icon={<CalendarDays size={18} />} label="Jobs today" value={formatNumber(overview?.todayPostings)} />
        <MetricCard icon={<Database size={18} />} label="Sources" value={formatNumber(overview?.sourceCount)} />
        <MetricCard icon={<TrendingUp size={18} />} label="Updated" value={latestUpdated} />
      </div>

      <div className="mt-6 grid gap-6 lg:grid-cols-[minmax(0,1fr)_380px]">
        <section className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
          <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <h2 className="text-lg font-bold text-slate-950">Demand movement</h2>
              <p className="mt-1 text-sm text-slate-500">Keyword mentions by posting date.</p>
            </div>
            <BarChart3 className="text-slate-400" size={22} />
          </div>

          <TrendChart
            points={trendPoints}
            selectedSkills={selectedSkills}
            hoveredPoint={hoveredPoint}
            onHoverPoint={setHoveredPoint}
          />
        </section>

        <section className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
          <div className="flex items-start justify-between gap-3">
            <div>
              <h2 className="text-lg font-bold text-slate-950">Trending skills</h2>
              <p className="mt-1 text-sm text-slate-500">Top signals across active openings.</p>
            </div>
            <span className="rounded-md bg-slate-100 px-2 py-1 text-xs font-bold text-slate-600">
              max 6
            </span>
          </div>

          <div className="mt-4 flex flex-wrap gap-2">
            {skills.map((skill) => (
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

          <SkillTable skills={skills.slice(0, 10)} />
        </section>
      </div>

      <div className="mt-6 grid gap-6 lg:grid-cols-3">
        <SegmentPanel title="Category mix" segments={categorySummaries} />
        <SegmentPanel title="Location mix" segments={locationSummaries} icon={<MapPin size={18} />} />
        <section className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
          <h2 className="text-lg font-bold text-slate-950">Today skills</h2>
          <SkillTable skills={todaySkills.slice(0, 6)} compact />
        </section>
      </div>

      <div className="mt-6 grid gap-6 xl:grid-cols-2">
        <JobList title="Jobs posted today" jobs={todayJobs} />
        <JobList title="Recent active jobs" jobs={recentJobs} />
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
        <span className="inline-flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-slate-100 text-slate-700">
          {icon}
        </span>
        <span className="text-xs font-bold uppercase text-slate-500">{label}</span>
      </div>
      <div className="mt-4 min-h-8 break-words text-2xl font-bold text-slate-950">{value}</div>
    </div>
  );
}

function SkillTable({ skills, compact = false }) {
  if (skills.length === 0) {
    return (
      <div className="mt-4 rounded-md border border-dashed border-slate-300 bg-slate-50 px-4 py-6 text-center text-sm font-semibold text-slate-500">
        No skill signals yet.
      </div>
    );
  }

  return (
    <div className="mt-5 overflow-hidden rounded-md border border-slate-200">
      <table className="w-full text-left text-sm">
        <thead className="bg-slate-50 text-xs uppercase text-slate-500">
          <tr>
            <th className="px-3 py-3">Skill</th>
            <th className="px-3 py-3">Mentions</th>
            {!compact && <th className="px-3 py-3">Growth</th>}
          </tr>
        </thead>
        <tbody className="divide-y divide-slate-100">
          {skills.map((skill) => (
            <tr key={skill.skillSlug}>
              <td className="px-3 py-3 font-semibold text-slate-800">{skill.skillName}</td>
              <td className="px-3 py-3 text-slate-600">{formatNumber(skill.mentionCount)}</td>
              {!compact && (
                <td className={`px-3 py-3 font-bold ${growthClass(skill.growthPercent)}`}>
                  {formatGrowth(skill.growthPercent)}
                </td>
              )}
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function SegmentPanel({ title, segments, icon = null }) {
  return (
    <section className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
      <div className="flex items-center gap-2">
        {icon && <span className="text-slate-400">{icon}</span>}
        <h2 className="text-lg font-bold text-slate-950">{title}</h2>
      </div>

      <div className="mt-5 space-y-4">
        {segments.length === 0 && (
          <div className="rounded-md border border-dashed border-slate-300 bg-slate-50 px-4 py-6 text-center text-sm font-semibold text-slate-500">
            No segment data.
          </div>
        )}

        {segments.map((segment) => (
          <div key={segment.name}>
            <div className="mb-1 flex items-center justify-between gap-3 text-sm">
              <span className="truncate font-semibold text-slate-700">{segment.name}</span>
              <span className="shrink-0 font-bold text-slate-950">{formatNumber(segment.count)}</span>
            </div>
            <div className="h-2 rounded-full bg-slate-100">
              <div
                className="h-2 rounded-full bg-blue-600"
                style={{ width: `${Math.min(100, Number(segment.percent || 0))}%` }}
              />
            </div>
          </div>
        ))}
      </div>
    </section>
  );
}

function JobList({ title, jobs }) {
  return (
    <section className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
      <h2 className="text-lg font-bold text-slate-950">{title}</h2>

      {jobs.length === 0 ? (
        <div className="mt-4 rounded-md border border-dashed border-slate-300 bg-slate-50 px-4 py-8 text-center text-sm font-semibold text-slate-500">
          No jobs found.
        </div>
      ) : (
        <div className="mt-4 divide-y divide-slate-100">
          {jobs.map((job) => (
            <article key={`${job.id}-${job.url}`} className="py-4 first:pt-0 last:pb-0">
              <div className="flex items-start justify-between gap-3">
                <div className="min-w-0">
                  <a
                    href={job.url}
                    target="_blank"
                    rel="noreferrer"
                    className="group inline-flex items-start gap-2 text-sm font-bold leading-6 text-slate-950 hover:text-blue-700"
                  >
                    <span>{job.title}</span>
                    <ExternalLink className="mt-1 shrink-0 opacity-50 group-hover:opacity-100" size={14} />
                  </a>
                  <div className="mt-1 text-sm font-semibold text-slate-600">
                    {[job.company, job.location].filter(Boolean).join(" - ") || "Unknown company"}
                  </div>
                </div>
                <span className="shrink-0 rounded-md bg-slate-100 px-2 py-1 text-xs font-bold text-slate-600">
                  {job.category || "Other"}
                </span>
              </div>

              <div className="mt-3 flex flex-wrap gap-2 text-xs font-semibold text-slate-500">
                {job.salary && <span className="rounded-md bg-emerald-50 px-2 py-1 text-emerald-700">{job.salary}</span>}
                {job.experience && <span className="rounded-md bg-blue-50 px-2 py-1 text-blue-700">{job.experience}</span>}
                {job.postDate && <span className="rounded-md bg-slate-100 px-2 py-1">{formatDate(job.postDate)}</span>}
              </div>

              {job.specialties?.length > 0 && (
                <div className="mt-3 flex flex-wrap gap-2">
                  {job.specialties.slice(0, 4).map((specialty) => (
                    <span key={specialty} className="rounded-md border border-slate-200 px-2 py-1 text-xs font-semibold text-slate-600">
                      {specialty}
                    </span>
                  ))}
                </div>
              )}
            </article>
          ))}
        </div>
      )}
    </section>
  );
}

function TrendChart({ points, selectedSkills, hoveredPoint, onHoverPoint }) {
  const width = 900;
  const height = 360;
  const padding = { top: 24, right: 28, bottom: 44, left: 54 };

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

  if (points.length === 0 || skills.length === 0) {
    return (
      <div className="flex aspect-[16/7] min-h-72 items-center justify-center rounded-md border border-dashed border-slate-300 bg-slate-50 text-center text-sm font-semibold text-slate-500">
        No trend data yet.
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
          if (index % Math.ceil(Math.max(1, dates.length / 7)) !== 0 && index !== dates.length - 1) {
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
          {hoveredPoint.skillName}: {formatNumber(hoveredPoint.mentionCount)} mentions in{" "}
          {formatNumber(hoveredPoint.postingCount)} jobs on {formatDate(hoveredPoint.date)}
        </div>
      )}
    </div>
  );
}

function formatNumber(value) {
  return numberFormatter.format(Number(value || 0));
}

function formatDate(value) {
  return dateFormatter.format(new Date(value));
}

function formatGrowth(value) {
  const normalized = Number(value || 0);
  const formatted = Number.isInteger(normalized) ? normalized.toString() : normalized.toFixed(1);
  return `${normalized > 0 ? "+" : ""}${formatted}%`;
}

function growthClass(value) {
  const normalized = Number(value || 0);

  if (normalized > 0) return "text-green-700";
  if (normalized < 0) return "text-red-700";
  return "text-slate-500";
}
