import { useEffect } from "react";
import { X } from "lucide-react";

function GuideSection({ title, children }) {
  return (
    <section className="rounded-2xl border border-[#D6E4DE] bg-[#FCFAF7] p-4">
      <h4 className="text-sm font-extrabold text-[#18332D]">{title}</h4>
      <div className="mt-2 text-sm font-semibold leading-6 text-slate-700">{children}</div>
    </section>
  );
}

function GuideList({ items }) {
  return (
    <ul className="space-y-1.5">
      {items.map((item) => (
        <li key={item} className="flex gap-2">
          <span className="mt-2 h-1.5 w-1.5 shrink-0 rounded-full bg-[#2FA084]" aria-hidden="true" />
          <span>{item}</span>
        </li>
      ))}
    </ul>
  );
}

export default function NodeEditorGuideModal({ isOpen, onClose }) {
  useEffect(() => {
    if (!isOpen) return undefined;

    function handleKeyDown(event) {
      if (event.key === "Escape") onClose();
    }

    window.addEventListener("keydown", handleKeyDown);
    return () => window.removeEventListener("keydown", handleKeyDown);
  }, [isOpen, onClose]);

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-[#18332D]/35 px-4 py-8 backdrop-blur-sm animate-in fade-in duration-150">
      <button
        type="button"
        aria-label="Close guide"
        className="absolute inset-0 cursor-default"
        onClick={onClose}
      />

      <div className="relative flex max-h-[82vh] w-full max-w-2xl flex-col overflow-hidden rounded-3xl border border-[#B9D8CC] bg-white shadow-2xl animate-in zoom-in-95 duration-150">
        <header className="flex shrink-0 items-start justify-between gap-4 border-b border-[#B9D8CC]/70 bg-[#F7F1E8]/80 px-5 py-4">
          <div>
            <h3 className="text-lg font-black text-[#18332D]">Node editing guide</h3>
          </div>
          <button
            type="button"
            aria-label="Close guide"
            onClick={onClose}
            className="grid h-9 w-9 shrink-0 place-items-center rounded-xl border border-[#B9D8CC] bg-white text-[#18332D] transition hover:bg-[#EAF8F1]"
          >
            <X size={17} />
          </button>
        </header>

        <div className="min-h-0 flex-1 space-y-4 overflow-y-auto p-5 [scrollbar-color:#2FA084_#EAF8F1] [scrollbar-width:thin] [&::-webkit-scrollbar]:w-2 [&::-webkit-scrollbar-thumb]:rounded-full [&::-webkit-scrollbar-thumb]:bg-[#2FA084] [&::-webkit-scrollbar-track]:rounded-full [&::-webkit-scrollbar-track]:bg-[#EAF8F1]">
          <GuideSection title="Containers">
            <GuideList
              items={[
                "Phases, groups, choice groups, and resource groups organize the roadmap.",
                "They can have titles, descriptions, outcomes, and completion criteria.",
                "They do not have hours, difficulty, skills, or resources because learners do not complete them as standalone learning tasks.",
              ]}
            />
          </GuideSection>

          <GuideSection title="Learning nodes">
            <GuideList
              items={[
                "Topics and choice options are learning items.",
                "They can have hours, difficulty, skills, resources, outcomes, and completion criteria.",
                "Skills and resources should point to what the learner studies or practices for that item.",
              ]}
            />
          </GuideSection>

          <GuideSection title="Projects">
            <GuideList
              items={[
                "Projects ask learners to build a reviewable artifact.",
                "They can have hours, difficulty, skills, resources, outcomes, completion criteria, and a project guide.",
                "Use the project guide for the brief and build steps.",
              ]}
            />
          </GuideSection>

          <GuideSection title="Checkpoints">
            <GuideList
              items={[
                "Checkpoints are assessment points for completed roadmap work.",
                "They can have hours, difficulty, outcomes, completion criteria, and a checkpoint guide.",
                "Skills and resources are not mapped on checkpoints because checkpoints review earlier learning rather than introduce a new study path.",
              ]}
            />
          </GuideSection>

        </div>
      </div>
    </div>
  );
}
