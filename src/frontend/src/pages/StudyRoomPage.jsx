/* eslint-disable react-hooks/set-state-in-effect */
import { useEffect, useMemo, useRef, useState } from "react";
import { useNavigate, useParams, useSearchParams } from "react-router-dom";
import { ArrowLeft, Bot, Clock, FileText, PanelLeftOpen } from "lucide-react";
import { toast } from "react-toastify";

import DocumentLoading from "../components/document/DocumentLoading";
import LearningModuleLessonReader from "../features/learningModules/components/LearningModuleLessonReader";
import StudyLessonSidebar from "../features/studyRoom/components/StudyLessonSidebar";
import StudyQuiz from "../features/studyRoom/components/StudyQuiz";
import StudyRoomChatPanel from "../features/studyRoom/components/StudyRoomChatPanel";
import StudyRoomHeader from "../features/studyRoom/components/StudyRoomHeader";
import {
  areAllLessonsCompleted,
  HEADER_HEIGHT,
  PAGE_GAP,
} from "../features/studyRoom/utils/studyRoomUtils";
import { useStudyRoomLayout } from "../features/studyRoom/hooks/useStudyRoomLayout";
import { useLearningModuleStore } from "../stores/useLearningModuleStore";
import { useStreakStore } from "../stores/useStreakStore";
import {
  formatHours,
  getEnrollmentStatus,
  getLessonProgress,
  ModuleBadge,
  ModuleButton,
} from "../features/learningModules/components/learningModuleUi";

