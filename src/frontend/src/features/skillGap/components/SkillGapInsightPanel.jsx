import { BookOpenCheck, CheckCircle2, Map, Target } from "lucide-react";
import { getSelectionSummary, toArray } from "../utils/skillGapUtils";

function MiniStat({ label, value }) {
  return (
    <div className="rounded-2xl border border-[#B9D8CC]/80 bg-white px-4 py-3">
      <p className="text-[11px] font-extrabold uppercase tracking-[0.16em] text-slate-500">{label}</p>
      <p className="mt-1 text-xl font-black text-[#18332D]">{value}</p>
    </div>
  );
}

export default function SkillGapInsightPanel({
  selectedRole,
  selectedRoadmap,
  categories,
  selectedSkillIds,
}) {
  const summary = getSelectionSummary(categories, selectedSkillIds);

  return (
    <aside className="rounded-3xl border border-[#B9D8CC]/80 bg-white/90 p-5 shadow-sm">
      <p className="inline-flex items-center gap-2 rounded-full border border-[#B9D8CC] bg-[#EAF7F1] px-3 py-1 text-xs font-extrabold uppercase tracking-[0.18em] text-[#1F6F5F]">
        <Target size={14} /> Skill gap scope
      </p>

      <div className="mt-5 space-y-3">
        <div className="rounded-2xl border border-[#B9D8CC]/80 bg-[#F7F1E8]/70 p-4">
          <div className="flex items-center gap-2 text-sm font-extrabold text-[#18332D]">
            <BookOpenCheck size={17} className="text-[#2FA084]" /> Career role
          </div>
          <p className="mt-2 text-sm font-bold text-slate-600">{selectedRole?.name || "No role selected"}</p>
        </div>

        <div className="rounded-2xl border border-[#B9D8CC]/80 bg-[#F7F1E8]/70 p-4">
          <div className="flex items-center gap-2 text-sm font-extrabold text-[#18332D]">
            <Map size={17} className="text-[#2FA084]" /> Roadmap
          </div>
          <p className="mt-2 text-sm font-bold text-slate-600">
            {selectedRoadmap?.title || selectedRoadmap?.roadmapName || "No roadmap selected"}
          </p>
        </div>
      </div>

      <div className="mt-4 grid grid-cols-2 gap-3">
        <MiniStat label="Categories" value={toArray(categories).length} />
        <MiniStat label="Marked" value={`${summary.selectedCount}/${summary.totalCount}`} />
      </div>

      {categories.length > 0 && (
        <div className="mt-5">
          <h3 className="mb-3 text-xs font-extrabold uppercase tracking-[0.16em] text-slate-500">Checklist categories</h3>
          <div className="space-y-2">
            {categories.map((category) => (
              <div key={category.categoryName} className="flex items-center justify-between gap-3 rounded-xl border border-slate-200 bg-slate-50 px-3 py-2">
                <span className="text-xs font-extrabold text-[#18332D]">{category.categoryName}</span>
                <span className="inline-flex items-center gap-1 text-xs font-bold text-[#1F6F5F]">
                  <CheckCircle2 size={13} /> {toArray(category.skills).length} skills
                </span>
              </div>
            ))}
          </div>
        </div>
      )}

    </aside>
  );
}
