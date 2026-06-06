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
      <div className="w-full max-w-xl rounded-3xl border border-slate-200 bg-white p-6 shadow-2xl shadow-slate-900/20">
        <div className="flex items-start gap-4">
          <div
            className={`mt-1 flex h-10 w-10 shrink-0 items-center justify-center rounded-full ${
              danger ? "bg-red-50 text-red-600" : "bg-emerald-50 text-indigo-700"
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
                className="rounded-xl p-2 text-slate-400 transition hover:bg-slate-100 hover:text-slate-600 disabled:opacity-60"
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
                className="h-11 rounded-xl border border-slate-200 bg-white px-5 text-sm font-semibold text-slate-700 transition hover:bg-[#F7F1E8] disabled:cursor-not-allowed disabled:opacity-60"
              >
                {cancelLabel}
              </button>

              <button
                type="button"
                onClick={onConfirm}
                disabled={loading}
                className={`h-11 rounded-xl px-5 text-sm font-semibold text-white shadow-lg transition disabled:cursor-not-allowed disabled:opacity-60 ${
                  danger
                    ? "bg-red-600 shadow-red-600/20 hover:bg-red-700"
                    : "bg-indigo-700 shadow-indigo-700/20 hover:bg-indigo-800"
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