import { AlertTriangle, X } from "lucide-react";

export default function ConfirmModal({
  title = "Confirm action",
  message,
  confirmLabel = "Confirm",
  cancelLabel = "Cancel",
  loading = false,
  danger = true,
  onCancel,
  onConfirm,
}) {
  return (
    <div className="fixed inset-0 z-[120] flex items-center justify-center bg-slate-950/40 px-4 backdrop-blur-sm">
      <div className="w-full max-w-xl rounded-lg border border-slate-200 bg-white p-6 shadow-2xl shadow-slate-900/20">
        <div className="flex items-start gap-4">
          <div
            className={`mt-1 flex h-10 w-10 shrink-0 items-center justify-center rounded-full ${
              danger ? "bg-red-50 text-red-600" : "bg-[#5A9CB5]/12 text-[#2F7F98]"
            }`}
          >
            <AlertTriangle size={20} />
          </div>

          <div className="min-w-0 flex-1">
            <div className="flex items-start justify-between gap-4">
              <h2 className="text-xl font-bold tracking-tight text-slate-900">
                {title}
              </h2>

              <button
                type="button"
                onClick={onCancel}
                disabled={loading}
                className="rounded-lg p-2 text-slate-400 transition hover:bg-slate-100 hover:text-slate-600 disabled:opacity-60"
              >
                <X size={18} />
              </button>
            </div>

            <p className="mt-3 text-sm leading-6 text-slate-500">
              {message}
            </p>

            <div className="mt-6 flex justify-end gap-3">
              <button
                type="button"
                onClick={onCancel}
                disabled={loading}
                className="h-11 rounded-lg border border-slate-200 bg-white px-5 text-sm font-semibold text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-60"
              >
                {cancelLabel}
              </button>

              <button
                type="button"
                onClick={onConfirm}
                disabled={loading}
                className={`h-11 rounded-lg px-5 text-sm font-semibold text-white shadow-lg transition disabled:cursor-not-allowed disabled:opacity-60 ${
                  danger
                    ? "bg-red-600 shadow-red-600/20 hover:bg-red-700"
                    : "bg-[#2FA084] shadow-[#2FA084]/20 hover:bg-[#1F6F5F]"
                }`}
              >
                {loading ? "Processing..." : confirmLabel}
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}