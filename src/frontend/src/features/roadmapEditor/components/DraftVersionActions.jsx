import { useMemo, useState } from "react";
import {
  GitBranch,
  History,
  Loader2,
  Send,
  ShieldCheck,
  Wrench,
} from "lucide-react";

import AppSelect from "../../../components/common/AppSelect";
import {
  ModuleButton,
  ModuleCard,
} from "../../learningModules/components/learningModuleUi";
import {
  formatVersionLabel,
  getNextMajorVersionLabel,
  getNextMinorVersionLabel,
  getNextPatchVersionLabel,
  prettyReleaseType,
  prettyStatus,
} from "../roadmapEditorUtils";

const updateTypeCopy = {
  patch: {
    icon: Wrench,
    label: "Patch update",
    description: "Safe wording, guide, and resource fixes.",
  },
  minor: {
    icon: GitBranch,
    label: "Minor update",
    description:
      "Optional content additions without changing the required path.",
  },
  major: {
    icon: GitBranch,
    label: "Major update",
    description: "Structural changes for a new roadmap version.",
  },
};

function sortDraftsNewestFirst(drafts) {
  return [...drafts].sort((first, second) => {
    const firstTime = Date.parse(first.updatedAt || first.createdAt || "") || 0;
    const secondTime =
      Date.parse(second.updatedAt || second.createdAt || "") || 0;

    if (firstTime !== secondTime) {
      return secondTime - firstTime;
    }

    return Number(second.versionNumber || 0) - Number(first.versionNumber || 0);
  });
}

export default function DraftVersionActions({
  detail,
  versionOptions = [],
  onVersionChange,
  isBusy,
  onCloneDraft,
  onCreatePatchDraft,
  onCreateMinorDraft,
  onPublishDraft,
}) {
  const [selectedUpdateType, setSelectedUpdateType] = useState("patch");
  const status = String(detail?.status || "").toLowerCase();
  const releaseType = String(detail?.releaseType || "").toLowerCase();
  const currentVersionLabel = formatVersionLabel(detail);

  const drafts = useMemo(
    () =>
      sortDraftsNewestFirst(
        detail?.versions?.filter(
          (version) => String(version.status).toLowerCase() === "draft",
        ) || [],
      ),
    [detail?.versions],
  );

  const existingDraft = drafts[0] || null;
  const existingDraftLabel = formatVersionLabel(existingDraft);
  const existingDraftReleaseType = String(
    existingDraft?.releaseType || "",
  ).toLowerCase();
  const ExistingDraftIcon =
    updateTypeCopy[existingDraftReleaseType]?.icon || GitBranch;

  const updateOptions = useMemo(
    () => [
      {
        value: "patch",
        label: `Patch draft ${getNextPatchVersionLabel(detail, detail?.versions)}`,
      },
      {
        value: "minor",
        label: `Minor draft ${getNextMinorVersionLabel(detail, detail?.versions)}`,
      },
      {
        value: "major",
        label: `Major draft ${getNextMajorVersionLabel(detail?.versions)}`,
      },
    ],
    [detail],
  );

  const selectedUpdateCopy =
    updateTypeCopy[selectedUpdateType] || updateTypeCopy.patch;
  const SelectedUpdateIcon = selectedUpdateCopy.icon;

  const handleCreateUpdateDraft = () => {
    if (selectedUpdateType === "minor") {
      onCreateMinorDraft();
      return;
    }

    if (selectedUpdateType === "major") {
      onCloneDraft();
      return;
    }

    onCreatePatchDraft();
  };

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
              <p className="truncate text-sm font-extrabold text-[#18332D]">
                Current version
              </p>
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
              <div className="min-w-0">
                <h2 className="min-w-0 text-sm font-extrabold text-[#18332D]">
                  Draft tools
                </h2>
                {releaseType === "patch" ? (
                  <p className="mt-0.5 text-xs font-semibold text-slate-600">
                    Patch drafts allow wording and resource fixes only.
                  </p>
                ) : null}
                {releaseType === "minor" ? (
                  <p className="mt-0.5 text-xs font-semibold text-slate-600">
                    Minor drafts allow optional additions and safe content
                    edits.
                  </p>
                ) : null}
              </div>
            </div>

            <ModuleButton onClick={onPublishDraft} disabled={isBusy}>
              {isBusy ? (
                <Loader2 size={14} className="animate-spin" />
              ) : (
                <Send size={14} />
              )}
              Publish draft
            </ModuleButton>
          </div>
        </ModuleCard>
      )}

      {status === "published" && existingDraft && (
        <ModuleCard className="overflow-hidden p-0">
          <div className="flex flex-wrap items-center justify-between gap-3 p-4">
            <div className="flex min-w-0 items-center gap-3">
              <div className="grid h-9 w-9 shrink-0 place-items-center rounded-lg bg-[#F7F1E8] text-[#B7791F]">
                <ExistingDraftIcon size={18} />
              </div>
              <div className="min-w-0">
                <h2 className="truncate text-sm font-extrabold text-[#18332D]">
                  Draft in progress: {existingDraftLabel}
                </h2>
                <p className="mt-0.5 text-xs font-semibold text-slate-600">
                  Finish or delete this{" "}
                  {prettyReleaseType(existingDraftReleaseType).toLowerCase()}{" "}
                  draft before creating another update.
                </p>
              </div>
            </div>
            <ModuleButton
              variant="secondary"
              onClick={() => onVersionChange(existingDraft.roadmapVersionId)}
              disabled={isBusy}
            >
              Open draft
            </ModuleButton>
          </div>
        </ModuleCard>
      )}

      {status === "published" && !existingDraft && (
        <ModuleCard className="overflow-visible p-0">
          <div className="grid gap-4 p-4 lg:grid-cols-[minmax(0,1fr)_auto] lg:items-center">
            <div className="flex min-w-0 items-center gap-3">
              <div className="grid h-9 w-9 shrink-0 place-items-center rounded-lg bg-[#6FCF97]/16 text-[#1F6F5F]">
                <SelectedUpdateIcon size={18} />
              </div>
              <div className="min-w-0">
                <h2 className="truncate text-sm font-extrabold text-[#18332D]">
                  Create update draft
                </h2>
                <p className="mt-0.5 text-xs font-semibold text-slate-600">
                  {selectedUpdateCopy.description}
                </p>
              </div>
            </div>
            <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
              <div className="w-full sm:w-56">
                <AppSelect
                  value={selectedUpdateType}
                  options={updateOptions}
                  ariaLabel="Select update draft type"
                  onChange={setSelectedUpdateType}
                />
              </div>
              <ModuleButton
                onClick={handleCreateUpdateDraft}
                disabled={isBusy}
                size="md"
              >
                {isBusy ? (
                  <Loader2 size={14} className="animate-spin" />
                ) : (
                  <SelectedUpdateIcon size={14} />
                )}
                Create draft
              </ModuleButton>
            </div>
          </div>
        </ModuleCard>
      )}
    </div>
  );
}
