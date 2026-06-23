import { useEffect, useMemo, useState } from "react";
import {
  Activity,
  AlertTriangle,
  BarChart3,
  CalendarDays,
  Database,
  Layers,
  LineChart,
  MapPin,
  RefreshCw,
  Search,
  ShieldCheck,
  SlidersHorizontal,
  Sparkles,
  Target,
  TrendingDown,
  TrendingUp,
  X,
} from "lucide-react";
import { marketPulseApi } from "../api/marketPulseApi";

const emptyArray = Object.freeze([]);
const emptyObject = Object.freeze({});
const dayOptions = [7, 14, 30, 90];
const colors = ["#2563eb", "#16a34a", "#dc2626", "#9333ea", "#ca8a04", "#0891b2"];
const numberFormatter = new Intl.NumberFormat("vi-VN");
const dateTimeFormatter = new Intl.DateTimeFormat("vi-VN", {
  day: "2-digit",
  month: "2-digit",
  year: "numeric",
  hour: "2-digit",
  minute: "2-digit",
});

const initialFilters = {
  category: "",
  location: "",
  seniority: "",
  source: "",
  skillQuery: "",
};

export default function MarketPulsePage() {
  const [overview, setOverview] = useState(null);
  const [days, setDays] = useState(30);
  const [filters, setFilters] = useState(initialFilters);
  const [selectedSkills, setSelectedSkills] = useState([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");
  const [refreshKey, setRefreshKey] = useState(0);

  const queryParams = useMemo(
    () => ({
      days,
      skills: selectedSkills,
      category: filters.category,
      location: filters.location,
      experience: filters.seniority,
      source: filters.source,
    }),
    [days, filters, selectedSkills],
  );

  useEffect(() => {
    let ignore = false;

    async function loadOverview() {
      setIsLoading(true);
      setError("");

      try {
        const data = await marketPulseApi.getOverview(queryParams);
        if (!ignore) {
          setOverview(data);
        }
      } catch (err) {
        if (!ignore) {
          setError(err?.message || "Unable to load Job Market Pulse.");
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
  }, [queryParams, refreshKey]);

  const skills = overview?.skills ?? emptyArray;
  const trendPoints = overview?.trendPoints ?? emptyArray;
  const categorySummaries = overview?.categorySummaries ?? emptyArray;
  const locationSummaries = overview?.locationSummaries ?? emptyArray;
  const sourceSummaries = overview?.sourceSummaries ?? emptyArray;
  const senioritySummaries = overview?.experienceSummaries ?? emptyArray;
  const insightCards = overview?.insightCards ?? emptyArray;
  const risingSkills = overview?.risingSkills ?? emptyArray;
  const fallingSkills = overview?.fallingSkills ?? emptyArray;
  const coOccurrences = overview?.skillCoOccurrences ?? emptyArray;
  const recommendations = overview?.learningRecommendations ?? emptyArray;
  const dataQuality = overview?.dataQuality ?? emptyObject;
  const insightMeta = overview?.insightMeta ?? emptyObject;
  const salaryInsight = overview?.salaryInsight ?? emptyObject;
  const selectedSkillSet = useMemo(() => new Set(selectedSkills), [selectedSkills]);

  const categoryOptions = buildOptions(categorySummaries, filters.category);
  const locationOptions = buildOptions(locationSummaries, filters.location);
  const seniorityOptions = buildOptions(senioritySummaries, filters.seniority);
  const sourceOptions = buildOptions(sourceSummaries, filters.source);
  const visibleSkillOptions = filterSkills(skills, selectedSkills, filters.skillQuery);
  const topSkill = skills[0];
  const topCategory = categorySummaries[0];
  const lastUpdated = overview?.lastUpdatedAt
    ? dateTimeFormatter.format(new Date(overview.lastUpdatedAt))
    : "No crawl yet";
  const hasData = Number(overview?.activePostings || overview?.totalPostings || 0) > 0;

  const updateFilter = (key, value) => {
    setFilters((current) => ({ ...current, [key]: value }));
  };

  const resetFilters = () => {
    setDays(30);
    setFilters(initialFilters);
    setSelectedSkills([]);
  };

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
            Job Market Pulse
          </div>
          <h1 className="mt-3 text-3xl font-bold text-slate-950">IT market demand signals</h1>
          <p className="mt-3 max-w-3xl text-sm leading-6 text-slate-600">
            Aggregated hiring signals across skills, technologies, categories, seniority, locations, and source freshness.
          </p>
        </div>

        <button
          type="button"
          onClick={() => setRefreshKey((value) => value + 1)}
          disabled={isLoading}
          className="inline-flex h-10 items-center gap-2 self-start rounded-md border border-slate-200 bg-white px-4 text-sm font-bold text-slate-700 transition hover:border-slate-400 disabled:cursor-not-allowed disabled:opacity-60 lg:self-auto"
        >
          <RefreshCw size={16} className={isLoading ? "animate-spin" : ""} />
          Refresh
        </button>
      </div>

      {error && <StateBanner tone="error" icon={AlertTriangle} message={error} />}

      <FilterPanel
        days={days}
        filters={filters}
        selectedSkills={selectedSkills}
        selectedSkillSet={selectedSkillSet}
        skillOptions={visibleSkillOptions}
        categoryOptions={categoryOptions}
        locationOptions={locationOptions}
        seniorityOptions={seniorityOptions}
        sourceOptions={sourceOptions}
        onSetDays={setDays}
        onUpdateFilter={updateFilter}
        onToggleSkill={toggleSkill}
        onReset={resetFilters}
      />

      {isLoading && !overview && (
        <StateBanner tone="neutral" icon={RefreshCw} message="Loading market analytics..." spin />
      )}

      {!isLoading && !error && overview && !hasData && (
        <StateBanner
          tone="neutral"
          icon={Database}
          message="No analyzed market data is available for this filter set yet."
        />
      )}

      <div className="mt-6 grid gap-4 sm:grid-cols-2 xl:grid-cols-5">
        <KpiCard icon={Database} label="Total analyzed jobs" value={formatNumber(overview?.totalPostings || overview?.activePostings)} />
        <KpiCard icon={Layers} label="Sources tracked" value={formatNumber(overview?.sourceCount || dataQuality?.sourceCount)} />
        <KpiCard icon={CalendarDays} label="Last updated" value={lastUpdated} compact />
        <KpiCard icon={Sparkles} label="Top trending skill" value={topSkill?.skillName || "No signal"} />
        <KpiCard icon={Target} label="Most active category" value={topCategory?.name || "Unspecified"} />
      </div>

      <QualityPanel quality={dataQuality} meta={insightMeta} />

      <div className="mt-6 grid gap-6 lg:grid-cols-[minmax(0,1fr)_360px]">
        <DashboardPanel
          icon={LineChart}
          title="Skill demand over time"
          description={`Mention trend across the latest ${formatNumber(insightMeta?.periodDays || days)} day window.`}
        >
          <TrendChart points={trendPoints} selectedSkills={selectedSkills} topSkills={skills.slice(0, 6)} />
        </DashboardPanel>

        <DashboardPanel
          icon={TrendingUp}
          title="Top skills"
          description="Frequency of normalized skill mentions in the analyzed sample."
        >
          <HorizontalBars data={skills.slice(0, 10)} labelKey="skillName" valueKey="mentionCount" />
        </DashboardPanel>
      </div>

      <div className="mt-6 grid gap-6 lg:grid-cols-3">
        <DashboardPanel icon={BarChart3} title="Category distribution" description="Demand mix by normalized IT category.">
          <HorizontalBars data={categorySummaries} labelKey="name" valueKey="count" />
        </DashboardPanel>
        <DashboardPanel icon={MapPin} title="Location distribution" description="Market concentration by captured location.">
          <HorizontalBars data={locationSummaries} labelKey="name" valueKey="count" />
        </DashboardPanel>
        <DashboardPanel icon={ShieldCheck} title="Seniority distribution" description="Role level inferred from title and experience signals.">
          <HorizontalBars data={senioritySummaries} labelKey="name" valueKey="count" />
        </DashboardPanel>
      </div>

      <div className="mt-6 grid gap-6 lg:grid-cols-3">
        <MovementPanel title="Fast-growing skills" icon={TrendingUp} skills={risingSkills} />
        <MovementPanel title="Declining skills" icon={TrendingDown} skills={fallingSkills} negative />
        <DashboardPanel icon={Sparkles} title="Top technologies" description="Skill pairs that frequently appear together.">
          <PairList pairs={coOccurrences} />
        </DashboardPanel>
      </div>

      <div className="mt-6 grid gap-6 lg:grid-cols-[minmax(0,1fr)_360px]">
        <DashboardPanel icon={Activity} title="What the market is asking for" description="Generated summaries from the current analytical snapshot.">
          <InsightGrid cards={insightCards} />
        </DashboardPanel>

        <DashboardPanel icon={Target} title="Recommended learning focus" description="Learning priorities derived from rising demand and co-occurrence signals.">
          <RecommendationList recommendations={recommendations} />
        </DashboardPanel>
      </div>

      <div className="mt-6 grid gap-6 lg:grid-cols-3">
        <DashboardPanel icon={Database} title="Source distribution" description="Share of analyzed signals by source.">
          <HorizontalBars data={sourceSummaries} labelKey="name" valueKey="count" />
        </DashboardPanel>
        <DashboardPanel icon={ShieldCheck} title="Data freshness status" description="Confidence combines sample size, coverage, freshness, and source diversity.">
          <FreshnessPanel quality={dataQuality} lastUpdated={lastUpdated} />
        </DashboardPanel>
        <DashboardPanel icon={BarChart3} title="Salary signal coverage" description="Salary analytics use only parseable salary strings.">
          <SalarySignal insight={salaryInsight} />
        </DashboardPanel>
      </div>
    </section>
  );
}

function FilterPanel({
  days,
  filters,
  selectedSkills,
  selectedSkillSet,
  skillOptions,
  categoryOptions,
  locationOptions,
  seniorityOptions,
  sourceOptions,
  onSetDays,
  onUpdateFilter,
  onToggleSkill,
  onReset,
}) {
  return (
    <section className="rounded-lg border border-slate-200 bg-white p-4 shadow-sm">
      <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
        <div className="flex items-center gap-2 text-sm font-bold text-slate-800">
          <span className="inline-flex h-9 w-9 items-center justify-center rounded-md bg-cyan-50 text-cyan-700">
            <SlidersHorizontal size={17} />
          </span>
          Market filters
        </div>

        <div className="flex flex-wrap items-center gap-2">
          {dayOptions.map((option) => (
            <button
              key={option}
              type="button"
              onClick={() => onSetDays(option)}
              className={`h-9 rounded-md border px-3 text-sm font-bold transition ${
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
            onClick={onReset}
            className="inline-flex h-9 items-center gap-2 rounded-md border border-slate-200 bg-slate-50 px-3 text-sm font-bold text-slate-600 transition hover:border-slate-400 hover:bg-white"
          >
            <X size={15} />
            Reset
          </button>
        </div>
      </div>

      <div className="mt-4 grid gap-3 md:grid-cols-2 xl:grid-cols-4">
        <FilterSelect label="Category" value={filters.category} options={categoryOptions} onChange={(value) => onUpdateFilter("category", value)} />
        <FilterSelect label="Location" value={filters.location} options={locationOptions} onChange={(value) => onUpdateFilter("location", value)} />
        <FilterSelect label="Seniority" value={filters.seniority} options={seniorityOptions} onChange={(value) => onUpdateFilter("seniority", value)} />
        <FilterSelect label="Source" value={filters.source} options={sourceOptions} onChange={(value) => onUpdateFilter("source", value)} />
      </div>

      <div className="mt-4 rounded-md border border-slate-200 bg-slate-50 p-3">
        <div className="grid gap-3 lg:grid-cols-[280px_minmax(0,1fr)] lg:items-start">
          <label className="relative block">
            <Search className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" size={16} />
            <input
              value={filters.skillQuery}
              onChange={(event) => onUpdateFilter("skillQuery", event.target.value)}
              className="h-10 w-full rounded-md border border-slate-200 bg-white pl-9 pr-3 text-sm font-semibold text-slate-800 outline-none transition focus:border-cyan-500 focus:ring-2 focus:ring-cyan-100"
              placeholder="Filter skills"
            />
          </label>

          <div className="flex max-h-28 flex-wrap gap-2 overflow-y-auto">
            {skillOptions.length === 0 ? (
              <span className="rounded-md border border-dashed border-slate-300 bg-white px-3 py-2 text-sm font-semibold text-slate-500">
                No matching skill signal.
              </span>
            ) : (
              skillOptions.map((skill) => (
                <button
                  key={skill.skillSlug}
                  type="button"
                  onClick={() => onToggleSkill(skill.skillSlug)}
                  className={`rounded-md border px-3 py-2 text-sm font-bold transition ${
                    selectedSkillSet.has(skill.skillSlug)
                      ? "border-blue-500 bg-blue-50 text-blue-800"
                      : "border-slate-200 bg-white text-slate-600 hover:border-slate-400"
                  }`}
                >
                  {skill.skillName}
                </button>
              ))
            )}
          </div>
        </div>

        {selectedSkills.length > 0 && (
          <div className="mt-3 flex flex-wrap gap-2">
            {selectedSkills.map((slug) => (
              <span key={slug} className="rounded-md bg-slate-900 px-2.5 py-1 text-xs font-bold text-white">
                {skillLabelFromSlug(slug)}
              </span>
            ))}
          </div>
        )}
      </div>
    </section>
  );
}

function FilterSelect({ label, value, options, onChange }) {
  return (
    <label className="block">
      <span className="text-xs font-bold uppercase text-slate-500">{label}</span>
      <select
        value={value}
        onChange={(event) => onChange(event.target.value)}
        className="mt-1 h-10 w-full rounded-md border border-slate-200 bg-white px-3 text-sm font-semibold text-slate-800 outline-none transition focus:border-cyan-500 focus:ring-2 focus:ring-cyan-100"
      >
        <option value="">All</option>
        {options.map((option) => (
          <option key={option} value={option}>
            {option}
          </option>
        ))}
      </select>
    </label>
  );
}

function KpiCard({ icon: Icon, label, value, compact = false }) {
  return (
    <div className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
      <div className="flex items-center gap-3 text-slate-500">
        <span className="inline-flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-slate-100 text-slate-700">
          <Icon size={18} />
        </span>
        <span className="text-xs font-bold uppercase text-slate-500">{label}</span>
      </div>
      <div className={`${compact ? "text-base" : "text-2xl"} mt-4 min-h-8 break-words font-bold text-slate-950`}>
        {value}
      </div>
    </div>
  );
}

function DashboardPanel({ icon: Icon, title, description, children }) {
  return (
    <section className="rounded-lg border border-slate-200 bg-white p-5 shadow-sm">
      <div className="mb-4 flex items-start justify-between gap-3">
        <div>
          <h2 className="text-lg font-bold text-slate-950">{title}</h2>
          <p className="mt-1 text-sm leading-6 text-slate-500">{description}</p>
        </div>
        <Icon className="shrink-0 text-slate-400" size={21} />
      </div>
      {children}
    </section>
  );
}

function QualityPanel({ quality, meta }) {
  const level = String(quality?.level || "low").toLowerCase();
  const toneClass = level === "high"
    ? "border-emerald-200 bg-emerald-50 text-emerald-800"
    : level === "medium"
      ? "border-amber-200 bg-amber-50 text-amber-800"
      : "border-red-200 bg-red-50 text-red-800";

  return (
    <div className={`mt-6 rounded-lg border px-4 py-3 ${toneClass}`}>
      <div className="flex flex-col gap-2 lg:flex-row lg:items-center lg:justify-between">
        <div className="flex items-center gap-2 text-sm font-bold">
          <ShieldCheck size={17} />
          Data confidence: {capitalize(level)} ({formatDecimal(quality?.score)}/100)
        </div>
        <div className="text-xs font-semibold">
          Sample {formatNumber(meta?.sampleSize || quality?.sampleSize)} | Sources {formatNumber(quality?.sourceCount)} | Freshness {formatNumber(quality?.freshnessHours)}h
        </div>
      </div>
      {quality?.warnings?.length > 0 && (
        <div className="mt-2 flex flex-wrap gap-2">
          {quality.warnings.slice(0, 4).map((warning) => (
            <span key={warning} className="rounded-md bg-white/70 px-2 py-1 text-xs font-semibold">
              {warning}
            </span>
          ))}
        </div>
      )}
    </div>
  );
}

function StateBanner({ icon: Icon, message, tone, spin = false }) {
  const className = tone === "error"
    ? "border-red-200 bg-red-50 text-red-700"
    : "border-slate-200 bg-white text-slate-600";

  return (
    <div className={`mb-6 flex items-start gap-2 rounded-md border px-4 py-3 text-sm font-semibold ${className}`}>
      <Icon size={17} className={spin ? "animate-spin" : ""} />
      {message}
    </div>
  );
}

function TrendChart({ points, selectedSkills, topSkills }) {
  const width = 900;
  const height = 320;
  const padding = { top: 22, right: 28, bottom: 42, left: 54 };
  const dates = [...new Set(points.map((point) => point.date?.slice(0, 10)).filter(Boolean))].sort();
  const fallbackSlugs = topSkills.map((skill) => skill.skillSlug);
  const skillSlugs = selectedSkills.length > 0 ? selectedSkills : fallbackSlugs.slice(0, 6);
  const maxValue = Math.max(1, ...points.map((point) => Number(point.mentionCount || 0)));
  const innerWidth = width - padding.left - padding.right;
  const innerHeight = height - padding.top - padding.bottom;

  if (points.length === 0 || skillSlugs.length === 0) {
    return <EmptyBlock message="No trend data yet." />;
  }

  const xForDate = (date) => {
    if (dates.length <= 1) return padding.left + innerWidth / 2;
    return padding.left + (dates.indexOf(date) / (dates.length - 1)) * innerWidth;
  };
  const yForValue = (value) => padding.top + innerHeight - (value / maxValue) * innerHeight;

  const series = skillSlugs.map((slug, index) => {
    const seriesPoints = points
      .filter((point) => point.skillSlug === slug)
      .sort((a, b) => String(a.date).localeCompare(String(b.date)));

    return {
      slug,
      label: seriesPoints[0]?.skillName || skillLabelFromSlug(slug),
      color: colors[index % colors.length],
      points: seriesPoints,
    };
  });

  return (
    <div className="overflow-x-auto">
      <svg viewBox={`0 0 ${width} ${height}`} className="min-h-72 w-full min-w-[720px]" role="img" aria-label="Skill demand trend">
        {[0, 0.25, 0.5, 0.75, 1].map((ratio) => {
          const y = padding.top + innerHeight * ratio;
          const value = Math.round(maxValue * (1 - ratio));

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

        {series.map((item) => {
          const path = item.points
            .map((point, index) => {
              const date = point.date.slice(0, 10);
              return `${index === 0 ? "M" : "L"} ${xForDate(date)} ${yForValue(point.mentionCount || 0)}`;
            })
            .join(" ");

          return (
            <g key={item.slug}>
              <path d={path} fill="none" stroke={item.color} strokeWidth="3" strokeLinecap="round" strokeLinejoin="round" />
              {item.points.map((point) => {
                const date = point.date.slice(0, 10);
                return <circle key={`${item.slug}-${date}`} cx={xForDate(date)} cy={yForValue(point.mentionCount || 0)} r="4" fill={item.color} />;
              })}
            </g>
          );
        })}
      </svg>

      <div className="mt-3 flex flex-wrap gap-3">
        {series.map((item) => (
          <span key={item.slug} className="inline-flex items-center gap-2 text-xs font-bold text-slate-600">
            <span className="h-2.5 w-2.5 rounded-full" style={{ backgroundColor: item.color }} />
            {item.label}
          </span>
        ))}
      </div>
    </div>
  );
}

function HorizontalBars({ data, labelKey, valueKey }) {
  if (!data?.length) {
    return <EmptyBlock message="No distribution data yet." />;
  }

  const maxValue = Math.max(1, ...data.map((item) => Number(item[valueKey] || 0)));

  return (
    <div className="space-y-3">
      {data.map((item, index) => {
        const value = Number(item[valueKey] || 0);
        const percent = Math.max(4, Math.round((value / maxValue) * 100));

        return (
          <div key={`${item[labelKey]}-${index}`}>
            <div className="mb-1 flex items-center justify-between gap-3 text-xs font-bold text-slate-600">
              <span className="truncate">{item[labelKey] || "Unspecified"}</span>
              <span>{formatNumber(value)}</span>
            </div>
            <div className="h-2.5 overflow-hidden rounded-full bg-slate-100">
              <div className="h-full rounded-full" style={{ width: `${percent}%`, backgroundColor: colors[index % colors.length] }} />
            </div>
          </div>
        );
      })}
    </div>
  );
}

function MovementPanel({ title, icon: Icon, skills, negative = false }) {
  return (
    <DashboardPanel
      icon={Icon}
      title={title}
      description={negative ? "Skills cooling in the comparison window." : "Skills accelerating in the comparison window."}
    >
      {!skills?.length ? (
        <EmptyBlock message="No movement signal yet." />
      ) : (
        <div className="space-y-3">
          {skills.slice(0, 6).map((skill) => (
            <div key={skill.skillSlug} className="rounded-md border border-slate-200 bg-slate-50 px-3 py-3">
              <div className="flex items-center justify-between gap-3">
                <div className="min-w-0">
                  <div className="truncate text-sm font-bold text-slate-950">{skill.skillName}</div>
                  <div className="mt-1 text-xs font-semibold text-slate-500">
                    {formatNumber(skill.currentMentions)} current mentions
                  </div>
                </div>
                <span className={`shrink-0 rounded-md px-2 py-1 text-xs font-bold ${negative ? "bg-red-100 text-red-700" : "bg-emerald-100 text-emerald-700"}`}>
                  {formatDelta(skill.delta)}
                </span>
              </div>
            </div>
          ))}
        </div>
      )}
    </DashboardPanel>
  );
}

function PairList({ pairs }) {
  if (!pairs?.length) {
    return <EmptyBlock message="No co-occurrence signal yet." />;
  }

  return (
    <div className="space-y-3">
      {pairs.slice(0, 8).map((pair) => (
        <div key={`${pair.skillASlug}-${pair.skillBSlug}`} className="rounded-md border border-slate-200 bg-slate-50 px-3 py-3">
          <div className="text-sm font-bold text-slate-950">
            {pair.skillA} + {pair.skillB}
          </div>
          <div className="mt-1 text-xs font-semibold text-slate-500">
            {formatNumber(pair.postingCount)} postings | {formatDecimal(pair.percentOfSample)}%
          </div>
        </div>
      ))}
    </div>
  );
}

function InsightGrid({ cards }) {
  if (!cards?.length) {
    return <EmptyBlock message="No market insight summary yet." />;
  }

  return (
    <div className="grid gap-3 md:grid-cols-2">
      {cards.map((card) => (
        <div key={card.title} className="rounded-md border border-slate-200 bg-slate-50 p-4">
          <div className="text-xs font-bold uppercase text-slate-500">{card.title}</div>
          <div className="mt-2 text-xl font-bold text-slate-950">{card.value}</div>
          <p className="mt-2 text-sm leading-6 text-slate-600">{card.detail}</p>
        </div>
      ))}
    </div>
  );
}

function RecommendationList({ recommendations }) {
  if (!recommendations?.length) {
    return <EmptyBlock message="No recommendation signal yet." />;
  }

  return (
    <div className="space-y-3">
      {recommendations.slice(0, 6).map((item) => (
        <div key={`${item.title}-${item.skillSlug || ""}`} className="rounded-md border border-slate-200 bg-slate-50 px-3 py-3">
          <div className="flex items-start justify-between gap-3">
            <div>
              <div className="text-sm font-bold text-slate-950">{item.title}</div>
              <p className="mt-1 text-xs font-semibold leading-5 text-slate-500">{item.detail}</p>
            </div>
            <span className="shrink-0 rounded-md bg-blue-100 px-2 py-1 text-xs font-bold text-blue-700">
              {capitalize(item.priority)}
            </span>
          </div>
        </div>
      ))}
    </div>
  );
}

function FreshnessPanel({ quality, lastUpdated }) {
  return (
    <div className="space-y-3 text-sm font-semibold text-slate-600">
      <MetricLine label="Last updated" value={lastUpdated} />
      <MetricLine label="Freshness hours" value={formatNumber(quality?.freshnessHours)} />
      <MetricLine label="Detail coverage" value={`${formatDecimal(quality?.detailCoveragePercent)}%`} />
      <MetricLine label="Category coverage" value={`${formatDecimal(quality?.categoryCoveragePercent)}%`} />
      <MetricLine label="Location coverage" value={`${formatDecimal(quality?.locationCoveragePercent)}%`} />
    </div>
  );
}

function SalarySignal({ insight }) {
  return (
    <div className="space-y-3 text-sm font-semibold text-slate-600">
      <MetricLine label="Coverage" value={`${formatDecimal(insight?.coveragePercent)}%`} />
      <MetricLine label="Sample size" value={formatNumber(insight?.sampleSize)} />
      <MetricLine label="Median min" value={formatMoneyVnd(insight?.medianMinMonthlyVnd)} />
      <MetricLine label="Median max" value={formatMoneyVnd(insight?.medianMaxMonthlyVnd)} />
      <MetricLine label="Confidence" value={capitalize(insight?.confidence || "low")} />
    </div>
  );
}

function MetricLine({ label, value }) {
  return (
    <div className="flex items-center justify-between gap-3 rounded-md bg-slate-50 px-3 py-2">
      <span>{label}</span>
      <span className="text-right font-bold text-slate-950">{value}</span>
    </div>
  );
}

function EmptyBlock({ message }) {
  return (
    <div className="flex min-h-32 items-center justify-center rounded-md border border-dashed border-slate-300 bg-slate-50 px-4 py-6 text-center text-sm font-semibold text-slate-500">
      {message}
    </div>
  );
}

function buildOptions(segments, selectedValue) {
  const values = segments
    .map((segment) => segment.name)
    .filter((name) => name && name !== "Unspecified");

  if (selectedValue && !values.some((name) => name.toLowerCase() === selectedValue.toLowerCase())) {
    values.unshift(selectedValue);
  }

  return [...new Set(values)].sort((a, b) => a.localeCompare(b));
}

function filterSkills(skills, selectedSkills, query) {
  const term = query.trim().toLowerCase();
  const selectedSet = new Set(selectedSkills);
  const options = skills.filter((skill) => {
    if (!term) return true;
    return `${skill.skillName} ${skill.skillSlug}`.toLowerCase().includes(term);
  });

  const selectedFallbacks = selectedSkills
    .filter((slug) => !options.some((skill) => skill.skillSlug === slug))
    .map((slug) => ({
      skillSlug: slug,
      skillName: skillLabelFromSlug(slug),
      mentionCount: 0,
    }));

  return [...selectedFallbacks, ...options]
    .sort((a, b) => {
      const selectedDiff = Number(selectedSet.has(b.skillSlug)) - Number(selectedSet.has(a.skillSlug));
      return selectedDiff || Number(b.mentionCount || 0) - Number(a.mentionCount || 0);
    })
    .slice(0, 24);
}

function skillLabelFromSlug(slug) {
  if (!slug) return "Skill";

  return String(slug)
    .split("-")
    .filter(Boolean)
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(" ");
}

function formatNumber(value) {
  return numberFormatter.format(Number(value || 0));
}

function formatDecimal(value) {
  const normalized = Number(value || 0);
  return Number.isInteger(normalized) ? normalized.toString() : normalized.toFixed(1);
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

function capitalize(value) {
  const normalized = String(value || "");
  return normalized ? normalized.charAt(0).toUpperCase() + normalized.slice(1) : "";
}
