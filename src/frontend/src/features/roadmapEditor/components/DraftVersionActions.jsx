import { useMemo, useState } from "react";
import { Clock3, GitBranch, History, Loader2, Wrench } from "lucide-react";

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
} from "../roadmapEditorUtils";

const updateTypeCopy = {
  patch: {
    icon: Wrench,
    label: "Patch update",
  },
  minor: {
    icon: GitBranch,
    label: "Minor update",
  },
  major: {
    icon: GitBranch,
    label: "Major update",
  },
};

const statusTagCopy = {
  draft: {
    label: "Draft",
    className: "border-slate-200 bg-slate-50 text-slate-700",
  },
  pending_review: {
    label: "Awaiting reviewer approval",
    className: "border-amber-200 bg-amber-50 text-amber-800",
  },
  changes_requested: {
    label: "Changes requested",
    className: "border-rose-200 bg-rose-50 text-rose-700",
  },
  published: {
    label: "Published",
    className: "border-emerald-200 bg-emerald-50 text-emerald-700",
  },
  archived: {
    label: "Archived",
    className: "border-slate-200 bg-slate-100 text-slate-600",
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
}) {
  const [selectedUpdateType, setSelectedUpdateType] = useState("patch");
  const status = String(detail?.status || "").toLowerCase();
  const releaseType = String(detail?.releaseType || "").toLowerCase();
  const currentVersionLabel = formatVersionLabel(detail);
  const releaseLabel = releaseType ? prettyReleaseType(releaseType) : "Roadmap";
  const statusTag = statusTagCopy[status] || {
    label: status ? status.replace(/_/g, " ") : "Unknown status",
    className: "border-slate-200 bg-slate-50 text-slate-700",
  };

  const drafts = useMemo(
    () =>
      sortDraftsNewestFirst(
        detail?.versions?.filter((version) => {
          const versionStatus = String(version.status).toLowerCase();
          return ["draft", "changes_requested", "pending_review"].includes(
            versionStatus,
          );
        }) || [],
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
  const selectedUpdateLabel =
    updateOptions.find((option) => option.value === selectedUpdateType)?.label ||
    selectedUpdateCopy.label;

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
    <ModuleCard className="overflow-visible p-0">
      <div className="border-b border-[#B9D8CC]/70 px-4 py-3">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div className="flex items-center gap-2 text-xs font-extrabold uppercase tracking-wide text-slate-700">
            <History size={14} className="text-[#1F6F5F]" /> Versions
          </div>
          {versionOptions.length > 0 && (
            <div className="w-64 sm:w-72">
              <AppSelect
                className="min-w-0"
                buttonClassName="!h-10 !px-3 !text-sm !font-medium"
                optionClassName="!text-sm !font-medium"
                value={detail.roadmapVersionId}
                options={versionOptions}
                ariaLabel="Select roadmap version"
                onChange={onVersionChange}
              />
            </div>
          )}
        </div>
      </div>

      <div className="space-y-0">
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
                {releaseLabel} version
              </p>
            </div>
          </div>

          <span
            className={`inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 text-xs font-extrabold ${statusTag.className}`}
          >
            {status === "pending_review" ? <Clock3 size={13} /> : null}
            {statusTag.label}
          </span>
        </div>

        {status === "published" && existingDraft && (
          <div className="border-t border-[#B9D8CC]/70 p-4">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <div className="flex min-w-0 items-center gap-3">
                <div className="grid h-9 w-9 shrink-0 place-items-center rounded-lg bg-[#F7F1E8] text-[#B7791F]">
                  <ExistingDraftIcon size={18} />
                </div>
                <div className="min-w-0">
                  <h2 className="truncate text-sm font-extrabold text-[#18332D]">
                    Open workflow
                  </h2>
                  <p className="mt-0.5 truncate text-xs font-semibold text-slate-600">
                    {existingDraftLabel} ·{" "}
                    {prettyReleaseType(existingDraftReleaseType)} update
                  </p>
                </div>
              </div>
              <ModuleButton
                variant="secondary"
                onClick={() => onVersionChange(existingDraft.roadmapVersionId)}
                disabled={isBusy}
              >
                Open version
              </ModuleButton>
            </div>
          </div>
        )}

        {status === "published" && !existingDraft && (
          <div className="border-t border-[#B9D8CC]/70 p-4">
            <div className="grid gap-4 lg:grid-cols-[minmax(0,1fr)_auto] lg:items-center">
              <div className="flex min-w-0 items-center gap-3">
                <div className="grid h-9 w-9 shrink-0 place-items-center rounded-lg bg-[#6FCF97]/16 text-[#1F6F5F]">
                  <SelectedUpdateIcon size={18} />
                </div>
                <div className="min-w-0">
                  <h2 className="truncate text-sm font-extrabold text-[#18332D]">
                    Create update draft
                  </h2>
                  <p className="mt-0.5 truncate text-xs font-semibold text-slate-600">
                    {selectedUpdateLabel}
                  </p>
                </div>
              </div>
              <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
                <div className="w-full sm:w-64">
                  <AppSelect
                    buttonClassName="!text-sm !font-medium"
                    optionClassName="!text-sm !font-medium"
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
          </div>
        )}
      </div>
    </ModuleCard>
  );
}
