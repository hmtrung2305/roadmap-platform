import { FolderGit2 } from "lucide-react";

export default function PortfolioEmptyState() {
  return (
    <section className="rounded-[1.5rem] border border-dashed border-emerald-200 bg-emerald-50/40 p-8 text-center">
      <div className="mx-auto flex h-12 w-12 items-center justify-center rounded-2xl bg-white text-emerald-700 shadow-sm">
        <FolderGit2 size={22} />
      </div>
      <h3 className="mt-4 text-lg font-black text-slate-950">No repositories selected yet</h3>
      <p className="mx-auto mt-2 max-w-md text-sm leading-6 text-slate-500">
        Choose a few projects that best represent your skills. A smaller focused showcase usually looks stronger.
      </p>
    </section>
  );
}
