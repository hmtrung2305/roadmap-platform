import { useState } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import {
  resendRegistrationVerificationApi,
  verifyRegistrationEmailApi,
} from "../api/authApi";

export default function VerifyEmailPage() {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();

  const emailFromQuery = searchParams.get("email") || "";

  const [otp, setOtp] = useState("");
  const [email, setEmail] = useState(emailFromQuery);
  const [loading, setLoading] = useState(false);
  const [resending, setResending] = useState(false);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");

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

    if (!email.trim()) {
      setError("Please enter your email address.");
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

      await verifyRegistrationEmailApi({
        Email: email,
        Otp: otp,
      });

      navigate("/login?verified=1", {
        replace: true,
      });
    } catch (error) {
      console.error("Verify email failed:", error.response?.data || error);

      const serverMessage =
        error.response?.data?.message ||
        "Invalid or expired verification code.";

      setError(serverMessage);
    } finally {
      setLoading(false);
    }
  };

  const handleResendCode = async () => {
    if (!email.trim()) {
      setError("Please enter your email before requesting a new code.");
      return;
    }

    try {
      setResending(true);
      setError("");
      setMessage("");

      await resendRegistrationVerificationApi({
        Email: email,
      });

      setMessage("A new verification code has been sent to your email.");
    } catch (error) {
      console.error("Resend code failed:", error.response?.data || error);

      const serverMessage =
        error.response?.data?.message ||
        "Unable to resend verification code.";

      setError(serverMessage);
    } finally {
      setResending(false);
    }
  };

  return (
    <div className="min-h-dvh bg-[#F7F1E8] px-6 py-10 text-slate-900">
      <div className="mx-auto flex min-h-[calc(100dvh-5rem)] max-w-md items-center justify-center">
        <div
          className={`w-full rounded-3xl border border-slate-200 bg-white px-6 py-7 shadow-xl shadow-slate-200/60 transition duration-200 ${
            loading ? "scale-[0.99] opacity-70" : ""
          }`}
        >
          <div className="text-center">
            <p className="text-xs font-bold uppercase tracking-[0.22em] text-emerald-600">
              Email verification
            </p>

            <h1 className="mt-3 text-2xl font-bold tracking-tight text-slate-900">
              Verify your email
            </h1>

            <p className="mt-2 text-sm leading-6 text-slate-500">
              Enter the verification code we sent to your email address.
            </p>
          </div>

          {error && (
            <div className="mt-5 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-600">
              {error}
            </div>
          )}

          {message && (
            <div className="mt-5 rounded-xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700">
              {message}
            </div>
          )}

          <form onSubmit={handleVerifyEmail} className="mt-6 space-y-4">
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
                className="h-11 w-full rounded-xl border border-slate-300 px-4 text-sm text-slate-900 outline-none transition placeholder:text-slate-300 focus:border-emerald-500 focus:ring-4 focus:ring-emerald-100"
                required
              />
            </div>

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
                className="h-12 w-full rounded-xl border border-slate-300 px-4 text-center text-lg font-semibold tracking-[0.4em] text-slate-900 outline-none transition placeholder:text-sm placeholder:font-normal placeholder:tracking-normal placeholder:text-slate-300 focus:border-emerald-500 focus:ring-4 focus:ring-emerald-100"
                required
              />
            </div>

            <button
              type="submit"
              disabled={loading}
              className="h-11 w-full rounded-xl bg-emerald-700 text-sm font-semibold text-white shadow-lg shadow-emerald-700/20 transition hover:bg-emerald-800 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {loading ? "Verifying..." : "Verify email"}
            </button>
          </form>

          <div className="mt-5 text-center">
            <button
              type="button"
              onClick={handleResendCode}
              disabled={resending}
              className="text-sm font-semibold text-emerald-600 transition hover:text-emerald-700 disabled:cursor-not-allowed disabled:opacity-60"
            >
              {resending ? "Sending..." : "Resend verification code"}
            </button>
          </div>

          <p className="mt-6 text-center text-sm text-slate-500">
            Already verified?{" "}
            <Link
              to="/login"
              className="font-semibold text-emerald-600 transition hover:text-emerald-700"
            >
              Sign in
            </Link>
          </p>
        </div>
      </div>
    </div>
  );
}