import { useCallback, useState } from "react";
import {
  Link,
  useLocation,
  useNavigate,
  useSearchParams,
} from "react-router-dom";
import {
  resendRegistrationVerificationApi,
  verifyRegistrationEmailApi,
} from "../api/authApi";
import {
  resendLinkedLocalVerificationApi,
  resendLocalEmailChangeVerificationApi,
  verifyLinkedLocalEmailApi,
  verifyLocalEmailChangeApi,
} from "../api/authProviderApi";
import {
  getErrorMessage,
  isValidEmailFormat,
  normalizeVerificationPurpose,
  VERIFICATION_PURPOSES,
} from "../utils/authVerificationFlow";
import { CAPTCHA_ENABLED } from "../api/apiConfig";
import TurnstileCaptcha from "../components/common/TurnstileCaptcha";
import { useAuthStore } from "../stores/useAuthStore";

export default function VerifyEmailPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const [searchParams] = useSearchParams();
  const setAuthenticatedUser = useAuthStore((state) => state.setAuthenticatedUser);

  const verificationPurpose = normalizeVerificationPurpose(
    location.state?.verificationPurpose || searchParams.get("purpose"),
  );

  const isRegisterVerification =
    verificationPurpose === VERIFICATION_PURPOSES.REGISTER;

  const isLinkLocalVerification =
    verificationPurpose === VERIFICATION_PURPOSES.LINK_LOCAL;

  const isChangeEmailVerification =
    verificationPurpose === VERIFICATION_PURPOSES.CHANGE_EMAIL;

  const emailFromQuery =
    location.state?.email || searchParams.get("email") || "";

  const [otp, setOtp] = useState("");
  const [email, setEmail] = useState(emailFromQuery);
  const [loading, setLoading] = useState(false);
  const [resending, setResending] = useState(false);
  const [error, setError] = useState("");
  const [message, setMessage] = useState(location.state?.message || "");
  const [resendCaptchaToken, setResendCaptchaToken] = useState("");
  const [resendCaptchaResetKey, setResendCaptchaResetKey] = useState(0);

  const handleResendCaptchaVerify = useCallback((token) => {
    setResendCaptchaToken(token);
    setError("");
  }, []);

  const handleOtpChange = (e) => {
    const value = e.target.value.replace(/\D/g, "").slice(0, 6);

    if (error) {
      setError("");
    }

    setOtp(value);
  };

  const handleEmailChange = (e) => {
    if (error) {
      setError("");
    }

    setEmail(e.target.value);
  };

  const handleVerifyEmail = async (e) => {
    e.preventDefault();

    if (isRegisterVerification && !email.trim()) {
      setError("Please enter your email address.");
      return;
    }

    if (isRegisterVerification && !isValidEmailFormat(email)) {
      setError("Please enter a valid email address.");
      return;
    }

    if (!otp.trim()) {
      setError("Please enter the verification code.");
      return;
    }

    try {
      setLoading(true);
      setError("");
      setMessage("");

      if (isLinkLocalVerification) {
        await verifyLinkedLocalEmailApi({
          Otp: otp,
        });

        navigate("/settings/account", {
          replace: true,
          state: {
            message: "Password login verified successfully.",
          },
        });

        return;
      }

      if (isChangeEmailVerification) {
        await verifyLocalEmailChangeApi({
          Otp: otp,
        });

        navigate("/settings/account", {
          replace: true,
          state: {
            message: "Email changed successfully.",
          },
        });

        return;
      }

      const data = await verifyRegistrationEmailApi({
        Email: email.trim(),
        Otp: otp,
      });

      if (data?.user) {
        setAuthenticatedUser(data.user);
      }

      navigate("/roadmaps", {
        replace: true,
        state: {
          message: data?.message || "Email verified successfully.",
        },
      });
    } catch (error) {
      console.error("Verify email failed:", error.response?.data || error);

      setError(getErrorMessage(error, "Invalid or expired verification code."));
    } finally {
      setLoading(false);
    }
  };

  const handleResendCode = async () => {
    if (isRegisterVerification && !email.trim()) {
      setError("Please enter your email before requesting a new code.");
      return;
    }

    if (isRegisterVerification && !isValidEmailFormat(email)) {
      setError("Please enter a valid email address before requesting a new code.");
      return;
    }

    if (CAPTCHA_ENABLED && isRegisterVerification && !resendCaptchaToken) {
      setError("Please complete the CAPTCHA challenge.");
      return;
    }

    try {
      setResending(true);
      setError("");
      setMessage("");

      if (isLinkLocalVerification) {
        await resendLinkedLocalVerificationApi();
      } else if (isChangeEmailVerification) {
        await resendLocalEmailChangeVerificationApi();
      } else {
        await resendRegistrationVerificationApi({
          Email: email.trim(),
          CaptchaToken: resendCaptchaToken,
        });
      }

      setMessage("A new verification code has been sent to your email.");
    } catch (error) {
      console.error("Resend code failed:", error.response?.data || error);

      setError(getErrorMessage(error, "Unable to resend verification code."));
    } finally {
      setResending(false);
      setResendCaptchaToken("");
      setResendCaptchaResetKey((prev) => prev + 1);
    }
  };

  const title = isLinkLocalVerification
    ? "Verify password login"
    : isChangeEmailVerification
      ? "Verify new email"
      : "Verify your email";

  const description = isLinkLocalVerification
    ? "Enter the verification code we sent to finish adding password login."
    : isChangeEmailVerification
      ? "Enter the verification code we sent to confirm your new email address."
      : "Enter the verification code we sent to your email address.";

  const submitLabel = isLinkLocalVerification
    ? "Verify password login"
    : isChangeEmailVerification
      ? "Verify new email"
      : "Verify email";

  return (
    <div className="min-h-dvh bg-[#F7F1E8] px-6 py-10 text-slate-900">
      <div className="mx-auto flex min-h-[calc(100dvh-5rem)] max-w-md items-center justify-center">
        <div
          className={`w-full rounded-lg border border-slate-200 bg-white px-6 py-7 shadow-xl shadow-slate-200/60 transition duration-200 ${
            loading ? "scale-[0.99] opacity-70" : ""
          }`}
        >
          <div className="text-center">
            <p className="text-xs font-bold uppercase tracking-[0.22em] text-[#1F6F5F]">
              Email verification
            </p>

            <h1 className="mt-3 text-2xl font-bold tracking-tight text-slate-900">
              {title}
            </h1>

            <p className="mt-2 text-sm leading-6 text-slate-500">
              {description}
            </p>
          </div>

          {error && (
            <div className="mt-5 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-600">
              {error}
            </div>
          )}

          {message && (
            <div className="mt-5 rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700">
              {message}
            </div>
          )}

          <form onSubmit={handleVerifyEmail} className="mt-6 space-y-4">
            {isRegisterVerification && (
              <div>
                <label className="mb-2 block text-sm font-semibold text-slate-700">
                  Email address
                </label>

                <input
                  name="email"
                  type="email"
                  value={email}
                  onChange={handleEmailChange}
                  placeholder="name@example.com"
                  className="h-11 w-full rounded-xl border border-slate-300 px-4 text-sm text-slate-900 outline-none transition placeholder:text-slate-300 focus:border-[#2FA084] focus:ring-4 focus:ring-[#6FCF97]/20"
                  required
                />
              </div>
            )}

            {!isRegisterVerification && email && (
              <div className="rounded-xl border border-slate-200 bg-[#F7F1E8] px-4 py-3 text-sm text-slate-600">
                Code sent to{" "}
                <span className="font-semibold text-slate-800">{email}</span>
              </div>
            )}

            <div>
              <label className="mb-2 block text-sm font-semibold text-slate-700">
                Verification code
              </label>

              <input
                name="otp"
                value={otp}
                onChange={handleOtpChange}
                placeholder="Enter 6-digit code"
                inputMode="numeric"
                className="h-12 w-full rounded-lg border border-slate-300 px-4 text-center text-lg font-semibold tracking-[0.4em] text-slate-900 outline-none transition placeholder:text-sm placeholder:font-normal placeholder:tracking-normal placeholder:text-slate-300 focus:border-[#2FA084] focus:ring-4 focus:ring-[#6FCF97]/20"
                required
              />
            </div>

            <button
              type="submit"
              disabled={loading}
              className="h-11 w-full rounded-lg bg-[#2FA084] text-sm font-semibold text-white shadow-lg shadow-emerald-900/10 transition hover:bg-[#1F6F5F] disabled:cursor-not-allowed disabled:opacity-60"
            >
              {loading ? "Verifying..." : submitLabel}
            </button>
          </form>

          <div className="mt-5 text-center">
            {isRegisterVerification && (
              <TurnstileCaptcha
                action="resend-registration-verification"
                resetKey={resendCaptchaResetKey}
                onVerify={handleResendCaptchaVerify}
                className="mb-4 flex justify-center"
              />
            )}

            <button
              type="button"
              onClick={handleResendCode}
              disabled={
                resending ||
                (CAPTCHA_ENABLED && isRegisterVerification && !resendCaptchaToken)
              }
              className="text-sm font-semibold text-[#1F6F5F] transition hover:text-[#1F6F5F] disabled:cursor-not-allowed disabled:opacity-60"
            >
              {resending ? "Sending..." : "Resend verification code"}
            </button>
          </div>

          {isRegisterVerification && (
            <p className="mt-6 text-center text-sm text-slate-500">
              Already verified?{" "}
              <Link
                to="/login"
                className="font-semibold text-[#1F6F5F] transition hover:text-[#1F6F5F]"
              >
                Sign in
              </Link>
            </p>
          )}

          {!isRegisterVerification && (
            <p className="mt-6 text-center text-sm text-slate-500">
              Need to go back?{" "}
              <Link
                to="/settings/account"
                className="font-semibold text-[#1F6F5F] transition hover:text-[#1F6F5F]"
              >
                Account settings
              </Link>
            </p>
          )}
        </div>
      </div>
    </div>
  );
}
