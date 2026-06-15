import { useEffect, useMemo, useRef, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import {
  ArrowLeft,
  Bot,
  CheckCircle2,
  Circle,
  Clock,
  Coins,
  FileText,
  Loader2,
  PanelLeftOpen,
  Send,
  Trash2,
  Trophy,
  X,
} from "lucide-react";
import { toast } from "react-toastify";

import { aiCreditApi } from "../api/aiCreditApi";
import { learningModuleApi } from "../api/learningModuleApi";
import DocumentLoading from "../components/document/DocumentLoading";
import DocumentReader from "../components/document/DocumentReader";
import {
  formatHours,
  getEnrollmentStatus,
  getLessonProgress,
  getProgress,
  ModuleBadge,
  ModuleButton,
} from "../components/learningModules/learningModuleUi";

const HEADER_HEIGHT = 72;
const PAGE_GAP = 20;

const SIDEBAR_MIN_WIDTH = 240;
const SIDEBAR_MAX_WIDTH = 420;
const CHAT_MIN_WIDTH = 320;
const CHAT_MAX_WIDTH = 640;

function areAllLessonsCompleted(module) {
  const lessons = module?.lessons || [];
  const lessonProgress = module?.enrollment?.lessonProgress || {};

  if (lessons.length === 0) {
    return true;
  }

  return lessons.every(
    (lesson) => lessonProgress[lesson.skillModuleLessonId] === "completed",
  );
}

function isSubmittedToday(attempt) {
  if (attempt?.status !== "submitted" || !attempt.submittedAt) {
    return false;
  }

  const submittedAt = new Date(attempt.submittedAt);
  const now = new Date();

  return (
    submittedAt.getFullYear() === now.getFullYear()
    && submittedAt.getMonth() === now.getMonth()
    && submittedAt.getDate() === now.getDate()
  );
}

export default function StudyRoomPage() {
  const { slug } = useParams();
  const navigate = useNavigate();
  const lastScrollTopRef = useRef(0);

  const [module, setModule] = useState(null);
  const [activeLessonId, setActiveLessonId] = useState(null);
  const [lessonContent, setLessonContent] = useState(null);

  const [isLoadingModule, setIsLoadingModule] = useState(true);
  const [isLoadingLesson, setIsLoadingLesson] = useState(false);
  const [isStarting, setIsStarting] = useState(false);
  const [moduleError, setModuleError] = useState(null);
  const [lessonError, setLessonError] = useState(null);

  const [showQuiz, setShowQuiz] = useState(false);
  const [isSidebarOpen, setIsSidebarOpen] = useState(true);
  const [isChatOpen, setIsChatOpen] = useState(true);
  const [isHeaderVisible, setIsHeaderVisible] = useState(true);

  const [sidebarWidth, setSidebarWidth] = useState(280);
  const [chatWidth, setChatWidth] = useState(380);
  const [resizingPanel, setResizingPanel] = useState(null);

  const activeLesson = useMemo(() => {
    if (!module?.lessons?.length) return null;

    return (
      module.lessons.find((lesson) => lesson.skillModuleLessonId === activeLessonId) ||
      module.lessons[0]
    );
  }, [module, activeLessonId]);

  const sidePanelTopOffset = isHeaderVisible ? HEADER_HEIGHT : 0;
  const mainLeftPadding = isSidebarOpen ? sidebarWidth + PAGE_GAP : PAGE_GAP;
  const mainRightPadding = isChatOpen ? chatWidth + PAGE_GAP : PAGE_GAP;

  const loadModule = async ({ silent = false } = {}) => {
    if (!slug) return;

    try {
      if (!silent) {
        setIsLoadingModule(true);
      }

      setModuleError(null);

      const data = await learningModuleApi.getPublishedModuleBySlug(slug);

      setModule(data);

      const nextLessonId =
        data.enrollment?.lastAccessedLessonId ||
        data.lessons?.[0]?.skillModuleLessonId ||
        null;

      setActiveLessonId((current) => current || nextLessonId);
    } catch (error) {
      setModuleError(error?.message || "Unable to load this learning module.");
      setModule(null);
    } finally {
      if (!silent) {
        setIsLoadingModule(false);
      }
    }
  };

  useEffect(() => {
    loadModule();
  }, [slug]);

  useEffect(() => {
    if (!resizingPanel) return;

    document.body.style.userSelect = "none";
    document.body.style.cursor = "col-resize";

    const handleMouseMove = (event) => {
      event.preventDefault();

      if (resizingPanel === "sidebar") {
        const nextWidth = Math.min(
          Math.max(event.clientX - PAGE_GAP, SIDEBAR_MIN_WIDTH),
          SIDEBAR_MAX_WIDTH,
        );

        setSidebarWidth(nextWidth);
        return;
      }

      if (resizingPanel === "chat") {
        const nextWidth = Math.min(
          Math.max(window.innerWidth - event.clientX - PAGE_GAP, CHAT_MIN_WIDTH),
          CHAT_MAX_WIDTH,
        );

        setChatWidth(nextWidth);
      }
    };

    const handleMouseUp = () => {
      setResizingPanel(null);
      document.body.style.userSelect = "";
      document.body.style.cursor = "";
    };

    document.addEventListener("mousemove", handleMouseMove);
    document.addEventListener("mouseup", handleMouseUp);

    return () => {
      document.body.style.userSelect = "";
      document.body.style.cursor = "";

      document.removeEventListener("mousemove", handleMouseMove);
      document.removeEventListener("mouseup", handleMouseUp);
    };
  }, [resizingPanel]);

  useEffect(() => {
    let ignore = false;

    async function loadLesson() {
      if (!module?.skillModuleId || !activeLessonId || showQuiz) {
        return;
      }

      try {
        setIsLoadingLesson(true);
        setLessonError(null);

        const data = await learningModuleApi.getLessonContent(
          module.skillModuleId,
          activeLessonId,
        );

        if (!ignore) {
          setLessonContent(data);
        }
      } catch (error) {
        if (!ignore) {
          setLessonContent(null);
          setLessonError(error?.message || "Unable to load this lesson.");
        }
      } finally {
        if (!ignore) {
          setIsLoadingLesson(false);
        }
      }
    }

    loadLesson();

    return () => {
      ignore = true;
    };
  }, [module?.skillModuleId, activeLessonId, showQuiz]);

  useEffect(() => {
    if (
      !module?.skillModuleId ||
      !module?.enrollment ||
      !activeLessonId ||
      showQuiz
    ) {
      return;
    }

    const currentStatus = getLessonProgress(module.enrollment, activeLessonId);

    if (currentStatus) {
      return;
    }

    learningModuleApi
      .updateLessonProgress(module.skillModuleId, activeLessonId, "in_progress")
      .then((result) => {
        setModule((current) => {
          if (!current?.enrollment) return current;

          return {
            ...current,
            enrollment: {
              ...current.enrollment,
              progressPercent: result?.progressPercent ?? current.enrollment.progressPercent,
              lessonProgress: {
                ...(current.enrollment.lessonProgress || {}),
                [activeLessonId]: "in_progress",
              },
              lastAccessedLessonId: activeLessonId,
            },
          };
        });
      })
      .catch(() => {
        // Opening a lesson should not be blocked by a progress tracking failure.
      });
  }, [module?.skillModuleId, module?.enrollment, activeLessonId, showQuiz]);

  const handleDocumentScroll = (event) => {
    const currentScrollTop = event.currentTarget.scrollTop;
    const previousScrollTop = lastScrollTopRef.current;
    const scrollDelta = currentScrollTop - previousScrollTop;

    if (currentScrollTop <= 12) {
      setIsHeaderVisible(true);
      lastScrollTopRef.current = currentScrollTop;
      return;
    }

    if (scrollDelta > 8) {
      setIsHeaderVisible(false);
    }

    if (scrollDelta < -8) {
      setIsHeaderVisible(true);
    }

    lastScrollTopRef.current = currentScrollTop;
  };

  const handleStartModule = async () => {
    if (!module) return;

    try {
      setIsStarting(true);
      await learningModuleApi.enroll(module.skillModuleId);
      toast.success("Module started.");
      await loadModule({ silent: true });
    } catch (error) {
      toast.error(error?.message || "Unable to start this module.");
    } finally {
      setIsStarting(false);
    }
  };

  const handleCompleteLesson = async () => {
    if (!module?.skillModuleId || !activeLesson) return;

    try {
      const result = await learningModuleApi.updateLessonProgress(
        module.skillModuleId,
        activeLesson.skillModuleLessonId,
        "completed",
      );

      setModule((current) => {
        if (!current?.enrollment) return current;

        return {
          ...current,
          enrollment: {
            ...current.enrollment,
            status: result?.enrollmentStatus || current.enrollment.status,
            progressPercent: result?.progressPercent ?? current.enrollment.progressPercent,
            lessonProgress: {
              ...(current.enrollment.lessonProgress || {}),
              [activeLesson.skillModuleLessonId]: "completed",
            },
            lastAccessedLessonId: activeLesson.skillModuleLessonId,
          },
        };
      });

      toast.success("Lesson marked complete.");
    } catch (error) {
      toast.error(error?.message || "Unable to update lesson progress.");
    }
  };

  if (isLoadingModule) {
    return (
      <div className="min-h-screen bg-[#F7F1E8] p-6">
        <DocumentLoading />
      </div>
    );
  }

  if (moduleError || !module) {
    return (
      <div className="min-h-screen bg-[#F7F1E8] p-6 text-[#18332D]">
        <div className="mx-auto max-w-2xl rounded-lg border border-red-200 bg-red-50 p-6 text-sm font-bold text-red-700">
          {moduleError || "This learning module could not be loaded."}
        </div>
      </div>
    );
  }

  if (getEnrollmentStatus(module) === "not_started") {
    return (
      <div className="min-h-screen bg-[#F7F1E8] p-6 text-[#18332D]">
        <button
          type="button"
          onClick={() => navigate("/learning-modules")}
          className="mb-5 inline-flex items-center gap-2 text-sm font-bold text-[#1F6F5F]"
        >
          <ArrowLeft size={16} />
          Back to learning modules
        </button>

        <section className="mx-auto max-w-4xl rounded-xl border border-[#B9D8CC] bg-white p-7 shadow-[0_20px_60px_rgba(31,111,95,0.10)]">
          <ModuleBadge>{module.skillName || "Learning module"}</ModuleBadge>

          <h1 className="mt-3 text-3xl font-black tracking-[-0.035em] text-[#18332D]">
            {module.title}
          </h1>

          <p className="mt-3 max-w-3xl text-sm font-semibold leading-7 text-slate-700">
            {module.description || "Start this module to open the study room."}
          </p>

          <div className="mt-5 flex flex-wrap gap-3 text-xs font-bold text-slate-600">
            <span className="inline-flex items-center gap-1 rounded-lg border border-[#B9D8CC] bg-[#F7F1E8] px-3 py-2">
              <FileText size={14} />
              {module.lessons?.length || 0} lessons
            </span>
            <span className="inline-flex items-center gap-1 rounded-lg border border-[#B9D8CC] bg-[#F7F1E8] px-3 py-2">
              <Clock size={14} />
              {formatHours(module.estimatedHours)}
            </span>
          </div>

          <div className="mt-7">
            <ModuleButton size="md" onClick={handleStartModule} disabled={isStarting}>
              {isStarting ? "Starting..." : "Start module"}
            </ModuleButton>
          </div>
        </section>
      </div>
    );
  }

  const activeLessonStatus = getLessonProgress(
    module.enrollment,
    activeLesson?.skillModuleLessonId,
  );
  const isQuizUnlocked = areAllLessonsCompleted(module);

  return (
    <div className="relative h-screen overflow-hidden bg-[#F7F1E8] text-[#18332D]">
      <div
        className={`fixed left-0 right-0 top-0 z-50 transition-transform duration-300 ease-out ${
          isHeaderVisible ? "translate-y-0" : "-translate-y-full"
        }`}
      >
        <StudyRoomHeader
          module={module}
          activeLesson={activeLesson}
          showQuiz={showQuiz}
          activeLessonStatus={activeLessonStatus}
          onBack={() => navigate("/learning-modules")}
          onCompleteLesson={handleCompleteLesson}
        />
      </div>

      <StudyLessonSidebar
        module={module}
        activeLessonId={activeLessonId}
        showQuiz={showQuiz}
        isOpen={isSidebarOpen}
        width={sidebarWidth}
        topOffset={sidePanelTopOffset}
        onStartResize={(event) => {
          event.preventDefault();
          setResizingPanel("sidebar");
        }}
        onClose={() => setIsSidebarOpen(false)}
        onSelectLesson={(lessonId) => {
          setShowQuiz(false);
          setActiveLessonId(lessonId);
        }}
        canStartQuiz={isQuizUnlocked}
        onShowQuiz={() => {
          if (!isQuizUnlocked) {
            toast.info("Complete all lessons before starting the quiz.");
            return;
          }

          setShowQuiz(true);
        }}
      />

      <StudyRoomChatPanel
        module={module}
        activeLessonId={showQuiz ? null : activeLesson?.skillModuleLessonId}
        isOpen={isChatOpen}
        width={chatWidth}
        topOffset={sidePanelTopOffset}
        onStartResize={(event) => {
          event.preventDefault();
          setResizingPanel("chat");
        }}
        onClose={() => setIsChatOpen(false)}
      />

      <main
        onScroll={handleDocumentScroll}
        className="h-full min-h-0 overflow-y-auto transition-[padding] duration-300"
        style={{
          paddingTop: isHeaderVisible ? HEADER_HEIGHT + PAGE_GAP : PAGE_GAP,
          paddingLeft: mainLeftPadding,
          paddingRight: mainRightPadding,
          paddingBottom: PAGE_GAP,
        }}
      >
        <section className="mx-auto min-w-0 max-w-5xl">
          {showQuiz ? (
            <StudyQuiz module={module} canStartQuiz={isQuizUnlocked} onProgressChanged={() => loadModule({ silent: true })} />
          ) : isLoadingLesson ? (
            <DocumentLoading />
          ) : lessonError ? (
            <div className="rounded-lg border border-red-200 bg-red-50 p-6 text-sm font-semibold text-red-700 shadow-sm">
              {lessonError}
            </div>
          ) : (
            <DocumentReader
              markdownContent={
                lessonContent?.markdown ||
                "# Empty lesson\n\nNo lesson content was found."
              }
            />
          )}
        </section>
      </main>

      {!isSidebarOpen && (
        <button
          type="button"
          onClick={() => setIsSidebarOpen(true)}
          className="fixed left-5 z-50 inline-flex items-center gap-2 rounded-lg border border-[#B9D8CC] bg-white px-4 py-2 text-sm font-bold text-[#1F6F5F] shadow-lg shadow-emerald-900/10 transition hover:-translate-y-0.5 hover:bg-[#6FCF97]/20"
          style={{ top: isHeaderVisible ? HEADER_HEIGHT + 16 : 16 }}
        >
          <PanelLeftOpen size={17} />
          Lessons
        </button>
      )}

      {!isChatOpen && (
        <button
          type="button"
          onClick={() => setIsChatOpen(true)}
          className="fixed bottom-6 right-6 z-50 inline-flex items-center gap-2 rounded-lg bg-[#2FA084] px-6 py-3 text-sm font-bold text-white shadow-xl shadow-emerald-900/20 transition hover:-translate-y-0.5 hover:bg-[#1F6F5F]"
        >
          <Bot size={18} />
          Module chat
        </button>
      )}
    </div>
  );
}

function StudyRoomHeader({
  module,
  activeLesson,
  showQuiz,
  activeLessonStatus,
  onBack,
  onCompleteLesson,
}) {
  return (
    <header className="h-[72px] border-b border-[#B9D8CC] bg-white/95 shadow-sm shadow-emerald-900/5 backdrop-blur-xl">
      <div className="flex h-full items-center justify-between gap-4 px-4 sm:px-6">
        <div className="flex min-w-0 items-center gap-4">
          <button
            type="button"
            onClick={onBack}
            className="inline-flex shrink-0 items-center gap-2 rounded-lg px-3 py-2 text-sm font-bold text-slate-600 transition hover:bg-[#6FCF97]/20 hover:text-[#1F6F5F]"
          >
            <ArrowLeft size={18} />
            Back to modules
          </button>

          <div className="hidden h-8 w-px bg-[#B9D8CC] sm:block" />

          <div className="min-w-0">
            <h1 className="line-clamp-1 text-lg font-extrabold text-[#1F6F5F]">
              {showQuiz ? `${module.title} Quiz` : activeLesson?.title || module.title}
            </h1>

            <div className="mt-1 flex flex-wrap items-center gap-3 text-xs font-medium text-slate-500">
              <span className="inline-flex items-center gap-1">
                <FileText size={13} />
                {module.skillName || "Learning module"}
              </span>

              {!showQuiz && activeLesson?.estimatedHours && (
                <span className="inline-flex items-center gap-1">
                  <Clock size={13} />
                  {formatHours(activeLesson.estimatedHours)}
                </span>
              )}
            </div>
          </div>
        </div>

        {!showQuiz && activeLesson && (
          <button
            type="button"
            onClick={onCompleteLesson}
            disabled={activeLessonStatus === "completed"}
            className="inline-flex shrink-0 items-center justify-center gap-2 rounded-lg bg-[#2FA084] px-4 py-2 text-sm font-bold text-white shadow-sm transition hover:bg-[#1F6F5F] disabled:bg-slate-300 disabled:text-slate-600"
          >
            <CheckCircle2 size={16} />
            {activeLessonStatus === "completed" ? "Completed" : "Mark as Completed"}
          </button>
        )}
      </div>
    </header>
  );
}

function StudyLessonSidebar({
  module,
  activeLessonId,
  showQuiz,
  isOpen,
  width,
  topOffset,
  onStartResize,
  onClose,
  onSelectLesson,
  onShowQuiz,
  canStartQuiz,
}) {
  const progress = getProgress(module);

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
        <div
          onMouseDown={onStartResize}
          className="absolute right-0 top-0 h-full w-1 cursor-col-resize bg-transparent hover:bg-[#6FCF97]"
          title="Resize lessons panel"
        />

        <div className="flex h-24 shrink-0 items-start justify-between gap-3 border-b border-[#B9D8CC] px-5 py-4">
          <div className="min-w-0">
            <ModuleBadge>{module.skillName || "Module"}</ModuleBadge>
            <h2 className="mt-2 line-clamp-2 text-sm font-extrabold leading-5 text-[#18332D]">
              {module.title}
            </h2>
          </div>

          <button
            type="button"
            onClick={onClose}
            className="rounded-lg p-2 text-slate-400 hover:bg-[#6FCF97]/20 hover:text-[#1F6F5F]"
            aria-label="Close lessons"
          >
            <X size={18} />
          </button>
        </div>

        <div className="border-b border-[#B9D8CC] px-5 py-4">
          <div className="mb-1 flex justify-between text-xs font-bold text-slate-600">
            <span>{Math.round(progress)}% complete</span>
            <span>{module.lessons?.length || 0} lessons</span>
          </div>
          <div className="h-2 rounded-full bg-slate-100">
            <div
              className="h-2 rounded-full bg-[#2FA084]"
              style={{ width: `${progress}%` }}
            />
          </div>
        </div>

        <div className="min-h-0 flex-1 overflow-y-auto p-3">
          <div className="space-y-2">
            {(module.lessons || []).map((lesson, index) => {
              const status = getLessonProgress(module.enrollment, lesson.skillModuleLessonId);
              const isActive = activeLessonId === lesson.skillModuleLessonId && !showQuiz;

              return (
                <button
                  key={lesson.skillModuleLessonId}
                  type="button"
                  onClick={() => onSelectLesson(lesson.skillModuleLessonId)}
                  className={`w-full rounded-lg px-3 py-3 text-left text-sm transition ${
                    isActive
                      ? "bg-[#6FCF97]/20 text-[#1F6F5F] ring-1 ring-[#6FCF97]"
                      : "text-slate-600 hover:bg-[#F7F1E8] hover:text-[#18332D]"
                  }`}
                >
                  <div className="flex items-start gap-2">
                    {status === "completed" ? (
                      <CheckCircle2 size={16} className="mt-0.5 shrink-0 text-[#2FA084]" />
                    ) : status === "in_progress" ? (
                      <Circle size={16} className="mt-0.5 shrink-0 text-[#2FA084]" />
                    ) : (
                      <span className="mt-0.5 grid h-4 w-4 shrink-0 place-items-center rounded-full border border-slate-300 text-[10px] font-black text-slate-500">
                        {lesson.orderIndex || index + 1}
                      </span>
                    )}

                    <div className="min-w-0">
                      <p className="line-clamp-2 font-extrabold">
                        {lesson.title || "Untitled lesson"}
                      </p>

                      <p className="mt-1 text-xs opacity-75">
                        {lesson.estimatedHours ? formatHours(lesson.estimatedHours) : "Lesson"}
                      </p>
                    </div>
                  </div>
                </button>
              );
            })}

            <button
              type="button"
              onClick={onShowQuiz}
              disabled={!canStartQuiz}
              className={`mt-3 flex w-full items-start gap-2 rounded-lg px-3 py-3 text-left text-sm font-extrabold transition ${
                showQuiz
                  ? "bg-[#6FCF97]/20 text-[#1F6F5F] ring-1 ring-[#6FCF97]"
                  : canStartQuiz
                    ? "text-slate-600 hover:bg-[#F7F1E8] hover:text-[#18332D]"
                    : "cursor-not-allowed bg-slate-50 text-slate-400"
              }`}
              title={canStartQuiz ? "Open final quiz" : "Complete all lessons before starting the quiz."}
            >
              <Trophy size={16} className="mt-0.5 shrink-0" />
              <span>
                Final quiz
                {!canStartQuiz && (
                  <span className="mt-1 block text-xs font-semibold">
                    Complete all lessons first
                  </span>
                )}
              </span>
            </button>
          </div>
        </div>
      </aside>
    </>
  );
}

