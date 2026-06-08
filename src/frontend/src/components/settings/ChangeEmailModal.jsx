import { useState } from "react";
import { X } from "lucide-react";
import { requestLocalEmailChangeApi } from "../../api/authProviderApi";
import { getErrorMessage, isEmailVerificationRequired, isValidEmailFormat } from "../../utils/authVerificationFlow";

export default function ChangeEmailModal({
  currentEmail,
  onClose,
  onSuccess,
}) {
  const [email, setEmail] = useState(currentEmail || "");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState("");

  const validateEmail = () => {
    const trimmedEmail = email.trim();

    if (!trimmedEmail) {
      return "Please enter your new email address.";
    }

    if (!isValidEmailFormat(trimmedEmail)) {
      return "Please enter a valid email address.";
    }

    if (trimmedEmail === currentEmail) {
      return "New email must be different from your current email.";
    }

    return "";
  };

  const handleSubmit = async (e) => {
    e.preventDefault();

    const validationError = validateEmail();

    if (validationError) {
      setError(validationError);
      return;
    }

    try {
      setLoading(true);
      setError("");

      const response = await requestLocalEmailChangeApi({
        NewEmail: email.trim(),
      });

      if (isEmailVerificationRequired(response)) {
        onSuccess?.(response.email || email.trim(), response);
        return;
      }

      onSuccess?.(email.trim(), response);
    } catch (error) {
      console.error("Request email change failed:", error.response?.data || error);

      setError(getErrorMessage(error, "Unable to request email change."));
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="fixed inset-0 z-[100] flex items-center justify-center bg-slate-950/40 px-4 backdrop-blur-sm">
      <div className="w-full max-w-lg rounded-3xl border border-slate-200 bg-white p-6 shadow-2xl shadow-slate-900/20">
        <div className="flex items-start justify-between gap-4">
          <div>
            <h2 className="text-xl font-bold tracking-tight text-slate-900">
              Change email
            </h2>

            <p className="mt-1 text-sm leading-6 text-slate-500">
              Enter a new email address. We will send a verification code to
              confirm the change.
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
              New email address
            </label>

            <input
              type="email"
              value={email}
              onChange={(e) => {
                if (error) setError("");
                setEmail(e.target.value);
              }}
              placeholder="name@example.com"
              className="h-11 w-full rounded-xl border border-slate-300 bg-white px-4 text-sm text-slate-900 outline-none transition placeholder:text-slate-300 focus:border-[#2FA084] focus:ring-4 focus:ring-[#6FCF97]/20"
              autoFocus
            />
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
              {loading ? "Sending code..." : "Continue"}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}