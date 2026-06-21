import { Coins, Loader2 } from "lucide-react";

export default function AiCreditStatus({ status, isLoading = false }) {
  if (isLoading && !status) {
    return (
      <div className="mt-2 flex shrink-0 items-center gap-2 rounded-lg border border-[#DCEBE5] bg-[#F7F1E8]/65 px-3 py-2 text-xs font-bold text-[#667A73]">
        <Loader2 className="animate-spin text-[#1F6F5F]" size={14} />
        Loading AI credits...
      </div>
    );
  }

  if (!status) {
    return (
      <div className="mt-2 flex shrink-0 items-center gap-2 rounded-lg border border-slate-200 bg-slate-50 px-3 py-2 text-xs font-bold text-slate-500">
        <Coins size={14} />
        AI credit status unavailable.
      </div>
    );
  }

  const remaining = Number(status.remainingCreditsToday ?? 0);
  const limit = Number(status.dailyCreditLimit ?? 0);
  const isEmpty = remaining <= 0;

  return (
    <div
      className={`mt-2 flex shrink-0 flex-wrap items-center justify-between gap-2 rounded-lg border px-3 py-2 text-xs font-bold ${
        isEmpty
          ? "border-rose-200 bg-rose-50 text-rose-700"
          : "border-[#B9D8CC] bg-[#EAF8F1] text-[#1F6F5F]"
      }`}
    >
      <span className="inline-flex items-center gap-1.5">
        <Coins size={14} />
        AI credits: {remaining}/{limit} remaining
      </span>
      <span className="text-[11px] font-semibold opacity-80">
        Each generation consumes 1 credit.
      </span>
    </div>
  );
}
