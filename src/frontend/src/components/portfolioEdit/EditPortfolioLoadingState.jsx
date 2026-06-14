import { Loader2 } from "lucide-react";

export default function EditPortfolioLoadingState() {
  return (
    <main className="min-h-[calc(100vh-4rem)] bg-[#F7F1E8]/60 px-4 py-8 sm:px-6">
      <section className="mx-auto max-w-6xl rounded-lg border border-[#B9D8CC] bg-white p-6 shadow-[0_18px_45px_rgba(31,111,95,0.08)]">
        <div className="flex items-center gap-3 text-sm font-semibold text-slate-600">
          <Loader2 className="animate-spin text-[#1F6F5F]" size={18} />
          Loading your portfolio editor...
        </div>
      </section>
    </main>
  );
}
