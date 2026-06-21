import { Eye, EyeOff, Settings } from "lucide-react";

function getVisibilityCopy(isPublic) {
  if (isPublic) {
    return {
      label: "Public",
      title: "Public portfolio enabled",
      description: "Anyone with the public link can view your portfolio.",
      badgeClassName: "border-[#B9D8CC] bg-[#EAF8F1] text-[#1F6F5F]",
      icon: Eye,
    };
  }

  return {
    label: "Private",
    title: "Private portfolio",
    description: "Public visitors cannot view your portfolio until visibility is enabled.",
    badgeClassName: "border-slate-200 bg-slate-100 text-slate-600",
    icon: EyeOff,
  };
}

export default function PortfolioVisibilityCard({ isPublic = false, onManageVisibility }) {
  const visibility = getVisibilityCopy(Boolean(isPublic));
  const Icon = visibility.icon;

  return (
    <button
      type="button"
      onClick={onManageVisibility}
      className="group w-full rounded-lg border border-[#B9D8CC] bg-white p-5 text-left shadow-[0_18px_45px_rgba(31,111,95,0.08)] transition hover:-translate-y-0.5 hover:border-[#2FA084] hover:shadow-[0_22px_48px_rgba(31,111,95,0.12)]"
    >
      <div className="flex items-start justify-between gap-3">
        <div className="flex min-w-0 items-start gap-3">
          <span className="grid size-11 shrink-0 place-items-center rounded-lg bg-[#EAF8F1] text-[#1F6F5F] ring-1 ring-[#B9D8CC]">
            <Icon size={20} />
          </span>

          <div className="min-w-0">
            <p className="text-xs font-bold uppercase tracking-[0.18em] text-[#2FA084]">
              Portfolio visibility
            </p>
            <h2 className="mt-1 text-lg font-bold text-[#18332D]">
              {visibility.title}
            </h2>
          </div>
        </div>

        <span
          className={`shrink-0 rounded-full border px-3 py-1 text-xs font-extrabold ${visibility.badgeClassName}`}
        >
          {visibility.label}
        </span>
      </div>

      <p className="mt-4 text-sm font-semibold leading-6 text-[#667A73]">
        {visibility.description}
      </p>

      <span className="mt-5 inline-flex items-center gap-2 rounded-lg border border-[#B9D8CC] bg-[#F7F1E8]/70 px-3 py-2 text-sm font-bold text-[#1F6F5F] transition group-hover:border-[#1F6F5F] group-hover:bg-[#EAF8F1]">
        <Settings size={14} />
        Manage visibility
      </span>
    </button>
  );
}
