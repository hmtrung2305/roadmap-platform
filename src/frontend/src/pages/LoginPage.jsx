import { useCallback, useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { startGitHubLogin, startGoogleLogin } from "../api/authApi";
import { CAPTCHA_ENABLED } from "../api/apiConfig";
import AuthLogo from "../components/auth/AuthLogo";
import AuthRoadmapPanel from "../components/auth/AuthRoadmapPanel";
import TurnstileCaptcha from "../components/common/TurnstileCaptcha";
import { FcGoogle } from "react-icons/fc";
import { FaGithub } from "react-icons/fa";
import { useAuthStore } from "../stores/useAuthStore";
import MotionWrapper from "../components/auth/MotionWrapper";
import { getFriendlyApiErrorMessage } from "../utils/apiErrorUtils";



import {
  goToVerificationPage,
  isEmailVerificationRequired,
  isValidEmailFormat,
  VERIFICATION_PURPOSES,
} from "../utils/authVerificationFlow";

export default function LoginPage() {
  const navigate = useNavigate();

  const authError = useAuthStore((state) => state.authError);
  const clearAuthError = useAuthStore((state) => state.clearAuthError);
  const authLoading = useAuthStore((state) => state.authLoading);
  const login = useAuthStore((state) => state.login);

  const [searchParams] = useSearchParams();
  const oauthError = searchParams.get("oauthError");
  const verified = searchParams.get("verified");

  const [form, setForm] = useState({
    emailOrUsername: "",
    password: "",
  });

  const [showPassword, setShowPassword] = useState(false);
  const [error, setError] = useState("");
  const [passwordFocused, setPasswordFocused] = useState(false);
  const [captchaToken, setCaptchaToken] = useState("");
  const [captchaResetKey, setCaptchaResetKey] = useState(0);
  const [captchaError, setCaptchaError] = useState("");
  const [oauthLoadingProvider, setOauthLoadingProvider] = useState("");

  const handleCaptchaVerify = useCallback((token) => {
    setCaptchaToken(token);
    setCaptchaError("");
    setError("");
  }, []);

  const handleChange = (e) => {
    const { name, value } = e.target;

    if (authError) {
      clearAuthError();
    }

    if (error) {
      setError("");
    }

    if (captchaError) {
      setCaptchaError("");
    }

    setForm((prev) => ({
      ...prev,
      [name]: value,
    }));
  };


  const validateForm = () => {
    const emailOrUsername = form.emailOrUsername.trim();

    if (!emailOrUsername) {
      return "Please enter an email address or username.";
    }

    if (emailOrUsername.includes("@") && !isValidEmailFormat(emailOrUsername)) {
      return "Please enter a valid email address.";
    }

    if (!form.password) {
      return "Please enter a password.";
    }

    if (CAPTCHA_ENABLED && !captchaToken) {
      return "Please complete the CAPTCHA challenge.";
    }

    return "";
  };

  const handleLogin = async (e) => {
    e.preventDefault();

    const validationError = validateForm();

    if (validationError) {
      setError(validationError);

      if (CAPTCHA_ENABLED && !captchaToken) {
        setCaptchaError(validationError);
      }

      return;
    }

    try {
      setError("");
      clearAuthError();

      await login({
        ...form,
        emailOrUsername: form.emailOrUsername.trim(),
        captchaToken,
      });

      setTimeout(() => {
        navigate("/roadmaps");
      }, 250);
    } catch (error) {
      console.log("Login failed:", error.response?.data || error);

      if (isEmailVerificationRequired(error)) {
        clearAuthError();
        goToVerificationPage(
          navigate,
          error,
          form.emailOrUsername.includes("@") ? form.emailOrUsername.trim() : "",
          VERIFICATION_PURPOSES.REGISTER,
        );
      }
    } finally {
      setCaptchaToken("");
      setCaptchaResetKey((prev) => prev + 1);
    }
  };

  const handleGoRegister = () => {
    setTimeout(() => {
      navigate("/register");
    }, 250);
  };

  const handleGoogleLogin = async () => {
    if (oauthLoadingProvider) return;

    try {
      setError("");
      setOauthLoadingProvider("google");
      await startGoogleLogin();
    } catch (error) {
      setError(getFriendlyApiErrorMessage(error, "Unable to start Google sign in."));
    } finally {
      setOauthLoadingProvider("");
    }
  };

  const handleGithubLogin = async () => {
    if (oauthLoadingProvider) return;

    try {
      setError("");
      setOauthLoadingProvider("github");
      await startGitHubLogin();
    } catch (error) {
      setError(getFriendlyApiErrorMessage(error, "Unable to start GitHub sign in."));
    } finally {
      setOauthLoadingProvider("");
    }
  };

  return (
    <div className="min-h-dvh overflow-hidden bg-[#F7F1E8] text-slate-900">
      <MotionWrapper className="grid min-h-dvh grid-cols-1 lg:grid-cols-[1.08fr_0.92fr]">
        <AuthRoadmapPanel />

        <section className="flex min-h-dvh items-center justify-center bg-[#F7F1E8] px-6 py-6">
          <div
            className={`w-full max-w-[390px] rounded-lg border border-[#B9D8CC] bg-white/90 px-6 py-7 shadow-[0_18px_44px_rgba(31,111,95,0.08)] backdrop-blur transition duration-200 ${
              authLoading ? "scale-[0.99] opacity-70" : ""
            }`}
          >
            <div className="mb-8 lg:hidden">
              <AuthLogo />
            </div>

            <div>
              <p className="text-xs font-bold uppercase tracking-[0.22em] text-[#1F6F5F]">
                Welcome back
              </p>

              <h1 className="mt-3 text-2xl font-bold tracking-tight text-slate-900">
                Sign in to TechMap
              </h1>

              <p className="mt-2 text-sm leading-6 text-slate-500">
                Continue your personalized learning roadmap.
              </p>
            </div>

            {(error || authError || oauthError || captchaError) && (
              <div className="mt-5 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-600">
                {error || authError || oauthError || captchaError}
              </div>
            )}

            {verified === "1" && (
              <div className="mt-5 rounded-lg border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm text-emerald-700">
                Your email has been verified. You can now sign in.
              </div>
            )}

            <form onSubmit={handleLogin} className="mt-6 space-y-4">
              <div>
                <label className="mb-2 block text-sm font-semibold text-slate-700">
                  Email or username
                </label>

                <input
                  name="emailOrUsername"
                  type="text"
                  value={form.emailOrUsername}
                  onChange={handleChange}
                  placeholder="name@example.com"
                  className="h-11 w-full rounded-lg border border-slate-300 bg-white px-4 text-sm text-slate-900 outline-none transition placeholder:text-slate-300 focus:border-[#2FA084] focus:ring-4 focus:ring-[#6FCF97]/20"
                  required
                />
              </div>

              <div>
                <div className="mb-2 flex items-center justify-between">
                  <label className="block text-sm font-semibold text-slate-700">
                    Password
                  </label>

                  <button
                    type="button"
                    className="!text-[14px] font-semibold text-[#1F6F5F] transition hover:text-[#1F6F5F]"
                  >
                    Forgot password?
                  </button>
                </div>

                <div className="relative">
                  <input
                    name="password"
                    type={showPassword ? "text" : "password"}
                    value={form.password}
                    onChange={handleChange}
                    onFocus={() => setPasswordFocused(true)}
                    onBlur={() => setPasswordFocused(false)}
                    placeholder="••••••••"
                    className="h-10 w-full rounded-lg border border-slate-300 px-4 pr-12 text-sm text-slate-900 outline-none transition placeholder:text-slate-300 focus:border-[#2FA084] focus:ring-4 focus:ring-[#6FCF97]/20"
                    required
                  />

                  <button
                    type="button"
                    onClick={() => setShowPassword((prev) => !prev)}
                    className="absolute right-4 top-1/2 -translate-y-1/2 text-xs font-semibold text-slate-400 transition hover:text-slate-600"
                  >
                    {showPassword ? "Hide" : "Show"}
                  </button>
                </div>
              </div>

              <TurnstileCaptcha
                action="login"
                resetKey={captchaResetKey}
                onVerify={handleCaptchaVerify}
                className="flex justify-center"
              />

              <button
                type="submit"
                disabled={authLoading || (CAPTCHA_ENABLED && !captchaToken)}
                className="h-11 w-full rounded-lg bg-[#2FA084] text-sm font-semibold text-white shadow-lg shadow-emerald-900/10 transition hover:bg-[#1F6F5F] disabled:cursor-not-allowed disabled:opacity-60"
              >
                {authLoading ? "Signing in..." : "Sign in"}
              </button>
            </form>

            <div className="my-6 flex items-center gap-4">
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
                disabled={authLoading || Boolean(oauthLoadingProvider)}
                className="flex h-10 items-center justify-center gap-2.5 rounded-full border border-[#dadce0] bg-white text-sm font-semibold text-slate-700 shadow-sm transition hover:bg-[#f8fafd] hover:shadow-md"
              >
                <FcGoogle className="text-lg" />
                {oauthLoadingProvider === "google" ? "Starting..." : "Google"}
              </button>

              <button
                type="button"
                onClick={handleGithubLogin}
                disabled={authLoading || Boolean(oauthLoadingProvider)}
                className="flex h-10 items-center justify-center gap-2.5 rounded-full border border-slate-900 bg-slate-950 text-sm font-semibold text-white shadow-lg shadow-slate-900/15 transition hover:bg-black"
              >
                <FaGithub className="text-lg text-white" />
                {oauthLoadingProvider === "github" ? "Starting..." : "GitHub"}
              </button>
            </div>

            <p className="mt-6 text-center text-sm text-slate-500">
              Don't have an account?{" "}
              <button
                type="button"
                onClick={handleGoRegister}
                disabled={authLoading}
                className="font-semibold text-[#1F6F5F] transition hover:text-[#1F6F5F] disabled:opacity-60"
              >
                {authLoading ? "Opening..." : "Sign up"}
              </button>
            </p>
          </div>
        </section>
      </MotionWrapper>
    </div>
  );
}