function StudyRoomChatPanel({
  module,
  activeLessonId,
  isOpen,
  width,
  topOffset,
  onStartResize,
  onClose,
}) {
  const [messages, setMessages] = useState([
    {
      role: "assistant",
      content: "Ask about the current module material. I will answer from the lesson content.",
    },
  ]);
  const [draft, setDraft] = useState("");
  const [isSending, setIsSending] = useState(false);
  const [creditStatus, setCreditStatus] = useState(null);
  const [isLoadingCredits, setIsLoadingCredits] = useState(false);

  const loadCreditStatus = async () => {
    try {
      setIsLoadingCredits(true);
      const status = await aiCreditApi.getStatus();
      setCreditStatus(status);
    } catch {
      setCreditStatus(null);
    } finally {
      setIsLoadingCredits(false);
    }
  };

  useEffect(() => {
    if (isOpen) {
      loadCreditStatus();
    }
  }, [isOpen]);

  const handleSend = async () => {
    const message = draft.trim();

    if (!message || isSending) return;

    if (creditStatus?.remainingCreditsToday <= 0) {
      setMessages((items) => [
        ...items,
        {
          role: "assistant",
          content: "You have no AI credits left today. Try again after the daily reset.",
        },
      ]);
      return;
    }

    const nextMessages = [...messages, { role: "user", content: message }];
    setMessages(nextMessages);
    setDraft("");

    try {
      setIsSending(true);

      const response = await learningModuleApi.sendModuleChatMessage(module.skillModuleId, {
        skillModuleLessonId: activeLessonId,
        message,
        recentMessages: messages.slice(-6).map((item) => ({
          role: item.role,
          content: item.content,
        })),
      });

      setMessages((items) => [
        ...items,
        {
          role: "assistant",
          content: response.answer || "No answer was generated.",
          sources: response.sources || [],
        },
      ]);

      await loadCreditStatus();
    } catch (error) {
      if (error?.status === 429 && error?.raw?.creditStatus) {
        setCreditStatus(error.raw.creditStatus);
      }

      setMessages((items) => [
        ...items,
        {
          role: "assistant",
          content: error?.message || "Unable to send this question right now.",
        },
      ]);
    } finally {
      setIsSending(false);
    }
  };

  const handleClear = () => {
    const confirmed = window.confirm("Clear this module chat?");
    if (!confirmed) return;

    setMessages([
      {
        role: "assistant",
        content: "Ask about the current module material. I will answer from the lesson content.",
      },
    ]);
  };

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
        className={`fixed right-0 z-40 flex max-w-[calc(100vw-1rem)] flex-col border-l border-[#B9D8CC] bg-[#F7F1E8]/95 shadow-xl shadow-emerald-900/10 backdrop-blur-xl transition-[transform,top,height] duration-300 ${
          isOpen ? "translate-x-0" : "translate-x-full"
        }`}
      >
        <div
          onMouseDown={onStartResize}
          className="absolute left-0 top-0 h-full w-1 cursor-col-resize bg-transparent hover:bg-[#6FCF97]"
          title="Resize chat panel"
        />

        <div className="border-b border-[#B9D8CC] bg-white p-4">
          <div className="flex items-start justify-between gap-3">
            <div className="flex min-w-0 items-center gap-3">
              <div className="flex h-10 w-10 shrink-0 items-center justify-center rounded-lg bg-[#6FCF97]/20 text-[#1F6F5F]">
                <Bot size={20} />
              </div>

              <div className="min-w-0">
                <h2 className="font-extrabold text-[#18332D]">Module chat</h2>
                <CreditPill status={creditStatus} isLoading={isLoadingCredits} />
              </div>
            </div>

            <div className="flex shrink-0 items-center gap-1">
              <button
                type="button"
                onClick={handleClear}
                className="rounded-lg p-2 text-slate-400 hover:bg-slate-100 hover:text-red-500"
                title="Clear chat"
              >
                <Trash2 size={16} />
              </button>

              <button
                type="button"
                onClick={onClose}
                className="rounded-lg p-2 text-slate-400 hover:bg-slate-100 hover:text-slate-700"
                title="Close chat"
              >
                <X size={16} />
              </button>
            </div>
          </div>
        </div>

        <div className="min-h-0 flex-1 space-y-3 overflow-y-auto p-4">
          {messages.map((message, index) => (
            <div
              key={`${message.role}-${index}`}
              className={`rounded-lg p-3 text-sm font-medium leading-6 ${
                message.role === "assistant"
                  ? "bg-white text-slate-700 shadow-sm"
                  : "ml-8 bg-[#2FA084] text-white"
              }`}
            >
              {message.content}

              {message.sources?.length > 0 && (
                <div className="mt-2 border-t border-[#B9D8CC]/70 pt-2 text-[11px] font-bold text-[#1F6F5F]">
                  Sources:{" "}
                  {message.sources
                    .map((source) => source.lessonTitle)
                    .filter(Boolean)
                    .join(", ")}
                </div>
              )}
            </div>
          ))}

          {isSending && (
            <div className="inline-flex items-center gap-2 rounded-lg bg-white px-3 py-2 text-sm font-bold text-slate-600 shadow-sm">
              <Loader2 className="animate-spin" size={15} />
              Thinking...
            </div>
          )}
        </div>

        <div className="border-t border-[#B9D8CC] bg-white p-3">
          <textarea
            value={draft}
            onChange={(event) => setDraft(event.target.value)}
            onKeyDown={(event) => {
              if (event.key === "Enter" && !event.shiftKey) {
                event.preventDefault();
                handleSend();
              }
            }}
            className="min-h-20 w-full resize-none rounded-lg border border-[#B9D8CC] bg-white px-3 py-2 text-sm font-semibold outline-none transition placeholder:text-slate-500 focus:border-[#2FA084] focus:ring-2 focus:ring-[#6FCF97]/25"
            placeholder={creditStatus?.remainingCreditsToday <= 0 ? "Daily AI credits used up" : "Ask about this lesson"}
          />

          <ModuleButton
            className="mt-2 w-full"
            onClick={handleSend}
            disabled={isSending || !draft.trim() || creditStatus?.remainingCreditsToday <= 0}
          >
            <Send size={14} />
            {isSending ? "Sending..." : "Send"}
          </ModuleButton>
        </div>
      </aside>
    </>
  );
}


