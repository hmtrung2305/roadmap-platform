import { useCallback, useState } from "react";
import {
  useLocation,
  useNavigate,
  useSearchParams,
} from "react-router-dom";
import { FcGoogle } from "react-icons/fc";
import { FaGithub } from "react-icons/fa";

import {
  startGitHubLogin,
  startGoogleLogin,
} from "../api/authApi";
import { CAPTCHA_ENABLED } from "../api/apiConfig";

import AuthLogo from "../features/auth/components/AuthLogo";
import AuthRoadmapPanel from "../features/auth/components/AuthRoadmapPanel";
import MotionWrapper from "../features/auth/components/MotionWrapper";
import TurnstileCaptcha from "../components/common/TurnstileCaptcha";

import { useAuthStore } from "../stores/useAuthStore";
import { getFriendlyApiErrorMessage } from "../utils/apiErrorUtils";
import { resolvePostLoginRedirect } from "../utils/navigationUtils";

import {
  goToVerificationPage,
  isEmailVerificationRequired,
  isValidEmailFormat,
  VERIFICATION_PURPOSES,
} from "../utils/authVerificationFlow";

export default function LoginPage() {
  const navigate = useNavigate();
  const location = useLocation();

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
  const [captchaToken, setCaptchaToken] = useState("");
  const [captchaResetKey, setCaptchaResetKey] = useState(0);
  const [captchaError, setCaptchaError] = useState("");
  const [oauthLoadingProvider, setOauthLoadingProvider] = useState("");

  const handleCaptchaVerify = useCallback((token) => {
    setCaptchaToken(token);
    setCaptchaError("");
    setError("");
  }, []);

  const handleChange = (event) => {
    const { name, value } = event.target;

    if (authError) clearAuthError();
    if (error) setError("");
    if (captchaError) setCaptchaError("");

    setForm((previousForm) => ({
      ...previousForm,
      [name]: value,
    }));
  };

  const validateForm = () => {
    const emailOrUsername = form.emailOrUsername.trim();

    if (!emailOrUsername) {
      return "Please enter an email address or username.";
    }

    if (
      emailOrUsername.includes("@") &&
      !isValidEmailFormat(emailOrUsername)
    ) {
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

  const handleLogin = async (event) => {
    event.preventDefault();

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
      setCaptchaError("");
      clearAuthError();

      const currentUser = await login({
        ...form,
        emailOrUsername: form.emailOrUsername.trim(),
        captchaToken,
      });

      const redirectTo = resolvePostLoginRedirect(
        currentUser,
        location.state?.from,
      );

      setTimeout(() => {
        navigate(redirectTo, {
          replace: true,
        });
      }, 250);
    } catch (loginError) {
      console.log(
        "Login failed:",
        loginError.response?.data || loginError,
      );

      if (isEmailVerificationRequired(loginError)) {
        clearAuthError();

        goToVerificationPage(
          navigate,
          loginError,
          form.emailOrUsername.includes("@")
            ? form.emailOrUsername.trim()
            : "",
          VERIFICATION_PURPOSES.REGISTER,
        );
      }
    } finally {
      setCaptchaToken("");
      setCaptchaResetKey((previousKey) => previousKey + 1);
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
    } catch (googleError) {
      setError(
        getFriendlyApiErrorMessage(
          googleError,
          "Unable to start Google sign in.",
        ),
      );
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
    } catch (githubError) {
      setError(
        getFriendlyApiErrorMessage(
          githubError,
          "Unable to start GitHub sign in.",
        ),
      );
    } finally {
      setOauthLoadingProvider("");
    }
  };

  const displayedError = error || authError || oauthError || captchaError;
  const oauthIsLoading = Boolean(oauthLoadingProvider);

  return (
    <div className="min-h-dvh overflow-hidden bg-[#F8F5EF] text-[#243429]">
      <MotionWrapper
        className="
          grid min-h-dvh grid-cols-1
          lg:grid-cols-[1.1fr_0.9fr]
          xl:grid-cols-[1.14fr_0.86fr]
          2xl:grid-cols-[1.17fr_0.83fr]
        "
      >
        <AuthRoadmapPanel />

        <section className="relative flex min-h-dvh items-center justify-center overflow-hidden bg-[linear-gradient(145deg,#FFFDF8_0%,#F8F5EF_48%,#EEF5F0_100%)] px-5 py-7 sm:px-8 lg:px-8 xl:px-10">
          <div className="pointer-events-none absolute -right-32 top-20 h-96 w-96 rounded-full bg-[#DDEBE2]/60 blur-3xl" />
          <div className="pointer-events-none absolute -left-24 bottom-10 h-80 w-80 rounded-full bg-[#EFE7D8]/70 blur-3xl" />

          <div
            className={`relative z-10 w-full max-w-[520px] rounded-[2.25rem] border border-[#D8E6DD] bg-white/[0.85] px-8 py-9 shadow-[0_30px_100px_rgba(80,102,88,0.17)] backdrop-blur-xl transition duration-200 sm:px-10 sm:py-10 lg:px-9 xl:px-11 ${
              authLoading ? "scale-[0.99] opacity-70" : ""
            }`}
          >
            <div className="mb-8 lg:hidden">
              <AuthLogo />
            </div>

            <div className="text-center">
              <p className="text-[11px] font-black uppercase tracking-[0.28em] text-[#5C7C68]">
                Welcome back
              </p>

              <h1 className="mt-4 text-[31px] font-black tracking-[-0.04em] text-[#243429] sm:text-[35px]">
                Sign in to TechMap
              </h1>

              <p className="mx-auto mt-3 max-w-[360px] text-[15px] leading-6 text-[#66736A]">
                Continue your personalized learning roadmap.
              </p>
            </div>

            {displayedError && (
              <div
                role="alert"
                className="mt-6 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm font-medium text-red-600"
              >
                {displayedError}
              </div>
            )}

            {verified === "1" && (
              <div
                role="status"
                className="mt-6 rounded-2xl border border-[#B8DEC8] bg-[#EEF8F2] px-4 py-3 text-sm font-medium text-[#3D7A50]"
              >
                Your email has been verified. You can now sign in.
              </div>
            )}

            <form onSubmit={handleLogin} className="mt-7 space-y-5">
              <div>
                <label
                  htmlFor="emailOrUsername"
                  className="mb-2 block text-sm font-bold text-[#2E4034]"
                >
                  Email or username
                </label>

                <input
                  id="emailOrUsername"
                  name="emailOrUsername"
                  type="text"
                  value={form.emailOrUsername}
                  onChange={handleChange}
                  placeholder="name@example.com"
                  autoComplete="username"
                  className="h-[52px] w-full rounded-2xl border border-[#CBDDD1] bg-[#FFFDF8] px-4 text-sm font-medium text-[#243429] outline-none transition placeholder:text-[#A7B3AB] focus:border-[#5C7C68] focus:bg-white focus:ring-4 focus:ring-[#DDEBE2]"
                  required
                />
              </div>

              <div>
                <div className="mb-2 flex items-center justify-between gap-4">
                  <label
                    htmlFor="password"
                    className="block text-sm font-bold text-[#2E4034]"
                  >
                    Password
                  </label>

                  <button
                    type="button"
                    disabled
                    aria-disabled="true"
                    title="Forgot password is not available yet"
                    className="cursor-not-allowed text-[13px] font-bold text-[#5C7C68] opacity-70"
                  >
                    Forgot password?
                  </button>
                </div>

                <div className="relative">
                  <input
                    id="password"
                    name="password"
                    type={showPassword ? "text" : "password"}
                    value={form.password}
                    onChange={handleChange}
                    placeholder="••••••••"
                    autoComplete="current-password"
                    className="h-[52px] w-full rounded-2xl border border-[#CBDDD1] bg-[#FFFDF8] px-4 pr-16 text-sm font-medium text-[#243429] outline-none transition placeholder:text-[#A7B3AB] focus:border-[#5C7C68] focus:bg-white focus:ring-4 focus:ring-[#DDEBE2]"
                    required
                  />

                  <button
                    type="button"
                    onClick={() => {
                      setShowPassword((previousValue) => !previousValue);
                    }}
                    aria-label={
                      showPassword ? "Hide password" : "Show password"
                    }
                    className="absolute right-4 top-1/2 -translate-y-1/2 text-xs font-black uppercase tracking-[0.08em] text-[#7C8D82] transition hover:text-[#435E4C]"
                  >
                    {showPassword ? "Hide" : "Show"}
                  </button>
                </div>
              </div>

              {CAPTCHA_ENABLED && (
                <TurnstileCaptcha
                  action="login"
                  resetKey={captchaResetKey}
                  onVerify={handleCaptchaVerify}
                  className="flex justify-center"
                />
              )}

              <button
                type="submit"
                disabled={authLoading || (CAPTCHA_ENABLED && !captchaToken)}
                className="h-[52px] w-full rounded-2xl bg-[#5C7C68] text-sm font-black text-white shadow-[0_18px_36px_rgba(92,124,104,0.22)] transition hover:-translate-y-0.5 hover:bg-[#435E4C] disabled:cursor-not-allowed disabled:opacity-60 disabled:hover:translate-y-0"
              >
                {authLoading ? "Signing in..." : "Sign in"}
              </button>
            </form>

            <div className="my-7 flex items-center gap-4">
              <div className="h-px flex-1 bg-[#E5ECE7]" />

              <span className="whitespace-nowrap text-xs font-medium uppercase tracking-[0.05em] text-[#8B9590]">
                Or continue with
              </span>

              <div className="h-px flex-1 bg-[#E5ECE7]" />
            </div>

            <div className="grid grid-cols-2 gap-3">
              <button
                type="button"
                onClick={handleGoogleLogin}
                disabled={authLoading || oauthIsLoading}
                className="flex h-[52px] items-center justify-center gap-2.5 rounded-2xl border border-[#D8E6DD] bg-[#FFFDF8] text-sm font-bold text-[#2E4034] shadow-sm transition hover:-translate-y-0.5 hover:bg-white hover:shadow-md disabled:cursor-not-allowed disabled:opacity-60 disabled:hover:translate-y-0"
              >
                <FcGoogle className="text-lg" />

                <span>
                  {oauthLoadingProvider === "google"
                    ? "Starting..."
                    : "Google"}
                </span>
              </button>

              <button
                type="button"
                onClick={handleGithubLogin}
                disabled={authLoading || oauthIsLoading}
                className="flex h-[52px] items-center justify-center gap-2.5 rounded-2xl border border-[#243429] bg-[#243429] text-sm font-bold text-white shadow-[0_14px_30px_rgba(36,52,41,0.18)] transition hover:-translate-y-0.5 hover:bg-[#17211B] disabled:cursor-not-allowed disabled:opacity-60 disabled:hover:translate-y-0"
              >
                <FaGithub className="text-lg text-white" />

                <span>
                  {oauthLoadingProvider === "github"
                    ? "Starting..."
                    : "GitHub"}
                </span>
              </button>
            </div>

            <p className="mt-7 text-center text-sm text-[#66736A]">
              Don&apos;t have an account?{" "}
              <button
                type="button"
                onClick={handleGoRegister}
                disabled={authLoading}
                className="font-black text-[#5C7C68] transition hover:text-[#435E4C] disabled:cursor-not-allowed disabled:opacity-60"
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