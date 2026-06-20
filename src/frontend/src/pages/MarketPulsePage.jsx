import { useEffect, useMemo, useState } from "react";
import {
  Activity,
  AlertTriangle,
  BadgeCheck,
  BarChart3,
  BriefcaseBusiness,
  CalendarDays,
  Database,
  DollarSign,
  ExternalLink,
  GraduationCap,
  MapPin,
  Network,
  RefreshCw,
  ShieldCheck,
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
  const sourceSummaries = overview?.sourceSummaries ?? [];
  const todayJobs = overview?.todayJobs ?? [];
  const recentJobs = overview?.recentJobs ?? [];
  const insightMeta = overview?.insightMeta ?? {};
  const dataQuality = overview?.dataQuality ?? {};
  const insightCards = overview?.insightCards ?? [];
  const risingSkills = overview?.risingSkills ?? [];
  const fallingSkills = overview?.fallingSkills ?? [];
  const coOccurrences = overview?.skillCoOccurrences ?? [];
  const salaryInsight = overview?.salaryInsight ?? {};
  const experienceSummaries = overview?.experienceSummaries ?? [];
  const learningRecommendations = overview?.learningRecommendations ?? [];

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
    <section className="tm-page tm-soft-enter mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
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
        <MetricCard icon={<ShieldCheck size={18} />} label="Confidence" value={formatConfidence(dataQuality?.level)} />
        <MetricCard icon={<TrendingUp size={18} />} label="Updated" value={latestUpdated} />
      </div>

      <QualityBanner meta={insightMeta} quality={dataQuality} />

      <div className="mt-6 grid gap-4 md:grid-cols-2 xl:grid-cols-3">
        {insightCards.map((card) => (
          <InsightCard key={card.title} card={card} />
        ))}
      </div>

      <div className="mt-6 grid gap-6 lg:grid-cols-[minmax(0,1fr)_380px]">
        <section className="tm-animate-item rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
          <div className="mb-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
            <div>
              <h2 className="text-lg font-bold text-slate-950">Demand movement</h2>
              <p className="mt-1 text-sm text-slate-500">
                Keyword mentions by posting date over {formatNumber(insightMeta?.periodDays || days)} days.
              </p>
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

        <section className="tm-animate-item rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
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
        <SegmentPanel title="Source mix" segments={sourceSummaries} icon={<Database size={18} />} />
      </div>

      <div className="mt-6 grid gap-6 lg:grid-cols-3">
        <MovementPanel title="Rising skills" skills={risingSkills} />
        <MovementPanel title="Falling skills" skills={fallingSkills} isFalling />
        <CoOccurrencePanel pairs={coOccurrences} />
      </div>

      <div className="mt-6 grid gap-6 lg:grid-cols-3">
        <SalaryPanel insight={salaryInsight} />
        <SegmentPanel title="Experience mix" segments={experienceSummaries} icon={<BadgeCheck size={18} />} />
        <RecommendationsPanel recommendations={learningRecommendations} />
      </div>

      <div className="mt-6 grid gap-6 lg:grid-cols-3">
        <section className="tm-animate-item rounded-lg border border-slate-200 bg-white p-5 shadow-sm lg:col-span-1">
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
    <div className="tm-animate-item rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
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

function QualityBanner({ meta, quality }) {
  const warnings = quality?.warnings ?? [];
  const level = quality?.level || "low";

  return (
    <section className={`mt-6 rounded-lg border p-4 ${qualityBannerClass(level)}`}>
      <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
        <div className="flex items-start gap-3">
          <span className="inline-flex h-10 w-10 shrink-0 items-center justify-center rounded-md bg-white/70 text-slate-800">
            <ShieldCheck size={20} />
          </span>
          <div>
            <h2 className="text-sm font-bold uppercase text-slate-900">Data confidence</h2>
            <p className="mt-1 text-sm leading-6 text-slate-700">
              {formatNumber(meta?.sampleSize)} postings, {formatNumber(quality?.sourceCount)} source(s),{" "}
              {formatNumber(meta?.periodDays)} day period, quality score {formatDecimal(quality?.score)}/100.
            </p>
            {meta?.methodology && (
              <p className="mt-1 text-xs leading-5 text-slate-600">{meta.methodology}</p>
            )}
          </div>
        </div>

        <div className="grid min-w-0 gap-2 sm:grid-cols-2 lg:w-[420px]">
          <QualityStat label="Salary" value={quality?.salaryCoveragePercent} />
          <QualityStat label="Category" value={quality?.categoryCoveragePercent} />
          <QualityStat label="Location" value={quality?.locationCoveragePercent} />
          <QualityStat label="Detail" value={quality?.detailCoveragePercent} />
        </div>
      </div>

      {warnings.length > 0 && (
        <div className="mt-4 grid gap-2 md:grid-cols-2">
          {warnings.map((warning) => (
            <div key={warning} className="flex items-start gap-2 rounded-md bg-white/75 px-3 py-2 text-xs font-semibold text-slate-700">
              <AlertTriangle size={14} className="mt-0.5 shrink-0 text-amber-600" />
              <span>{warning}</span>
            </div>
          ))}
        </div>
      )}
    </section>
  );
}

function QualityStat({ label, value }) {
  return (
    <div className="rounded-md bg-white/75 px-3 py-2">
      <div className="text-xs font-bold uppercase text-slate-500">{label}</div>
      <div className="mt-1 text-lg font-bold text-slate-950">{formatPercent(value)}</div>
    </div>
  );
}

function InsightCard({ card }) {
  return (
    <article className={`tm-animate-item rounded-lg border bg-white p-5 shadow-sm ${toneBorderClass(card.tone)}`}>
      <div className="flex items-start justify-between gap-3">
        <div>
          <h2 className="text-sm font-bold uppercase text-slate-500">{card.title}</h2>
          <div className="mt-2 break-words text-2xl font-bold text-slate-950">{card.value}</div>
        </div>
        <span className={`rounded-md px-2 py-1 text-xs font-bold ${confidenceBadgeClass(card.confidence)}`}>
          {formatConfidence(card.confidence)}
        </span>
      </div>
      <p className="mt-3 text-sm leading-6 text-slate-600">{card.detail}</p>
      <div className="mt-4 text-xs font-semibold text-slate-400">
        sample {formatNumber(card.sampleSize)} | {formatNumber(card.periodDays)}d
      </div>
    </article>
  );
}

function MovementPanel({ title, skills, isFalling = false }) {
  return (
    <section className="tm-animate-item rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
      <div className="flex items-center justify-between gap-3">
        <h2 className="text-lg font-bold text-slate-950">{title}</h2>
        <TrendingUp className={isFalling ? "rotate-180 text-red-500" : "text-green-600"} size={20} />
      </div>

      {skills.length === 0 ? (
        <div className="mt-4 rounded-md border border-dashed border-slate-300 bg-slate-50 px-4 py-6 text-center text-sm font-semibold text-slate-500">
          Not enough period data.
        </div>
      ) : (
        <div className="mt-4 space-y-3">
          {skills.slice(0, 6).map((skill) => (
            <div key={skill.skillSlug} className="rounded-md border border-slate-100 bg-slate-50 px-3 py-3">
              <div className="flex items-start justify-between gap-3">
                <span className="font-bold text-slate-800">{skill.skillName}</span>
                <span className={`shrink-0 text-sm font-bold ${isFalling ? "text-red-700" : "text-green-700"}`}>
                  {formatDelta(skill.delta)}
                </span>
              </div>
              <div className="mt-1 text-xs font-semibold text-slate-500">
                {formatNumber(skill.currentMentions)} vs {formatNumber(skill.previousMentions)} mentions |{" "}
                {formatConfidence(skill.confidence)}
              </div>
            </div>
          ))}
        </div>
      )}
    </section>
  );
}

function CoOccurrencePanel({ pairs }) {
  return (
    <section className="tm-animate-item rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
      <div className="flex items-center justify-between gap-3">
        <h2 className="text-lg font-bold text-slate-950">Skill pairs</h2>
        <Network className="text-slate-400" size={20} />
      </div>

      {pairs.length === 0 ? (
        <div className="mt-4 rounded-md border border-dashed border-slate-300 bg-slate-50 px-4 py-6 text-center text-sm font-semibold text-slate-500">
          No co-occurrence signal yet.
        </div>
      ) : (
        <div className="mt-4 space-y-3">
          {pairs.slice(0, 6).map((pair) => (
            <div key={`${pair.skillASlug}-${pair.skillBSlug}`} className="rounded-md border border-slate-100 bg-slate-50 px-3 py-3">
              <div className="font-bold text-slate-800">
                {pair.skillA} + {pair.skillB}
              </div>
              <div className="mt-1 text-xs font-semibold text-slate-500">
                {formatNumber(pair.postingCount)} postings | {formatPercent(pair.percentOfSample)} of sample
              </div>
            </div>
          ))}
        </div>
      )}
    </section>
  );
}

function SalaryPanel({ insight }) {
  return (
    <section className="tm-animate-item rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
      <div className="flex items-center justify-between gap-3">
        <h2 className="text-lg font-bold text-slate-950">Salary signal</h2>
        <DollarSign className="text-slate-400" size={20} />
      </div>

      <div className="mt-4 grid gap-3 sm:grid-cols-2">
        <div className="rounded-md bg-slate-50 px-3 py-3">
          <div className="text-xs font-bold uppercase text-slate-500">Coverage</div>
          <div className="mt-1 text-xl font-bold text-slate-950">{formatPercent(insight?.coveragePercent)}</div>
        </div>
        <div className="rounded-md bg-slate-50 px-3 py-3">
          <div className="text-xs font-bold uppercase text-slate-500">Median</div>
          <div className="mt-1 text-xl font-bold text-slate-950">
            {formatMoneyVnd(insight?.medianMinMonthlyVnd)} - {formatMoneyVnd(insight?.medianMaxMonthlyVnd)}
          </div>
        </div>
      </div>

      <div className="mt-4 space-y-3">
        {(insight?.byCategory ?? []).slice(0, 4).map((segment) => (
          <div key={segment.name}>
            <div className="mb-1 flex items-center justify-between gap-3 text-sm">
              <span className="truncate font-semibold text-slate-700">{segment.name}</span>
              <span className="shrink-0 font-bold text-slate-950">
                {formatMoneyVnd(segment.medianMinMonthlyVnd)} - {formatMoneyVnd(segment.medianMaxMonthlyVnd)}
              </span>
            </div>
            <div className="text-xs font-semibold text-slate-500">
              {formatNumber(segment.sampleSize)} salary samples, {formatPercent(segment.coveragePercent)} coverage
            </div>
          </div>
        ))}
      </div>
    </section>
  );
}

function RecommendationsPanel({ recommendations }) {
  return (
    <section className="tm-animate-item rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
      <div className="flex items-center justify-between gap-3">
        <h2 className="text-lg font-bold text-slate-950">Learning actions</h2>
        <GraduationCap className="text-slate-400" size={20} />
      </div>

      {recommendations.length === 0 ? (
        <div className="mt-4 rounded-md border border-dashed border-slate-300 bg-slate-50 px-4 py-6 text-center text-sm font-semibold text-slate-500">
          No recommendation yet.
        </div>
      ) : (
        <div className="mt-4 space-y-3">
          {recommendations.slice(0, 5).map((item) => (
            <div key={`${item.title}-${item.skillSlug || item.actionLabel}`} className="rounded-md border border-slate-100 bg-slate-50 px-3 py-3">
              <div className="flex items-start justify-between gap-3">
                <span className="font-bold text-slate-800">{item.title}</span>
                <span className={`shrink-0 rounded-md px-2 py-1 text-[11px] font-bold uppercase ${priorityClass(item.priority)}`}>
                  {item.priority}
                </span>
              </div>
              <p className="mt-2 text-sm leading-6 text-slate-600">{item.detail}</p>
              <div className="mt-2 text-xs font-bold text-blue-700">{item.actionLabel}</div>
            </div>
          ))}
        </div>
      )}
    </section>
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
    <section className="tm-animate-item rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
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
    <section className="tm-animate-item rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
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

function formatDecimal(value) {
  const normalized = Number(value || 0);
  return Number.isInteger(normalized) ? normalized.toString() : normalized.toFixed(1);
}

function formatPercent(value) {
  return `${formatDecimal(value)}%`;
}

function formatConfidence(value) {
  const normalized = String(value || "low").toLowerCase();
  return normalized.charAt(0).toUpperCase() + normalized.slice(1);
}

function formatDelta(value) {
  const normalized = Number(value || 0);
  return `${normalized > 0 ? "+" : ""}${formatNumber(normalized)}`;
}

function formatMoneyVnd(value) {
  const normalized = Number(value || 0);
  if (!normalized) return "n/a";
  if (normalized >= 1_000_000) return `${formatDecimal(normalized / 1_000_000)}M`;
  return numberFormatter.format(normalized);
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

function qualityBannerClass(level) {
  if (level === "high") return "border-emerald-200 bg-emerald-50";
  if (level === "medium") return "border-amber-200 bg-amber-50";
  return "border-red-200 bg-red-50";
}

function toneBorderClass(tone) {
  if (tone === "positive") return "border-emerald-200";
  if (tone === "warning") return "border-amber-200";
  return "border-slate-200";
}

function confidenceBadgeClass(confidence) {
  if (confidence === "high") return "bg-emerald-100 text-emerald-700";
  if (confidence === "medium") return "bg-amber-100 text-amber-700";
  return "bg-red-100 text-red-700";
}

function priorityClass(priority) {
  if (priority === "high") return "bg-red-100 text-red-700";
  if (priority === "low") return "bg-slate-100 text-slate-600";
  return "bg-blue-100 text-blue-700";
}
