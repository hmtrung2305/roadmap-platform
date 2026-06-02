import { BookOpen, CheckCircle2, Circle, X } from "lucide-react";
import { useNavigate, useParams } from "react-router-dom";

export default function LearningResourceSidebar({
  resources,
  isOpen,
  width = 200,
  onStartResize,
  onClose,
}) {
  const navigate = useNavigate();
  const { resourceId } = useParams();

  return (
    <>
      {isOpen && (
        <div
          className="fixed inset-0 z-30 bg-slate-950/20 lg:hidden"
          onClick={onClose}
        />
      )}

      <aside
        style={{ width }}
        className={`fixed left-0 top-16 z-40 h-[calc(100vh-4rem)] border-r border-slate-200 bg-white shadow-xl transition-transform duration-300 ${
          isOpen ? "translate-x-0" : "-translate-x-full"
        }`}
      >
        <div className="flex items-center justify-between border-b border-slate-200 p-5">
          <div>
            <h2 className="text-lg font-bold text-blue-700">TechMap</h2>
            <p className="mt-1 text-sm text-slate-500">Learning Resources</p>
          </div>

          <button
            type="button"
            onClick={onClose}
            className="rounded-xl p-2 text-slate-400 hover:bg-slate-100 hover:text-slate-700"
          >
            <X size={18} />
          </button>
        </div>

        <div className="h-[calc(100%-81px)] overflow-y-auto p-4">
          <div className="mb-3 flex items-center gap-2 text-sm font-semibold text-slate-800">
            <BookOpen size={16} />
            Documents
          </div>

          {resources.length === 0 ? (
            <div className="rounded-xl border border-dashed border-slate-300 bg-slate-50 p-4 text-sm text-slate-500">
              Chưa có tài liệu nào.
            </div>
          ) : (
            <div className="space-y-2">
              {resources.map((resource, index) => {
                const currentResourceId =
                  resource.resourceId ||
                  resource.id ||
                  resource.learningResourceId ||
                  resource.documentId;

                const isActive =
                  String(currentResourceId) === String(resourceId);

                return (
                  <button
                    key={currentResourceId || `${resource.title}-${index}`}
                    type="button"
                    onClick={() => {
                      if (!currentResourceId) return;

                      navigate(`/study/${currentResourceId}`, {
                        state: {
                          resource,
                        },
                      });
                    }}
                    className={`w-full rounded-xl px-3 py-3 text-left text-sm transition ${
                      isActive
                        ? "bg-blue-50 text-blue-700 ring-1 ring-blue-200"
                        : "text-slate-600 hover:bg-slate-50 hover:text-slate-900"
                    }`}
                  >
                    <div className="flex items-start gap-2">
                      {resource.isCompleted ? (
                        <CheckCircle2
                          size={16}
                          className="mt-0.5 shrink-0 text-emerald-600"
                        />
                      ) : (
                        <Circle
                          size={16}
                          className={`mt-0.5 shrink-0 ${
                            isActive ? "text-blue-600" : "text-slate-400"
                          }`}
                        />
                      )}

                      <div className="min-w-0">
                        <p className="line-clamp-2 font-semibold">
                          {resource.title}
                        </p>

                        <p className="mt-1 text-xs opacity-75">
                          {resource.skillName || "General"} ·{" "}
                          {resource.durationMinutes || 15} phút
                        </p>
                      </div>
                    </div>
                  </button>
                );
              })}
            </div>
          )}
        </div>

        {/* Resize handle */}
        <div
          onMouseDown={onStartResize}
          className="absolute right-0 top-0 h-full w-1 cursor-col-resize bg-transparent hover:bg-blue-300"
          title="Resize sidebar"
        />
      </aside>
    </>
  );
}
