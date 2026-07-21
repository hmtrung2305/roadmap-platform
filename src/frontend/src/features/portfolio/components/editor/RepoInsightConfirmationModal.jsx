import { useEffect, useRef } from "react";
import { createPortal } from "react-dom";
import { CheckCircle2, Sparkles, X } from "lucide-react";

export default function RepoInsightConfirmationModal({
  open,
  repositoryName,
  force = false,
  onCancel,
  onConfirm,
}) {
  const confirmButtonRef = useRef(null);

  useEffect(() => {
    if (!open) return undefined;

    const previousOverflow = document.body.style.overflow;
    document.body.style.overflow = "hidden";
    confirmButtonRef.current?.focus();

    const handleKeyDown = (event) => {
      if (event.key === "Escape") {
        onCancel?.();
      }
    };

    window.addEventListener("keydown", handleKeyDown);

    return () => {
      document.body.style.overflow = previousOverflow;
      window.removeEventListener("keydown", handleKeyDown);
    };
  }, [open, onCancel]);

  if (!open || typeof document === "undefined") return null;

  const actionLabel = force ? "Regenerate insight" : "Generate insight";

  return createPortal(
    <div
      className="fixed inset-0 z-[100] flex items-center justify-center bg-[#18332D]/45 px-4 py-6 backdrop-blur-[2px]"
      role="presentation"
      onMouseDown={(event) => {
        if (event.target === event.currentTarget) {
          onCancel?.();
        }
      }}
    >
      <section
        role="dialog"
        aria-modal="true"
        aria-labelledby="repo-insight-confirmation-title"
        className="w-full max-w-[480px] rounded-2xl border border-[#B9D8CC] bg-[#FFFDF8] p-5 text-[#18332D] shadow-[0_24px_70px_rgba(24,51,45,0.24)] sm:p-6"
      >
        <div className="flex items-start justify-between gap-4">
          <div className="min-w-0">
            <div className="inline-flex size-10 items-center justify-center rounded-xl bg-[#6FCF97]/20 text-[#1F6F5F] ring-1 ring-[#B9D8CC]">
              <Sparkles size={20} />
            </div>
            <h2
              id="repo-insight-confirmation-title"
              className="mt-3 text-xl font-extrabold leading-7"
            >
              {force
                ? "Regenerate repository insight?"
                : "Generate repository insight?"}
            </h2>
            <p
              className="mt-1 truncate text-sm font-semibold text-[#667A73]"
              title={repositoryName}
            >
              {repositoryName || "Selected repository"}
            </p>
          </div>

          <button
            type="button"
            onClick={onCancel}
            aria-label="Close confirmation"
            className="grid size-9 shrink-0 place-items-center rounded-lg text-[#667A73] transition-colors hover:bg-[#F7F1E8] hover:text-[#18332D]"
          >
            <X size={18} />
          </button>
        </div>

        <div className="mt-5 rounded-xl border border-[#DCEBE5] bg-white p-4">
          <p className="text-sm font-extrabold text-[#18332D]">
            Your README should include:
          </p>

          <ul className="mt-3 space-y-2.5 text-sm leading-5 text-[#34544C]">
            <li className="flex gap-2.5">
              <CheckCircle2
                className="mt-0.5 shrink-0 text-[#2FA084]"
                size={17}
              />
              <span>
                A README file with enough written project information.
              </span>
            </li>
            <li className="flex gap-2.5">
              <CheckCircle2
                className="mt-0.5 shrink-0 text-[#2FA084]"
                size={17}
              />
              <span>
                At least 400 meaningful characters and 70 valid words after
                cleanup.
              </span>
            </li>
            <li className="flex gap-2.5">
              <CheckCircle2
                className="mt-0.5 shrink-0 text-[#2FA084]"
                size={17}
              />
              <span>
                At least 2 content signals, such as Features, Setup, Tech Stack,
                lists, or code examples.
              </span>
            </li>
          </ul>
        </div>

        <p className="mt-3 rounded-xl bg-[#F7F1E8] px-3.5 py-3 text-sm leading-5 text-[#34544C]">
          AI also checks whether the README clearly explains the project purpose
          or main use case, and it only uses the provided README and repository
          metadata.
        </p>

        <div className="mt-5 flex justify-end gap-2.5">
          <button
            type="button"
            onClick={onCancel}
            className="rounded-lg border border-[#B9D8CC] bg-white px-4 py-2 text-sm font-bold text-[#34544C] transition-colors hover:bg-[#F7F1E8]"
          >
            Cancel
          </button>
          <button
            ref={confirmButtonRef}
            type="button"
            onClick={onConfirm}
            className="inline-flex items-center gap-2 rounded-lg bg-[#2FA084] px-4 py-2 text-sm font-extrabold text-white shadow-sm shadow-emerald-900/20 transition-colors hover:bg-[#1F6F5F] focus:outline-none focus:ring-2 focus:ring-[#2FA084]/35 focus:ring-offset-2"
          >
            <Sparkles size={15} />
            {actionLabel}
          </button>
        </div>
      </section>
    </div>,
    document.body,
  );
}
