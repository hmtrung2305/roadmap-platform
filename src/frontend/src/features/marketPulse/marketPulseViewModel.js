const numberFormatter = new Intl.NumberFormat("en-US");
const decimalFormatter = new Intl.NumberFormat("en-US", { maximumFractionDigits: 1 });
const dateFormatter = new Intl.DateTimeFormat("en-GB", { day: "numeric", month: "short", year: "numeric" });
const dateTimeFormatter = new Intl.DateTimeFormat("en-GB", {
  day: "numeric",
  month: "short",
  year: "numeric",
  hour: "2-digit",
  minute: "2-digit",
});

export const EMPTY_ARRAY = Object.freeze([]);
export const EMPTY_OBJECT = Object.freeze({});
export const PERIOD_OPTIONS = [7, 14, 30, 90];
export const CHART_COLORS = ["#1F6F5F", "#C47B35", "#6D5DA8"];

export function formatNumber(value) {
  return numberFormatter.format(Number(value || 0));
}

export function formatDecimal(value) {
  return decimalFormatter.format(Number(value || 0));
}

export function formatEstimate(value, approximate = false) {
  if (!Number.isFinite(Number(value))) return "Not available";
  return `${approximate ? "~" : ""}${formatDecimal(value)}`;
}

export function formatDate(value) {
  const date = toDate(value);
  return date ? dateFormatter.format(date) : "Not available";
}

export function formatDateTime(value) {
  const date = toDate(value);
  return date ? dateTimeFormatter.format(date) : "No successful TopCV crawl yet";
}

export function formatRange(start, end) {
  if (!start || !end) return "Date range unavailable";
  return `${formatDate(start)} - ${formatDate(end)}`;
}

export function formatMoneyVnd(value) {
  const normalized = Number(value || 0);
  if (!normalized) return "Not available";
  if (normalized >= 1_000_000) return `${formatDecimal(normalized / 1_000_000)}M VND`;
  return `${formatNumber(normalized)} VND`;
}

export function capitalize(value) {
  const normalized = String(value || "").trim();
  return normalized ? normalized.charAt(0).toUpperCase() + normalized.slice(1) : "";
}

export function signedDecimal(value) {
  const normalized = Number(value || 0);
  return `${normalized > 0 ? "+" : ""}${formatDecimal(normalized)}`;
}

export const signedNumber = signedDecimal;

export function comparisonCopy(comparison) {
  if (!comparison || comparison.direction === "insufficient") {
    return "The previous period is not fully covered yet, so a reliable comparison cannot be made.";
  }

  if (comparison.direction === "new") {
    return "New demand; no postings were found in the fully covered previous period.";
  }
  if (comparison.direction === "flat" || Number(comparison.delta || 0) === 0) {
    return "Demand is unchanged from the previous period.";
  }

  const growth = nullableNumber(comparison.growthPercent);
  const direction = Number(comparison.delta || 0) > 0 ? "Up" : "Down";
  const growthCopy = growth === null ? "" : ` ${formatDecimal(Math.abs(growth))}%`;
  return `${direction}${growthCopy} (${signedDecimal(comparison.delta)}) versus the previous period.`;
}

