export const prettyModuleStatus = {
  draft: "Draft",
  published: "Published",
  archived: "Archived",
};

export const prettyEnrollmentStatus = {
  in_progress: "In progress",
  completed: "Completed",
  not_started: "Not started",
};

export const inputClass =
  "w-full rounded-lg border border-[#B9D8CC] bg-white px-3 py-2 text-sm font-semibold text-slate-800 outline-none transition placeholder:text-slate-500 focus:border-[#2FA084] focus:ring-2 focus:ring-[#6FCF97]/25";

export const selectClass = `${inputClass} cursor-pointer`;

export function ModuleButton({
  children,
  variant = "primary",
  size = "sm",
  className = "",
  type = "button",
  ...props
}) {
  const variants = {
    primary:
      "bg-[#2FA084] text-white shadow-sm hover:bg-[#1F6F5F] disabled:bg-slate-300 disabled:text-slate-600",
    secondary:
      "border border-[#B9D8CC] bg-white text-[#18332D] hover:border-[#2FA084] hover:bg-[#F7F1E8] disabled:bg-slate-50 disabled:text-slate-500",
    ghost:
      "text-slate-700 hover:bg-[#F7F1E8] hover:text-[#1F6F5F] disabled:text-slate-500",
    danger:
      "border border-rose-200 bg-white text-rose-600 hover:border-rose-300 hover:bg-rose-50 disabled:bg-slate-50 disabled:text-slate-500",
    soft:
      "border border-[#6FCF97]/40 bg-[#6FCF97]/15 text-[#1F6F5F] hover:bg-[#6FCF97]/25 disabled:bg-slate-50 disabled:text-slate-500",
  };

  const sizes = {
    xs: "min-w-[72px] px-2.5 py-1.5 text-[11px]",
    sm: "min-w-[88px] px-3 py-1.5 text-xs",
    md: "min-w-[112px] px-4 py-2 text-[13px]",
    icon: "h-8 w-8 min-w-0 px-0 text-xs",
  };

  return (
    <button
      type={type}
      className={`inline-flex items-center justify-center gap-2 whitespace-nowrap rounded-lg font-bold leading-tight transition focus:outline-none focus:ring-2 focus:ring-[#6FCF97]/30 ${variants[variant]} ${sizes[size]} ${className}`}
      {...props}
    >
      {children}
    </button>
  );
}

export function ModuleCard({ children, className = "" }) {
  return (
    <div className={`rounded-xl border border-[#B9D8CC]/80 bg-white/95 shadow-sm ${className}`}>
      {children}
    </div>
  );
}

export function ModuleBadge({ children, tone = "green", className = "" }) {
  const tones = {
    green: "border-[#B9D8CC] bg-[#6FCF97]/20 text-[#1F6F5F]",
    slate: "border-slate-200 bg-slate-50 text-slate-700",
    amber: "border-amber-200 bg-amber-50 text-amber-700",
    blue: "border-sky-200 bg-sky-50 text-sky-700",
    rose: "border-rose-200 bg-rose-50 text-rose-700",
    purple: "border-violet-200 bg-violet-50 text-violet-700",
  };

  return (
    <span
      className={`inline-flex w-fit max-w-full items-center justify-center rounded-full border px-2.5 py-0.5 text-[11px] font-bold leading-5 ${tones[tone]} ${className}`}
    >
      {children}
    </span>
  );
}

export function ModuleField({ label, children, hint }) {
  return (
    <label className="block">
      <span className="mb-1.5 block text-[11px] font-bold uppercase tracking-wide text-slate-700">
        {label}
      </span>
      {children}
      {hint && <span className="mt-1.5 block text-[11px] font-semibold text-slate-600">{hint}</span>}
    </label>
  );
}

export function ModuleEmptyState({ title, children, action }) {
  return (
    <ModuleCard className="p-8 text-center">
      <div className="mx-auto mb-3 grid h-10 w-10 place-items-center rounded-full bg-[#6FCF97]/20 text-[#1F6F5F]">
        •
      </div>
      <h3 className="text-sm font-bold text-[#18332D]">{title}</h3>
      {children && (
        <p className="mx-auto mt-2 max-w-md text-sm font-medium leading-6 text-slate-700">
          {children}
        </p>
      )}
      {action && <div className="mt-4">{action}</div>}
    </ModuleCard>
  );
}

export function ModulePageShell({ children, compact = false }) {
  return (
    <main className="min-h-screen bg-[#F7F1E8] px-4 py-7 sm:px-6 lg:px-8">
      <div className={`mx-auto ${compact ? "max-w-[1520px]" : "max-w-7xl"}`}>{children}</div>
    </main>
  );
}

export function formatHours(value) {
  if (value === null || value === undefined || value === "") return "—";
  const numeric = Number(value);
  if (!Number.isFinite(numeric)) return `${value}h`;
  return `${numeric % 1 === 0 ? numeric : numeric.toFixed(1)}h`;
}

export function getEnrollmentStatus(module) {
  return module?.enrollment?.status || "not_started";
}

export function getProgress(module) {
  return Number(module?.enrollment?.progressPercent ?? 0);
}

export function getStatusTone(status) {
  if (status === "published" || status === "completed") return "green";
  if (status === "draft") return "blue";
  if (status === "in_progress") return "blue";
  if (status === "archived") return "slate";
  return "slate";
}

export function getLessonProgress(enrollment, lessonId) {
  if (!enrollment?.lessonProgress || !lessonId) return null;
  return enrollment.lessonProgress[lessonId] || null;
}
