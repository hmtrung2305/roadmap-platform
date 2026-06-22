import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import {
  Activity,
  AlertTriangle,
  ArrowRight,
  BadgeCheck,
  BarChart3,
  BookOpenCheck,
  BriefcaseBusiness,
  CalendarDays,
  ChevronRight,
  CircleDot,
  Database,
  DollarSign,
  ExternalLink,
  Filter,
  GraduationCap,
  Info,
  Layers,
  Lightbulb,
  MapPin,
  Network,
  RefreshCw,
  Search,
  ShieldCheck,
  SlidersHorizontal,
  Sparkles,
  Target,
  TrendingUp,
  X,
} from "lucide-react";
import { marketPulseApi } from "../api/marketPulseApi";

const chartColors = ["#2563eb", "#dc2626", "#ca8a04", "#16a34a", "#7c3aed", "#0891b2"];
const chartDashPatterns = ["", "8 5", "3 5", "10 3 2 3", "2 3", "12 4"];
const emptyArray = Object.freeze([]);
const emptyObject = Object.freeze({});
const dayOptions = [7, 14, 30, 90];
const emptyFilters = {
  category: "",
  location: "",
  experience: "",
  source: "",
  salaryRange: "",
  salaryMinMillion: "",
  salaryMaxMillion: "",
  skillQuery: "",
};
const salaryRanges = [
  { value: "", label: "Any salary", min: "", max: "" },
  { value: "lt15", label: "Under 15M", min: "", max: "15" },
  { value: "15-30", label: "15M - 30M", min: "15", max: "30" },
  { value: "30-50", label: "30M - 50M", min: "30", max: "50" },
  { value: "gt50", label: "50M+", min: "50", max: "" },
];
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
  const [days, setDays] = useState(30);
  const [filters, setFilters] = useState(emptyFilters);
  const [selectedSkills, setSelectedSkills] = useState([]);
  const [selectedJob, setSelectedJob] = useState(null);
  const [hoveredPoint, setHoveredPoint] = useState(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");
  const [refreshKey, setRefreshKey] = useState(0);

  const queryParams = useMemo(
    () => ({
      days,
      skills: selectedSkills,
      category: filters.category,
      location: filters.location,
      experience: filters.experience,
      source: filters.source,
      salaryMinMonthlyVnd: toMonthlyVnd(filters.salaryMinMillion),
      salaryMaxMonthlyVnd: toMonthlyVnd(filters.salaryMaxMillion),
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

        if (ignore) return;

        setOverview(data);
      } catch (err) {
        if (!ignore) {
          setError(normalizeOverviewError(err));
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

  const selectedSkillSet = useMemo(() => new Set(selectedSkills), [selectedSkills]);
  const skills = overview?.skills ?? emptyArray;
  const todaySkills = overview?.todaySkills ?? emptyArray;
  const trendPoints = overview?.trendPoints ?? emptyArray;
  const categorySummaries = overview?.categorySummaries ?? emptyArray;
  const locationSummaries = overview?.locationSummaries ?? emptyArray;
  const sourceSummaries = overview?.sourceSummaries ?? emptyArray;
  const todayJobs = overview?.todayJobs ?? emptyArray;
  const recentJobs = overview?.recentJobs ?? emptyArray;
  const insightMeta = overview?.insightMeta ?? emptyObject;
  const dataQuality = overview?.dataQuality ?? emptyObject;
  const insightCards = overview?.insightCards ?? emptyArray;
  const risingSkills = overview?.risingSkills ?? emptyArray;
  const fallingSkills = overview?.fallingSkills ?? emptyArray;
  const coOccurrences = overview?.skillCoOccurrences ?? emptyArray;
  const salaryInsight = overview?.salaryInsight ?? emptyObject;
  const experienceSummaries = overview?.experienceSummaries ?? emptyArray;
  const learningRecommendations = overview?.learningRecommendations ?? emptyArray;
  const categoryOptions = buildSegmentOptions(categorySummaries, filters.category);
  const locationOptions = buildSegmentOptions(locationSummaries, filters.location);
  const sourceOptions = buildSegmentOptions(sourceSummaries, filters.source);
  const experienceOptions = buildSegmentOptions(experienceSummaries, filters.experience);
  const visibleSkillOptions = useMemo(
    () => filterSkillOptions(skills, selectedSkills, filters.skillQuery),
    [filters.skillQuery, selectedSkills, skills],
  );
  const activeFilterChips = useMemo(
    () => buildActiveFilterChips({ days, filters, selectedSkills, skills }),
    [days, filters, selectedSkills, skills],
  );
  const narrativeInsights = useMemo(
    () =>
      buildNarrativeInsights({
        activePostings: overview?.activePostings ?? 0,
        todayPostings: overview?.todayPostings ?? 0,
        periodDays: insightMeta?.periodDays || days,
        risingSkills,
        fallingSkills,
        categorySummaries,
        salaryInsight,
        dataQuality,
      }),
    [
      categorySummaries,
      dataQuality,
      days,
      fallingSkills,
      insightMeta?.periodDays,
      overview?.activePostings,
      overview?.todayPostings,
      risingSkills,
      salaryInsight,
    ],
  );
  const hasOverviewData = Number(overview?.activePostings || 0) > 0;

  const latestUpdated = overview?.lastUpdatedAt
    ? dateTimeFormatter.format(new Date(overview.lastUpdatedAt))
    : "No sync yet";

  const updateFilter = (key, value) => {
    setFilters((current) => ({ ...current, [key]: value }));
  };

  const applySalaryRange = (value) => {
    const range = salaryRanges.find((item) => item.value === value) || salaryRanges[0];

    setFilters((current) => ({
      ...current,
      salaryRange: range.value,
      salaryMinMillion: range.min,
      salaryMaxMillion: range.max,
    }));
  };

  const resetFilters = () => {
    setDays(30);
    setFilters(emptyFilters);
    setSelectedSkills([]);
    setHoveredPoint(null);
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
            Market Pulse
          </div>
          <h1 className="mt-3 text-3xl font-bold text-slate-950">IT hiring demand</h1>
          <p className="mt-3 max-w-3xl text-sm leading-6 text-slate-600">
            Analyze persisted Jobs API snapshots by period, role, location, salary, source, and skill focus.
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-2 lg:justify-end">
          <Link
            to="/learning-modules/browse"
            className="inline-flex h-10 items-center gap-2 rounded-md border border-emerald-200 bg-emerald-50 px-4 text-sm font-bold text-emerald-800 transition hover:border-emerald-400"
          >
            <BookOpenCheck size={16} />
            Learning modules
          </Link>
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
        <div className="mb-6 flex items-start gap-2 rounded-md border border-red-200 bg-red-50 px-4 py-3 text-sm font-semibold text-red-700">
          <AlertTriangle size={17} />
          {error}
        </div>
      )}

      <FilterBar
        days={days}
        filters={filters}
        selectedSkills={selectedSkills}
        selectedSkillSet={selectedSkillSet}
        skillOptions={visibleSkillOptions}
        categoryOptions={categoryOptions}
        locationOptions={locationOptions}
        sourceOptions={sourceOptions}
        experienceOptions={experienceOptions}
        onSetDays={setDays}
        onUpdateFilter={updateFilter}
        onApplySalaryRange={applySalaryRange}
        onToggleSkill={toggleSkill}
        onReset={resetFilters}
      />

      <FilterChips chips={activeFilterChips} />

      <div className="grid gap-4 md:grid-cols-4">
        <MetricCard icon={<BriefcaseBusiness size={18} />} label="Active jobs" value={formatNumber(overview?.activePostings)} />
        <MetricCard icon={<CalendarDays size={18} />} label="Jobs today" value={formatNumber(overview?.todayPostings)} />
        <MetricCard icon={<ShieldCheck size={18} />} label="Confidence" value={formatConfidence(dataQuality?.level)} />
        <MetricCard icon={<TrendingUp size={18} />} label="Updated" value={latestUpdated} />
      </div>

      <QualityBanner meta={insightMeta} quality={dataQuality} />

      <MarketStateNotice
        error={error}
        isLoading={isLoading}
        hasOverview={Boolean(overview)}
        hasOverviewData={hasOverviewData}
        quality={dataQuality}
        lastUpdatedAt={overview?.lastUpdatedAt}
      />

      <NarrativeInsightGrid insights={narrativeInsights} />

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
            periodDays={insightMeta?.periodDays || days}
            sampleSize={insightMeta?.sampleSize}
            sourceCount={dataQuality?.sourceCount}
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
              <div key={skill.skillSlug} className="inline-flex overflow-hidden rounded-md border border-slate-200 bg-slate-50">
                <button
                  type="button"
                  onClick={() => toggleSkill(skill.skillSlug)}
                  className={`px-3 py-2 text-left text-sm font-bold transition ${
                    selectedSkillSet.has(skill.skillSlug)
                      ? "bg-blue-50 text-blue-800"
                      : "text-slate-600 hover:bg-white"
                  }`}
                >
                  {skill.skillName}
                </button>
                <Link
                  to={buildLearningModulesHref(skill)}
                  className="inline-flex w-9 items-center justify-center border-l border-slate-200 text-slate-500 transition hover:bg-emerald-50 hover:text-emerald-700"
                  title={`Find learning modules for ${skill.skillName}`}
                >
                  <GraduationCap size={15} />
                </Link>
              </div>
            ))}
          </div>

          <SkillTable skills={skills.slice(0, 10)} onToggleSkill={toggleSkill} selectedSkillSet={selectedSkillSet} />
        </section>
      </div>

      <div className="mt-6 grid gap-6 lg:grid-cols-3">
        <SegmentPanel title="Role/category mix" segments={categorySummaries} hrefForSegment={buildRoadmapHref} />
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
        <JobList title="Jobs posted today" jobs={todayJobs} onInspectJob={setSelectedJob} />
        <JobList title="Recent active jobs" jobs={recentJobs} onInspectJob={setSelectedJob} />
      </div>

      {isLoading && (
        <div className="mt-6 rounded-md border border-slate-200 bg-white px-4 py-3 text-sm font-semibold text-slate-500">
          Loading market signals...
        </div>
      )}

      <JobBreakdownDrawer job={selectedJob} onClose={() => setSelectedJob(null)} />
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

function FilterBar({
  days,
  filters,
  selectedSkills,
  selectedSkillSet,
  skillOptions,
  categoryOptions,
  locationOptions,
  sourceOptions,
  experienceOptions,
  onSetDays,
  onUpdateFilter,
  onApplySalaryRange,
  onToggleSkill,
  onReset,
}) {
  return (
    <section className="tm-animate-item rounded-lg border border-slate-200 bg-white p-4 shadow-sm">
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
        <FilterSelect
          label="Role/category"
          value={filters.category}
          options={categoryOptions}
          placeholder="All roles"
          onChange={(value) => onUpdateFilter("category", value)}
        />
        <FilterSelect
          label="Location"
          value={filters.location}
          options={locationOptions}
          placeholder="All locations"
          onChange={(value) => onUpdateFilter("location", value)}
        />
        <FilterSelect
          label="Experience"
          value={filters.experience}
          options={experienceOptions}
          placeholder="All levels"
          onChange={(value) => onUpdateFilter("experience", value)}
        />
        <FilterSelect
          label="Source"
          value={filters.source}
          options={sourceOptions}
          placeholder="All sources"
          onChange={(value) => onUpdateFilter("source", value)}
        />
      </div>

      <div className="mt-4 grid gap-3 lg:grid-cols-[minmax(0,1.35fr)_minmax(280px,0.9fr)]">
        <div className="rounded-md border border-slate-200 bg-slate-50 p-3">
          <div className="mb-3 flex items-center justify-between gap-3">
            <label className="text-xs font-bold uppercase text-slate-500">Skill focus</label>
            <span className="text-xs font-semibold text-slate-500">
              {selectedSkills.length}/6 selected
            </span>
          </div>

          <label className="relative block">
            <Search className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" size={16} />
            <input
              value={filters.skillQuery}
              onChange={(event) => onUpdateFilter("skillQuery", event.target.value)}
              className="h-10 w-full rounded-md border border-slate-200 bg-white pl-9 pr-3 text-sm font-semibold text-slate-800 outline-none transition focus:border-cyan-500 focus:ring-2 focus:ring-cyan-100"
              placeholder="Search skill"
            />
          </label>

          <div className="mt-3 flex max-h-32 flex-wrap gap-2 overflow-y-auto pr-1">
            {skillOptions.length === 0 ? (
              <div className="rounded-md border border-dashed border-slate-300 bg-white px-3 py-2 text-sm font-semibold text-slate-500">
                No matching skill.
              </div>
            ) : (
              skillOptions.map((skill) => (
                <button
                  key={skill.skillSlug}
                  type="button"
                  onClick={() => onToggleSkill(skill.skillSlug)}
                  className={`rounded-md border px-3 py-1.5 text-sm font-bold transition ${
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

        <div className="rounded-md border border-slate-200 bg-slate-50 p-3">
          <div className="mb-3 flex items-center gap-2 text-xs font-bold uppercase text-slate-500">
            <DollarSign size={14} />
            Salary range
          </div>

          <select
            value={filters.salaryRange}
            onChange={(event) => onApplySalaryRange(event.target.value)}
            className="h-10 w-full rounded-md border border-slate-200 bg-white px-3 text-sm font-semibold text-slate-800 outline-none transition focus:border-cyan-500 focus:ring-2 focus:ring-cyan-100"
          >
            {filters.salaryRange === "custom" && <option value="custom">Custom</option>}
            {salaryRanges.map((range) => (
              <option key={range.value || "all"} value={range.value}>
                {range.label}
              </option>
            ))}
          </select>

          <div className="mt-3 grid grid-cols-2 gap-2">
            <label className="block">
              <span className="text-xs font-bold text-slate-500">Min M/month</span>
              <input
                value={filters.salaryMinMillion}
                onChange={(event) => {
                  onUpdateFilter("salaryRange", "custom");
                  onUpdateFilter("salaryMinMillion", event.target.value);
                }}
                inputMode="decimal"
                className="mt-1 h-10 w-full rounded-md border border-slate-200 bg-white px-3 text-sm font-semibold text-slate-800 outline-none transition focus:border-cyan-500 focus:ring-2 focus:ring-cyan-100"
                placeholder="0"
              />
            </label>
            <label className="block">
              <span className="text-xs font-bold text-slate-500">Max M/month</span>
              <input
                value={filters.salaryMaxMillion}
                onChange={(event) => {
                  onUpdateFilter("salaryRange", "custom");
                  onUpdateFilter("salaryMaxMillion", event.target.value);
                }}
                inputMode="decimal"
                className="mt-1 h-10 w-full rounded-md border border-slate-200 bg-white px-3 text-sm font-semibold text-slate-800 outline-none transition focus:border-cyan-500 focus:ring-2 focus:ring-cyan-100"
                placeholder="Any"
              />
            </label>
          </div>
        </div>
      </div>
    </section>
  );
}

function FilterSelect({ label, value, options, placeholder, onChange }) {
  return (
    <label className="block">
      <span className="text-xs font-bold uppercase text-slate-500">{label}</span>
      <select
        value={value}
        onChange={(event) => onChange(event.target.value)}
        className="mt-1 h-10 w-full rounded-md border border-slate-200 bg-slate-50 px-3 text-sm font-semibold text-slate-800 outline-none transition focus:border-cyan-500 focus:bg-white focus:ring-2 focus:ring-cyan-100"
      >
        <option value="">{placeholder}</option>
        {options.map((option) => (
          <option key={option} value={option}>
            {option}
          </option>
        ))}
      </select>
    </label>
  );
}

function FilterChips({ chips }) {
  if (chips.length === 0) {
    return null;
  }

  return (
    <div className="mt-3 flex flex-wrap gap-2">
      {chips.map((chip) => (
        <span
          key={`${chip.label}-${chip.value}`}
          className="inline-flex items-center gap-2 rounded-md border border-cyan-200 bg-cyan-50 px-2.5 py-1 text-xs font-bold text-cyan-800"
        >
          <Filter size={13} />
          {chip.label}: {chip.value}
        </span>
      ))}
    </div>
  );
}

function MarketStateNotice({ error, isLoading, hasOverview, hasOverviewData, quality, lastUpdatedAt }) {
  if (isLoading) return null;

  if (error) {
    return (
      <section className="mt-6 rounded-lg border border-red-200 bg-red-50 px-4 py-4 text-sm text-red-800">
        <div className="flex items-start gap-3">
          <AlertTriangle className="mt-0.5 shrink-0" size={18} />
          <div>
            <h2 className="font-bold">Backend unavailable</h2>
            <p className="mt-1 font-semibold leading-6">
              Market Pulse could not reach the roadmap backend or the current user is not allowed to read the overview.
            </p>
          </div>
        </div>
      </section>
    );
  }

  if (!hasOverview || hasOverviewData) {
    return null;
  }

  const hasSnapshot = Boolean(lastUpdatedAt);
  const title = hasSnapshot ? "No matching market signal" : "No snapshot yet";
  const detail = hasSnapshot
    ? "The backend is healthy, but the current filters do not match any active job posting."
    : "Run the Jobs API crawler and Market Pulse refresh job before opening the analytics view.";

  return (
    <section className="mt-6 rounded-lg border border-amber-200 bg-amber-50 px-4 py-4 text-sm text-amber-900">
      <div className="flex items-start gap-3">
        <Info className="mt-0.5 shrink-0" size={18} />
        <div>
          <h2 className="font-bold">{title}</h2>
          <p className="mt-1 font-semibold leading-6">{detail}</p>
          {(quality?.warnings ?? []).length > 0 && (
            <p className="mt-2 text-xs font-bold text-amber-800">
              Latest warning: {quality.warnings[0]}
            </p>
          )}
        </div>
      </div>
    </section>
  );
}

function NarrativeInsightGrid({ insights }) {
  if (insights.length === 0) {
    return null;
  }

  return (
    <section className="mt-6 grid gap-4 md:grid-cols-2 xl:grid-cols-5">
      {insights.map((insight) => (
        <NarrativeInsightCard key={insight.title} insight={insight} />
      ))}
    </section>
  );
}

function NarrativeInsightCard({ insight }) {
  const Icon = insight.icon;

  const content = (
    <>
      <div className="flex items-start justify-between gap-3">
        <span className={`inline-flex h-9 w-9 items-center justify-center rounded-md ${insight.iconClass}`}>
          <Icon size={17} />
        </span>
        <span className="rounded-md bg-slate-100 px-2 py-1 text-[11px] font-bold uppercase text-slate-500">
          {insight.confidence}
        </span>
      </div>
      <h2 className="mt-4 text-xs font-bold uppercase text-slate-500">{insight.title}</h2>
      <div className="mt-2 break-words text-xl font-bold text-slate-950">{insight.value}</div>
      <p className="mt-2 text-sm leading-6 text-slate-600">{insight.detail}</p>
      <div className="mt-4 inline-flex items-center gap-1 text-xs font-bold text-blue-700">
        {insight.actionLabel}
        <ChevronRight size={14} />
      </div>
    </>
  );

  if (insight.href) {
    return (
      <Link
        to={insight.href}
        className="tm-animate-item rounded-lg border border-slate-200 bg-white p-4 shadow-sm transition hover:-translate-y-0.5 hover:border-cyan-300 hover:shadow-md"
      >
        {content}
      </Link>
    );
  }

  return (
    <article className="tm-animate-item rounded-lg border border-slate-200 bg-white p-4 shadow-sm">
      {content}
    </article>
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
        <Lightbulb className="text-slate-400" size={20} />
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
              <Link
                to={item.skillSlug ? buildLearningModulesHref({ skillSlug: item.skillSlug, skillName: item.title }) : "/roadmaps"}
                className="mt-2 inline-flex items-center gap-1 text-xs font-bold text-blue-700 hover:text-blue-900"
              >
                {item.actionLabel}
                <ArrowRight size={13} />
              </Link>
            </div>
          ))}
        </div>
      )}
    </section>
  );
}

function SkillTable({ skills, compact = false, onToggleSkill = null, selectedSkillSet = null }) {
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
              <td className="px-3 py-3">
                <div className="flex min-w-0 items-center gap-2">
                  {onToggleSkill ? (
                    <button
                      type="button"
                      onClick={() => onToggleSkill(skill.skillSlug)}
                      className={`truncate text-left font-semibold hover:text-blue-700 ${
                        selectedSkillSet?.has(skill.skillSlug) ? "text-blue-800" : "text-slate-800"
                      }`}
                    >
                      {skill.skillName}
                    </button>
                  ) : (
                    <span className="truncate font-semibold text-slate-800">{skill.skillName}</span>
                  )}
                  <Link
                    to={buildLearningModulesHref(skill)}
                    className="shrink-0 text-slate-400 transition hover:text-emerald-700"
                    title={`Find learning modules for ${skill.skillName}`}
                  >
                    <GraduationCap size={14} />
                  </Link>
                </div>
              </td>
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

function SegmentPanel({ title, segments, icon = null, hrefForSegment = null }) {
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
              {hrefForSegment ? (
                <Link
                  to={hrefForSegment(segment.name)}
                  className="inline-flex min-w-0 items-center gap-1 truncate font-semibold text-slate-700 hover:text-blue-700"
                >
                  <span className="truncate">{segment.name}</span>
                  <ArrowRight size={13} className="shrink-0" />
                </Link>
              ) : (
                <span className="truncate font-semibold text-slate-700">{segment.name}</span>
              )}
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

function JobList({ title, jobs, onInspectJob }) {
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
                  <button
                    type="button"
                    onClick={() => onInspectJob(job)}
                    className="block text-left text-sm font-bold leading-6 text-slate-950 hover:text-blue-700"
                  >
                    <span>{job.title}</span>
                  </button>
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

              <div className="mt-3 flex flex-wrap gap-2">
                <button
                  type="button"
                  onClick={() => onInspectJob(job)}
                  className="inline-flex items-center gap-1 rounded-md border border-blue-200 bg-blue-50 px-2.5 py-1.5 text-xs font-bold text-blue-700 transition hover:border-blue-400"
                >
                  Requirement breakdown
                  <ArrowRight size={13} />
                </button>
                <a
                  href={job.url}
                  target="_blank"
                  rel="noreferrer"
                  className="inline-flex items-center gap-1 rounded-md border border-slate-200 bg-white px-2.5 py-1.5 text-xs font-bold text-slate-600 transition hover:border-slate-400"
                >
                  Source
                  <ExternalLink size={13} />
                </a>
              </div>
            </article>
          ))}
        </div>
      )}
    </section>
  );
}

function JobBreakdownDrawer({ job, onClose }) {
  if (!job) {
    return null;
  }

  const requirements = job.requirements ?? [];
  const specialties = job.specialties ?? [];
  const firstSkill = specialties[0] ? { skillName: specialties[0], skillSlug: slugify(specialties[0]) } : null;

  return (
    <div className="fixed inset-0 z-50 flex justify-end bg-slate-950/35 p-3 sm:p-6" role="dialog" aria-modal="true">
      <button
        type="button"
        aria-label="Close job breakdown"
        onClick={onClose}
        className="absolute inset-0 cursor-default"
      />

      <aside className="relative flex h-full w-full max-w-2xl flex-col overflow-hidden rounded-lg bg-white shadow-2xl">
        <div className="border-b border-slate-200 px-5 py-4">
          <div className="flex items-start justify-between gap-4">
            <div className="min-w-0">
              <div className="mb-2 flex flex-wrap gap-2">
                <span className="rounded-md bg-slate-100 px-2 py-1 text-xs font-bold text-slate-600">
                  {job.category || "Other"}
                </span>
                {job.source && (
                  <span className="rounded-md bg-cyan-50 px-2 py-1 text-xs font-bold text-cyan-700">
                    {job.source}
                  </span>
                )}
              </div>
              <h2 className="text-xl font-bold leading-7 text-slate-950">{job.title}</h2>
              <p className="mt-1 text-sm font-semibold text-slate-600">
                {[job.company, job.location].filter(Boolean).join(" - ") || "Unknown company"}
              </p>
            </div>
            <button
              type="button"
              onClick={onClose}
              className="inline-flex h-9 w-9 shrink-0 items-center justify-center rounded-md border border-slate-200 text-slate-500 transition hover:border-slate-400 hover:text-slate-900"
            >
              <X size={17} />
            </button>
          </div>
        </div>

        <div className="min-h-0 flex-1 overflow-y-auto px-5 py-5">
          <div className="grid gap-3 sm:grid-cols-3">
            <DrawerStat label="Salary" value={job.salary || "n/a"} icon={<DollarSign size={16} />} />
            <DrawerStat label="Experience" value={job.experience || "n/a"} icon={<BadgeCheck size={16} />} />
            <DrawerStat label="Posted" value={job.postDate ? formatDate(job.postDate) : job.postDateText || "n/a"} icon={<CalendarDays size={16} />} />
          </div>

          <section className="mt-5">
            <h3 className="text-sm font-bold uppercase text-slate-500">Requirement breakdown</h3>
            {requirements.length === 0 ? (
              <div className="mt-3 rounded-md border border-dashed border-slate-300 bg-slate-50 px-4 py-6 text-sm font-semibold text-slate-500">
                No enriched requirements were captured for this posting yet.
              </div>
            ) : (
              <ul className="mt-3 space-y-2">
                {requirements.slice(0, 10).map((requirement, index) => (
                  <li key={`${requirement}-${index}`} className="flex gap-2 rounded-md bg-slate-50 px-3 py-2 text-sm leading-6 text-slate-700">
                    <CircleDot className="mt-1 shrink-0 text-cyan-600" size={14} />
                    <span>{requirement}</span>
                  </li>
                ))}
              </ul>
            )}
          </section>

          <section className="mt-5">
            <h3 className="text-sm font-bold uppercase text-slate-500">Matched skills</h3>
            {specialties.length === 0 ? (
              <div className="mt-3 rounded-md border border-dashed border-slate-300 bg-slate-50 px-4 py-6 text-sm font-semibold text-slate-500">
                No normalized skill list is available yet.
              </div>
            ) : (
              <div className="mt-3 flex flex-wrap gap-2">
                {specialties.map((skill) => (
                  <Link
                    key={skill}
                    to={buildLearningModulesHref({ skillName: skill, skillSlug: slugify(skill) })}
                    className="rounded-md border border-emerald-200 bg-emerald-50 px-2.5 py-1.5 text-xs font-bold text-emerald-800 transition hover:border-emerald-400"
                  >
                    {skill}
                  </Link>
                ))}
              </div>
            )}
          </section>
        </div>

        <div className="border-t border-slate-200 px-5 py-4">
          <div className="flex flex-wrap gap-2">
            {firstSkill && (
              <Link
                to={buildLearningModulesHref(firstSkill)}
                className="inline-flex items-center gap-2 rounded-md bg-emerald-600 px-3 py-2 text-sm font-bold text-white transition hover:bg-emerald-700"
              >
                <GraduationCap size={16} />
                Learn this skill
              </Link>
            )}
            <Link
              to={buildRoadmapHref(job.category || job.title)}
              className="inline-flex items-center gap-2 rounded-md border border-slate-200 bg-white px-3 py-2 text-sm font-bold text-slate-700 transition hover:border-slate-400"
            >
              <Target size={16} />
              Find roadmap
            </Link>
            <a
              href={job.url}
              target="_blank"
              rel="noreferrer"
              className="inline-flex items-center gap-2 rounded-md border border-slate-200 bg-white px-3 py-2 text-sm font-bold text-slate-700 transition hover:border-slate-400"
            >
              <ExternalLink size={16} />
              Open source
            </a>
          </div>
        </div>
      </aside>
    </div>
  );
}

function DrawerStat({ label, value, icon }) {
  return (
    <div className="rounded-md border border-slate-200 bg-slate-50 px-3 py-3">
      <div className="flex items-center gap-2 text-xs font-bold uppercase text-slate-500">
        {icon}
        {label}
      </div>
      <div className="mt-2 break-words text-sm font-bold text-slate-900">{value}</div>
    </div>
  );
}

function TrendChart({ points, selectedSkills, hoveredPoint, onHoverPoint, periodDays, sampleSize, sourceCount }) {
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
      dashPattern: chartDashPatterns[index % chartDashPatterns.length],
      marker: index + 1,
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
              <path
                d={path}
                fill="none"
                stroke={series.color}
                strokeWidth="3"
                strokeLinecap="round"
                strokeLinejoin="round"
                strokeDasharray={series.dashPattern}
              />
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
              {series.points.length > 0 && (
                <text
                  x={xForDate(series.points[series.points.length - 1].date.slice(0, 10)) + 8}
                  y={yForValue(series.points[series.points.length - 1].mentionCount) + 4}
                  className="fill-slate-500 text-[11px] font-bold"
                >
                  {series.marker}
                </text>
              )}
            </g>
          );
        })}
      </svg>

      <div className="mt-3 flex flex-wrap gap-3">
        {pointsBySkill.map((series) => (
          <span key={series.skillSlug} className="inline-flex items-center gap-2 text-xs font-bold text-slate-600">
            <svg width="30" height="10" aria-hidden>
              <line
                x1="1"
                x2="28"
                y1="5"
                y2="5"
                stroke={series.color}
                strokeWidth="3"
                strokeLinecap="round"
                strokeDasharray={series.dashPattern}
              />
            </svg>
            <span className="rounded-sm bg-slate-100 px-1 text-[10px] text-slate-500">{series.marker}</span>
            {series.label}
          </span>
        ))}
      </div>

      {hoveredPoint && (
        <div className="mt-3 rounded-md border border-slate-200 bg-slate-50 px-3 py-2 text-sm font-semibold text-slate-700">
          {hoveredPoint.skillName}: {formatNumber(hoveredPoint.mentionCount)} mentions in{" "}
          {formatNumber(hoveredPoint.postingCount)} jobs on {formatDate(hoveredPoint.date)}. Period{" "}
          {formatNumber(periodDays)}d, sample {formatNumber(sampleSize)}, sources {formatNumber(sourceCount)}.
        </div>
      )}
    </div>
  );
}

function toMonthlyVnd(value) {
  if (value === undefined || value === null || value === "") {
    return "";
  }

  const normalized = Number(String(value).replace(",", "."));
  if (!Number.isFinite(normalized) || normalized <= 0) {
    return "";
  }

  return Math.round(normalized * 1_000_000);
}

function normalizeOverviewError(error) {
  const status = error?.response?.status;

  if (status === 401 || status === 403) {
    return "Backend reached, but the current session cannot read Market Pulse. Sign in with a role that has market pulse permission.";
  }

  if (status >= 500) {
    return "Roadmap backend returned an error while building Market Pulse overview.";
  }

  if (error?.message === "Network Error" || !status) {
    return "Backend unavailable. Check VITE_BACKEND_BASE_URL, CORS, and whether the roadmap API is running.";
  }

  return error?.message || "Unable to load market pulse data.";
}

function buildSegmentOptions(segments, selectedValue) {
  const names = segments
    .map((segment) => segment.name)
    .filter((name) => name && name !== "Unspecified");

  if (selectedValue && !names.some((name) => name.toLowerCase() === selectedValue.toLowerCase())) {
    names.unshift(selectedValue);
  }

  return names;
}

function filterSkillOptions(skills, selectedSkills, query) {
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
      postingCount: 0,
      growthPercent: 0,
    }));

  return [...selectedFallbacks, ...options]
    .sort((a, b) => {
      const aSelected = selectedSet.has(a.skillSlug) ? 1 : 0;
      const bSelected = selectedSet.has(b.skillSlug) ? 1 : 0;
      return bSelected - aSelected || Number(b.mentionCount || 0) - Number(a.mentionCount || 0);
    })
    .slice(0, 24);
}

function buildActiveFilterChips({ days, filters, selectedSkills, skills }) {
  const skillNameBySlug = new Map(skills.map((skill) => [skill.skillSlug, skill.skillName]));
  const chips = [{ label: "Window", value: `${days}d` }];

  [
    ["Role", filters.category],
    ["Location", filters.location],
    ["Experience", filters.experience],
    ["Source", filters.source],
  ].forEach(([label, value]) => {
    if (value) {
      chips.push({ label, value });
    }
  });

  selectedSkills.forEach((slug) => {
    chips.push({
      label: "Skill",
      value: skillNameBySlug.get(slug) || skillLabelFromSlug(slug),
    });
  });

  if (filters.salaryMinMillion || filters.salaryMaxMillion) {
    chips.push({
      label: "Salary",
      value: `${filters.salaryMinMillion || "0"}M - ${filters.salaryMaxMillion || "any"}M`,
    });
  }

  return chips;
}

function buildNarrativeInsights({
  activePostings,
  todayPostings,
  periodDays,
  risingSkills,
  fallingSkills,
  categorySummaries,
  salaryInsight,
  dataQuality,
}) {
  const topRising = risingSkills[0];
  const topFalling = fallingSkills[0];
  const topRole = categorySummaries[0];
  const bestSalarySegment = [...(salaryInsight?.byCategory ?? [])]
    .sort((a, b) => Number(b.coveragePercent || 0) - Number(a.coveragePercent || 0) || Number(b.sampleSize || 0) - Number(a.sampleSize || 0))[0];
  const confidence = formatConfidence(dataQuality?.level);

  return [
    {
      title: periodDays <= 7 ? "Top rising skill this week" : "Top rising skill",
      value: topRising?.skillName || "No movement yet",
      detail: topRising
        ? `${formatDelta(topRising.delta)} mentions versus the previous ${formatNumber(periodDays)}d window.`
        : "Not enough dated postings to compare skill movement.",
      confidence: formatConfidence(topRising?.confidence || dataQuality?.level),
      href: topRising ? buildLearningModulesHref(topRising) : "/learning-modules/browse",
      actionLabel: "Open learning path",
      icon: Sparkles,
      iconClass: "bg-amber-50 text-amber-700",
    },
    {
      title: "Most demanded role",
      value: topRole?.name || "Unspecified",
      detail: topRole
        ? `${formatNumber(topRole.count)} postings, ${formatPercent(topRole.percent)} of the filtered sample.`
        : "No category distribution is available for this filter set.",
      confidence,
      href: topRole?.name ? buildRoadmapHref(topRole.name) : "/roadmaps",
      actionLabel: "Suggest roadmap",
      icon: BriefcaseBusiness,
      iconClass: "bg-blue-50 text-blue-700",
    },
    {
      title: "Salary-backed signal",
      value: bestSalarySegment?.name || `${formatPercent(salaryInsight?.coveragePercent)} covered`,
      detail: bestSalarySegment
        ? `${formatNumber(bestSalarySegment.sampleSize)} salary samples, median ${formatMoneyVnd(bestSalarySegment.medianMinMonthlyVnd)} - ${formatMoneyVnd(bestSalarySegment.medianMaxMonthlyVnd)}.`
        : `${formatPercent(salaryInsight?.coveragePercent)} of postings have parseable salary strings.`,
      confidence: formatConfidence(salaryInsight?.confidence || dataQuality?.level),
      href: bestSalarySegment?.name ? buildRoadmapHref(bestSalarySegment.name) : null,
      actionLabel: "Compare role",
      icon: DollarSign,
      iconClass: "bg-emerald-50 text-emerald-700",
    },
    {
      title: "Data confidence",
      value: confidence,
      detail: `${formatDecimal(dataQuality?.score)}/100 from freshness, coverage, source diversity, and detail enrichment.`,
      confidence,
      href: null,
      actionLabel: "Review quality",
      icon: ShieldCheck,
      iconClass: "bg-cyan-50 text-cyan-700",
    },
    {
      title: "What changed",
      value: `${formatNumber(todayPostings)} new today`,
      detail: topRising || topFalling
        ? `${formatNumber(risingSkills.length)} rising and ${formatNumber(fallingSkills.length)} cooling skills in ${formatNumber(activePostings)} active postings.`
        : "No movement signal yet; wait for another snapshot or widen the period.",
      confidence,
      href: topRising ? buildLearningModulesHref(topRising) : null,
      actionLabel: "Inspect signal",
      icon: Layers,
      iconClass: "bg-violet-50 text-violet-700",
    },
  ];
}

function buildLearningModulesHref(skill) {
  const query = skill?.skillName || skillLabelFromSlug(skill?.skillSlug);
  const params = new URLSearchParams();

  if (query) {
    params.set("q", query);
  }

  return `/learning-modules/browse${params.toString() ? `?${params}` : ""}`;
}

function buildRoadmapHref(value) {
  const params = new URLSearchParams();

  if (value) {
    params.set("q", value);
  }

  return `/roadmaps${params.toString() ? `?${params}` : ""}`;
}

function skillLabelFromSlug(slug) {
  if (!slug) return "Skill";

  return String(slug)
    .split("-")
    .filter(Boolean)
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1))
    .join(" ");
}

function slugify(value) {
  return String(value || "")
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/^-+|-+$/g, "");
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