export function normalizePublicationAnalytics(overview, days = 30) {
  const raw = overview?.publicationAnalytics ?? EMPTY_OBJECT;
  const currentRaw = raw.currentPeriod ?? raw.current ?? EMPTY_OBJECT;
  const previousRaw = raw.previousPeriod ?? raw.previous ?? EMPTY_OBJECT;
  const comparisonRaw = raw.marketComparison ?? EMPTY_OBJECT;
  const qualityRaw = raw.postDateQuality ?? EMPTY_OBJECT;
  const marketPointsRaw = raw.marketTrendPoints ?? EMPTY_ARRAY;
  const skillPointsRaw = raw.skillTrendPoints ?? EMPTY_ARRAY;
  const fallbackEnd = datePart(raw.anchorDate ?? raw.sourceDataAt ?? overview?.lastUpdatedAt);
  const currentEnd = datePart(firstValue(currentRaw.endDate, fallbackEnd));
  const currentStart = datePart(firstValue(currentRaw.startDate, currentEnd ? addDays(currentEnd, -(days - 1)) : null));
  const previousEnd = datePart(firstValue(previousRaw.endDate, currentStart ? addDays(currentStart, -1) : null));
  const previousStart = datePart(firstValue(previousRaw.startDate, previousEnd ? addDays(previousEnd, -(days - 1)) : null));
  const availability = String(raw.availability ?? (marketPointsRaw.length ? "available" : "no_history")).toLowerCase();
  const currentPeriod = normalizePublicationPeriod(currentRaw, currentStart, currentEnd, days);
  const previousPeriod = normalizePublicationPeriod(previousRaw, previousStart, previousEnd, days);
  const marketTrendPoints = marketPointsRaw
    .map((point) => {
      const exact = nullableNumber(firstValue(point.exactPostings, point.exactCount));
      const relative = nullableNumber(firstValue(point.relativeEstimate, point.estimatedPostings));
      const total = nullableNumber(firstValue(point.totalEstimate, point.estimatedTotal, sumNullable(exact, relative)));
      const available = point.isAvailable ?? point.available ?? point.hasCoverage ?? total !== null;
      return {
        date: datePart(point.date),
        available: Boolean(available),
        exactPostings: available ? exact ?? 0 : null,
        relativeEstimate: available ? relative ?? 0 : null,
        totalEstimate: available ? total ?? 0 : null,
      };
    })
    .filter((point) => point.date);
  const quality = normalizePostDateQuality(qualityRaw);
  const comparison = normalizePublicationComparison(comparisonRaw, currentPeriod, previousPeriod, raw.confidence);
  const hasTrend = marketTrendPoints.some((point) => point.available && Number.isFinite(point.totalEstimate));

  return {
    hasContract: Object.keys(raw).length > 0,
    hasHistory: hasTrend,
    basis: raw.basis ?? "published_date",
    dateModel: raw.dateModel ?? "interval_weighted",
    availability,
    historyMessage: publicationHistoryMessage(availability, quality),
    anchorDate: datePart(raw.anchorDate ?? fallbackEnd),
    sourceDataAt: raw.sourceDataAt ?? raw.latestSuccessfulCrawlAt ?? overview?.lastUpdatedAt ?? null,
    historyCoverageStart: datePart(raw.historyCoverageStart),
    historyCoverageEnd: datePart(raw.historyCoverageEnd),
    confidence: String(comparison.confidence ?? raw.confidence ?? quality.confidence ?? "low").toLowerCase(),
    currentStart,
    currentEnd,
    previousStart,
    previousEnd,
    currentPeriod,
    previousPeriod,
    marketTrendPoints,
    marketComparison: comparison,
    skillTrendPoints: skillPointsRaw.map((point) => {
      const exact = nullableNumber(firstValue(point.exactPostings, point.exactCount));
      const relative = nullableNumber(firstValue(point.relativeEstimate, point.estimatedPostings));
      const total = nullableNumber(firstValue(point.totalEstimate, point.estimatedTotal, point.postingCount, sumNullable(exact, relative)));
      const available = point.isAvailable ?? point.available ?? point.hasCoverage ?? total !== null;
      return {
        date: datePart(point.date),
        skillName: point.skillName,
        skillSlug: point.skillSlug,
        available: Boolean(available),
        exactPostings: available ? exact ?? 0 : null,
        relativeEstimate: available ? relative ?? 0 : null,
        totalEstimate: available ? total ?? 0 : null,
      };
    }).filter((point) => point.date && point.skillSlug),
    skillComparisons: (raw.skillComparisons ?? EMPTY_ARRAY).map((item) => ({
      skillName: item.skillName,
      skillSlug: item.skillSlug,
      ...normalizePublicationComparison(item, EMPTY_OBJECT, EMPTY_OBJECT, item.confidence ?? raw.confidence),
    })),
    postDateQuality: quality,
  };
}

