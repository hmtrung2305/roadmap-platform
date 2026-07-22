import { useState } from "react";

import ConfirmActionDialog from "../../learningModules/components/ConfirmActionDialog";

export default function RoadmapMigrationNotice({
  availableUpdate,
  isMigrating = false,
  onMigrate,
}) {
  const [isConfirmationOpen, setIsConfirmationOpen] = useState(false);

  if (!availableUpdate) return null;

  const releaseType = String(availableUpdate.releaseType || "version").toLowerCase();
  const isMajorUpdate = releaseType === "major";
  const currentVersionLabel = availableUpdate.currentVersionLabel || "current version";
  const targetVersionLabel = availableUpdate.targetVersionLabel || "new version";
  const targetTitle = availableUpdate.title?.trim();
  const migrationDescription = [
    `Move your enrollment from ${currentVersionLabel} to ${targetVersionLabel}${
      targetTitle ? ` (${targetTitle})` : ""
    }.`,
    isMajorUpdate
      ? "This major update may change the roadmap structure and required learning path."
      : "This minor update may add optional learning while preserving compatible progress.",
    "You can continue learning on your current version until you confirm this migration.",
  ].join(" ");

  async function handleConfirm() {
    const didMigrate = await onMigrate?.();

    if (didMigrate !== false) {
      setIsConfirmationOpen(false);
    }
  }

  return (
    <>
      <div className="mt-2 flex flex-wrap items-center justify-between gap-3 rounded-lg border border-[#B9D8CC] bg-[#F4FBF8] px-3 py-2">
        <div className="min-w-0">
          <p className="text-xs font-black text-[#18332D]">
            {releaseType.toUpperCase()} update available
          </p>
          <p className="mt-0.5 text-xs font-semibold text-slate-600">
            Continue learning on {currentVersionLabel}, or review the update to {targetVersionLabel}.
          </p>
        </div>
        <button
          type="button"
          onClick={() => setIsConfirmationOpen(true)}
          disabled={isMigrating}
          className="rounded-lg border border-[#B9D8CC] bg-[#2FA084] px-4 py-2 text-xs font-extrabold tracking-[0.08em] text-white shadow-sm disabled:opacity-60"
        >
          Review update
        </button>
      </div>

      <ConfirmActionDialog
        isOpen={isConfirmationOpen}
        tone="warning"
        title={isMajorUpdate ? "Confirm major roadmap migration" : "Confirm roadmap migration"}
        description={migrationDescription}
        confirmLabel={isMajorUpdate ? "Confirm major migration" : "Confirm migration"}
        isConfirming={isMigrating}
        onCancel={() => setIsConfirmationOpen(false)}
        onConfirm={handleConfirm}
      />
    </>
  );
}
