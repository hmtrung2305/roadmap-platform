import { BookOpenText } from "lucide-react";

export default function PortfolioAbout({ portfolio }) {
  return (
    <section className="rounded-[1.5rem] border border-slate-200 bg-white p-5 shadow-sm">
      <div className="flex items-center gap-3">
        <div className="flex h-10 w-10 items-center justify-center rounded-2xl bg-emerald-100 text-emerald-700">
          <BookOpenText size={20} />
        </div>
        <div>
          <h2 className="text-xl font-black tracking-tight text-slate-950">About</h2>
          <p className="text-sm text-slate-500">Profile overview</p>
        </div>
      </div>

      <p className="mt-5 whitespace-pre-line text-sm leading-7 text-slate-600">
        {portfolio?.bio || "No profile description has been added yet."}
      </p>
    </section>
  );
}
