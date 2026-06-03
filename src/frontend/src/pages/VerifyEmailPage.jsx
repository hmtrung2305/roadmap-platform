import { useState } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import AuthLogo from "../components/auth/AuthLogo";
import AuthRoadmapPanel from "../components/auth/AuthRoadmapPanel";
import MotionWrapper from "../components/auth/MotionWrapper";
import { resendRegistrationVerificationApi, verifyRegistrationEmailApi } from "../api/authApi";

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
      setError("Vui lòng nhập email.");
      return;
    }

    if (!otp.trim()) {
      setError("Vui lòng nhập mã xác thực.");
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

      window.location.replace("/home");
    } catch (error) {
      console.error("Verify email failed:", error.response?.data || error);

      const serverMessage =
        error.response?.data?.message ||
        "Mã xác thực không hợp lệ hoặc đã hết hạn.";

      setError(serverMessage);
    } finally {
      setLoading(false);
    }
  };

  const handleResendCode = async () => {
    if (!email.trim()) {
      setError("Vui lòng nhập email trước khi gửi lại mã.");
      return;
    }

    try {
      setResending(true);
      setError("");
      setMessage("");

      await resendRegistrationVerificationApi({
        Email: email,
      });

      setMessage("Mã xác thực mới đã được gửi. Vui lòng kiểm tra email.");
    } catch (error) {
      console.error("Resend code failed:", error.response?.data || error);

      const serverMessage =
        error.response?.data?.message ||
        error.response?.data?.Message ||
        "Không thể gửi lại mã lúc này.";

      setError(serverMessage);
    } finally {
      setResending(false);
    }
  };

  return (
    <div className="min-h-dvh overflow-hidden bg-slate-50 text-slate-900">
      <MotionWrapper className="grid min-h-dvh grid-cols-1 lg:grid-cols-[1.08fr_0.92fr]">
        <AuthRoadmapPanel />

        <section className="flex min-h-dvh items-center justify-center bg-white px-6 py-6">
          <div
            className={`w-full max-w-[390px] rounded-3xl border border-slate-100 bg-white px-6 py-7 shadow-xl shadow-slate-200/60 transition duration-200 ${
              loading ? "scale-[0.99] opacity-70" : ""
            }`}
          >
            <div className="mb-8 lg:hidden">
              <AuthLogo />
            </div>

            <div>
              <p className="text-xs font-bold uppercase tracking-[0.22em] text-blue-600">
                Verify email
              </p>

              <h1 className="mt-3 text-2xl font-bold tracking-tight text-slate-900">
                Check your inbox
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
                  className="h-10 w-full rounded-xl border border-slate-300 px-4 text-sm text-slate-900 outline-none transition placeholder:text-slate-300 focus:border-blue-500 focus:ring-4 focus:ring-blue-100"
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
                  className="h-12 w-full rounded-xl border border-slate-300 px-4 text-center text-lg font-semibold tracking-[0.4em] text-slate-900 outline-none transition placeholder:text-sm placeholder:font-normal placeholder:tracking-normal placeholder:text-slate-300 focus:border-blue-500 focus:ring-4 focus:ring-blue-100"
                  required
                />
              </div>

              <button
                type="submit"
                disabled={loading}
                className="h-11 w-full rounded-xl bg-blue-700 text-sm font-semibold text-white shadow-lg shadow-blue-700/20 transition hover:bg-blue-800 disabled:cursor-not-allowed disabled:opacity-60"
              >
                {loading ? "Verifying..." : "Verify email"}
              </button>
            </form>

            <div className="mt-5 text-center">
              <button
                type="button"
                onClick={handleResendCode}
                disabled={resending}
                className="text-sm font-semibold text-blue-600 transition hover:text-blue-700 disabled:cursor-not-allowed disabled:opacity-60"
              >
                {resending ? "Sending..." : "Resend verification code"}
              </button>
            </div>

            <p className="mt-6 text-center text-sm text-slate-500">
              Already verified?{" "}
              <Link
                to="/login"
                className="font-semibold text-blue-600 transition hover:text-blue-700"
              >
                Sign in
              </Link>
            </p>
          </div>
        </section>
      </MotionWrapper>
    </div>
  );
}