function CreditPill({ status, isLoading }) {
  if (isLoading && !status) {
    return (
      <div className="mt-1 inline-flex items-center gap-1.5 rounded-full border border-[#B9D8CC] bg-[#F7F1E8] px-2.5 py-1 text-[11px] font-extrabold text-slate-500">
        <Loader2 size={11} className="animate-spin" />
        Loading credits
      </div>
    );
  }

  if (!status) {
    return (
      <div className="mt-1 inline-flex items-center gap-1.5 rounded-full border border-slate-200 bg-slate-50 px-2.5 py-1 text-[11px] font-extrabold text-slate-500">
        <Coins size={11} />
        Credits unavailable
      </div>
    );
  }

  const remaining = status.remainingCreditsToday ?? 0;
  const limit = status.dailyCreditLimit ?? 0;
  const isEmpty = remaining <= 0;

  return (
    <div
      className={`mt-1 inline-flex items-center gap-1.5 rounded-full border px-2.5 py-1 text-[11px] font-extrabold ${
        isEmpty
          ? "border-rose-200 bg-rose-50 text-rose-700"
          : "border-[#B9D8CC] bg-[#F7F1E8] text-[#1F6F5F]"
      }`}
      title="Daily AI credits"
    >
      <Coins size={11} />
      {remaining}/{limit} AI credits
    </div>
  );
}

