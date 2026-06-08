import { useState } from "react";
import { X } from "lucide-react";
import {
  resendLinkedLocalVerificationApi,
  verifyLinkedLocalEmailApi,
  verifyLocalEmailChangeApi,
} from "../../api/authProviderApi";

export default function VerifyLocalEmailModal({
  email,
  mode = "link-local",
  onClose,
  onSuccess,
}) {
  const [otp, setOtp] = useState("");
  const [loading, setLoading] = useState(false);
  const [resending, setResending] = useState(false);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");

  const isChangeEmailMode = mode === "change-email";

  const handleOtpChange = (e) => {
    const value = e.target.value.replace(/\D/g, "").slice(0, 6);

    if (error) {
      setError("");
    }

    if (message) {
      setMessage("");
    }

    setOtp(value);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    if (!otp.trim()) {
      setError("Please enter the verification code.");
      return;
    }

    try {
      setLoading(true);
      setError("");
      setMessage("");

      if (isChangeEmailMode) {
        await verifyLocalEmailChangeApi({
          Otp: otp,
        });
      } else {
        await verifyLinkedLocalEmailApi({
          Otp: otp,
        });
      }

      await onSuccess?.();
    } catch (error) {
      console.error("Verify email failed:", error.response?.data || error);

      const serverMessage =
        error.response?.data?.message ||
        error.response?.data?.Message ||
        "Invalid or expired verification code.";

      setError(serverMessage);
    } finally {
      setLoading(false);
    }
  };

  const handleResendCode = async () => {
    try {
      setResending(true);
      setError("");
      setMessage("");

      await resendLinkedLocalVerificationApi();

      setMessage("A new verification code has been sent.");
    } catch (error) {
      console.error(
        "Resend verification failed:",
        error.response?.data || error
      );

      const serverMessage =
        error.response?.data?.message ||
        error.response?.data?.Message ||
        "Unable to resend verification code.";

      setError(serverMessage);
    } finally {
      setResending(false);
    }
  };

  return (
    <div className="fixed inset-0 z-[100] flex items-center justify-center bg-slate-950/40 px-4 backdrop-blur-sm">
      <div className="w-full max-w-lg rounded-lg border border-slate-200 bg-white p-6 shadow-2xl shadow-slate-900/20">
        <div className="flex items-start justify-between gap-4">
          <div>
            <h2 className="text-xl font-bold tracking-tight text-slate-900">
              Verify your email
            </h2>

            <p className="mt-1 text-sm leading-6 text-slate-500">
              Enter the verification code sent to{" "}
              <span className="font-semibold text-slate-700">{email}</span>.
            </p>
          </div>

          <button
            type="button"
            onClick={onClose}
            disabled={loading || resending}
            className="rounded-lg p-2 text-slate-400 transition hover:bg-slate-100 hover:text-slate-600 disabled:opacity-60"
          >
            <X size={18} />
          </button>
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

        <form onSubmit={handleSubmit} className="mt-6 space-y-5">
          <div>
            <label className="mb-2 block text-sm font-semibold text-slate-700">
              Verification code
            </label>

            <input
              value={otp}
              onChange={handleOtpChange}
              placeholder="Enter 6-digit code"
              inputMode="numeric"
              className="h-12 w-full rounded-lg border border-slate-300 bg-white px-4 text-center text-lg font-semibold tracking-[0.4em] text-slate-900 outline-none transition placeholder:text-sm placeholder:font-normal placeholder:tracking-normal placeholder:text-slate-300 focus:border-[#2FA084] focus:ring-4 focus:ring-[#6FCF97]/20"
            />
          </div>

          <div className="flex items-center justify-between gap-3 pt-2">
            {!isChangeEmailMode ? (
              <button
                type="button"
                onClick={handleResendCode}
                disabled={resending || loading}
                className="text-sm font-semibold text-[#1F6F5F] transition hover:text-[#1F6F5F] disabled:cursor-not-allowed disabled:opacity-60"
              >
                {resending ? "Sending..." : "Resend code"}
              </button>
            ) : (
              <span className="text-xs text-slate-500">
                Use the latest code sent to your new email.
              </span>
            )}

            <div className="flex gap-3">
              <button
                type="button"
                onClick={onClose}
                disabled={loading || resending}
                className="h-10 rounded-lg bg-slate-100 px-5 text-sm font-semibold text-slate-700 transition hover:bg-slate-200 disabled:opacity-60"
              >
                Cancel
              </button>

              <button
                type="submit"
                disabled={loading || resending}
                className="h-10 rounded-lg bg-[#2FA084] px-5 text-sm font-semibold text-white shadow-lg shadow-emerald-900/10 transition hover:bg-[#1F6F5F] disabled:cursor-not-allowed disabled:opacity-60"
              >
                {loading ? "Verifying..." : "Verify"}
              </button>
            </div>
          </div>
        </form>
      </div>
    </div>
  );
}