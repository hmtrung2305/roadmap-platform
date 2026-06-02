import { ArrowLeft, Clock, FileText } from "lucide-react";
import { useNavigate } from "react-router-dom";

export default function DocumentHeader({ resource }) {
  const navigate = useNavigate();

  return (
    <header className="sticky top-0 z-20 border-b border-slate-200 bg-white/95 backdrop-blur">
      <div className="flex h-16 items-center justify-between px-6">
        <div className="flex items-center gap-4">
          <button
            type="button"
            onClick={() => navigate("/resources")}
            className="inline-flex items-center gap-2 rounded-xl px-3 py-2 text-sm font-medium text-slate-600 hover:bg-slate-100 hover:text-slate-900"
          >
            <ArrowLeft size={18} />
            Back to Resources
          </button>

          <div className="h-6 w-px bg-slate-200" />

          <div>
            <h1 className="line-clamp-1 text-lg font-bold text-blue-700">
              {resource?.title || "Learning Document"}
            </h1>

            <div className="mt-0.5 flex items-center gap-3 text-xs text-slate-500">
              <span className="inline-flex items-center gap-1">
                <FileText size={13} />
                {resource?.skillName || "General"}
              </span>

              <span className="inline-flex items-center gap-1">
                <Clock size={13} />
                {resource?.durationMinutes || 15} phút đọc
              </span>
            </div>
          </div>
        </div>

        <button
          type="button"
          className="rounded-xl bg-blue-600 px-4 py-2 text-sm font-semibold text-white shadow-sm hover:bg-blue-700"
        >
          Mark as Completed
        </button>
      </div>
    </header>
  );
}