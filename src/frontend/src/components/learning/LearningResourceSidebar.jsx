import { BookOpen, CheckCircle2, Circle, X } from "lucide-react";
import { useNavigate, useParams } from "react-router-dom";
import AuthLogo from "../auth/AuthLogo";

export default function LearningResourceSidebar({
  resources = [],
  isOpen,
  width = 248,
  topOffset = 0,
  onStartResize,
  onClose,
}) {
  const navigate = useNavigate();
  const { resourceId } = useParams();

  return (
    <>
      {isOpen && (
        <div
          className="fixed inset-0 z-30 bg-slate-950/20 xl:hidden"
          onClick={onClose}
        />
      )}

      <aside
        style={{
          width,
          top: topOffset,
          height: `calc(100vh - ${topOffset}px)`,
        }}
        className={`fixed left-0 z-40 flex max-w-[calc(100vw-1rem)] flex-col overflow-hidden border-r border-[#B9D8CC] bg-white shadow-xl shadow-emerald-900/10 transition-[transform,top,height] duration-300 ${
          isOpen ? "translate-x-0" : "-translate-x-full"
        }`}
      >
        <div className="flex h-20 shrink-0 items-center justify-between border-b border-[#B9D8CC] px-5">
          <div className="min-w-0">
            <AuthLogo compact showTagline={false} />
            <p className="mt-1 text-sm font-medium text-slate-500">
              Learning Resources
            </p>
          </div>

          <button
            type="button"
            onClick={onClose}
            className="rounded-lg p-2 text-slate-400 hover:bg-[#6FCF97]/20 hover:text-[#1F6F5F]"
            aria-label="Close resources"
          >
            <X size={18} />
          </button>
        </div>

        <div className="min-h-0 flex-1 overflow-y-auto p-4">
          {resources.length === 0 ? (
            <div className="rounded-lg border border-dashed border-[#B9D8CC] bg-[#F7F1E8] p-4 text-sm font-medium text-slate-500">
              No documents yet.
            </div>
          ) : (
            <div className="space-y-2">
              {resources.map((resource, index) => {
                const currentResourceId =
                  resource.resourceId ||
                  resource.id ||
                  resource.learningResourceId ||
                  resource.documentId;

                const isActive = String(currentResourceId) === String(resourceId);

                return (
                  <button
                    key={currentResourceId || `${resource.title}-${index}`}
                    type="button"
                    onClick={() => {
                      if (!currentResourceId) return;

                      navigate(`/study/${currentResourceId}`, {
                        state: { resource },
                      });
                    }}
                    className={`w-full rounded-lg px-3 py-3 text-left text-sm transition ${
                      isActive
                        ? "bg-[#6FCF97]/20 text-[#1F6F5F] ring-1 ring-[#6FCF97]"
                        : "text-slate-600 hover:bg-[#F7F1E8] hover:text-[#18332D]"
                    }`}
                  >
                    <div className="flex items-start gap-2">
                      {resource.isCompleted ? (
                        <CheckCircle2
                          size={16}
                          className="mt-0.5 shrink-0 text-[#2FA084]"
                        />
                      ) : (
                        <Circle
                          size={16}
                          className={`mt-0.5 shrink-0 ${
                            isActive ? "text-[#2FA084]" : "text-slate-400"
                          }`}
                        />
                      )}

                      <div className="min-w-0">
                        <p className="line-clamp-2 font-extrabold">
                          {resource.title || "Untitled document"}
                        </p>

                        <p className="mt-1 text-xs opacity-75">
                          {resource.skillName || "General"} · {resource.durationMinutes || 15} min
                        </p>
                      </div>
                    </div>
                  </button>
                );
              })}
            </div>
          )}
        </div>

        <button
          type="button"
          onClick={() => navigate("/resources")}
          className="mx-4 mb-4 inline-flex items-center justify-center gap-2 rounded-lg border border-[#B9D8CC] bg-white px-4 py-2 text-sm font-bold text-[#1F6F5F] transition hover:bg-[#6FCF97]/20"
        >
          <BookOpen size={16} />
          Manage resources
        </button>

        <div
          onMouseDown={onStartResize}
          className="absolute right-0 top-0 h-full w-1 cursor-col-resize bg-transparent hover:bg-[#6FCF97]"
          title="Resize sidebar"
        />
      </aside>
    </>
  );
}
