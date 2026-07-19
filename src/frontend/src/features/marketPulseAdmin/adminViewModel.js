const integerFormatter = new Intl.NumberFormat("en-US");
const decimalFormatter = new Intl.NumberFormat("en-US", { maximumFractionDigits: 1 });
const dateTimeFormatter = new Intl.DateTimeFormat("en-GB", {
  day: "2-digit",
  month: "short",
  year: "numeric",
  hour: "2-digit",
  minute: "2-digit",
});

export const ACTIVE_OPERATION_STATUSES = new Set(["queued", "crawling", "importing"]);

export function normalizeDashboard(raw = {}) {
  const healthItems = arrayValue(raw.pipelineHealth);
  const healthItem = (...keys) => healthItems.find((item) => keys.includes(String(item?.key ?? "").toLowerCase())) ?? {};
  const crawler = raw.crawler ?? raw.pipelineHealth?.crawler ?? healthItem("crawler");
  const importer = raw.importer ?? raw.import ?? raw.pipelineHealth?.import ?? healthItem("import", "importer");
  const history = raw.publicationHistory ?? raw.pipelineHealth?.publicationHistory ?? raw.pipelineHealth?.history ?? healthItem("history", "publication_history");
  const quality = raw.dataQuality ?? raw.pipelineHealth?.dataQuality ?? healthItem("quality", "data_quality");
  const postDateQualityRaw = raw.postDateQuality ?? raw.publicationDateQuality ?? {};
  const exactCount = numberValue(postDateQualityRaw.exactCount);
  const relativeCount = numberValue(postDateQualityRaw.relativeCount);
  const unknownCount = numberValue(postDateQualityRaw.unknownCount);
  const dateSampleSize = numberValue(postDateQualityRaw.sampleSize, exactCount + relativeCount + unknownCount);
  const reliableCoverage = numberValue(
    raw.reliablePostDateCoveragePercent,
    raw.reliablePostDateCoverage,
    postDateQualityRaw.reliablePercent,
    postDateQualityRaw.reliableCoveragePercent,
    quality.reliablePostDateCoveragePercent,
    quality.reliableCoveragePercent,
  );
  const failures = raw.failures ?? {};
  const recentOperations = arrayValue(raw.recentOperations ?? raw.refreshOperations).slice(0, 5).map(normalizeOperation);
  const currentOperation = raw.currentOperation
    ? normalizeOperation(raw.currentOperation)
    : recentOperations.find((item) => ACTIVE_OPERATION_STATUSES.has(item.status)) ?? null;
  return {
    overallStatus: normalizeStatus(raw.overallStatus ?? raw.status ?? "unknown"),
    latestSuccessfulRefreshAt: raw.latestSuccessfulRefreshAt ?? raw.latestSuccessfulEndToEndRefreshAt ?? null,
    activeJobs: numberValue(raw.activeJobs, raw.activePostings),
    estimatedPostings7Days: numberValue(raw.estimatedPostings7Days, raw.postingsLast7Days),
    crawlerFreshnessHours: nullableNumber(raw.crawlerFreshnessHours ?? crawler.freshnessHours),
    reliablePostDateCoveragePercent: reliableCoverage,
    analyticsConfidence: normalizeStatus(raw.analyticsConfidence ?? postDateQualityRaw.confidence ?? "low"),
    postDateQuality: {
      sampleSize: dateSampleSize,
      exactCount,
      relativeCount,
      unknownCount,
      exactPercent: percentageValue(postDateQualityRaw.exactPercent, exactCount, dateSampleSize),
      relativePercent: percentageValue(postDateQualityRaw.relativePercent, relativeCount, dateSampleSize),
      unknownPercent: percentageValue(postDateQualityRaw.unknownPercent, unknownCount, dateSampleSize),
      reliablePercent: reliableCoverage,
      averageIntervalWidthDays: numberValue(postDateQualityRaw.averageIntervalWidthDays),
      broadRangeSharePercent: numberValue(postDateQualityRaw.broadRangeSharePercent),
    },
    importLagMinutes: nullableNumber(raw.importLagMinutes ?? importer.lagMinutes),
    openCrawlerFailures: numberValue(raw.openCrawlerFailures, failures.openCrawler, failures.crawler),
    openImportFailures: numberValue(raw.openImportFailures, failures.openImport, failures.import),
    pipeline: {
      crawler: normalizeHealth(crawler, "Python TopCV crawler"),
      importer: normalizeHealth(importer, ".NET TopCV import"),
      history: normalizeHealth(history, "Publication history"),
      quality: normalizeHealth(quality, "Detail and field quality"),
    },
    alerts: arrayValue(raw.alerts).map(normalizeAlert),
    demandPoints: arrayValue(raw.publicationDemandPoints ?? raw.miniPublicationDemand ?? raw.marketTrendPoints ?? raw.demandTrend).map((point) => ({
      date: String(point.date ?? "").slice(0, 10),
      value: nullableNumber(point.totalEstimate ?? point.estimatedPostings ?? point.value),
      approximate: Number(point.relativeEstimate ?? 0) > 0,
    })).filter((point) => point.date),
    recentOperations,
    currentOperation,
  };
}

