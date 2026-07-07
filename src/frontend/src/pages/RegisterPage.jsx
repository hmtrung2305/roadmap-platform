import { useCallback, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { FcGoogle } from "react-icons/fc";
import { FaGithub } from "react-icons/fa";

import {
  startGitHubLogin,
  startGoogleLogin,
} from "../api/authApi";
import { CAPTCHA_ENABLED } from "../api/apiConfig";

import { useAuthStore } from "../stores/useAuthStore";

import AuthLogo from "../features/auth/components/AuthLogo";
import AuthRoadmapPanel from "../features/auth/components/AuthRoadmapPanel";
import MotionWrapper from "../features/auth/components/MotionWrapper";
import TurnstileCaptcha from "../components/common/TurnstileCaptcha";

import { getFriendlyApiErrorMessage } from "../utils/apiErrorUtils";

import {
  getErrorMessage,
  goToVerificationPage,
  isEmailVerificationRequired,
  isValidEmailFormat,
  VERIFICATION_PURPOSES,
} from "../utils/authVerificationFlow";

const PASSWORD_REQUIREMENT_MESSAGE =
  "Password must contain at least 8 characters with uppercase, lowercase, number, and special character.";

const PASSWORD_REQUIREMENT_PATTERN =
  /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z0-9]).{8,}$/;

const USERNAME_REQUIREMENT_MESSAGE =
  "Username must be 3-20 characters and can only contain letters, numbers, underscores, or dots.";

const USERNAME_REQUIREMENT_PATTERN =
  /^[a-zA-Z0-9._]{3,20}$/;

function isValidPasswordFormat(password) {
  return PASSWORD_REQUIREMENT_PATTERN.test(password);
}

function isValidUsernameFormat(username) {
  return USERNAME_REQUIREMENT_PATTERN.test(username);
}

