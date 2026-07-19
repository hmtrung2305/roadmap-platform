import { AlertTriangle, RefreshCw, SearchX } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import { marketPulseApi } from "../api/marketPulseApi";
import DataMethodologyDisclosure from "../features/marketPulse/components/DataMethodologyDisclosure";
import DemandBreakdownSection from "../features/marketPulse/components/DemandBreakdownSection";
import LearningSignalsSection from "../features/marketPulse/components/LearningSignalsSection";
import MarketPulseFilters from "../features/marketPulse/components/MarketPulseFilters";
import MarketPulseHeader from "../features/marketPulse/components/MarketPulseHeader";
import MarketSnapshotSection from "../features/marketPulse/components/MarketSnapshotSection";
import TrendAnalysisSection from "../features/marketPulse/components/TrendAnalysisSection";
import {
  mergeOptionCatalog,
  normalizePublicationAnalytics,
} from "../features/marketPulse/marketPulseViewModel";

const emptyFilters = Object.freeze({
  category: "",
  location: "",
  seniority: "",
});

const emptyOptions = Object.freeze({
  category: [],
  location: [],
  seniority: [],
});

export default function MarketPulsePage() {
  const [overview, setOverview] = useState(null);
  const [days, setDays] = useState(30);
  const [appliedFilters, setAppliedFilters] = useState(emptyFilters);
  const [comparisonSkills, setComparisonSkills] = useState([]);
  const [trendTab, setTrendTab] = useState("market");
  const [optionCatalog, setOptionCatalog] = useState(emptyOptions);
  const [isInitialLoading, setIsInitialLoading] = useState(true);
  const [isRefreshing, setIsRefreshing] = useState(false);
  const [error, setError] = useState("");
  const [reloadKey, setReloadKey] = useState(0);
  const [viewResetKey, setViewResetKey] = useState(0);

  const queryParams = useMemo(() => ({
    days,
    skills: comparisonSkills,
    category: appliedFilters.category,
    location: appliedFilters.location,
    experience: appliedFilters.seniority,
  }), [appliedFilters, comparisonSkills, days]);

  useEffect(() => {
    const controller = new AbortController();
    let active = true;

    async function loadOverview() {
      if (overview) setIsRefreshing(true);
      else setIsInitialLoading(true);
      setError("");

      try {
        const data = await marketPulseApi.getOverview({ ...queryParams, signal: controller.signal });
        if (!active) return;
        setOverview(data);
        setOptionCatalog((current) => ({
          category: mergeOptionCatalog(current.category, data?.categorySummaries),
          location: mergeOptionCatalog(current.location, data?.locationSummaries),
          seniority: mergeOptionCatalog(current.seniority, data?.experienceSummaries),
        }));
      } catch (requestError) {
        if (!active || isCanceledRequest(requestError)) return;
        setError(requestError?.message || "Unable to load Job Market Pulse.");
      } finally {
        if (active) {
          setIsInitialLoading(false);
          setIsRefreshing(false);
        }
      }
    }

    loadOverview();
    return () => {
      active = false;
      controller.abort();
    };
  // `overview` is intentionally excluded: loading a response must not trigger a second GET.
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [queryParams, reloadKey]);

  const analytics = useMemo(
    () => normalizePublicationAnalytics(overview, days),
    [overview, days],
  );
  const hasData = Number(overview?.activePostings || overview?.totalPostings || 0) > 0;

  const clearAll = () => {
    setDays(30);
    setAppliedFilters(emptyFilters);
    setComparisonSkills([]);
    setTrendTab("market");
    setViewResetKey((value) => value + 1);
  };

  const exploreSkill = (skillSlug) => {
    setComparisonSkills([skillSlug]);
    setTrendTab("skill");
    requestAnimationFrame(() => {
      const heading = document.getElementById("trend-analysis-title");
      heading?.scrollIntoView({ behavior: "smooth", block: "start" });
      heading?.focus({ preventScroll: true });
    });
  };

  return (
    <section className="tm-page mx-auto min-h-[calc(100vh-4rem)] max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
      <MarketPulseHeader
        overview={overview}
        analytics={analytics}
        isRefreshing={isRefreshing}
        onReload={() => setReloadKey((value) => value + 1)}
      />

      <MarketPulseFilters
        days={days}
        appliedFilters={appliedFilters}
        optionCatalog={optionCatalog}
        resetKey={viewResetKey}
        onDaysChange={setDays}
        onApply={(filters) => setAppliedFilters((current) => sameFilters(current, filters) ? current : filters)}
        onClear={clearAll}
      />

      <div aria-live="polite" aria-atomic="true">
        {isRefreshing && overview && (
          <div role="status" className="mt-4 flex items-center gap-2 rounded-xl border border-[#B9D8CC] bg-[#EAF8F1] px-4 py-3 text-sm font-bold text-[#1F6F5F]">
            <RefreshCw size={16} className="animate-spin" aria-hidden="true" />
            Updating insights...
          </div>
        )}
        {error && overview && (
          <StatusBanner
            icon={AlertTriangle}
            tone="warning"
            message="Couldn't update insights. Showing the last loaded view."
            action="Try again"
            onAction={() => setReloadKey((value) => value + 1)}
          />
        )}
      </div>

      {isInitialLoading && !overview ? (
        <MarketPulseSkeleton />
      ) : error && !overview ? (
        <StatusBanner
          icon={AlertTriangle}
          tone="error"
          message={error}
          action="Try again"
          onAction={() => setReloadKey((value) => value + 1)}
          spacious
        />
      ) : overview && !hasData ? (
        <StatusBanner
          icon={SearchX}
          tone="neutral"
          message="No market data matches these filters."
          action="Clear filters"
          onAction={clearAll}
          spacious
        />
      ) : overview ? (
        <>
          <MarketSnapshotSection overview={overview} analytics={analytics} />
          <TrendAnalysisSection
            key={`trend-${viewResetKey}`}
            overview={overview}
            analytics={analytics}
            tab={trendTab}
            onTabChange={setTrendTab}
            comparisonSkills={comparisonSkills}
            onComparisonSkillsChange={setComparisonSkills}
          />
          <DemandBreakdownSection key={`breakdown-${viewResetKey}`} overview={overview} />
          <LearningSignalsSection
            recommendations={overview.learningRecommendations ?? []}
            pairs={overview.skillCoOccurrences ?? []}
            onExploreSkill={exploreSkill}
          />
          <DataMethodologyDisclosure overview={overview} analytics={analytics} />
        </>
      ) : null}
    </section>
  );
}

function StatusBanner({ icon: Icon, tone, message, action, onAction, spacious = false }) {
  const toneClass = tone === "error"
    ? "border-red-200 bg-red-50 text-red-800"
    : tone === "warning"
      ? "border-amber-200 bg-amber-50 text-amber-800"
      : "border-[#B9D8CC] bg-white text-slate-600";
  return (
    <div role={tone === "error" ? "alert" : "status"} className={`mt-6 flex ${spacious ? "min-h-52" : ""} flex-col items-center justify-center gap-3 rounded-2xl border px-5 py-6 text-center text-sm font-bold ${toneClass}`}>
      <Icon size={22} aria-hidden="true" />
      <span>{message}</span>
      <button
        type="button"
        onClick={onAction}
        className="min-h-11 rounded-xl border border-current px-4 text-sm font-extrabold focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-[#2FA084]"
      >
        {action}
      </button>
    </div>
  );
}

function MarketPulseSkeleton() {
  return (
    <div role="status" aria-label="Loading market insights" className="mt-8 animate-pulse space-y-5">
      <span className="sr-only">Loading market insights...</span>
      <div className="grid grid-cols-2 gap-3 lg:grid-cols-4">
        {[0, 1, 2, 3].map((item) => <div key={item} className="h-28 rounded-2xl border border-[#DCEBE5] bg-white" />)}
      </div>
      <div className="h-80 rounded-2xl border border-[#DCEBE5] bg-white" />
      <div className="grid gap-4 lg:grid-cols-2">
        <div className="h-52 rounded-2xl border border-[#DCEBE5] bg-white" />
        <div className="h-52 rounded-2xl border border-[#DCEBE5] bg-white" />
      </div>
    </div>
  );
}

function isCanceledRequest(error) {
  return error?.name === "CanceledError" || error?.name === "AbortError" || error?.code === "ERR_CANCELED";
}

function sameFilters(left, right) {
  return Object.keys(emptyFilters).every((key) => left[key] === right[key]);
}
