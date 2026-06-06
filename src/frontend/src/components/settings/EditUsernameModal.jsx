import { useState } from "react";
import { X } from "lucide-react";
import { toast } from "react-toastify";
import { updateCurrentUserApi } from "../../api/authApi";

export default function EditUsernameModal({
  currentUsername,
  onClose,
  onSuccess,
}) {
  const [username, setUsername] = useState(currentUsername || "");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const validateUsername = () => {
    const trimmedUsername = username.trim();

    if (!trimmedUsername) {
      return "Username is required.";
    }

    if (trimmedUsername.length < 3 || trimmedUsername.length > 40) {
      return "Username must be between 3 and 40 characters.";
    }

    if (!/^[a-zA-Z0-9._-]+$/.test(trimmedUsername)) {
      return "Username may only contain letters, numbers, ., _, and -.";
    }

    if (trimmedUsername === currentUsername) {
      return "Please enter a different username.";
    }

    return "";
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    const validationError = validateUsername();

    if (validationError) {
      setError(validationError);
      return;
    }

    try {
      setLoading(true);
      setError("");

      await updateCurrentUserApi({
        Username: username.trim(),
      });

      await onSuccess?.();
      toast.success("Username updated successfully.");
      onClose();
    } catch (error) {
      console.error("Update username failed:", error.response?.data || error);

      const serverMessage =
        error.response?.data?.message ||
        error.response?.data?.Message ||
        "Unable to update username.";

      setError(serverMessage);
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 z-[100] flex items-center justify-center bg-slate-950/40 px-4">
      <div className="w-full max-w-lg rounded-3xl border border-slate-200 bg-white p-6 shadow-2xl shadow-slate-900/20">
        <div className="flex items-start justify-between gap-4">
          <div>
            <h2 className="text-xl font-bold tracking-tight text-slate-900">
              Update username
            </h2>

            <p className="mt-1 text-sm leading-6 text-slate-500">
              Your username is used across your TechMap account.
            </p>
          </div>

          <button
            type="button"
            onClick={onClose}
            disabled={loading}
            className="rounded-xl p-2 text-slate-400 transition hover:bg-slate-100 hover:text-slate-600 disabled:opacity-60"
          >
            <X size={18} />
          </button>
        </div>

        {error && (
          <div className="mt-5 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm text-red-600">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="mt-6 space-y-5">
          <div>
            <label className="mb-2 block text-sm font-semibold text-slate-700">
              Username
            </label>

            <input
              value={username}
              onChange={(e) => {
                if (error) setError("");
                setUsername(e.target.value);
              }}
              placeholder="Enter username"
              className="h-11 w-full rounded-xl border border-slate-300 bg-white px-4 text-sm text-slate-900 outline-none transition placeholder:text-slate-300 focus:border-[#2FA084] focus:ring-4 focus:ring-[#6FCF97]/20"
              autoFocus
            />

            <p className="mt-2 text-xs leading-5 text-slate-500">
              Use 3-40 characters. Letters, numbers, dot, underscore, and dash
              are allowed.
            </p>
          </div>

          <div className="flex justify-end gap-3 pt-2">
            <button
              type="button"
              onClick={onClose}
              disabled={loading}
              className="h-10 rounded-xl bg-slate-100 px-5 text-sm font-semibold text-slate-700 transition hover:bg-slate-200 disabled:opacity-60"
            >
              Cancel
            </button>

            <button
              type="submit"
              disabled={loading}
              className="h-10 rounded-xl bg-[#2FA084] px-5 text-sm font-semibold text-white shadow-lg shadow-emerald-900/10 transition hover:bg-[#1F6F5F] disabled:cursor-not-allowed disabled:opacity-60"
            >
              {loading ? "Saving..." : "Save"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}