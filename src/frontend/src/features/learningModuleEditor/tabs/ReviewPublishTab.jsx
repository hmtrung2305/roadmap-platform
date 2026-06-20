import { CheckCircle2, Circle } from "lucide-react";
import { ModuleBadge, ModuleButton, ModuleCard } from "../../../components/learningModules/learningModuleUi";

export default function ReviewPublishTab({ detail, isPublishing, onPublish }) {
  const module = detail.module;
  const checks = detail.publishReadiness?.checks || [];
  const canPublish = Boolean(detail.publishReadiness?.canPublish);

  if (module.status !== "draft") {
    return (
      <ModuleCard className="p-8 text-center">
        <CheckCircle2 size={34} className="mx-auto text-[#1F6F5F]" />
        <h2 className="mt-3 text-lg font-extrabold text-[#18332D]">
          This module is {module.status}
        </h2>
        <p className="mx-auto mt-2 max-w-md text-sm font-semibold leading-6 text-slate-600">
          Publishing is only available while the module is still a draft.
        </p>
      </ModuleCard>
    );
  }

  return (
    <ModuleCard className="flex h-full min-h-0 flex-col overflow-hidden p-5">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h2 className="text-lg font-extrabold text-[#18332D]">Publish module</h2>
        </div>

        <ModuleBadge tone={canPublish ? "green" : "rose"}>
          {canPublish ? "Ready" : "Not ready"}
        </ModuleBadge>
      </div>

      <div className="mt-5 grid gap-3">
        {checks.map((check) => (
          <div
            key={check.label}
            className={`flex items-start gap-3 rounded-xl border p-4 ${
              check.isComplete
                ? "border-[#B9D8CC] bg-[#6FCF97]/10"
                : "border-rose-200 bg-rose-50"
            }`}
          >
            {check.isComplete ? (
              <CheckCircle2 size={20} className="mt-0.5 shrink-0 text-[#1F6F5F]" />
            ) : (
              <Circle size={20} className="mt-0.5 shrink-0 text-rose-600" />
            )}
            <div>
              <div className="text-sm font-extrabold text-[#18332D]">{check.label}</div>
              <div className="mt-1 text-sm font-semibold text-slate-600">{check.description}</div>
            </div>
          </div>
        ))}
      </div>


      <div className="mt-5 flex justify-end">
        <ModuleButton onClick={onPublish} disabled={!canPublish || isPublishing}>
          {isPublishing ? "Publishing..." : "Publish module"}
        </ModuleButton>
      </div>
    </ModuleCard>
  );
}