export function normalizeOperation(raw = {}) {
  return {
    id: raw.id ?? raw.operationId ?? null,
    status: normalizeStatus(raw.status ?? "queued"),
    currentStep: normalizeStatus(raw.currentStep ?? "crawler"),
    createdAt: raw.createdAt ?? raw.queuedAt ?? raw.requestedAt ?? null,
    startedAt: raw.startedAt ?? null,
    finishedAt: raw.finishedAt ?? raw.completedAt ?? null,
    crawlerRunId: raw.crawlerRunId ?? null,
    importRunId: raw.importRunId ?? null,
    baselineCrawlAt: raw.baselineCrawlAt ?? raw.baselineCrawlerSuccessAt ?? null,
    crawlerCompletedAt: raw.crawlerCompletedAt ?? raw.crawlerSuccessAt ?? null,
    error: errorMessage(raw.error ?? raw.errorSummary ?? raw.failure ?? raw.errorMessage, ""),
  };
}

export function operationStepState(operation, step) {
  if (!operation) return "pending";
  const status = normalizeStatus(operation.status);
  if (status === "success") return "complete";
  if (status === "failed") {
    if (operation.currentStep === step) return "failed";
    if (step === "crawler" && !operation.crawlerCompletedAt && !operation.importRunId) return "failed";
    if (step === "import" && operation.crawlerCompletedAt && !operation.importRunId) return "failed";
    if (step === "analytics" && operation.importRunId) return "failed";
    return "complete";
  }
  if (status === "queued") return step === "crawler" ? "active" : "pending";
  if (status === "crawling") return step === "crawler" ? "active" : "pending";
  if (status === "importing") return step === "crawler" ? "complete" : step === "import" ? "active" : "pending";
  return "pending";
}

export function errorMessage(error, fallback = "The operation could not be completed.") {
  if (!error) return fallback;
  if (typeof error === "string") return error;
  if (typeof error?.response?.data?.error?.message === "string") return error.response.data.error.message;
  if (typeof error?.response?.data?.message === "string") return error.response.data.message;
  if (typeof error?.message === "string") return error.message;
  if (typeof error?.error?.message === "string") return error.error.message;
  try {
    return JSON.stringify(error);
  } catch {
    return fallback;
  }
}

export function formatInteger(value) {
  return integerFormatter.format(Number(value || 0));
}

export function formatDecimal(value) {
  return decimalFormatter.format(Number(value || 0));
}

export function formatDateTime(value) {
  if (!value) return "Not available";
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? "Not available" : dateTimeFormatter.format(date);
}

export function formatDuration(minutes) {
  if (!Number.isFinite(Number(minutes))) return "Not available";
  const value = Number(minutes);
  if (value < 60) return `${formatDecimal(value)} min`;
  return `${formatDecimal(value / 60)} hr`;
}

export function statusTone(status) {
  const value = normalizeStatus(status);
  if (["healthy", "success", "complete", "ready"].includes(value)) return "success";
  if (["warning", "degraded", "partial", "stale", "queued", "crawling", "importing"].includes(value)) return "warning";
  if (["failed", "error", "critical", "blocked"].includes(value)) return "danger";
  return "neutral";
}

function normalizeHealth(raw, fallbackLabel) {
  return {
    label: raw.label ?? raw.name ?? fallbackLabel,
    status: normalizeStatus(raw.status ?? raw.health ?? "unknown"),
    summary: raw.summary ?? raw.message ?? raw.detail ?? "No status detail available.",
    updatedAt: raw.updatedAt ?? raw.lastSuccessAt ?? raw.latestSuccessfulAt ?? null,
    metricLabel: raw.metricLabel ?? null,
    metricValue: raw.metricValue ?? null,
  };
}

function normalizeAlert(raw) {
  if (typeof raw === "string") return { id: raw, severity: "warning", title: raw, message: "", action: null, actionType: null };
  const code = String(raw.code ?? "").toUpperCase();
  const knownActionTypes = {
    TOPCV_IMPORT_INCOMPLETE: "view_imports",
    POST_DATE_QUALITY_LOW: "post_date_backfill",
  };
  const inferredActionType = knownActionTypes[code] ?? (code.includes("HISTORY")
    ? "history_sync"
    : code.includes("FAILURE") || code.includes("CRAWLER")
      ? "view_failures"
      : null);
  return {
    id: raw.id ?? raw.code ?? `${raw.title}-${raw.message}`,
    severity: normalizeStatus(raw.severity ?? "warning"),
    title: raw.title ?? raw.code ?? "Pipeline attention required",
    message: raw.message ?? raw.reason ?? "",
    action: raw.actionLabel ?? raw.action ?? null,
    actionType: raw.actionType ?? raw.actionKey ?? inferredActionType,
  };
}

function normalizeStatus(value) {
  return String(value || "unknown").trim().toLowerCase();
}

function nullableNumber(value) {
  if (value === null || value === undefined || value === "") return null;
  const number = Number(value);
  return Number.isFinite(number) ? number : null;
}

function numberValue(...values) {
  for (const value of values) {
    const number = nullableNumber(value);
    if (number !== null) return number;
  }
  return 0;
}

function percentageValue(explicitValue, count, total) {
  const explicit = nullableNumber(explicitValue);
  if (explicit !== null) return explicit;
  return total > 0 ? Math.round((count / total) * 10000) / 100 : 0;
}

function arrayValue(value) {
  return Array.isArray(value) ? value : [];
}
