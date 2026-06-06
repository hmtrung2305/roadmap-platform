import { useRef, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { GITHUB_AUTH_URL, GOOGLE_AUTH_URL } from "../api/authApi";
import { useAuthStore } from "../stores/useAuthStore";
import AuthLogo from "../components/auth/AuthLogo";
import { FcGoogle } from "react-icons/fc";
import { FaGithub } from "react-icons/fa";
import MotionWrapper from "../components/auth/MotionWrapper";
import AuthRoadmapPanel from "../components/auth/AuthRoadmapPanel";

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

  const lastSubmittedFormRef = useRef(null);

  const [error, setError] = useState("");

  const displayError = error || authError;

  const handleChange = (event) => {
    const { name, value, type, checked } = event.target;

    setForm((prev) => ({
      ...prev,
      [name]: type === "checkbox" ? checked : value,
    }));
  };

  const validateForm = () => {
    const username = form.username.trim();
    const email = form.email.trim();

    if (!username) {
      return "Please enter your username.";
    }

    if (username.length < 3) {
      return "Username must be at least 3 characters long.";
    }

    if (!email) {
      return "Please enter your email address.";
    }

    if (!/^\S+@\S+\.\S+$/.test(email)) {
      return "Please enter a valid email address.";
    }

    if (!form.password) {
      return "Please enter your password.";
    }

    if (form.password.length < 8) {
      return "Password must be at least 8 characters long.";
    }

    if (form.password !== form.confirmPassword) {
      return "Confirm password does not match.";
    }

    if (!form.agreeTerms) {
      return "You need to agree to the Terms of Service and Privacy Policy.";
    }

    return "";
  };

 const handleRegister = async (event) => {
  event.preventDefault();

  const currentForm = {
    ...form,
    username: form.username,
    email: form.email,
    password: form.password,
    confirmPassword: form.confirmPassword,
    agreeTerms: form.agreeTerms,
  };

  lastSubmittedFormRef.current = currentForm;

  const validationError = validateForm();

  if (validationError) {
    setError(validationError);
    return;
  }

  try {
    setError("");
    clearAuthError();

    await register({
      Username: currentForm.username.trim(),
      Email: currentForm.email.trim(),
      Password: currentForm.password,
    });

    navigate(
      `/verify-email?email=${encodeURIComponent(currentForm.email.trim())}`,
    );
  } catch (error) {
    console.error("Register failed:", error);

    setForm(lastSubmittedFormRef.current);

    setError(error?.message || "Register failed. Please try again.");
  }
};

  const handleGoogleLogin = () => {
    window.location.href = GOOGLE_AUTH_URL;
  };

  const handleGithubLogin = () => {
    window.location.href = GITHUB_AUTH_URL;
  };

  return (
    <div className="min-h-dvh overflow-hidden bg-[#F7F1E8] text-slate-900">
      <MotionWrapper className="grid min-h-dvh grid-cols-1 lg:grid-cols-[1.08fr_0.92fr]">
        <AuthRoadmapPanel />

        <section className="flex min-h-dvh items-center justify-center bg-white px-6 py-5">
          <div
            className={`w-full max-w-[420px] rounded-3xl border border-slate-100 bg-white px-6 py-6 shadow-xl shadow-slate-200/60 transition duration-200 ${
              authLoading ? "scale-[0.99] opacity-70" : ""
            }`}
          >
            <div className="mb-7 lg:hidden">
              <AuthLogo />
            </div>

            <div>
              <p className="text-xs font-bold uppercase tracking-[0.22em] text-emerald-600">
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
              <div className="mt-5 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-600">
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
                  className="h-10 w-full rounded-xl border border-slate-300 bg-white px-4 text-sm text-slate-900 outline-none transition placeholder:text-slate-300 focus:border-emerald-500 focus:ring-4 focus:ring-emerald-100"
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
                  className="h-10 w-full rounded-xl border border-slate-300 bg-white px-4 text-sm text-slate-900 outline-none transition placeholder:text-slate-300 focus:border-emerald-500 focus:ring-4 focus:ring-emerald-100"
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
                    className="h-10 w-full rounded-xl border border-slate-300 bg-white px-4 text-sm text-slate-900 outline-none transition placeholder:text-slate-300 focus:border-emerald-500 focus:ring-4 focus:ring-emerald-100"
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
                    className="h-10 w-full rounded-xl border border-slate-300 bg-white px-4 text-sm text-slate-900 outline-none transition placeholder:text-slate-300 focus:border-emerald-500 focus:ring-4 focus:ring-emerald-100"
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
                  className="mt-1 h-4 w-4 rounded border-slate-300 text-emerald-600 focus:ring-emerald-500"
                />

                <span>
                  I agree to the{" "}
                  <button
                    type="button"
                    className="font-semibold text-emerald-600 transition hover:text-emerald-700"
                  >
                    Terms of Service
                  </button>{" "}
                  and{" "}
                  <button
                    type="button"
                    className="font-semibold text-emerald-600 transition hover:text-emerald-700"
                  >
                    Privacy Policy
                  </button>
                  .
                </span>
              </label>

              <button
                type="submit"
                disabled={authLoading}
                className="h-11 w-full rounded-xl bg-emerald-700 text-sm font-semibold text-white shadow-lg shadow-emerald-700/20 transition hover:bg-emerald-800 disabled:cursor-not-allowed disabled:opacity-60"
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
                className="font-semibold text-emerald-600 transition hover:text-emerald-700"
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