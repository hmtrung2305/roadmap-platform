export const SKILL_GAP_STEPS = ["Choose target", "Mark skills", "Review gaps"];

export const ASSESSMENT_LEVEL_STYLES = {
  beginner: {
    label: "Beginner",
    badge: "border-[#CFE4EB] bg-[#EEF7FA] text-[#2D6577]",
    panel: "border-[#CFE4EB] bg-[#EEF7FA]/70",
  },
  intermediate: {
    label: "Intermediate",
    badge: "border-[#F1D9A8] bg-[#FFF7E6] text-[#8A5A12]",
    panel: "border-[#F1D9A8] bg-[#FFF7E6]/70",
  },
  advanced: {
    label: "Advanced",
    badge: "border-[#B9D8CC] bg-[#EAF7F1] text-[#1F6F5F]",
    panel: "border-[#B9D8CC] bg-[#EAF7F1]/70",
  },
  advance: {
    label: "Advanced",
    badge: "border-[#B9D8CC] bg-[#EAF7F1] text-[#1F6F5F]",
    panel: "border-[#B9D8CC] bg-[#EAF7F1]/70",
  },
  default: {
    label: "Assessment level",
    badge: "border-slate-200 bg-slate-50 text-slate-600",
    panel: "border-slate-200 bg-slate-50",
  },
};

export const SKILL_GAP_PRIORITY_STYLES = {
  1: {
    label: "Start here",
    dot: "bg-[#2FA084]",
    badge: "border-[#B9D8CC] bg-[#EAF7F1] text-[#1F6F5F]",
    bar: "bg-[#2FA084]",
  },
  2: {
    label: "Focus next",
    dot: "bg-[#D99A3D]",
    badge: "border-[#F1D9A8] bg-[#FFF7E6] text-[#8A5A12]",
    bar: "bg-[#D99A3D]",
  },
  3: {
    label: "Build later",
    dot: "bg-[#5A9CB5]",
    badge: "border-[#CFE4EB] bg-[#EEF7FA] text-[#2D6577]",
    bar: "bg-[#5A9CB5]",
  },
  4: {
    label: "Optional polish",
    dot: "bg-slate-400",
    badge: "border-slate-200 bg-slate-50 text-slate-600",
    bar: "bg-slate-400",
  },
};
