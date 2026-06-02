import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { GITHUB_AUTH_URL, GOOGLE_AUTH_URL, registerApi } from "../api/authApi";
import AuthLogo from "../components/auth/AuthLogo";
import { FcGoogle } from "react-icons/fc";
import { FaGithub } from "react-icons/fa";
import { MdArrowOutward } from "react-icons/md";
import MotionWrapper from "../components/auth/MotionWrapper";

export default function RegisterPage() {
  const navigate = useNavigate();

  const [form, setForm] = useState({
    username: "",
    email: "",
    password: "",
    confirmPassword: "",
    agreeTerms: false,
  });

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const handleChange = (e) => {
    const { name, value, type, checked } = e.target;

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

    if (form.password.length < 6) {
      return "Mật khẩu nên có ít nhất 6 ký tự.";
    }

    if (form.password !== form.confirmPassword) {
      return "Confirm password không khớp.";
    }

    if (!form.agreeTerms) {
      return "Bạn cần đồng ý với Terms of Service và Privacy Policy.";
    }

    return "";
  };

  const handleRegister = async (e) => {
    e.preventDefault();

    const validationError = validateForm();

    if (validationError) {
      setError(validationError);
      return;
    }

    try {
      setLoading(true);
      setError("");

      await registerApi({
        Username: form.username,
        Email: form.email,
        Password: form.password,
      });

      navigate("/login");
    } catch (error) {
      console.error("Register failed:", error.response?.data || error);
      setError("Đăng ký thất bại. Vui lòng kiểm tra lại thông tin.");
    } finally {
      setLoading(false);
    }
  };
  const [routeLoading, setRouteLoading] = useState(false);

  const handleGoLogin = () => {
    setTimeout(() => {
      navigate("/login");
    }, 250);
  };

  const handleGoogleLogin = () => {
    window.location.href = GOOGLE_AUTH_URL;
  };

  const handleGithubLogin = () => {
    window.location.href = GITHUB_AUTH_URL;
  };

  return (
    <div className="min-h-screen overflow-hidden bg-linear-to-br from-white via-slate-50 to-blue-50 text-slate-900">
      <MotionWrapper className="grid min-h-dvh grid-cols-1 lg:grid-cols-2">
        {/* Left form panel */}
        <section className="flex items-center justify-center px-10 py-6 border-r border-[#C3C6D7]">
          <div
            className={`w-full max-w-100 transition duration-200 ${
              routeLoading ? "scale-[0.99] opacity-70" : ""
            }`}
          >
            <div className="mb-10 lg:hidden">
              <AuthLogo />
            </div>

            <div>
              <h1 className="text-3xl font-bold tracking-tight text-slate-900">
                Create your account
              </h1>

              <p className="mt-3 text-sm text-slate-500">
                Start building your personalized career roadmap today.
              </p>
            </div>

            {error && (
              <div className="mt-6 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-600">
                {error}
              </div>
            )}

            <form onSubmit={handleRegister} className="mt-4 space-y-4">
              <div>
                <label className="mb-2 block text-sm font-semibold text-slate-700">
                  Username
                </label>

                <input
                  name="username"
                  value={form.username}
                  onChange={handleChange}
                  placeholder="John Doe"
                  className="h-10 w-full rounded-xl border border-slate-300 bg-white px-4 text-slate-900 outline-none transition placeholder:text-slate-300 focus:border-blue-500 focus:ring-4 focus:ring-blue-100"
                  required
                />
              </div>

              <div>
                <label className="mb-2 block text-sm font-semibold text-slate-700">
                  Email Address
                </label>

                <input
                  name="email"
                  type="email"
                  value={form.email}
                  onChange={handleChange}
                  placeholder="name@company.com"
                  className="h-12 w-full rounded-xl border border-slate-300 bg-white px-4 text-slate-900 outline-none transition placeholder:text-slate-300 focus:border-blue-500 focus:ring-4 focus:ring-blue-100"
                  required
                />
              </div>

              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <div>
                  <label className="mb-2 block text-sm font-semibold text-slate-700">
                    Password
                  </label>

                  <input
                    name="password"
                    type="password"
                    value={form.password}
                    onChange={handleChange}
                    placeholder="••••••••"
                    className="h-10 w-full rounded-xl border border-slate-300 bg-white px-4 text-slate-900 outline-none transition placeholder:text-slate-300 focus:border-blue-500 focus:ring-4 focus:ring-blue-100"
                    required
                  />
                </div>

                <div>
                  <label className="mb-2 block text-sm font-semibold text-slate-700">
                    Confirm Password
                  </label>

                  <input
                    name="confirmPassword"
                    type="password"
                    value={form.confirmPassword}
                    onChange={handleChange}
                    placeholder="••••••••"
                    className="h-10 w-full rounded-xl border border-slate-300 bg-white px-4 text-slate-900 outline-none transition placeholder:text-slate-300 focus:border-blue-500 focus:ring-4 focus:ring-blue-100"
                    required
                  />
                </div>
              </div>

              <label className="flex items-start gap-3 text-sm text-slate-500">
                <input
                  name="agreeTerms"
                  type="checkbox"
                  checked={form.agreeTerms}
                  onChange={handleChange}
                  className="mt-1 h-4 w-4 rounded border-slate-300 text-blue-600 focus:ring-blue-500"
                />

                <span>
                  I agree to the{" "}
                  <button
                    type="button"
                    className="font-medium text-blue-600 hover:text-blue-700"
                  >
                    Terms of Service
                  </button>{" "}
                  and{" "}
                  <button
                    type="button"
                    className="font-medium text-blue-600 hover:text-blue-700"
                  >
                    Privacy Policy
                  </button>
                  .
                </span>
              </label>

              <button
                type="submit"
                disabled={loading}
                className="h-[52px] w-full rounded-xl bg-blue-600 font-semibold text-white shadow-lg shadow-blue-600/20 transition hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-60"
              >
                {loading ? "Creating account..." : "Create account"}
              </button>
            </form>

            <div className="my-6 flex items-center gap-4">
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

            <p className="mt-10 text-center text-sm text-slate-500">
              Already have an account?{" "}
              <Link
                to="/login"
                className="font-semibold text-blue-600 hover:text-blue-700"
              >
                Sign in
              </Link>
            </p>
          </div>
        </section>

        {/* Right marketing panel */}
        <div className="relative hidden overflow-hidden px-25 py-6 lg:flex lg:flex-col lg:justify-center">
          <div className="absolute inset-0 bg-linear-to-br from-white via-slate-50 to-blue-50" />

          <div className="relative z-10 max-w-xl">
            <AuthLogo />

            <h2 className="mt-10 text-3xl font-bold leading-tight tracking-tight text-slate-900">
              Start your personalized roadmap today.
            </h2>

            <p className="mt-4 text-xl leading-relaxed text-slate-600">
              Join 50,000+ developers using AI-driven insights to navigate their
              career growth, master new stacks, and land dream roles.
            </p>

            <div className="relative mt-8 h-80">
              <div className="absolute right-0 top-10 w-70 rounded-2xl border border-slate-200 bg-white p-8 shadow-xl shadow-slate-200/70">
                <div className="mb-6 flex items-center justify-between">
                  <div>
                    <p className="text-sm font-semibold text-slate-500">
                      Career Progress
                    </p>
                    <h3 className="mt-1 text-xl font-bold text-slate-900">
                      Backend Developer
                    </h3>
                  </div>

                  <div className="flex h-12 w-12 items-center justify-center rounded-full bg-emerald-50 text-emerald-600">
                    <MdArrowOutward className="text-2xl font-bold" />
                  </div>
                </div>

                <div className="flex items-end gap-4 border-b border-slate-200 pb-6">
                  <div className="h-20 w-10 rounded-t-lg bg-blue-600" />
                  <div className="h-14 w-10 rounded-t-lg bg-slate-100" />
                  <div className="h-24 w-10 rounded-t-lg bg-slate-100" />
                  <div className="h-10 w-10 rounded-t-lg bg-slate-100" />
                </div>

                <p className="mt-5 text-sm text-slate-500">
                  Recommended by 4 mentors
                </p>
              </div>

              <div className="absolute left-0 top-0 w-75 rounded-2xl border border-slate-200 bg-white p-6 shadow-xl shadow-slate-200/70">
                <div className="mb-4 inline-flex rounded-md bg-emerald-200 px-3 py-1 text-xs font-bold tracking-widest text-emerald-800">
                  PROJECT
                </div>

                <div className="h-32 rounded-xl bg-slate-800 p-4">
                  <div className="mb-2 h-2 w-24 rounded-full bg-slate-500" />
                  <div className="mb-2 h-2 w-36 rounded-full bg-slate-600" />
                  <div className="mb-2 h-2 w-28 rounded-full bg-slate-500" />
                  <div className="mb-2 h-2 w-40 rounded-full bg-slate-700" />
                  <div className="h-2 w-20 rounded-full bg-slate-600" />
                </div>

                <div className="mt-4 flex items-end justify-between">
                  <div>
                    <h3 className="font-bold text-slate-900">E-Commerce API</h3>

                    <div className="mt-2 h-2 w-40 overflow-hidden rounded-full bg-blue-100">
                      <div className="h-full w-[85%] rounded-full bg-emerald-600" />
                    </div>
                  </div>

                  <span className="text-xs font-bold text-emerald-700">
                    85%
                  </span>
                </div>
              </div>

              <div className="absolute bottom-0 right-0 rounded-full border border-slate-200 bg-white px-6 py-4 text-sm font-medium text-slate-700 shadow-lg">
                <span className="mr-3 inline-block h-2 w-2 rounded-full bg-emerald-600" />
                AI Mentor: "You're 3 skills away from Senior Dev"
              </div>
            </div>
          </div>
        </div>
      </MotionWrapper>
    </div>
  );
}