export default function RegisterPage() {
  const navigate = useNavigate();

  const register = useAuthStore((state) => state.register);
  const authLoading = useAuthStore((state) => state.authLoading);
  const authError = useAuthStore((state) => state.authError);
  const clearAuthError = useAuthStore(
    (state) => state.clearAuthError,
  );

  const [form, setForm] = useState({
    username: "",
    email: "",
    password: "",
    confirmPassword: "",
    agreeTerms: false,
  });

  const [error, setError] = useState("");
  const [usernameFocused, setUsernameFocused] = useState(false);
  const [passwordFocused, setPasswordFocused] = useState(false);

  const [captchaToken, setCaptchaToken] = useState("");
  const [captchaResetKey, setCaptchaResetKey] = useState(0);

  const [
    oauthLoadingProvider,
    setOauthLoadingProvider,
  ] = useState("");

  const displayedError = error || authError;
  const oauthIsLoading = Boolean(oauthLoadingProvider);

  const handleCaptchaVerify = useCallback((token) => {
    setCaptchaToken(token);
    setError("");
  }, []);

  const handleChange = (event) => {
    const {
      name,
      value,
      type,
      checked,
    } = event.target;

    if (error) {
      setError("");
    }

    if (authError) {
      clearAuthError();
    }

    setForm((previousForm) => ({
      ...previousForm,
      [name]: type === "checkbox" ? checked : value,
    }));
  };

  const validateForm = () => {
    const username = form.username.trim();
    const email = form.email.trim();

    if (!username) {
      return "Please enter a username.";
    }

    if (!isValidUsernameFormat(username)) {
      return USERNAME_REQUIREMENT_MESSAGE;
    }

    if (!email) {
      return "Please enter an email address.";
    }

    if (!isValidEmailFormat(email)) {
      return "Please enter a valid email address.";
    }

    if (!form.password) {
      return "Please enter a password.";
    }

    if (!isValidPasswordFormat(form.password)) {
      return PASSWORD_REQUIREMENT_MESSAGE;
    }

    if (!form.confirmPassword) {
      return "Please confirm your password.";
    }

    if (form.password !== form.confirmPassword) {
      return "Passwords do not match.";
    }

    if (!form.agreeTerms) {
      return "Please agree to the Terms of Service and Privacy Policy.";
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

      const data = await register({
        Username: form.username.trim(),
        Email: form.email.trim(),
        Password: form.password,
        CaptchaToken: captchaToken,
      });

      if (isEmailVerificationRequired(data)) {
        goToVerificationPage(
          navigate,
          data,
          form.email.trim(),
          VERIFICATION_PURPOSES.REGISTER,
        );

        return;
      }

      navigate(
        `/verify-email?email=${encodeURIComponent(
          form.email.trim(),
        )}&purpose=register`,
      );
    } catch (registerError) {
      console.error("Register failed:", registerError);

      if (isEmailVerificationRequired(registerError)) {
        goToVerificationPage(
          navigate,
          registerError,
          form.email.trim(),
          VERIFICATION_PURPOSES.REGISTER,
        );

        return;
      }

      setError(
        getErrorMessage(
          registerError,
          "Registration failed. Please try again.",
        ),
      );
    } finally {
      setCaptchaToken("");

      setCaptchaResetKey(
        (previousKey) => previousKey + 1,
      );
    }
  };

  const handleGoogleLogin = async () => {
    if (oauthLoadingProvider) {
      return;
    }

    try {
      setError("");
      setOauthLoadingProvider("google");

      await startGoogleLogin();
    } catch (googleError) {
      setError(
        getFriendlyApiErrorMessage(
          googleError,
          "Unable to start Google sign up.",
        ),
      );
    } finally {
      setOauthLoadingProvider("");
    }
  };

  const handleGithubLogin = async () => {
    if (oauthLoadingProvider) {
      return;
    }

    try {
      setError("");
      setOauthLoadingProvider("github");

      await startGitHubLogin();
    } catch (githubError) {
      setError(
        getFriendlyApiErrorMessage(
          githubError,
          "Unable to start GitHub sign up.",
        ),
      );
    } finally {
      setOauthLoadingProvider("");
    }
  };

  return (
    <div className="min-h-dvh w-full overflow-hidden bg-[#F8F5EF] text-[#243429]">
      <MotionWrapper
        className="
          grid min-h-dvh w-full grid-cols-1
          lg:grid-cols-[1.1fr_0.9fr]
          xl:grid-cols-[1.14fr_0.86fr]
          2xl:grid-cols-[1.17fr_0.83fr]
        "
      >
        <AuthRoadmapPanel />

        <section className="relative flex min-h-dvh w-full items-center justify-center overflow-hidden bg-[linear-gradient(145deg,#FFFDF8_0%,#F8F5EF_48%,#EEF5F0_100%)] px-5 py-6 sm:px-8 lg:px-8 xl:px-10">
          <div className="pointer-events-none absolute -right-32 top-20 h-96 w-96 rounded-full bg-[#DDEBE2]/60 blur-3xl" />

          <div className="pointer-events-none absolute -left-24 bottom-10 h-80 w-80 rounded-full bg-[#EFE7D8]/70 blur-3xl" />

          <div
            className={`relative z-10 max-h-[calc(100dvh-2rem)] w-full max-w-[520px] overflow-y-auto rounded-[2.25rem] border border-[#D8E6DD] bg-white/[0.86] px-8 py-9 shadow-[0_30px_100px_rgba(80,102,88,0.17)] backdrop-blur-xl transition duration-200 sm:px-10 sm:py-10 lg:px-9 xl:px-11 ${authLoading
                ? "scale-[0.99] opacity-70"
                : ""
              }`}
          >
            <div className="mb-8 lg:hidden">
              <AuthLogo />
            </div>

            <div className="text-center">
              <p className="text-[11px] font-black uppercase tracking-[0.28em] text-[#5C7C68]">
                Start learning
              </p>

              <h1 className="mt-4 text-[31px] font-black tracking-[-0.04em] text-[#243429] sm:text-[35px]">
                Create your account
              </h1>

              <p className="mx-auto mt-3 max-w-[370px] text-[15px] leading-6 text-[#66736A]">
                Start building your personalized career roadmap
                today.
              </p>
            </div>

            {displayedError && (
              <div
                role="alert"
                aria-live="polite"
                className="mt-6 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm font-medium text-red-600"
              >
                {displayedError}
              </div>
            )}

            <form
              onSubmit={handleRegister}
              className="mt-7 space-y-5"
            >
              <div className="relative">
                <label
                  htmlFor="username"
                  className="mb-2 block text-sm font-bold text-[#2E4034]"
                >
                  Username
                </label>

                <input
                  id="username"
                  name="username"
                  type="text"
                  value={form.username}
                  onChange={handleChange}
                  onFocus={() => setUsernameFocused(true)}
                  onBlur={() => setUsernameFocused(false)}
                  placeholder="john_doe"
                  autoComplete="username"
                  className="h-[52px] w-full rounded-2xl border border-[#CBDDD1] bg-[#FFFDF8] px-4 text-sm font-medium text-[#243429] outline-none transition placeholder:text-[#A7B3AB] focus:border-[#5C7C68] focus:bg-white focus:ring-4 focus:ring-[#DDEBE2]"
                  required
                />

                {usernameFocused && (
                  <div className="pointer-events-none absolute left-0 top-[calc(100%+0.5rem)] z-30 w-full rounded-2xl border border-[#D8E6DD] bg-white px-4 py-3 text-xs leading-5 text-[#66736A] shadow-[0_16px_40px_rgba(80,102,88,0.14)]">
                    {USERNAME_REQUIREMENT_MESSAGE}
                  </div>
                )}
              </div>

              <div>
                <label
                  htmlFor="email"
                  className="mb-2 block text-sm font-bold text-[#2E4034]"
                >
                  Email address
                </label>

                <input
                  id="email"
                  name="email"
                  type="email"
                  value={form.email}
                  onChange={handleChange}
                  placeholder="name@example.com"
                  autoComplete="email"
                  className="h-[52px] w-full rounded-2xl border border-[#CBDDD1] bg-[#FFFDF8] px-4 text-sm font-medium text-[#243429] outline-none transition placeholder:text-[#A7B3AB] focus:border-[#5C7C68] focus:bg-white focus:ring-4 focus:ring-[#DDEBE2]"
                  required
                />
              </div>

              <div className="grid grid-cols-1 gap-5 sm:grid-cols-2">
                <div className="relative">
                  <label
                    htmlFor="password"
                    className="mb-2 block text-sm font-bold text-[#2E4034]"
                  >
                    Password
                  </label>

                  <input
                    id="password"
                    name="password"
                    type="password"
                    value={form.password}
                    onChange={handleChange}
                    onFocus={() => setPasswordFocused(true)}
                    onBlur={() => setPasswordFocused(false)}
                    placeholder="••••••••"
                    autoComplete="new-password"
                    className="h-[52px] w-full rounded-2xl border border-[#CBDDD1] bg-[#FFFDF8] px-4 text-sm font-medium text-[#243429] outline-none transition placeholder:text-[#A7B3AB] focus:border-[#5C7C68] focus:bg-white focus:ring-4 focus:ring-[#DDEBE2]"
                    required
                  />

                  {passwordFocused && (
                    <div className="pointer-events-none absolute left-0 top-[calc(100%+0.5rem)] z-30 w-[min(320px,calc(100vw-4rem))] rounded-2xl border border-[#D8E6DD] bg-white px-4 py-3 text-xs leading-5 text-[#66736A] shadow-[0_16px_40px_rgba(80,102,88,0.14)]">
                      {PASSWORD_REQUIREMENT_MESSAGE}
                    </div>
                  )}
                </div>

                <div>
                  <label
                    htmlFor="confirmPassword"
                    className="mb-2 block text-sm font-bold text-[#2E4034]"
                  >
                    Confirm password
                  </label>

                  <input
                    id="confirmPassword"
                    name="confirmPassword"
                    type="password"
                    value={form.confirmPassword}
                    onChange={handleChange}
                    placeholder="••••••••"
                    autoComplete="new-password"
                    className="h-[52px] w-full rounded-2xl border border-[#CBDDD1] bg-[#FFFDF8] px-4 text-sm font-medium text-[#243429] outline-none transition placeholder:text-[#A7B3AB] focus:border-[#5C7C68] focus:bg-white focus:ring-4 focus:ring-[#DDEBE2]"
                    required
                  />
                </div>
              </div>

              <label className="flex cursor-pointer items-start gap-3 text-sm leading-6 text-[#66736A]">
                <input
                  name="agreeTerms"
                  type="checkbox"
                  checked={form.agreeTerms}
                  onChange={handleChange}
                  className="mt-1 h-4 w-4 shrink-0 rounded border-[#CBDDD1] accent-[#5C7C68] focus:ring-[#DDEBE2]"
                />

                <span>
                  I agree to the{" "}
                  <button
                    type="button"
                    className="font-bold text-[#5C7C68] transition hover:text-[#435E4C]"
                  >
                    Terms of Service
                  </button>{" "}
                  and{" "}
                  <button
                    type="button"
                    className="font-bold text-[#5C7C68] transition hover:text-[#435E4C]"
                  >
                    Privacy Policy
                  </button>
                  .
                </span>
              </label>

              {CAPTCHA_ENABLED && (
                <TurnstileCaptcha
                  action="register"
                  resetKey={captchaResetKey}
                  onVerify={handleCaptchaVerify}
                  className="flex justify-center"
                />
              )}

              <button
                type="submit"
                disabled={
                  authLoading ||
                  (CAPTCHA_ENABLED && !captchaToken)
                }
                className="h-[52px] w-full rounded-2xl bg-[#5C7C68] text-sm font-black text-white shadow-[0_18px_36px_rgba(92,124,104,0.22)] transition hover:-translate-y-0.5 hover:bg-[#435E4C] disabled:cursor-not-allowed disabled:opacity-60 disabled:hover:translate-y-0"
              >
                {authLoading
                  ? "Creating account..."
                  : "Create account"}
              </button>
            </form>

            <div className="my-7 flex items-center gap-4">
              <div className="h-px flex-1 bg-[#D8E6DD]" />

              <span className="whitespace-nowrap text-xs font-medium uppercase tracking-[0.05em] text-[#8B9590]">
                Or continue with
              </span>

              <div className="h-px flex-1 bg-[#D8E6DD]" />
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
              Already have an account?{" "}
              <Link
                to="/login"
                className="font-black text-[#5C7C68] transition hover:text-[#435E4C]"
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