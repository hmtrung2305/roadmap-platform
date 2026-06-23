import { Check } from "lucide-react";
import { SKILL_GAP_STEPS } from "../constants/skillGapConstants";

export default function SkillGapStepper({ currentStep }) {
  return (
    <div className="grid gap-2 sm:grid-cols-3">
      {SKILL_GAP_STEPS.map((label, index) => {
        const stepNumber = index + 1;
        const isDone = currentStep > stepNumber;
        const isActive = currentStep === stepNumber;

        return (
          <div
            key={label}
            className={`flex items-center gap-3 rounded-xl border px-3 py-2 text-sm font-bold transition ${
              isActive
                ? "border-[#2FA084] bg-[#6FCF97]/20 text-[#1F6F5F]"
                : isDone
                  ? "border-[#B9D8CC] bg-white text-[#1F6F5F]"
                  : "border-slate-200 bg-white/70 text-slate-500"
            }`}
          >
            <span
              className={`grid h-7 w-7 place-items-center rounded-full text-xs ${
                isActive || isDone
                  ? "bg-[#2FA084] text-white"
                  : "bg-slate-100 text-slate-500"
              }`}
            >
              {isDone ? <Check size={14} /> : stepNumber}
            </span>
            {label}
          </div>
        );
      })}
    </div>
  );
}
