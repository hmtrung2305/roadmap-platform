import { GitBranch, Loader2, Send, ShieldCheck, ArrowRight, History } from "lucide-react";

import AppSelect from "../../../components/common/AppSelect";
import { ModuleButton, ModuleCard } from "../../learningModules/components/learningModuleUi";
import { formatVersionLabel, getNextMajorVersionLabel, prettyStatus } from "../roadmapEditorUtils";

export default function DraftVersionActions({ detail, versionOptions = [], onVersionChange, isBusy, onCloneDraft, onPublishDraft }) {
  const status = String(detail?.status || "").toLowerCase();
  const existingDraft = detail?.versions?.find((version) => String(version.status).toLowerCase() === "draft");
  const currentVersionLabel = formatVersionLabel(detail);
  const existingDraftLabel = formatVersionLabel(existingDraft);
  const nextMajorVersionLabel = getNextMajorVersionLabel(detail?.versions);

  return (
    <div className="space-y-3">
      <ModuleCard className="overflow-visible p-0">
        <div className="border-b border-[#B9D8CC]/70 px-4 py-3">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div className="flex items-center gap-2 text-xs font-extrabold uppercase tracking-wide text-slate-700">
              <History size={14} className="text-[#1F6F5F]" /> Versions
            </div>
            {versionOptions.length > 0 && (
              <div className="w-44 sm:w-48">
                <AppSelect
                  className="min-w-0"
                  buttonClassName="!h-11 !px-3 !text-[15px] !font-bold"
                  optionClassName="!text-sm"
                  value={detail.roadmapVersionId}
                  options={versionOptions}
                  ariaLabel="Select roadmap version"
                  onChange={onVersionChange}
                />
              </div>
            )}
          </div>
        </div>
        <div className="flex flex-wrap items-center justify-between gap-3 p-4">
          <div className="flex min-w-0 items-center gap-3">
            <div className="grid h-10 min-w-16 shrink-0 place-items-center rounded-full border border-[#B9D8CC] bg-[#F7F1E8] px-3 text-xs font-black text-[#18332D]">
              {currentVersionLabel}
            </div>
            <div className="min-w-0">
              <p className="truncate text-sm font-extrabold text-[#18332D]">Current version</p>
              <p className="truncate text-xs font-semibold text-slate-600">
                {prettyStatus(status)} · {detail?.nodeCount || 0} nodes
              </p>
            </div>
          </div>
        </div>
      </ModuleCard>

      {status === "draft" && (
        <ModuleCard className="overflow-hidden p-0">
          <div className="flex flex-wrap items-center justify-between gap-3 p-4">
            <div className="flex min-w-0 items-center gap-3">
              <div className="grid h-9 w-9 place-items-center rounded-lg bg-[#6FCF97]/16 text-[#1F6F5F]">
                <ShieldCheck size={18} />
              </div>
              <h2 className="min-w-0 text-sm font-extrabold text-[#18332D]">Draft tools</h2>
            </div>

            <ModuleButton onClick={onPublishDraft} disabled={isBusy}>
              {isBusy ? <Loader2 size={14} className="animate-spin" /> : <Send size={14} />}
              Publish draft
            </ModuleButton>
          </div>
        </ModuleCard>
      )}

      {status === "published" && (
        <button
          type="button"
          onClick={() => {
            if (existingDraft?.roadmapVersionId) {
              onVersionChange(existingDraft.roadmapVersionId);
              return;
            }

            onCloneDraft();
          }}
          disabled={isBusy}
          className="group flex w-full items-center justify-between gap-4 rounded-xl border border-dashed border-[#9FBFB4] bg-white/95 p-4 text-left shadow-sm transition hover:-translate-y-0.5 hover:border-[#2FA084] hover:shadow-md disabled:cursor-not-allowed disabled:opacity-70 disabled:hover:translate-y-0 disabled:hover:border-[#9FBFB4]"
        >
          <div className="flex min-w-0 items-center gap-3">
            <div className="grid h-9 w-9 shrink-0 place-items-center rounded-lg bg-[#F7F1E8] text-[#B7791F]">
              {isBusy ? <Loader2 size={18} className="animate-spin" /> : <GitBranch size={18} />}
            </div>
            <h2 className="truncate text-sm font-extrabold text-[#18332D]">
              {existingDraft ? `Open draft ${existingDraftLabel}` : `Create major draft ${nextMajorVersionLabel}`}
            </h2>
          </div>
          <ArrowRight size={18} className="shrink-0 text-[#1F6F5F] transition group-hover:translate-x-1" />
        </button>
      )}
    </div>
  );
}
