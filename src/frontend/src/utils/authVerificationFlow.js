import { getApiErrorData, getFriendlyApiErrorMessage } from "./apiErrorUtils";

export const VERIFICATION_PURPOSES = {
  REGISTER: "register",
  LINK_LOCAL: "link_local",
  CHANGE_EMAIL: "change_email",
};

export function normalizeVerificationPurpose(value) {
  if (!value) return VERIFICATION_PURPOSES.REGISTER;

  const normalized = String(value).trim().toLowerCase();

  if (normalized === "link-local") return VERIFICATION_PURPOSES.LINK_LOCAL;
  if (normalized === "linklocal") return VERIFICATION_PURPOSES.LINK_LOCAL;
  if (normalized === "change-local-email") return VERIFICATION_PURPOSES.CHANGE_EMAIL;
  if (normalized === "changeemail") return VERIFICATION_PURPOSES.CHANGE_EMAIL;

  return normalized;
}

export function getErrorData(error) {
  return getApiErrorData(error);
}

export function getErrorMessage(errorOrData, fallback = "Something went wrong. Please try again.") {
  return getFriendlyApiErrorMessage(errorOrData, fallback);
}

export function isEmailVerificationRequired(errorOrData) {
  const data = getErrorData(errorOrData);

  return Boolean(
    data &&
      (data.requiresEmailVerification === true ||
        data.RequiresEmailVerification === true ||
        data.code === "EMAIL_NOT_VERIFIED" ||
        data.Code === "EMAIL_NOT_VERIFIED")
  );
}

export function getVerificationEmail(data, fallbackEmail = "") {
  const raw = getErrorData(data);

  return raw?.email || raw?.Email || fallbackEmail || "";
}

export function getVerificationPurpose(data, fallbackPurpose = VERIFICATION_PURPOSES.REGISTER) {
  const raw = getErrorData(data);

  return normalizeVerificationPurpose(
    raw?.verificationPurpose || raw?.VerificationPurpose || fallbackPurpose
  );
}

export function goToVerificationPage(
  navigate,
  data,
  fallbackEmail = "",
  fallbackPurpose = VERIFICATION_PURPOSES.REGISTER,
) {
  const email = getVerificationEmail(data, fallbackEmail);
  const verificationPurpose = getVerificationPurpose(data, fallbackPurpose);
  const message = getErrorMessage(data, "Please verify your email to continue.");
  const query = new URLSearchParams();

  if (email) query.set("email", email);
  if (verificationPurpose) query.set("purpose", verificationPurpose);

  navigate(`/verify-email?${query.toString()}`, {
    state: {
      email,
      verificationPurpose,
      message,
      canResendVerification:
        data?.canResendVerification ??
        data?.CanResendVerification ??
        data?.raw?.canResendVerification ??
        data?.raw?.CanResendVerification ??
        true,
    },
  });
}

export async function readResponseBody(response) {
  const text = await response.text();

  if (!text) {
    return null;
  }

  try {
    return JSON.parse(text);
  } catch {
    return text;
  }
}

export function isValidEmailFormat(value) {
  const email = String(value || "").trim();

  if (!email) return false;

  return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
}
