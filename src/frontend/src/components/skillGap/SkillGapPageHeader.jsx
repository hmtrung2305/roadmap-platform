import { BrainCircuit } from "lucide-react";

export default function SkillGapPageHeader() {
  return (
    <div className="mx-auto mb-7 max-w-4xl text-center">
      <div className="inline-flex items-center gap-2 rounded-lg border border-[#B9D8CC] bg-white px-3 py-1.5 text-xs font-extrabold uppercase tracking-wide text-[#1F6F5F] shadow-sm">
        <BrainCircuit size={15} />
        Skill Gap Analysis
      </div>
      <h1 className="mt-4 text-3xl font-extrabold tracking-tight text-[#18332D] sm:text-4xl lg:text-5xl">
        Find the skills you should learn next
      </h1>
      <p className="mx-auto mt-3 max-w-2xl text-sm font-semibold leading-6 text-slate-700">
        Pick a role, choose a level, then review what to learn next.
      </p>
    </div>
  );
}
