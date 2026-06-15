import { AlertTriangle } from "lucide-react";

export default function SkillGapErrorMessage({ children }) {
  if (!children) return null;

  return (
    <div className="mb-5 flex items-start gap-3 rounded-xl border border-rose-200 bg-rose-50 px-4 py-3 text-sm font-semibold leading-6 text-rose-700">
      <AlertTriangle className="mt-0.5 shrink-0" size={18} />
      <span>{children}</span>
    </div>
  );
}
