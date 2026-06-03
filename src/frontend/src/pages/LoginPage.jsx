import { useState } from "react";
import { useNavigate, useSearchParams } from "react-router-dom";
import { GITHUB_AUTH_URL, GOOGLE_AUTH_URL } from "../api/authApi";
import AuthLogo from "../components/auth/AuthLogo";
import AuthRoadmapPanel from "../components/auth/AuthRoadmapPanel";
import { FcGoogle } from "react-icons/fc";
import { FaGithub } from "react-icons/fa";
import { useAuthStore } from "../stores/useAuthStore";
import MotionWrapper from "../components/auth/MotionWrapper";

export default function LoginPage() {
  const navigate = useNavigate();

  const authError = useAuthStore((state) => state.authError);
  const clearAuthError = useAuthStore((state) => state.clearAuthError);
  const authLoading = useAuthStore((state) => state.authLoading);
  const login = useAuthStore((state) => state.login);

  const [searchParams] = useSearchParams();
  const oauthError = searchParams.get("oauthError");

  const [form, setForm] = useState({
    email: "",
    password: "",
  });

  const [showPassword, setShowPassword] = useState(false);

  const handleChange = (e) => {
    const { name, value } = e.target;

    if (authError) {
      clearAuthError();
    }

    setForm((prev) => ({
      ...prev,
      [name]: value,
    }));
  };

  const handleLogin = async (e) => {
    e.preventDefault();

    try {
      await login(form);

      setTimeout(() => {
        navigate("/home");
      }, 250);
    } catch (error) {
      console.log("Login failed:", error.response?.data || error);
    }
  };

  const handleGoRegister = () => {
    setTimeout(() => {
      navigate("/register");
    }, 250);
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

        <section className="flex min-h-dvh items-center justify-center bg-white px-6 py-6">
          <div
            className={`w-full max-w-[390px] rounded-3xl border border-slate-100 bg-white px-6 py-7 shadow-xl shadow-slate-200/60 transition duration-200 ${
              authLoading ? "scale-[0.99] opacity-70" : ""
            }`}
          >
            <div className="mb-8 lg:hidden">
              <AuthLogo />
            </div>

            <div>
              <p className="text-xs font-bold uppercase tracking-[0.22em] text-blue-600">
                Welcome back
              </p>

              <h1 className="mt-3 text-2xl font-bold tracking-tight text-slate-900">
                Sign in to TechMap
              </h1>

              <p className="mt-2 text-sm leading-6 text-slate-500">
                Continue your personalized learning roadmap.
              </p>
            </div>

            {(authError || oauthError) && (
              <div className="mt-5 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-600">
                {authError || oauthError}
              </div>
            )}

            <form onSubmit={handleLogin} className="mt-6 space-y-4">
              <div>
                <label className="mb-2 block text-sm font-semibold text-slate-700">
                  Email address
                </label>

                <input
                  name="email"
                  type="email"
                  value={form.email}
                  onChange={handleChange}
                  placeholder="name@example.com"
                  className="h-10 w-full rounded-xl border border-slate-300 px-4 text-sm text-slate-900 outline-none transition placeholder:text-slate-300 focus:border-blue-500 focus:ring-4 focus:ring-blue-100"
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
                    className="text-xs font-semibold text-blue-600 transition hover:text-blue-700"
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
                    placeholder="••••••••"
                    className="h-10 w-full rounded-xl border border-slate-300 px-4 pr-12 text-sm text-slate-900 outline-none transition placeholder:text-slate-300 focus:border-blue-500 focus:ring-4 focus:ring-blue-100"
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

              <button
                type="submit"
                disabled={authLoading}
                className="h-11 w-full rounded-xl bg-blue-700 text-sm font-semibold text-white shadow-lg shadow-blue-700/20 transition hover:bg-blue-800 disabled:cursor-not-allowed disabled:opacity-60"
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

            <p className="mt-6 text-center text-sm text-slate-500">
              Don't have an account?{" "}
              <button
                type="button"
                onClick={handleGoRegister}
                disabled={authLoading}
                className="font-semibold text-blue-600 transition hover:text-blue-700 disabled:opacity-60"
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