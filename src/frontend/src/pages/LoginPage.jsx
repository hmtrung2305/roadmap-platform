import { useState } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { GITHUB_AUTH_URL, GOOGLE_AUTH_URL } from "../api/authApi";
import AuthLogo from "../components/auth/AuthLogo";
import { FcGoogle } from "react-icons/fc";
import { FaGithub } from "react-icons/fa";
import { MdOutlineLockClock } from "react-icons/md";
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
    <div className="min-h-dvh overflow-y-hidden bg-slate-50 text-slate-900">
      <MotionWrapper className="grid min-h-screen grid-cols-1 lg:grid-cols-2">
        {/* Left visual panel */}
        <div className="hidden bg-slate-50 px-25 py-10 lg:flex lg:flex-col border-r-2 border-[#C3C6D7]">
          <AuthLogo />

          <div className="mt-6 max-w-xl">
            <h2 className="text-3xl font-bold leading-tight tracking-tight text-slate-900">
              Build your software engineering path with clarity.
            </h2>

            <p className="mt-3 max-w-lg text-lg leading-relaxed text-slate-600">
              Choose a role, follow a structured roadmap, and track your
              learning progress step by step.
            </p>
          </div>

          <div className="relative mt-8 flex flex-1 items-center justify-center">
            <div className="absolute h-105 w-105 rounded-full border border-dashed border-slate-200" />
            <div className="absolute h-75 w-75 rounded-full border border-slate-200" />

            <div className="relative z-10 flex flex-col items-center">
              <div className="rounded-xl border-2 border-amber-400 bg-amber-50 px-12 py-2  text-center shadow-sm">
                <p className="text-xs font-bold tracking-widest text-amber-800">
                  MILESTONE 1
                </p>
                <h3 className="mt-1 text-lg font-bold text-amber-950">
                  Internet
                  <br />
                  Fundamentals
                </h3>
              </div>

              <div className="h-12 w-px bg-slate-300" />

              <div className="rounded-xl border-2 border-blue-400 bg-blue-50 px-12 py-2 text-center shadow-sm">
                <p className="text-xs font-bold tracking-widest text-blue-800">
                  MILESTONE 2
                </p>
                <h3 className="mt-1 text-lg font-bold text-slate-800">
                  JavaScript Core
                </h3>
              </div>

              <div className="h-12 w-px bg-slate-300" />

              <div className="rounded-xl border border-slate-300 bg-slate-100 px-12 py-2 text-center text-slate-400 shadow-sm">
                <div className="flex gap-1 items-center">
                  <MdOutlineLockClock className="text-xl" />
                  <p className="text-xs font-bold tracking-widest">
                    MILESTONE 3
                  </p>
                </div>
                <h3 className="mt-1 text-xg font-bold">React Basics</h3>
              </div>
            </div>

            <div className="absolute left-8 top-28 rounded-full border bg-white px-5 py-2 text-sm shadow-sm">
              HTTP
            </div>

            <div className="absolute right-20 top-28 rounded-full border bg-white px-5 py-2 text-sm shadow-sm">
              DNS
            </div>

            <div className="absolute left-4 bottom-40 rounded-full border bg-white px-5 py-2 text-sm shadow-sm">
              DOM API
            </div>

            <div className="absolute right-0 bottom-44 rounded-full border bg-white px-5 py-2 text-sm shadow-sm">
              Async/Await
            </div>
          </div>
        </div>

        {/* Right form panel */}
        <section className="flex items-center justify-center bg-white px-6 py-6">
          <div
            className={`w-full max-w-100 transition duration-200 ${
              authLoading ? "scale-[0.99] opacity-70" : ""
            }`}
          >
            <div className="mb-10 lg:hidden">
              <AuthLogo />
            </div>

            <div>
              <h1 className="text-3xl font-bold tracking-tight text-slate-900">
                Welcome back
              </h1>

              <p className="mt-3 text-base text-slate-500">
                Continue your personalized learning roadmap.
              </p>
            </div>

            {(authError || oauthError ) && (
              <div className="mt-6 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-600">
                {authError || oauthError }
              </div>
            )}

            <form onSubmit={handleLogin} className="mt-7 space-y-4">
              <div>
                <label className="mb-2 block text-sm font-medium text-slate-700">
                  Email address
                </label>

                <input
                  name="email"
                  type="email"
                  value={form.email}
                  onChange={handleChange}
                  placeholder="name@example.com"
                  className="h-11 w-full rounded-xl border border-slate-300 px-4 text-slate-900 outline-none transition focus:border-blue-500 focus:ring-4 focus:ring-blue-100"
                  required
                />
              </div>

              <div>
                <div className="mb-2 flex items-center justify-between">
                  <label className="block text-sm font-medium text-slate-700">
                    Password
                  </label>

                  <button
                    type="button"
                    className="text-xs font-semibold text-blue-600 hover:text-blue-700"
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
                    className="h-11 w-full rounded-xl border border-slate-300 px-4 pr-12 text-slate-900 outline-none transition focus:border-blue-500 focus:ring-4 focus:ring-blue-100"
                    required
                  />

                  <button
                    type="button"
                    onClick={() => setShowPassword((prev) => !prev)}
                    className="absolute right-4 top-1/2 -translate-y-1/2 text-sm text-slate-400 hover:text-slate-600"
                  >
                    {showPassword ? "Hide" : "Show"}
                  </button>
                </div>
              </div>

              <button
                type="submit"
                disabled={authLoading}
                className="h-13 w-full rounded-xl bg-blue-700 py-3 font-semibold text-white shadow-lg shadow-blue-700/20 transition hover:bg-blue-800 disabled:cursor-not-allowed disabled:opacity-60"
              >
                {authLoading ? "Signing in..." : "Sign in"}
              </button>
            </form>

            <div className="my-8 flex items-center gap-4">
              <div className="h-px flex-1 bg-slate-200" />
              <span className="text-xs font-semibold tracking-widest text-slate-500">
                OR CONTINUE WITH
              </span>
              <div className="h-px flex-1 bg-slate-200" />
            </div>

            <div className="grid grid-cols-2 gap-3">
              <button
                type="button"
                onClick={handleGoogleLogin}
                className="flex h-11 items-center justify-center gap-2.5 rounded-full border border-[#dadce0] bg-white font-semibold text-slate-700 shadow-sm transition hover:bg-[#f8fafd] hover:shadow-md"
              >
                <FcGoogle className="text-xl" />
                Google
              </button>

              <button
                type="button"
                onClick={handleGithubLogin}
                className="flex h-11 items-center justify-center gap-2.5 rounded-full border border-slate-900 bg-slate-950 font-semibold text-white shadow-lg shadow-slate-900/15 transition hover:bg-black"
              >
                <FaGithub className="text-xl text-white" />
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
