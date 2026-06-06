import { AlertCircle, CheckCircle2 } from "lucide-react";

export default function RepositoryStatusMessage({ error, success }) {
  if (!error && !success) return null;

  return (
    <>
      {error && (
        <div className="flex items-start gap-3 rounded-2xl border border-red-200 bg-red-50 px-4 py-3 text-sm font-bold text-red-700">
          <AlertCircle className="mt-0.5 shrink-0" size={17} />
          {error}
        </div>
      )}

      {success && (
        <div className="flex items-start gap-3 rounded-2xl border border-emerald-200 bg-emerald-50 px-4 py-3 text-sm font-bold text-emerald-700">
          <CheckCircle2 className="mt-0.5 shrink-0" size={17} />
          {success}
        </div>
      )}
    </>
  );
}
