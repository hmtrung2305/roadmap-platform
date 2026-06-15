import { useMemo, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { API_BASE_URL } from "../api/apiConfig";
import {
  VERIFICATION_PURPOSES,
  getErrorMessage,
  normalizeVerificationPurpose,
  readResponseBody,
} from "../utils/authVerificationFlow";

const verificationConfig = {
  [VERIFICATION_PURPOSES.REGISTER]: {
    label: "Account verification",
    title: "Verify your email",
    description:
      "Enter the 6-digit code sent to your registration email. It may take a moment to arrive.",
    endpoint: "/api/auth/registration/verify-email",
    resendEndpoint: "/api/auth/registration/resend-verification",
    verifyBody: ({ email, otp }) => ({ email, otp }),
    resendBody: ({ email }) => ({ email }),
    requiresEmail: true,
    resendRequiresEmail: true,
    successMessage: "Email verified successfully.",
    actionLabel: "Verify account",
    defaultReturnTo: "/roadmaps",
    backLabel: "Back to sign in",
    missingContextMessage:
      "Registration email was not provided. Please restart registration to request a new verification code.",
    restartPath: "/login",
    restartLabel: "Back to sign in",
  },
  [VERIFICATION_PURPOSES.LINK_LOCAL]: {
    label: "Security verification",
    title: "Confirm password login",
    description:
      "Enter the verification code sent to the email used for password login.",
    endpoint: "/api/me/auth-providers/local/verify",
    resendEndpoint: "/api/me/auth-providers/local/resend-verification",
    verifyBody: ({ otp }) => ({ otp }),
    resendBody: () => null,
    requiresEmail: false,
    resendRequiresEmail: false,
    successMessage: "Password login email verified.",
    actionLabel: "Confirm password login",
    defaultReturnTo: "/settings",
    backLabel: "Back to settings",
    missingContextMessage:
      "This verification must be started from Settings after requesting password login.",
    restartPath: "/settings",
    restartLabel: "Back to settings",
  },
  [VERIFICATION_PURPOSES.CHANGE_EMAIL]: {
    label: "Security verification",
    title: "Confirm new email",
    description: "Enter the verification code sent to your new login email.",
    endpoint: "/api/me/auth-providers/local/email/verify",
    resendEndpoint: null,
    verifyBody: ({ otp }) => ({ otp }),
    resendBody: () => null,
    requiresEmail: false,
    resendRequiresEmail: false,
    successMessage: "Login email updated successfully.",
    actionLabel: "Confirm new email",
    defaultReturnTo: "/settings",
    backLabel: "Back to settings",
    missingContextMessage:
      "This verification must be started from Settings after requesting an email change.",
    restartPath: "/settings",
    restartLabel: "Back to settings",
  },
};

function EmailVerificationPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const routeState = location.state || {};

  const queryParams = new URLSearchParams(location.search);
  const requestedPurpose = routeState.purpose || queryParams.get("purpose");
  const purpose = normalizeVerificationPurpose(requestedPurpose);
  const config = verificationConfig[purpose] || verificationConfig[VERIFICATION_PURPOSES.REGISTER];

  const returnTo =
    routeState.returnTo ||
    queryParams.get("returnTo") ||
    queryParams.get("next") ||
    config.defaultReturnTo;

  const [otp, setOtp] = useState("");
  const [message, setMessage] = useState(
    routeState.message ||
      (purpose === VERIFICATION_PURPOSES.REGISTER
        ? "Enter the code when it arrives. You can resend it if needed."
        : "")
  );
  const [messageType, setMessageType] = useState(routeState.messageType || "info");
  const [isSubmitting, setIsSubmitting] = useState(false);

  const email = useMemo(() => {
    return (routeState.email || queryParams.get("email") || "").trim().toLowerCase();
  }, [routeState.email, location.search]);

  const hasRequiredContext = !config.requiresEmail || Boolean(email);
  const canResend = Boolean(
    config.resendEndpoint &&
      routeState.canResendVerification !== false &&
      (!config.resendRequiresEmail || email)
  );

  function showError(value) {
    setMessage(value);
    setMessageType("error");
  }

  function showInfo(value) {
    setMessage(value);
    setMessageType("info");
  }

  async function handleVerify(e) {
    e.preventDefault();

    if (!hasRequiredContext) {
      showError(config.missingContextMessage);
      return;
    }

    const trimmedOtp = otp.trim();

    if (!trimmedOtp) {
      showError("Please enter your verification code.");
      return;
    }

    setIsSubmitting(true);
    showInfo("Verifying code...");

    const payload = config.verifyBody({ email, otp: trimmedOtp });

    try {
      const response = await fetch(`${API_BASE_URL}${config.endpoint}`, {
        method: "POST",
        credentials: "include",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(payload),
      });

      const data = await readResponseBody(response);

      if (!response.ok) {
        showError(getErrorMessage(data, "Verification failed."));
        return;
      }

      showInfo(data.message || config.successMessage);
      navigate(returnTo, { replace: true });
    } catch (error) {
      console.error(error);
      showError("Verification failed. Check that the API is running.");
    } finally {
      setIsSubmitting(false);
    }
  }

  async function handleResend() {
    if (!canResend) return;

    setIsSubmitting(true);
    showInfo("Sending a new code...");

    const payload = config.resendBody({ email });
    const requestOptions = {
      method: "POST",
      credentials: "include",
      headers: { "Content-Type": "application/json" },
    };

    if (payload) {
      requestOptions.body = JSON.stringify(payload);
    }

    try {
      const response = await fetch(`${API_BASE_URL}${config.resendEndpoint}`, requestOptions);
      const data = await readResponseBody(response);

      if (!response.ok) {
        showError(getErrorMessage(data, "Could not resend code."));
        return;
      }

      setOtp("");
      showInfo(data.message || "A new code was sent to your inbox.");
    } catch (error) {
      console.error(error);
      showError("Resend failed. Check that the API is running.");
    } finally {
      setIsSubmitting(false);
    }
  }

  return (
    <main className="min-h-screen bg-[#F7F1E8] text-black">
      <section className="flex min-h-screen items-center justify-center px-5 py-10">
        <div className="w-full max-w-[520px]">
          <header className="mb-5 flex items-center justify-between rounded-2xl border border-slate-200 bg-white px-4 py-3 shadow-sm">
            <button
              type="button"
              onClick={() => navigate("/", { replace: true })}
              className="flex items-center gap-3 text-left"
            >
              <span className="flex h-9 w-9 items-center justify-center rounded-xl bg-[#2FA084] font-mono text-xs font-black text-white">
                TM
              </span>
              <span className="text-sm font-black tracking-[-0.03em]">TechMap</span>
            </button>

            <span className="rounded-full bg-emerald-50 px-3 py-1 text-[10px] font-black uppercase tracking-[0.16em] text-[#1F6F5F]">
              Verify
            </span>
          </header>

          <div className="overflow-hidden rounded-3xl border border-slate-200 bg-white shadow-xl shadow-slate-200/60">
            <div className="border-b border-slate-100 bg-slate-950 px-6 py-5 text-white">
              <p className="text-xs font-black uppercase tracking-[0.18em] text-emerald-300">
                {config.label}
              </p>

              <h1 className="mt-3 text-3xl font-black leading-tight tracking-[-0.04em] sm:text-4xl">
                {config.title}
              </h1>

              <p className="mt-3 max-w-md text-sm font-medium leading-6 text-slate-300">
                {config.description}
              </p>
            </div>

            <div className="space-y-5 p-6">
              {email && (
                <div className="rounded-2xl border border-emerald-100 bg-emerald-50 p-4">
                  <p className="text-[10px] font-black uppercase tracking-[0.16em] text-[#1F6F5F]">
                    Code sent to
                  </p>
                  <p className="mt-1 break-all text-sm font-black text-slate-900">{email}</p>
                </div>
              )}

              {!hasRequiredContext ? (
                <div className="rounded-2xl border border-sky-100 bg-sky-50 p-4 text-sm font-semibold leading-6 text-slate-800">
                  {config.missingContextMessage}
                </div>
              ) : (
                <form onSubmit={handleVerify} className="space-y-4">
                  <label className="block">
                    <span className="mb-2 block text-xs font-black uppercase tracking-[0.16em] text-slate-700">
                      Verification code
                    </span>
                    <input
                      type="text"
                      value={otp}
                      inputMode="numeric"
                      placeholder="Enter 6-digit code"
                      autoFocus
                      onChange={(e) => setOtp(e.target.value)}
                      className="w-full rounded-xl border border-slate-300 bg-white px-4 py-3 text-sm font-semibold text-slate-900 outline-none placeholder:text-slate-400 focus:border-[#2FA084] focus:ring-4 focus:ring-[#6FCF97]/20"
                    />
                  </label>

                  <button
                    type="submit"
                    disabled={isSubmitting}
                    className="w-full rounded-xl bg-[#2FA084] px-5 py-3 text-sm font-black text-white shadow-lg shadow-emerald-900/10 transition hover:bg-[#1F6F5F] disabled:cursor-not-allowed disabled:opacity-60"
                  >
                    {isSubmitting ? "Verifying..." : config.actionLabel}
                  </button>
                </form>
              )}

              {message && (
                <div
                  className={
                    messageType === "error"
                      ? "rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm font-semibold text-red-700"
                      : "rounded-2xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm font-semibold text-emerald-800"
                  }
                >
                  {message}
                </div>
              )}

              <footer className="flex flex-col gap-3 border-t border-slate-100 pt-5 sm:flex-row sm:items-center sm:justify-between">
                {canResend ? (
                  <button
                    type="button"
                    disabled={isSubmitting}
                    onClick={handleResend}
                    className="rounded-xl border border-slate-200 bg-white px-4 py-2 text-xs font-black uppercase tracking-[0.12em] text-slate-700 transition hover:bg-emerald-50 disabled:cursor-not-allowed disabled:opacity-60"
                  >
                    Resend code
                  </button>
                ) : (
                  <p className="text-xs font-semibold leading-5 text-slate-500">
                    Need a new code? Restart the same request flow.
                  </p>
                )}

                <button
                  type="button"
                  disabled={isSubmitting}
                  onClick={() => navigate(config.restartPath, { replace: true })}
                  className="rounded-xl border border-slate-200 bg-[#F7F1E8] px-4 py-2 text-xs font-black uppercase tracking-[0.12em] text-slate-700 transition hover:bg-slate-100 disabled:cursor-not-allowed disabled:opacity-60"
                >
                  {hasRequiredContext ? config.backLabel : config.restartLabel}
                </button>
              </footer>
            </div>
          </div>
        </div>
      </section>
    </main>
  );
}

export default EmailVerificationPage;
