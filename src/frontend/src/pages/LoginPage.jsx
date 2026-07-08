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

  const clearAuthError = useAuthStore(
    (state) => state.clearAuthError,
  );

  const authLoading = useAuthStore(
    (state) => state.authLoading,
  );

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

  const [
    oauthLoadingProvider,
    setOauthLoadingProvider,
  ] = useState("");

  const handleCaptchaVerify = useCallback((token) => {
    setCaptchaToken(token);
    setCaptchaError("");
    setError("");
  }, []);

  const handleChange = (event) => {
    const { name, value } = event.target;

    if (authError) {
      clearAuthError();
    }

    if (error) {
      setError("");
    }

    if (captchaError) {
      setCaptchaError("");
    }

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

      setCaptchaResetKey(
        (previousKey) => previousKey + 1,
      );
    }
  };

  const handleGoRegister = () => {
    setTimeout(() => {
      navigate("/register");
    }, 250);
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
          "Unable to start Google sign in.",
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
          "Unable to start GitHub sign in.",
        ),
      );
    } finally {
      setOauthLoadingProvider("");
    }
  };

  const displayedError =
    error || authError || oauthError || captchaError;

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

        <section
          className="
            relative flex min-h-dvh items-center justify-center
            overflow-hidden
            bg-[linear-gradient(145deg,#FFFDF8_0%,#F8F5EF_48%,#EEF5F0_100%)]
            px-4 py-5
            sm:px-6
            lg:px-6
            xl:px-8
          "
        >
          <div
            className="
              pointer-events-none absolute
              -right-24 top-20
              h-72 w-72
              rounded-full
              bg-[#DDEBE2]/60
              blur-3xl
            "
          />

          <div
            className="
              pointer-events-none absolute
              -left-20 bottom-10
              h-60 w-60
              rounded-full
              bg-[#EFE7D8]/70
              blur-3xl
            "
          />

          <div
            className={`
              relative z-10
              w-full max-w-[400px]
              rounded-[1.5rem]
              border border-[#D8E6DD]
              bg-white/[0.85]
              px-6 py-6
              shadow-[0_20px_60px_rgba(80,102,88,0.14)]
              backdrop-blur-xl
              transition duration-200
              sm:px-7 sm:py-7
              ${authLoading
                ? "scale-[0.99] opacity-70"
                : ""
              }
            `}
          >
            <div className="mb-5 lg:hidden">
              <AuthLogo />
            </div>

            <div className="text-center">
              <p
                className="
                  text-[9px] font-black
                  uppercase tracking-[0.24em]
                  text-[#5C7C68]
                "
              >
                Welcome back
              </p>

              <h1
                className="
                  mt-2.5
                  text-[25px] font-black
                  tracking-[-0.04em]
                  text-[#243429]
                  sm:text-[27px]
                "
              >
                Sign in to TechMap
              </h1>

              <p
                className="
                  mx-auto mt-2
                  max-w-[300px]
                  text-[13px] leading-5
                  text-[#66736A]
                "
              >
                Continue your personalized learning roadmap.
              </p>
            </div>

            {displayedError && (
              <div
                role="alert"
                className="
                  mt-4
                  rounded-xl
                  border border-red-200
                  bg-red-50
                  px-3 py-2.5
                  text-[12px] font-medium
                  text-red-600
                "
              >
                {displayedError}
              </div>
            )}

            {verified === "1" && (
              <div
                role="status"
                className="
                  mt-4
                  rounded-xl
                  border border-[#B8DEC8]
                  bg-[#EEF8F2]
                  px-3 py-2.5
                  text-[12px] font-medium
                  text-[#3D7A50]
                "
              >
                Your email has been verified. You can now sign in.
              </div>
            )}

            <form
              onSubmit={handleLogin}
              className="mt-5 space-y-3.5"
            >
              <div>
                <label
                  htmlFor="emailOrUsername"
                  className="
                    mb-1.5 block
                    text-[13.5px] font-bold
                    text-[#2E4034]
                  "
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
                  className="
                    h-[40px] w-full
                    rounded-xl
                    border border-[#CBDDD1]
                    bg-[#FFFDF8]
                    px-3.5
                    text-[11px] font-medium
                    text-[#243429]
                    outline-none
                    transition
                    placeholder:text-[14px]
                    placeholder:text-[#A7B3AB]
                    focus:border-[#5C7C68]
                    focus:bg-white
                    focus:ring-4
                    focus:ring-[#DDEBE2]
                  "
                  required
                />
              </div>

              <div>
                <div
                  className="
                    mb-1.5 flex
                    items-center justify-between
                    gap-4
                  "
                >
                  <label
                    htmlFor="password"
                    className="
                      block
                      text-[13.5px] font-bold
                      text-[#2E4034]
                    "
                  >
                    Password
                  </label>

                  <button
                    type="button"
                    disabled
                    aria-disabled="true"
                    title="Forgot password is not available yet"
                    className="
                      cursor-not-allowed
                      !text-[13px] font-bold
                      text-[#5C7C68]
                      opacity-70
                    "
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
                    className="
                      h-[40px] w-full
                      rounded-xl
                      border border-[#CBDDD1]
                      bg-[#FFFDF8]
                      px-3.5 pr-14
                      text-[11px] font-medium
                      text-[#243429]
                      outline-none
                      transition
                      placeholder:text-[19px]
                      placeholder:text-[#A7B3AB]
                      focus:border-[#5C7C68]
                      focus:bg-white
                      focus:ring-4
                      focus:ring-[#DDEBE2]
                    "
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
                    className="
                      absolute right-3.5 top-1/2
                      -translate-y-1/2
                      !text-[10px] font-black
                      uppercase tracking-[0.03em]
                      text-[#7C8D82]
                      transition
                      hover:text-[#435E4C]
                    "
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
                disabled={
                  authLoading ||
                  (CAPTCHA_ENABLED && !captchaToken)
                }
                className="
                  h-[40px] w-full
                  rounded-xl
                  bg-[#5C7C68]
                  text-[13px] font-black
                  text-white
                  shadow-[0_12px_24px_rgba(92,124,104,0.18)]
                  transition
                  hover:-translate-y-0.5
                  hover:bg-[#435E4C]
                  disabled:cursor-not-allowed
                  disabled:opacity-60
                  disabled:hover:translate-y-0
                "
              >
                {authLoading ? "Signing in..." : "Sign in"}
              </button>
            </form>

            <div className="my-5 flex items-center gap-3">
              <div className="h-px flex-1 bg-[#E5ECE7]" />

              <span
                className="
                  whitespace-nowrap
                  text-[10px] font-medium
                  uppercase tracking-[0.04em]
                  text-[#8B9590]
                "
              >
                Or continue with
              </span>

              <div className="h-px flex-1 bg-[#E5ECE7]" />
            </div>

            <div className="grid grid-cols-2 gap-2.5">
              <button
                type="button"
                onClick={handleGoogleLogin}
                disabled={authLoading || oauthIsLoading}
                className="
                  flex h-[40px]
                  items-center justify-center
                  gap-2
                  rounded-xl
                  border border-[#D8E6DD]
                  bg-[#FFFDF8]
                  text-[12px] font-bold
                  text-[#2E4034]
                  shadow-sm
                  transition
                  hover:-translate-y-0.5
                  hover:bg-white
                  hover:shadow-md
                  disabled:cursor-not-allowed
                  disabled:opacity-60
                  disabled:hover:translate-y-0
                "
              >
                <FcGoogle className="text-[15px]" />

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
                className="
                  flex h-[40px]
                  items-center justify-center
                  gap-2
                  rounded-xl
                  border border-[#243429]
                  bg-[#243429]
                  text-[12px] font-bold
                  text-white
                  shadow-[0_10px_20px_rgba(36,52,41,0.15)]
                  transition
                  hover:-translate-y-0.5
                  hover:bg-[#17211B]
                  disabled:cursor-not-allowed
                  disabled:opacity-60
                  disabled:hover:translate-y-0
                "
              >
                <FaGithub className="text-[15px] text-white" />

                <span>
                  {oauthLoadingProvider === "github"
                    ? "Starting..."
                    : "GitHub"}
                </span>
              </button>
            </div>

            <p
              className="
                mt-5
                text-center
                text-[12px]
                text-[#66736A]
              "
            >
              Don&apos;t have an account?{" "}
              <button
                type="button"
                onClick={handleGoRegister}
                disabled={authLoading}
                className="
                  font-black
                  text-[#5C7C68]
                  transition
                  hover:text-[#435E4C]
                  disabled:cursor-not-allowed
                  disabled:opacity-60
                "
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