function StudyQuiz({ module, canStartQuiz = true, onProgressChanged }) {
  const [attempts, setAttempts] = useState([]);
  const [attempt, setAttempt] = useState(null);
  const [answers, setAnswers] = useState({});
  const [review, setReview] = useState(null);
  const [viewMode, setViewMode] = useState("overview");
  const [isLoadingAttempts, setIsLoadingAttempts] = useState(false);
  const [isStarting, setIsStarting] = useState(false);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [isLoadingReview, setIsLoadingReview] = useState(false);

  const quiz = module.quiz;
  const submittedAttempts = attempts.filter((item) => item.status === "submitted");
  const submittedAttemptsToday = submittedAttempts.filter(isSubmittedToday);
  const remainingAttempts = quiz?.maxAttempts
    ? Math.max(quiz.maxAttempts - submittedAttemptsToday.length, 0)
    : null;
  const questions = attempt?.quiz?.questions || [];

  const loadAttempts = async () => {
    if (!module?.skillModuleId || !quiz) return;

    try {
      setIsLoadingAttempts(true);
      const data = await learningModuleApi.getQuizAttempts(module.skillModuleId);
      setAttempts(data);
    } catch {
      setAttempts([]);
    } finally {
      setIsLoadingAttempts(false);
    }
  };

  useEffect(() => {
    setAttempt(null);
    setReview(null);
    setAnswers({});
    setViewMode("overview");
    loadAttempts();
  }, [module.skillModuleId]);

  const startAttempt = async () => {
    if (!quiz || isStarting || remainingAttempts === 0) return;

    if (!canStartQuiz) {
      toast.info("Complete all lessons before starting the quiz.");
      return;
    }

    try {
      setIsStarting(true);
      setReview(null);
      setAnswers({});
      const data = await learningModuleApi.startQuizAttempt(module.skillModuleId);
      setAttempt(data);
      setViewMode("attempt");
      await loadAttempts();
    } catch (error) {
      toast.error(error?.message || "Unable to start quiz.");
    } finally {
      setIsStarting(false);
    }
  };

  const openReview = async (attemptId) => {
    try {
      setIsLoadingReview(true);
      const data = await learningModuleApi.getQuizAttemptReview(module.skillModuleId, attemptId);
      setReview(data);
      setAttempt(null);
      setAnswers({});
      setViewMode("review");
    } catch (error) {
      toast.error(error?.message || "Unable to load quiz attempt.");
    } finally {
      setIsLoadingReview(false);
    }
  };

  const handleSubmit = async () => {
    if (!attempt || isSubmitting) return;

    const payload = questions.map((question) => ({
      skillModuleQuizQuestionId: question.skillModuleQuizQuestionId,
      selectedOptionId: answers[question.skillModuleQuizQuestionId],
    }));

    if (payload.some((item) => !item.selectedOptionId)) {
      toast.error("Answer every question before submitting.");
      return;
    }

    try {
      setIsSubmitting(true);
      const data = await learningModuleApi.submitQuizAttempt(
        module.skillModuleId,
        attempt.skillModuleQuizAttemptId,
        payload,
      );

      setReview(data);
      setAttempt(null);
      setAnswers({});
      setViewMode("review");
      await loadAttempts();
      await onProgressChanged?.();
      toast.success("Quiz submitted.");
    } catch (error) {
      toast.error(error?.message || "Unable to submit quiz.");
    } finally {
      setIsSubmitting(false);
    }
  };

  if (!quiz) {
    return (
      <div className="rounded-lg border border-[#B9D8CC] bg-white p-6 text-sm font-bold text-slate-600">
        Quiz is not available.
      </div>
    );
  }

  if (viewMode === "review" && review) {
    return (
      <QuizReview
        review={review}
        isLoadingReview={isLoadingReview}
        onBack={() => setViewMode("overview")}
      />
    );
  }

  if (viewMode === "attempt" && attempt) {
    return (
      <div className="space-y-4 rounded-lg border border-[#B9D8CC] bg-white p-6 shadow-sm">
        <div className="flex flex-wrap items-start justify-between gap-3">
          <div>
            <h2 className="flex items-center gap-2 text-xl font-black text-[#18332D]">
              <Trophy size={20} /> {attempt.quiz?.title || quiz.title || "Module quiz"}
            </h2>
            <p className="mt-1 text-sm font-semibold text-slate-600">
              Attempt {attempt.attemptNo}. Answer every question before submitting.
            </p>
          </div>
          <ModuleButton variant="secondary" onClick={() => setViewMode("overview")}>Back to overview</ModuleButton>
        </div>

        {questions.map((question, index) => (
          <div key={question.skillModuleQuizQuestionId} className="rounded-lg border border-[#B9D8CC] bg-[#F7F1E8]/40 p-4">
            <div className="text-sm font-extrabold text-[#18332D]">
              {index + 1}. {question.questionText}
            </div>

            <div className="mt-3 space-y-2">
              {(question.options || []).map((option) => (
                <label
                  key={option.skillModuleQuizOptionId}
                  className={`flex cursor-pointer items-center gap-3 rounded-lg border px-3 py-2 text-sm font-semibold transition ${
                    answers[question.skillModuleQuizQuestionId] === option.skillModuleQuizOptionId
                      ? "border-[#2FA084] bg-[#6FCF97]/15 text-[#1F6F5F]"
                      : "border-[#B9D8CC] bg-white text-slate-700 hover:bg-white/70"
                  }`}
                >
                  <input
                    type="radio"
                    className="h-4 w-4 accent-[#2FA084]"
                    name={question.skillModuleQuizQuestionId}
                    checked={answers[question.skillModuleQuizQuestionId] === option.skillModuleQuizOptionId}
                    onChange={() =>
                      setAnswers((current) => ({
                        ...current,
                        [question.skillModuleQuizQuestionId]: option.skillModuleQuizOptionId,
                      }))
                    }
                  />
                  {option.optionText}
                </label>
              ))}
            </div>
          </div>
        ))}

        <div className="flex justify-end">
          <ModuleButton onClick={handleSubmit} disabled={isSubmitting || questions.length === 0}>
            {isSubmitting ? "Submitting..." : "Submit quiz"}
          </ModuleButton>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <div className="rounded-lg border border-[#B9D8CC] bg-white p-6 shadow-sm">
        <div className="flex flex-wrap items-start justify-between gap-4">
          <div>
            <h2 className="flex items-center gap-2 text-xl font-black text-[#18332D]">
              <Trophy size={20} /> {quiz.title || "Module quiz"}
            </h2>
            {quiz.description && (
              <p className="mt-2 max-w-2xl text-sm font-semibold leading-6 text-slate-600">
                {quiz.description}
              </p>
            )}
          </div>
          <ModuleBadge tone={remainingAttempts === 0 ? "rose" : "green"}>
            {remainingAttempts === null ? "Unlimited attempts" : `${remainingAttempts} attempts left today`}
          </ModuleBadge>
        </div>

        <div className="mt-5 grid gap-3 sm:grid-cols-3">
          <QuizStat label="Questions" value={quiz.questionCount || 0} />
          <QuizStat label="Passing score" value={`${quiz.passingScorePercent ?? 0}%`} />
          <QuizStat label="Attempts per day" value={quiz.maxAttempts || "Unlimited"} />
        </div>

        <div className="mt-5 flex justify-end">
          <ModuleButton onClick={startAttempt} disabled={isStarting || remainingAttempts === 0 || !canStartQuiz}>
            {isStarting
              ? "Starting..."
              : !canStartQuiz
                ? "Locked"
                : attempts.some((item) => item.status === "in_progress")
                  ? "Resume quiz"
                  : "Start quiz"}
          </ModuleButton>
        </div>
      </div>

      <div className="rounded-lg border border-[#B9D8CC] bg-white p-5 shadow-sm">
        <div className="flex items-center justify-between gap-3">
          <h3 className="text-sm font-extrabold text-[#18332D]">Past attempts</h3>
          {isLoadingAttempts && <span className="text-xs font-bold text-slate-500">Loading...</span>}
        </div>

        {attempts.length === 0 ? (
          <p className="mt-3 text-sm font-semibold text-slate-600">No attempts yet.</p>
        ) : (
          <div className="mt-3 space-y-2">
            {attempts.map((item) => (
              <button
                key={item.skillModuleQuizAttemptId}
                type="button"
                disabled={item.status !== "submitted" || isLoadingReview}
                onClick={() => openReview(item.skillModuleQuizAttemptId)}
                className="grid w-full gap-3 rounded-lg border border-[#B9D8CC] bg-[#F7F1E8]/45 px-4 py-3 text-left transition hover:bg-[#F7F1E8] disabled:cursor-default disabled:opacity-70 sm:grid-cols-[120px_1fr_120px] sm:items-center"
              >
                <div className="text-sm font-extrabold text-[#18332D]">Attempt {item.attemptNo}</div>
                <div className="text-sm font-semibold text-slate-600">
                  {item.status === "submitted" ? `${item.scorePercent ?? 0}% score` : "In progress"}
                </div>
                <div className={`text-sm font-extrabold ${item.passed ? "text-[#1F6F5F]" : "text-rose-600"}`}>
                  {item.status !== "submitted" ? "Resume" : item.passed ? "Passed" : "Not passed"}
                </div>
              </button>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}

function QuizStat({ label, value }) {
  return (
    <div className="rounded-lg border border-[#B9D8CC] bg-[#F7F1E8]/60 px-4 py-3">
      <div className="text-xs font-extrabold uppercase tracking-[0.12em] text-[#1F6F5F]">{label}</div>
      <div className="mt-1 text-lg font-black text-[#18332D]">{value}</div>
    </div>
  );
}

function QuizReview({ review, isLoadingReview, onBack }) {
  return (
    <div className="space-y-4 rounded-lg border border-[#B9D8CC] bg-white p-6 shadow-sm">
      <div className="flex flex-wrap items-start justify-between gap-3 rounded-lg border border-[#B9D8CC] bg-[#F7F1E8]/70 p-4">
        <div>
          <div className="text-lg font-extrabold text-[#18332D]">
            Attempt {review.attemptNo} · {review.scorePercent ?? 0}%
          </div>
          <div className="mt-1 text-sm font-bold text-slate-700">
            {review.earnedPoints}/{review.totalPoints} points · {review.passed ? "Passed" : "Not passed"}
          </div>
        </div>
        <ModuleButton variant="secondary" onClick={onBack} disabled={isLoadingReview}>Back to quiz</ModuleButton>
      </div>

      {(review.answers || []).map((answer, index) => (
        <div key={answer.skillModuleQuizAnswerId || `${answer.skillModuleQuizQuestionId}-${index}`} className="rounded-lg border border-[#B9D8CC] bg-white p-4">
          <div className="text-sm font-extrabold text-[#18332D]">
            {index + 1}. {answer.questionText}
          </div>
          <div className="mt-3 rounded-lg border border-slate-200 bg-[#F7F1E8]/50 px-3 py-2 text-sm font-bold text-slate-700">
            Your answer: {answer.selectedOptionText}
          </div>
          <div className={`mt-2 text-sm font-extrabold ${answer.isCorrect ? "text-[#1F6F5F]" : "text-rose-600"}`}>
            {answer.isCorrect ? "Correct" : "Incorrect"}
          </div>
        </div>
      ))}
    </div>
  );
}
