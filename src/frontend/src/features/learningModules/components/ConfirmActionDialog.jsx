import { useId } from "react";
import { createPortal } from "react-dom";
import { AlertTriangle, CheckCircle2 } from "lucide-react";
import { ModuleButton } from "../../../features/learningModules/components/learningModuleUi";

export default function ConfirmActionDialog({
  isOpen,
  tone = "danger",
  title,
  description,
  confirmLabel = "Confirm",
  cancelLabel = "Cancel",
  isConfirming = false,
  onCancel,
  onConfirm,
}) {
  const titleId = useId();
  const descriptionId = useId();

  if (!isOpen || typeof document === "undefined") return null;

  const isDanger = tone === "danger";
  const isSuccess = tone === "success";

  const iconClass = isDanger
    ? "bg-rose-50 text-rose-700"
    : isSuccess
      ? "bg-[#6FCF97]/20 text-[#1F6F5F]"
      : "bg-amber-50 text-amber-700";

  const buttonVariant = isDanger ? "danger" : isSuccess ? "primary" : "soft";
  const Icon = isSuccess ? CheckCircle2 : AlertTriangle;

  return createPortal(
    <div className="fixed inset-0 z-50 grid place-items-center bg-[#18332D]/35 px-4 backdrop-blur-sm">
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        aria-describedby={descriptionId}
        className="w-full max-w-md rounded-xl border border-[#B9D8CC] bg-white p-5 shadow-2xl"
      >
        <div className="flex items-start gap-3">
          <div
            className={`grid h-10 w-10 shrink-0 place-items-center rounded-full ${iconClass}`}
          >
            <Icon size={20} />
          </div>

          <div className="min-w-0">
            <h2 id={titleId} className="text-base font-extrabold text-[#18332D]">
              {title}
            </h2>
            <p
              id={descriptionId}
              className="mt-1 text-sm font-semibold leading-6 text-slate-600"
            >
              {description}
            </p>
          </div>
        </div>

        <div className="mt-5 flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
          <ModuleButton
            variant="secondary"
            onClick={onCancel}
            disabled={isConfirming}
          >
            {cancelLabel}
          </ModuleButton>
          <ModuleButton
            variant={buttonVariant}
            onClick={onConfirm}
            disabled={isConfirming}
          >
            {isConfirming ? "Working..." : confirmLabel}
          </ModuleButton>
        </div>
      </div>
    </div>,
    document.body,
  );
}
