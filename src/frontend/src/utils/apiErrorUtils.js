const DEFAULT_ERROR_MESSAGE = "Something went wrong. Please try again.";
const SERVER_ERROR_FALLBACK = "The server ran into a problem. Please try again shortly.";

function isPlainObject(value) {
  return value !== null && typeof value === "object" && !Array.isArray(value);
}

export function getApiErrorData(errorOrData) {
  if (!errorOrData) return null;

  if (errorOrData.raw !== undefined) return errorOrData.raw;
  if (errorOrData.response?.data !== undefined) return errorOrData.response.data;

  return errorOrData;
}

export function getApiErrorCode(errorOrData) {
  const data = getApiErrorData(errorOrData);

  return (
    errorOrData?.code ||
    data?.code ||
    data?.Code ||
    data?.error?.code ||
    data?.Error?.Code ||
    null
  );
}

export function getApiErrorStatus(errorOrData) {
  const data = getApiErrorData(errorOrData);

  const candidate =
    errorOrData?.status ??
    errorOrData?.response?.status ??
    (typeof data?.status === "number" ? data.status : null) ??
    (typeof data?.Status === "number" ? data.Status : null);

  const status = Number(candidate);
  return Number.isFinite(status) ? status : null;
}

export function getApiErrorTraceId(errorOrData) {
  const data = getApiErrorData(errorOrData);

  return (
    errorOrData?.traceId ||
    data?.traceId ||
    data?.TraceId ||
    data?.error?.traceId ||
    data?.Error?.TraceId ||
    null
  );
}

export function getApiErrorDetails(errorOrData) {
  const data = getApiErrorData(errorOrData);

  return (
    errorOrData?.details ||
    data?.details ||
    data?.Details ||
    data?.error?.details ||
    data?.Error?.Details ||
    null
  );
}

export function getApiValidationErrors(errorOrData) {
  const data = getApiErrorData(errorOrData);

  return (
    errorOrData?.errors ||
    data?.errors ||
    data?.Errors ||
    data?.error?.errors ||
    data?.Error?.Errors ||
    null
  );
}

export function getCreditStatus(errorOrData) {
  const data = getApiErrorData(errorOrData);

  return (
    errorOrData?.creditStatus ||
    data?.creditStatus ||
    data?.CreditStatus ||
    data?.credits ||
    data?.Credits ||
    null
  );
}

function getHeaderValue(headers, name) {
  if (!headers) return null;

  if (typeof headers.get === "function") {
    return headers.get(name) || headers.get(name.toLowerCase()) || null;
  }

  return headers[name] || headers[name.toLowerCase()] || null;
}

export function getRetryAfterSeconds(errorOrData) {
  const data = getApiErrorData(errorOrData);
  const headers = errorOrData?.headers || errorOrData?.response?.headers;
  const retryAfterHeader = getHeaderValue(headers, "Retry-After");

  const candidate =
    errorOrData?.retryAfterSeconds ??
    data?.retryAfterSeconds ??
    data?.RetryAfterSeconds ??
    retryAfterHeader;

  const seconds = Number(candidate);
  if (Number.isFinite(seconds) && seconds > 0) {
    return Math.ceil(seconds);
  }

  if (typeof candidate === "string") {
    const retryAt = Date.parse(candidate);

    if (Number.isFinite(retryAt)) {
      const secondsUntilRetry = Math.ceil((retryAt - Date.now()) / 1000);
      return secondsUntilRetry > 0 ? secondsUntilRetry : null;
    }
  }

  const messageRetryMatch = getErrorText(errorOrData).match(
    /(?:retry|try again)\s*(?:after|in)?\s*(\d+)\s*(second|seconds|sec|secs|minute|minutes|min|mins)?/i,
  );

  if (messageRetryMatch) {
    const value = Number(messageRetryMatch[1]);
    const unit = messageRetryMatch[2] || "seconds";

    if (Number.isFinite(value) && value > 0) {
      return /min/i.test(unit) ? Math.ceil(value * 60) : Math.ceil(value);
    }
  }

  return null;
}

export function isRateLimitError(errorOrData) {
  const code = getApiErrorCode(errorOrData);
  const status = getApiErrorStatus(errorOrData);
  const text = getErrorText(errorOrData);

  return (
    code === "RATE_LIMIT_EXCEEDED" ||
    code === "GITHUB_RATE_LIMITED" ||
    status === 429 ||
    text.includes("too many requests") ||
    text.includes("rate limit") ||
    text.includes("rate-limit") ||
    text.includes("status code 429")
  );
}