export default function StudyRoomPage() {
  const { slug } = useParams();
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const returnRoadmapSlug = searchParams.get("fromRoadmap");
  const returnNodeId = searchParams.get("roadmapNodeId");
  const guideToken = searchParams.get("guideToken");
  const shouldContinueRoadmapGuide =
    searchParams.get("guide") === "1" && Boolean(guideToken);
  const returnToRoadmapUrl = buildReturnToRoadmapUrl(
    returnRoadmapSlug,
    returnNodeId,
    shouldContinueRoadmapGuide,
    guideToken,
  );
  const [activeLessonId, setActiveLessonId] = useState(null);
  const [isStarting, setIsStarting] = useState(false);
  const hasTrackedLearningStreakRef = useRef(false);

  const module = useLearningModuleStore((state) =>
    state.getModuleSnapshot(slug),
  );
  const moduleLoaded = useLearningModuleStore((state) =>
    state.getModuleLoaded(slug),
  );
  const isLoadingModule = useLearningModuleStore((state) =>
    state.getModuleLoading(slug),
  );
  const moduleError = useLearningModuleStore((state) =>
    state.getModuleError(slug),
  );
  const loadModuleBySlug = useLearningModuleStore(
    (state) => state.loadModuleBySlug,
  );
  const enrollModule = useLearningModuleStore((state) => state.enrollModule);
  const loadLessonContent = useLearningModuleStore(
    (state) => state.loadLessonContent,
  );
  const updateLessonProgress = useLearningModuleStore(
    (state) => state.updateLessonProgress,
  );
  const trackStreakIfNeeded = useStreakStore(
    (state) => state.trackStreakIfNeeded,
  );

  const activeLesson = useMemo(() => {
    if (!module?.lessons?.length) return null;

    return (
      module.lessons.find(
        (lesson) => lesson.skillModuleLessonId === activeLessonId,
      ) || module.lessons[0]
    );
  }, [module, activeLessonId]);

  const lessonContent = useLearningModuleStore((state) =>
    state.getLessonContent(
      module?.skillModuleId,
      activeLesson?.skillModuleLessonId,
    ),
  );
  const isLoadingLesson = useLearningModuleStore((state) =>
    state.getLessonLoading(
      module?.skillModuleId,
      activeLesson?.skillModuleLessonId,
    ),
  );
  const lessonError = useLearningModuleStore((state) =>
    state.getLessonError(
      module?.skillModuleId,
      activeLesson?.skillModuleLessonId,
    ),
  );
  const isUpdatingActiveLessonProgress = useLearningModuleStore((state) =>
    state.getProgressUpdating(
      module?.skillModuleId,
      activeLesson?.skillModuleLessonId,
    ),
  );

  const {
    showQuiz,
    setShowQuiz,
    isSidebarOpen,
    setIsSidebarOpen,
    isChatOpen,
    setIsChatOpen,
    isHeaderVisible,
    sidebarWidth,
    chatWidth,
    sidePanelTopOffset,
    mainLeftPadding,
    mainRightPadding,
    handleDocumentScroll,
    startSidebarResize,
    startChatResize,
  } = useStudyRoomLayout();

  useEffect(() => {
    if (!slug) return;

    loadModuleBySlug(slug).catch(() => {});
  }, [slug, loadModuleBySlug]);

  useEffect(() => {
    hasTrackedLearningStreakRef.current = false;
  }, [slug]);

  useEffect(() => {
    if (hasTrackedLearningStreakRef.current) return;
    if (!module?.skillModuleId || !module?.enrollment) return;
    if (getEnrollmentStatus(module) === "not_started") return;

    hasTrackedLearningStreakRef.current = true;
    trackStreakIfNeeded().catch(() => {
      // Learning access should not be blocked by streak tracking.
    });
  }, [module, module?.skillModuleId, module?.enrollment, trackStreakIfNeeded]);

  useEffect(() => {
    if (!module?.lessons?.length) return;

    const nextLessonId =
      module.enrollment?.lastAccessedLessonId ||
      module.lessons?.[0]?.skillModuleLessonId ||
      null;

    setActiveLessonId((current) => {
      if (
        current &&
        module.lessons.some((lesson) => lesson.skillModuleLessonId === current)
      ) {
        return current;
      }

      return nextLessonId;
    });
  }, [
    module?.skillModuleId,
    module?.enrollment?.lastAccessedLessonId,
    module?.lessons,
  ]);

  useEffect(() => {
    if (
      !module?.skillModuleId ||
      !activeLesson?.skillModuleLessonId ||
      showQuiz
    ) {
      return;
    }

    loadLessonContent(
      module.skillModuleId,
      activeLesson.skillModuleLessonId,
    ).catch(() => {});
  }, [
    module?.skillModuleId,
    activeLesson?.skillModuleLessonId,
    showQuiz,
    loadLessonContent,
  ]);

  useEffect(() => {
    if (
      !module?.skillModuleId ||
      !module?.enrollment ||
      !activeLesson?.skillModuleLessonId ||
      showQuiz
    ) {
      return;
    }

    const currentStatus = getLessonProgress(
      module.enrollment,
      activeLesson.skillModuleLessonId,
    );

    if (currentStatus) {
      return;
    }

    updateLessonProgress(
      module.skillModuleId,
      activeLesson.skillModuleLessonId,
      "in_progress",
    ).catch(() => {
      // Opening a lesson should not be blocked by a progress tracking failure.
    });
  }, [
    module?.skillModuleId,
    module?.enrollment,
    activeLesson?.skillModuleLessonId,
    showQuiz,
    updateLessonProgress,
  ]);

  const handleStartModule = async () => {
    if (!module || isStarting) return;

    try {
      setIsStarting(true);
      await enrollModule(module.skillModuleId, { slug });
      hasTrackedLearningStreakRef.current = true;
      trackStreakIfNeeded().catch(() => {
        // Streak tracking should not block module enrollment.
      });
      toast.success("Module started.");
    } catch (error) {
      toast.error(error?.message || "Unable to start this module.");
    } finally {
      setIsStarting(false);
    }
  };

  const handleCompleteLesson = async () => {
    if (
      !module?.skillModuleId ||
      !activeLesson ||
      isUpdatingActiveLessonProgress
    )
      return;

    if (
      getLessonProgress(module.enrollment, activeLesson.skillModuleLessonId) ===
      "completed"
    ) {
      return;
    }

    try {
      const result = await updateLessonProgress(
        module.skillModuleId,
        activeLesson.skillModuleLessonId,
        "completed",
      );

      if (result) {
        toast.success("Lesson marked complete.");
      }
    } catch (error) {
      toast.error(error?.message || "Unable to update lesson progress.");
    }
  };

  const refreshModule = () =>
    loadModuleBySlug(slug, { force: true }).catch(() => {});

  if (isLoadingModule || (!moduleLoaded && !moduleError)) {
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
          onClick={() => navigate(returnToRoadmapUrl || "/learning-modules")}
          className="mb-5 inline-flex items-center gap-2 text-sm font-bold text-[#1F6F5F]"
        >
          <ArrowLeft size={16} />
          {returnToRoadmapUrl ? "Back to roadmap node" : "Back to learning modules"}
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
            <ModuleButton
              size="md"
              onClick={handleStartModule}
              disabled={isStarting}
            >
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
          isCompletingLesson={isUpdatingActiveLessonProgress}
          onBack={() => navigate(returnToRoadmapUrl || "/learning-modules")}
          onCompleteLesson={handleCompleteLesson}
          backLabel={returnToRoadmapUrl ? "Back to roadmap" : "Back to modules"}
        />
      </div>

      <StudyLessonSidebar
        module={module}
        activeLessonId={activeLessonId}
        showQuiz={showQuiz}
        isOpen={isSidebarOpen}
        width={sidebarWidth}
        topOffset={sidePanelTopOffset}
        onStartResize={startSidebarResize}
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
        onStartResize={startChatResize}
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
          {returnToRoadmapUrl && (
            <div className="mb-4 flex flex-col gap-3 rounded-lg border border-[#A8D3C4] bg-[#EAF8F1] px-4 py-3 text-sm font-bold leading-6 text-[#18332D] shadow-sm sm:flex-row sm:items-center sm:justify-between">
              <span>
                Roadmap flow: complete lessons, use Module chat when stuck, do
                the quiz when unlocked, then return and mark the roadmap node Done.
              </span>
              <button
                type="button"
                onClick={() => navigate(returnToRoadmapUrl)}
                className="shrink-0 rounded-lg border border-[#A8D3C4] bg-white px-3 py-2 text-xs font-black text-[#1F6F5F] shadow-sm hover:bg-[#F7F1E8]"
              >
                Return to roadmap
              </button>
            </div>
          )}

          {module.status === "archived" && (
            <div className="mb-4 rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm font-bold leading-6 text-amber-800 shadow-sm">
              This module is archived. You can still access lessons, quiz
              attempts, and module chat.
            </div>
          )}

          {showQuiz ? (
            <StudyQuiz
              module={module}
              canStartQuiz={isQuizUnlocked}
              onProgressChanged={refreshModule}
            />
          ) : isLoadingLesson ? (
            <DocumentLoading />
          ) : lessonError ? (
            <div className="rounded-lg border border-red-200 bg-red-50 p-6 text-sm font-semibold text-red-700 shadow-sm">
              {lessonError}
            </div>
          ) : (
            <LearningModuleLessonReader markdown={lessonContent?.markdown} />
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


function buildReturnToRoadmapUrl(
  roadmapSlug,
  roadmapNodeId,
  shouldContinueGuide,
  guideToken,
) {
  if (!roadmapSlug) return null;

  const params = new URLSearchParams();
  if (roadmapNodeId) params.set("nodeId", roadmapNodeId);
  if (shouldContinueGuide && guideToken) {
    params.set("guide", "1");
    params.set("guideToken", guideToken);
  }

  return `/roadmaps/${roadmapSlug}${params.toString() ? `?${params.toString()}` : ""}`;
}