// Kept as a temporary import alias for consumers compiled against the previous UI module.
export const normalizeObservationAnalytics = normalizePublicationAnalytics;

export function buildSkillSeries(trendPoints, skillSlugs, colors = CHART_COLORS) {
  return skillSlugs.map((slug, index) => {
    const points = (trendPoints ?? [])
      .filter((point) => point.skillSlug === slug)
      .map((point) => ({
        date: point.date,
        value: point.available === false ? null : nullableNumber(point.totalEstimate),
        exactValue: point.available === false ? null : nullableNumber(point.exactPostings),
        estimatedValue: point.available === false ? null : nullableNumber(point.relativeEstimate),
        approximate: Number(point.relativeEstimate || 0) > 0,
      }))
      .sort((a, b) => String(a.date).localeCompare(String(b.date)));
    const first = (trendPoints ?? []).find((point) => point.skillSlug === slug);
    return {
      key: slug,
      label: first?.skillName ?? skillLabelFromSlug(slug),
      color: colors[index % colors.length],
      points,
    };
  });
}

export function mergeOptionCatalog(current, segments) {
  const next = new Set(current ?? EMPTY_ARRAY);
  (segments ?? []).forEach((item) => {
    const name = String(item?.name || "").trim();
    if (name && name.toLowerCase() !== "unspecified") next.add(name);
  });
  return [...next].sort((a, b) => a.localeCompare(b));
}

export function toSelectOptions(values) {
  return [{ value: "", label: "All" }, ...(values ?? []).map((value) => ({ value, label: value }))];
}

export function skillLabelFromSlug(slug) {
  return String(slug || "Skill").split("-").filter(Boolean)
    .map((part) => part.charAt(0).toUpperCase() + part.slice(1)).join(" ");
}

export function segmentPercent(item, sampleSize, valueKey = "count") {
  if (item?.percent !== undefined && item?.percent !== null) return Number(item.percent || 0);
  const value = Number(item?.[valueKey] || 0);
  return sampleSize > 0 ? (value / sampleSize) * 100 : 0;
}

function normalizePublicationPeriod(period, startDate, endDate, days) {
  const exactCount = numberValue(period?.exactCount, period?.exactPostings);
  const relativeEstimate = numberValue(period?.relativeEstimate, period?.estimatedPostings);
  const estimatedTotal = numberValue(period?.estimatedTotal, period?.totalEstimate, period?.total, exactCount + relativeEstimate);
  return {
    startDate: datePart(period?.startDate ?? startDate),
    endDate: datePart(period?.endDate ?? endDate),
    expectedDays: numberValue(period?.expectedDays, days),
    coveredDays: numberValue(period?.coveredDays, period?.availableDays),
    coveragePercent: numberValue(period?.coveragePercent),
    estimatedTotal,
    exactCount,
    relativeEstimate,
    averagePerDay: numberValue(period?.averagePerDay, period?.dailyAverage, days > 0 ? estimatedTotal / days : 0),
    fullyCovered: Boolean(period?.fullyCovered ?? period?.isFullyCovered),
  };
}

function normalizePublicationComparison(comparison, currentPeriod, previousPeriod, fallbackConfidence) {
  const currentTotal = numberValue(comparison?.currentTotal, comparison?.currentEstimatedTotal, currentPeriod?.estimatedTotal);
  const previousTotal = numberValue(comparison?.previousTotal, comparison?.previousEstimatedTotal, previousPeriod?.estimatedTotal);
  const delta = numberValue(comparison?.delta, currentTotal - previousTotal);
  return {
    currentTotal,
    previousTotal,
    currentAverage: numberValue(comparison?.currentAverage, comparison?.currentAveragePerDay, currentPeriod?.averagePerDay),
    previousAverage: numberValue(comparison?.previousAverage, comparison?.previousAveragePerDay, previousPeriod?.averagePerDay),
    delta,
    growthPercent: nullableNumber(comparison?.growthPercent),
    direction: String(comparison?.direction ?? "insufficient").toLowerCase(),
    confidence: String(comparison?.confidence ?? fallbackConfidence ?? "low").toLowerCase(),
  };
}

