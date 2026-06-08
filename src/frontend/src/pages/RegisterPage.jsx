import { useCallback, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { GITHUB_AUTH_URL, GOOGLE_AUTH_URL } from "../api/authApi";
import { CAPTCHA_ENABLED } from "../api/apiConfig";
import { useAuthStore } from "../stores/useAuthStore";
import AuthLogo from "../components/auth/AuthLogo";
import { FcGoogle } from "react-icons/fc";
import { FaGithub } from "react-icons/fa";
import MotionWrapper from "../components/auth/MotionWrapper";
import AuthRoadmapPanel from "../components/auth/AuthRoadmapPanel";
import TurnstileCaptcha from "../components/common/TurnstileCaptcha";

export default function RegisterPage() {
  const navigate = useNavigate();

  const register = useAuthStore((state) => state.register);
  const authLoading = useAuthStore((state) => state.authLoading);
  const authError = useAuthStore((state) => state.authError);
  const clearAuthError = useAuthStore((state) => state.clearAuthError);

  const [form, setForm] = useState({
    username: "",
    email: "",
    password: "",
    confirmPassword: "",
    agreeTerms: false,
  });

  const [error, setError] = useState("");
  const [captchaToken, setCaptchaToken] = useState("");
  const [captchaResetKey, setCaptchaResetKey] = useState(0);

  const displayError = error || authError;

  const handleCaptchaVerify = useCallback((token) => {
    setCaptchaToken(token);
    setError("");
  }, []);

  const handleChange = (event) => {
    const { name, value, type, checked } = event.target;

    if (error) {
      setError("");
    }

    if (authError) {
      clearAuthError();
    }

    setForm((prev) => ({
      ...prev,
      [name]: type === "checkbox" ? checked : value,
    }));
  };

  const validateForm = () => {
    if (!form.username.trim()) {
      return "Vui lòng nhập username.";
    }

    if (!form.email.trim()) {
      return "Vui lòng nhập email.";
    }

    if (form.password.length < 8) {
      return "Mật khẩu nên có ít nhất 8 ký tự.";
    }

    if (form.password !== form.confirmPassword) {
      return "Confirm password không khớp.";
    }

    if (!form.agreeTerms) {
      return "Bạn cần đồng ý với Terms of Service và Privacy Policy.";
    }

    if (CAPTCHA_ENABLED && !captchaToken) {
      return "Please complete the CAPTCHA challenge.";
    }

    return "";
  };

  const handleRegister = async (event) => {
    event.preventDefault();

    const validationError = validateForm();

    if (validationError) {
      setError(validationError);
      return;
    }

    try {
      setError("");
      clearAuthError();

      await register({
        Username: form.username.trim(),
        Email: form.email.trim(),
        Password: form.password,
        CaptchaToken: captchaToken,
      });

      navigate(`/verify-email?email=${encodeURIComponent(form.email.trim())}`);
    } catch (error) {
      console.error("Register failed:", error);

      setError(error.message || "Register failed. Please try again.");
    } finally {
      setCaptchaToken("");
      setCaptchaResetKey((prev) => prev + 1);
    }
  };

  const handleGoogleLogin = () => {
    window.location.href = GOOGLE_AUTH_URL;
  };

  const handleGithubLogin = () => {
    window.location.href = GITHUB_AUTH_URL;
  };

  return (
    <div className="min-h-dvh overflow-hidden bg-slate-50 text-slate-900">
      <MotionWrapper className="grid min-h-dvh grid-cols-1 lg:grid-cols-[1.08fr_0.92fr]">
        <AuthRoadmapPanel />

        <section className="flex min-h-dvh items-center justify-center bg-white px-6 py-5">
          <div
            className={`w-full max-w-[420px] rounded-lg border border-slate-100 bg-white px-6 py-6 shadow-xl shadow-slate-200/60 transition duration-200 ${
              authLoading ? "scale-[0.99] opacity-70" : ""
            }`}
          >
            <div className="mb-7 lg:hidden">
              <AuthLogo />
            </div>

            <div>
              <p className="text-xs font-bold uppercase tracking-[0.22em] text-[#1F6F5F]">
                Start learning
              </p>

              <h1 className="mt-3 text-2xl font-bold tracking-tight text-slate-900">
                Create your account
              </h1>

              <p className="mt-2 text-sm leading-6 text-slate-500">
                Start building your personalized career roadmap today.
              </p>
            </div>

            {displayError && (
              <div className="mt-5 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-600">
                {displayError}
              </div>
            )}

            <form onSubmit={handleRegister} className="mt-5 space-y-3.5">
              <div>
                <label className="mb-1.5 block text-sm font-semibold text-slate-700">
                  Username
                </label>

                <input
                  name="username"
                  value={form.username}
                  onChange={handleChange}
                  placeholder="John Doe"
                  className="h-10 w-full rounded-lg border border-slate-300 bg-white px-4 text-sm text-slate-900 outline-none transition placeholder:text-slate-300 focus:border-[#2FA084] focus:ring-4 focus:ring-[#6FCF97]/20"
                  required
                />
              </div>

              <div>
                <label className="mb-1.5 block text-sm font-semibold text-slate-700">
                  Email address
                </label>

                <input
                  name="email"
                  type="email"
                  value={form.email}
                  onChange={handleChange}
                  placeholder="name@example.com"
                  className="h-10 w-full rounded-lg border border-slate-300 bg-white px-4 text-sm text-slate-900 outline-none transition placeholder:text-slate-300 focus:border-[#2FA084] focus:ring-4 focus:ring-[#6FCF97]/20"
                  required
                />
              </div>

              <div className="grid grid-cols-1 gap-3 sm:grid-cols-2">
                <div>
                  <label className="mb-1.5 block text-sm font-semibold text-slate-700">
                    Password
                  </label>

                  <input
                    name="password"
                    type="password"
                    value={form.password}
                    onChange={handleChange}
                    placeholder="••••••••"
                    className="h-10 w-full rounded-lg border border-slate-300 bg-white px-4 text-sm text-slate-900 outline-none transition placeholder:text-slate-300 focus:border-[#2FA084] focus:ring-4 focus:ring-[#6FCF97]/20"
                    required
                  />
                </div>

                <div>
                  <label className="mb-1.5 block text-sm font-semibold text-slate-700">
                    Confirm
                  </label>

                  <input
                    name="confirmPassword"
                    type="password"
                    value={form.confirmPassword}
                    onChange={handleChange}
                    placeholder="••••••••"
                    className="h-10 w-full rounded-lg border border-slate-300 bg-white px-4 text-sm text-slate-900 outline-none transition placeholder:text-slate-300 focus:border-[#2FA084] focus:ring-4 focus:ring-[#6FCF97]/20"
                    required
                  />
                </div>
              </div>

              <label className="flex items-start gap-3 text-xs leading-5 text-slate-500">
                <input
                  name="agreeTerms"
                  type="checkbox"
                  checked={form.agreeTerms}
                  onChange={handleChange}
                  className="mt-1 h-4 w-4 rounded border-slate-300 text-[#1F6F5F] focus:ring-blue-500"
                />

                <span>
                  I agree to the{" "}
                  <button
                    type="button"
                    className="font-semibold text-[#1F6F5F] transition hover:text-[#1F6F5F]"
                  >
                    Terms of Service
                  </button>{" "}
                  and{" "}
                  <button
                    type="button"
                    className="font-semibold text-[#1F6F5F] transition hover:text-[#1F6F5F]"
                  >
                    Privacy Policy
                  </button>
                  .
                </span>
              </label>

              <TurnstileCaptcha
                action="register"
                resetKey={captchaResetKey}
                onVerify={handleCaptchaVerify}
                className="flex justify-center"
              />

              <button
                type="submit"
                disabled={authLoading || (CAPTCHA_ENABLED && !captchaToken)}
                className="h-11 w-full rounded-lg bg-[#2FA084] text-sm font-semibold text-white shadow-lg shadow-emerald-900/10 transition hover:bg-[#1F6F5F] disabled:cursor-not-allowed disabled:opacity-60"
              >
                {authLoading ? "Creating account..." : "Create account"}
              </button>
            </form>

            <div className="my-5 flex items-center gap-4">
              <div className="h-px flex-1 bg-slate-200" />
              <span className="text-[11px] font-bold tracking-widest text-slate-400">
                OR CONTINUE WITH
              </span>
              <div className="h-px flex-1 bg-slate-200" />
            </div>

            <div className="grid grid-cols-2 gap-3">
              <button
                type="button"
                onClick={handleGoogleLogin}
                className="flex h-10 items-center justify-center gap-2.5 rounded-full border border-[#dadce0] bg-white text-sm font-semibold text-slate-700 shadow-sm transition hover:bg-[#f8fafd] hover:shadow-md"
              >
                <FcGoogle className="text-lg" />
                Google
              </button>

              <button
                type="button"
                onClick={handleGithubLogin}
                className="flex h-10 items-center justify-center gap-2.5 rounded-full border border-slate-900 bg-slate-950 text-sm font-semibold text-white shadow-lg shadow-slate-900/15 transition hover:bg-black"
              >
                <FaGithub className="text-lg text-white" />
                GitHub
              </button>
            </div>

            <p className="mt-5 text-center text-sm text-slate-500">
              Already have an account?{" "}
              <Link
                to="/login"
                className="font-semibold text-[#1F6F5F] transition hover:text-[#1F6F5F]"
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