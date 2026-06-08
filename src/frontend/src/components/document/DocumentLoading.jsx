import { Loader2 } from "lucide-react";

export default function DocumentLoading() {
  return (
    <div className="overflow-hidden rounded-[2rem] border border-[#B9D8CC] bg-white shadow-[0_20px_60px_rgba(31,111,95,0.10)]">
      <div className="flex items-center gap-3 border-b border-[#DCEBE5] bg-gradient-to-r from-[#F7F1E8] via-white to-[#EEF7F1] px-7 py-5">
        <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-[#6FCF97]/20 text-[#1F6F5F]">
          <Loader2 className="animate-spin" size={20} />
        </div>

        <div>
          <p className="text-sm font-extrabold text-[#18332D]">
            Loading document
          </p>
          <p className="mt-1 text-xs font-medium text-slate-500">
            Preparing markdown content...
          </p>
        </div>
      </div>

      <div className="px-7 py-8 sm:px-10">
        <div className="h-10 w-3/4 animate-pulse rounded-lg bg-[#DCEBE5]" />
        <div className="mt-8 space-y-4">
          <div className="h-4 animate-pulse rounded-full bg-[#DCEBE5]" />
          <div className="h-4 w-11/12 animate-pulse rounded-full bg-[#DCEBE5]" />
          <div className="h-4 w-10/12 animate-pulse rounded-full bg-[#DCEBE5]" />
        </div>

        <div className="mt-9 h-40 animate-pulse rounded-lg bg-[#EEF7F1]" />

        <div className="mt-8 space-y-4">
          <div className="h-4 w-11/12 animate-pulse rounded-full bg-[#DCEBE5]" />
          <div className="h-4 w-8/12 animate-pulse rounded-full bg-[#DCEBE5]" />
        </div>
      </div>
    </div>
  );
}