function normalizePostDateQuality(quality) {
  const exactCount = numberValue(quality?.exactCount);
  const relativeCount = numberValue(quality?.relativeCount);
  const unknownCount = numberValue(quality?.unknownCount);
  const sampleSize = numberValue(quality?.sampleSize, exactCount + relativeCount + unknownCount);
  const reliablePercent = numberValue(
    quality?.reliableCoveragePercent,
    quality?.reliablePercent,
    sampleSize > 0 ? ((exactCount + relativeCount) / sampleSize) * 100 : 0,
  );
  return {
    sampleSize,
    exactCount,
    relativeCount,
    unknownCount,
    exactPercent: numberValue(quality?.exactPercent, sampleSize > 0 ? (exactCount / sampleSize) * 100 : 0),
    relativePercent: numberValue(quality?.relativePercent, sampleSize > 0 ? (relativeCount / sampleSize) * 100 : 0),
    unknownPercent: numberValue(quality?.unknownPercent, sampleSize > 0 ? (unknownCount / sampleSize) * 100 : 0),
    reliablePercent,
    averageIntervalWidthDays: numberValue(quality?.averageIntervalWidthDays, quality?.averageIntervalWidth),
    broadRangeSharePercent: numberValue(quality?.broadRangeSharePercent, quality?.broadRangePercent),
    confidence: String(quality?.confidence ?? "low").toLowerCase(),
  };
}

function publicationHistoryMessage(availability, quality) {
  if (availability === "history_sync_required") {
    return "Publication history has not been synchronized yet. Ask an administrator to run historical sync to unlock publication-date trends.";
  }
  if (availability === "insufficient_history") {
    return "Publication history does not yet cover both comparison periods. Run historical sync to extend the coverage watermark.";
  }
  if (quality.reliablePercent <= 0 && quality.sampleSize > 0) {
    return "Posting dates are not reliable yet. Run the TopCV date backfill and historical sync, then refresh this view.";
  }
  if (["insufficient", "partial", "history_incomplete"].includes(availability)) {
    return "Publication history does not yet cover both periods. Run historical sync to unlock a reliable comparison.";
  }
  if (["no_history", "unavailable", "no_reliable_dates"].includes(availability)) {
    return "No reliable publication-date history is available for these filters yet.";
  }
  return "No published postings match this period and filter set.";
}

function nullableNumber(value) {
  if (value === null || value === undefined || value === "") return null;
  const normalized = Number(value);
  return Number.isFinite(normalized) ? normalized : null;
}

function numberValue(...values) {
  for (const value of values) {
    const normalized = nullableNumber(value);
    if (normalized !== null) return normalized;
  }
  return 0;
}

function sumNullable(left, right) {
  if (left === null && right === null) return null;
  return Number(left || 0) + Number(right || 0);
}

function firstValue(...values) {
  return values.find((value) => value !== undefined && value !== null && value !== "") ?? null;
}

function toDate(value) {
  if (!value) return null;
  const normalized = typeof value === "string" && /^\d{4}-\d{2}-\d{2}$/.test(value) ? `${value}T12:00:00` : value;
  const date = normalized instanceof Date ? normalized : new Date(normalized);
  return Number.isNaN(date.getTime()) ? null : date;
}

function datePart(value) {
  if (!value) return null;
  if (typeof value === "string" && /^\d{4}-\d{2}-\d{2}/.test(value)) return value.slice(0, 10);
  const date = toDate(value);
  return date ? `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, "0")}-${String(date.getDate()).padStart(2, "0")}` : null;
}

function addDays(value, amount) {
  const normalized = datePart(value);
  const date = normalized ? toDate(`${normalized}T12:00:00`) : null;
  if (!date) return null;
  date.setDate(date.getDate() + amount);
  return datePart(date);
}
