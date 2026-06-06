import { RefreshCw } from "lucide-react";

export default function RepositoryEmptyState() {
  return (
    <section className="rounded-[2rem] border border-dashed border-[#6FCF97] bg-white p-10 text-center shadow-sm">
      <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-2xl bg-[#6FCF97]/25 text-[#1F6F5F]">
        <RefreshCw size={22} />
      </div>
      <h2 className="mt-4 text-lg font-black text-slate-900">No repositories found</h2>
      <p className="mt-2 text-sm leading-6 text-slate-500">Click Sync GitHub to import your public repositories.</p>
    </section>
  );
}
