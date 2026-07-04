import { BrainCircuit } from "lucide-react";

export default function SkillGapPageHeader() {
  return (
    <div className="mx-auto mb-7 max-w-4xl text-center">
      <div className="inline-flex items-center gap-2 rounded-lg border border-[#B9D8CC] bg-white px-3 py-1.5 text-xs font-extrabold uppercase tracking-wide text-[#1F6F5F] shadow-sm">
        <BrainCircuit size={15} />
        Skill Gap Analysis
      </div>
      <h1 className="mt-4 text-3xl font-extrabold tracking-tight text-[#18332D] sm:text-4xl lg:text-5xl">
        Compare your skills with a roadmap
      </h1>
      <p className="mx-auto mt-3 max-w-2xl text-sm font-semibold leading-6 text-slate-700">
        Pick a career role, choose a published roadmap, then mark the skills you already have to see what is missing.
      </p>
    </div>
  );
}
