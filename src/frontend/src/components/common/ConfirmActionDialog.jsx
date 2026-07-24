import { useId } from "react";
import { createPortal } from "react-dom";
import { AlertTriangle, CheckCircle2 } from "lucide-react";

const BUTTON_VARIANTS = {
  primary:
    "bg-[#2FA084] text-white shadow-sm hover:bg-[#1F6F5F] disabled:cursor-not-allowed disabled:bg-slate-300 disabled:text-slate-600",
  secondary:
    "border border-[#B9D8CC] bg-white text-[#18332D] hover:border-[#2FA084] hover:bg-[#F7F1E8] disabled:cursor-not-allowed disabled:bg-slate-50 disabled:text-slate-500",
  danger:
    "border border-rose-200 bg-white text-rose-600 hover:border-rose-300 hover:bg-rose-50 disabled:cursor-not-allowed disabled:bg-slate-50 disabled:text-slate-500",
  soft:
    "border border-[#6FCF97]/40 bg-[#6FCF97]/15 text-[#1F6F5F] hover:bg-[#6FCF97]/25 disabled:cursor-not-allowed disabled:bg-slate-50 disabled:text-slate-500",
};

function DialogButton({
  children,
  variant = "primary",
  type = "button",
  className = "",
  ...props
}) {
  const variantClass =
    BUTTON_VARIANTS[variant] ?? BUTTON_VARIANTS.primary;

  return (
    <button
      type={type}
      className={[
        "inline-flex min-w-[88px] items-center justify-center gap-2",
        "whitespace-nowrap rounded-lg px-3 py-1.5",
        "text-xs font-bold leading-tight transition",
        "focus:outline-none focus:ring-2 focus:ring-[#6FCF97]/30",
        variantClass,
        className,
      ]
        .filter(Boolean)
        .join(" ")}
      {...props}
    >
      {children}
    </button>
  );
}

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

  if (!isOpen || typeof document === "undefined") {
    return null;
  }

  const isDanger = tone === "danger";
  const isSuccess = tone === "success";

  const iconClass = isDanger
    ? "bg-rose-50 text-rose-700"
    : isSuccess
      ? "bg-[#6FCF97]/20 text-[#1F6F5F]"
      : "bg-amber-50 text-amber-700";

  const buttonVariant = isDanger
    ? "danger"
    : isSuccess
      ? "primary"
      : "soft";

  const Icon = isSuccess ? CheckCircle2 : AlertTriangle;

  const handleBackdropClick = (event) => {
    if (event.target === event.currentTarget && !isConfirming) {
      onCancel?.();
    }
  };

  return createPortal(
    <div
      className="fixed inset-0 z-50 grid place-items-center bg-[#18332D]/35 px-4 backdrop-blur-sm"
      onMouseDown={handleBackdropClick}
    >
      <div
        role="dialog"
        aria-modal="true"
        aria-labelledby={titleId}
        aria-describedby={description ? descriptionId : undefined}
        className="w-full max-w-md rounded-xl border border-[#B9D8CC] bg-white p-5 shadow-2xl"
        onMouseDown={(event) => event.stopPropagation()}
      >
        <div className="flex items-start gap-3">
          <div
            aria-hidden="true"
            className={`grid h-10 w-10 shrink-0 place-items-center rounded-full ${iconClass}`}
          >
            <Icon size={20} />
          </div>

          <div className="min-w-0 flex-1">
            <h2
              id={titleId}
              className="text-base font-extrabold text-[#18332D]"
            >
              {title}
            </h2>

            {description ? (
              <p
                id={descriptionId}
                className="mt-1 text-sm font-semibold leading-6 text-slate-600"
              >
                {description}
              </p>
            ) : null}
          </div>
        </div>

        <div className="mt-5 flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
          <DialogButton
            variant="secondary"
            onClick={onCancel}
            disabled={isConfirming}
          >
            {cancelLabel}
          </DialogButton>

          <DialogButton
            variant={buttonVariant}
            onClick={onConfirm}
            disabled={isConfirming}
          >
            {isConfirming ? "Working..." : confirmLabel}
          </DialogButton>
        </div>
      </div>
    </div>,
    document.body,
  );
}