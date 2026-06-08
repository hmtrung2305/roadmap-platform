import { ChevronRight } from "lucide-react";

export default function SettingsRow({
  icon: Icon,
  title,
  value,
  actionLabel,
  danger = false,
  isDelete = false,
  disabled = false,
  iconClassName = "bg-[#5A9CB5]/12 text-[#2F7F98]",
  actionClassName = "",
  onClick,
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      disabled={disabled}
      className="flex w-full items-center gap-4 px-6 py-5 text-left transition hover:bg-[#6FCF97]/8 disabled:cursor-not-allowed disabled:opacity-60"
    >
      {Icon && (
        <div
          className={`flex h-10 w-10 shrink-0 items-center justify-center rounded-lg ${
            danger ? "bg-red-50 text-red-600" : iconClassName
          }`}
        >
          <Icon size={18} />
        </div>
      )}

      <div className="min-w-0 flex-1">
        <div className="flex flex-wrap items-center gap-2">
          <h3
            className={`text-sm font-bold ${
              danger ? "text-red-600" : "text-slate-900"
            }`}
          >
            {title}
          </h3>

          {value && (
            <span className="truncate text-sm text-slate-500">{value}</span>
          )}
        </div>
      </div>

      {actionLabel ? (
        <span
          className={`rounded-[10px] px-3 py-2 text-xs font-bold ${
            actionClassName
              ? actionClassName
              : isDelete
                ? "bg-red-50 text-red-600"
                : "bg-slate-100 text-slate-700"
          }`}
        >
          {actionLabel}
        </span>
      ) : (
        <ChevronRight size={18} className="text-slate-400" />
      )}
    </button>
  );
}