export function isAiCreditLimitError(errorOrData) {
  return getApiErrorCode(errorOrData) === "AI_CREDIT_LIMIT_EXCEEDED";
}

export function formatRetryAfter(seconds) {
  if (!seconds) return "shortly";
  if (seconds < 60) return `${seconds} second${seconds === 1 ? "" : "s"}`;

  const minutes = Math.ceil(seconds / 60);
  return `${minutes} minute${minutes === 1 ? "" : "s"}`;
}

function getFirstValidationMessage(errors) {
  if (!isPlainObject(errors)) return "";

  const messages = Object.values(errors).flat().filter(Boolean);
  return messages.length > 0 ? String(messages[0]) : "";
}

function getBackendMessage(errorOrData) {
  const data = getApiErrorData(errorOrData);

  if (typeof data === "string") return data;
  if (isPlainObject(data)) {
    const nestedError = data.error ?? data.Error;
    const nestedMessage = typeof nestedError === "string"
      ? nestedError
      : isPlainObject(nestedError)
        ? nestedError.message || nestedError.Message
        : "";
    const candidates = [
      data.message,
      data.Message,
      nestedMessage,
      data.title,
      data.Title,
    ];

    return candidates.find((value) => typeof value === "string" && value.trim()) || "";
  }

  return "";
}

function shouldIgnoreClientMessage(message) {
  if (!message) return true;

  return /^Request failed with status code \d+$/i.test(message);
}

function getErrorText(errorOrData) {
  const data = getApiErrorData(errorOrData);
  const nestedError = data?.error ?? data?.Error;
  const values = [
    errorOrData?.message,
    errorOrData?.statusText,
    typeof data === "string" ? data : "",
    data?.message,
    data?.Message,
    typeof nestedError === "string" ? nestedError : "",
    nestedError?.message,
    nestedError?.Message,
    data?.title,
    data?.Title,
  ];

  return values.filter((value) => typeof value === "string" && value).join(" ").toLowerCase();
}

export function getApiErrorMessage(errorOrData, fallback = DEFAULT_ERROR_MESSAGE) {
  const backendMessage = getBackendMessage(errorOrData);
  if (backendMessage) return backendMessage;

  const validationMessage = getFirstValidationMessage(getApiValidationErrors(errorOrData));
  if (validationMessage) return validationMessage;

  const clientMessage = errorOrData?.message;
  if (clientMessage && !shouldIgnoreClientMessage(clientMessage)) {
    return clientMessage;
  }

  const status = getApiErrorStatus(errorOrData);
  if (status === 429) return "Too many requests. Please slow down.";
  if (status >= 500) return SERVER_ERROR_FALLBACK;

  return fallback;
}

export function getFriendlyApiErrorMessage(errorOrData, fallback = DEFAULT_ERROR_MESSAGE) {
  const message = getApiErrorMessage(errorOrData, fallback);
  const retryAfterSeconds = getRetryAfterSeconds(errorOrData);

  if (!retryAfterSeconds || !isRateLimitError(errorOrData)) {
    return message;
  }

  const retryText = `Try again in ${formatRetryAfter(retryAfterSeconds)}.`;

  if (message.toLowerCase().includes("try again in")) {
    return message;
  }

  return `${message} ${retryText}`;
}

export function normalizeApiError({ status, data, headers, url, message }) {
  const raw = data ?? null;
  const baseError = { status, raw, headers, message };

  const error = {
    isApiError: true,
    status: getApiErrorStatus(baseError) || status || null,
    code: getApiErrorCode(baseError) || null,
    message: getFriendlyApiErrorMessage(baseError, message || DEFAULT_ERROR_MESSAGE),
    raw,
    url: url || null,
    headers: headers || null,
    details: getApiErrorDetails(baseError) || null,
    errors: getApiValidationErrors(baseError) || null,
    traceId: getApiErrorTraceId(baseError) || null,
    retryAfterSeconds: getRetryAfterSeconds(baseError),
    creditStatus: getCreditStatus(baseError),
  };

  error.isRateLimited = isRateLimitError(error);
  error.isAiCreditLimitExceeded = isAiCreditLimitError(error);

  return error;
}

export async function normalizeFetchErrorResponse(response, url = response?.url) {
  const contentType = response.headers?.get?.("content-type") || "";
  const text = await response.text();
  let data = text;

  if (contentType.includes("application/json") || text.trim().startsWith("{")) {
    try {
      data = text ? JSON.parse(text) : null;
    } catch {
      data = text;
    }
  }

  return normalizeApiError({
    status: response.status,
    data,
    headers: response.headers,
    url,
    message: response.statusText,
  });
}
