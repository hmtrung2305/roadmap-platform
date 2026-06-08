import { useState } from "react";
import { X } from "lucide-react";
import { linkLocalLoginApi } from "../../api/authProviderApi";

export default function AddLocalLoginModal({
  defaultEmail,
  onClose,
  onSuccess,
}) {
  const [form, setForm] = useState({
    email: defaultEmail || "",
    password: "",
    confirmPassword: "",
  });

  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const handleChange = (e) => {
    const { name, value } = e.target;

    if (error) {
      setError("");
    }

    setForm((prev) => ({
      ...prev,
      [name]: value,
    }));
  };

  const validateForm = () => {
    if (!form.email.trim()) {
      return "Please enter your email address.";
    }

    if (form.password.length < 6) {
      return "Password must be at least 6 characters.";
    }

    if (form.password !== form.confirmPassword) {
      return "Password confirmation does not match.";
    }

    return "";
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    const validationError = validateForm();

    if (validationError) {
      setError(validationError);
      return;
    }

    try {
      setLoading(true);
      setError("");

      await linkLocalLoginApi({
        Email: form.email,
        Password: form.password,
      });

      onSuccess?.(form.email);
    } catch (error) {
      console.error("Add local login failed:", error.response?.data || error);

      const serverMessage =
        error.response?.data?.message ||
        "Unable to add email and password login.";

      setError(serverMessage);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 z-[100] flex items-center justify-center bg-slate-950/40 px-4">
      <div className="w-full max-w-lg rounded-lg border border-slate-200 bg-white p-6 shadow-2xl shadow-slate-900/20">
        <div className="flex items-start justify-between gap-4">
          <div>
            <h2 className="text-xl font-bold tracking-tight text-slate-900">
              Add password login
            </h2>

            <p className="mt-1 text-sm leading-6 text-slate-500">
              Add an email and password so you can sign in without a social
              account.
            </p>
          </div>

          <button
            type="button"
            onClick={onClose}
            disabled={loading}
            className="rounded-lg p-2 text-slate-400 transition hover:bg-slate-100 hover:text-slate-600 disabled:opacity-60"
          >
            <X size={18} />
          </button>
        </div>

        {error && (
          <div className="mt-5 rounded-lg border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-600">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="mt-6 space-y-4">
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
              className="h-11 w-full rounded-lg border border-slate-300 bg-white px-4 text-sm text-slate-900 outline-none transition placeholder:text-slate-300 focus:border-[#2FA084] focus:ring-4 focus:ring-[#6FCF97]/20"
            />
          </div>

          <div>
            <label className="mb-2 block text-sm font-semibold text-slate-700">
              Password
            </label>

            <input
              name="password"
              type="password"
              value={form.password}
              onChange={handleChange}
              placeholder="Create a password"
              className="h-11 w-full rounded-lg border border-slate-300 bg-white px-4 text-sm text-slate-900 outline-none transition placeholder:text-slate-300 focus:border-[#2FA084] focus:ring-4 focus:ring-[#6FCF97]/20"
            />
          </div>

          <div>
            <label className="mb-2 block text-sm font-semibold text-slate-700">
              Confirm password
            </label>

            <input
              name="confirmPassword"
              type="password"
              value={form.confirmPassword}
              onChange={handleChange}
              placeholder="Confirm your password"
              className="h-11 w-full rounded-lg border border-slate-300 bg-white px-4 text-sm text-slate-900 outline-none transition placeholder:text-slate-300 focus:border-[#2FA084] focus:ring-4 focus:ring-[#6FCF97]/20"
            />
          </div>

          <div className="flex justify-end gap-3 pt-3">
            <button
              type="button"
              onClick={onClose}
              disabled={loading}
              className="h-10 rounded-lg bg-slate-100 px-5 text-sm font-semibold text-slate-700 transition hover:bg-slate-200 disabled:opacity-60"
            >
              Cancel
            </button>

            <button
              type="submit"
              disabled={loading}
              className="h-10 rounded-lg bg-[#2FA084] px-5 text-sm font-semibold text-white shadow-lg shadow-emerald-900/10 transition hover:bg-[#1F6F5F] disabled:cursor-not-allowed disabled:opacity-60"
            >
              {loading ? "Sending code..." : "Continue"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}