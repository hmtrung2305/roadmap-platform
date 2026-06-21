/* eslint-disable react-hooks/set-state-in-effect */
import { useEffect, useRef, useState } from "react";
import { useNavigate, useParams, useSearchParams } from "react-router-dom";
import { toast } from "react-toastify";
import {
  contentManagerLearningModuleApi,
  getLearningModuleRouteSegment,
} from "../../../api/learningModuleApi";
import {
  getEditorStorageKey,
  getModuleFromMutationResult,
  getValidEditorTab,
  removeSessionValue,
} from "../editorUtils";
import useEditorDirtyState from "./useEditorDirtyState";

function createEditorDetail(module = null, publishReadiness = null, current = null) {
  return {
    module,
    lessons: current?.lessons || [],
    quiz: current?.quiz || null,
    publishReadiness: publishReadiness || current?.publishReadiness || null,
  };
}

export default function useLearningModuleEditor() {
  const { moduleId } = useParams();
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();

  const [detail, setDetail] = useState(null);
  const [activeTab, setActiveTab] = useState(
    () => getValidEditorTab(searchParams.get("tab")) || "overview",
  );
  const [isLoading, setIsLoading] = useState(true);
  const [isActiveTabLoading, setIsActiveTabLoading] = useState(false);
  const [showLoadingState, setShowLoadingState] = useState(false);
  const [isPublishing, setIsPublishing] = useState(false);
  const [isPublishDialogOpen, setIsPublishDialogOpen] = useState(false);
  const [refreshKey, setRefreshKey] = useState(0);
  const [resolvedModuleId, setResolvedModuleId] = useState(moduleId || null);
  const [pendingNavigation, setPendingNavigation] = useState(null);
  const baseLoadRequestIdRef = useRef(0);
  const tabLoadRequestIdRef = useRef(0);

  const {
    hasUnsavedChanges,
    setDirtyState,
    clearDirtyState,
    clearAllDirtyStates,
  } = useEditorDirtyState();

  const module = detail?.module;
  const activeModuleId = module?.skillModuleId || resolvedModuleId;

  const reload = () => setRefreshKey((key) => key + 1);

  const applyTab = (tabKey) => {
    const nextTab = getValidEditorTab(tabKey) || "overview";

    setActiveTab(nextTab);
    setSearchParams((current) => {
      const next = new URLSearchParams(current);

      if (nextTab === "overview") {
        next.delete("tab");
      } else {
        next.set("tab", nextTab);
      }

      return next;
    }, { replace: true });
  };

  const selectTab = (tabKey) => {
    const nextTab = getValidEditorTab(tabKey) || "overview";
    if (nextTab === activeTab) return;

    if (hasUnsavedChanges) {
      setPendingNavigation({ type: "tab", tabKey: nextTab });
      return;
    }

    applyTab(nextTab);
  };

  const leaveEditor = () => {
    if (hasUnsavedChanges) {
      setPendingNavigation({ type: "leave" });
      return;
    }

    navigate("/content/learning-modules");
  };

  const clearPersistedQuizDrafts = () => {
    removeSessionValue(getEditorStorageKey(activeModuleId, "quizDraftQuestions"));
    removeSessionValue(getEditorStorageKey(activeModuleId, "activeQuizQuestionId"));
  };

  const discardChangesAndContinue = () => {
    const action = pendingNavigation;

    clearPersistedQuizDrafts();
    clearAllDirtyStates();
    setPendingNavigation(null);

    if (action?.type === "tab") {
      applyTab(action.tabKey);
      return;
    }

    navigate("/content/learning-modules");
  };

  useEffect(() => {
    setResolvedModuleId(moduleId || null);
    setDetail(null);
  }, [moduleId]);

  useEffect(() => {
    const requestId = baseLoadRequestIdRef.current + 1;
    baseLoadRequestIdRef.current = requestId;

    async function loadBaseEditorData() {
      try {
        setIsLoading(true);
        if (!moduleId) {
          throw new Error("Learning module route is missing.");
        }

        const force = refreshKey > 0;
        const [moduleData, readiness] = await Promise.all([
          contentManagerLearningModuleApi.getModuleOverview(moduleId, { force }),
          contentManagerLearningModuleApi.getPublishReadiness(moduleId),
        ]);

        if (baseLoadRequestIdRef.current === requestId) {
          setResolvedModuleId(moduleId);
          setDetail((current) => createEditorDetail(moduleData, readiness, current));
        }
      } catch (error) {
        if (baseLoadRequestIdRef.current === requestId) {
          setResolvedModuleId(null);
          setDetail(null);
          toast.error(error?.message || "Unable to load module.");
        }
      } finally {
        if (baseLoadRequestIdRef.current === requestId) setIsLoading(false);
      }
    }

    loadBaseEditorData();
  }, [moduleId, refreshKey]);

  useEffect(() => {
    const nextTab = getValidEditorTab(searchParams.get("tab")) || "overview";
    setActiveTab((current) => current === nextTab ? current : nextTab);
  }, [searchParams]);

  useEffect(() => {
    if (!detail?.module?.skillModuleId) return undefined;

    const requestId = tabLoadRequestIdRef.current + 1;
    tabLoadRequestIdRef.current = requestId;

    async function loadActiveTabData() {
      const force = refreshKey > 0;
      const currentModuleId = detail.module.skillModuleId;

      try {
        if (activeTab === "overview" || activeTab === "publish") {
          return;
        }

        setIsActiveTabLoading(true);

        if (activeTab === "lessons") {
          const lessons = await contentManagerLearningModuleApi.getModuleLessons(
            currentModuleId,
            { force },
          );

          if (tabLoadRequestIdRef.current === requestId) {
            setDetail((current) => current ? { ...current, lessons } : current);
          }
          return;
        }

        if (activeTab === "quiz") {
          const quiz = await contentManagerLearningModuleApi.getModuleQuiz(
            currentModuleId,
            { force },
          );

          if (tabLoadRequestIdRef.current === requestId) {
            setDetail((current) => current ? { ...current, quiz } : current);
          }
          return;
        }

        if (activeTab === "preview") {
          const [lessons, quiz] = await Promise.all([
            contentManagerLearningModuleApi.getModuleLessons(currentModuleId, { force }),
            contentManagerLearningModuleApi.getModuleQuiz(currentModuleId, { force }),
          ]);

          if (tabLoadRequestIdRef.current === requestId) {
            setDetail((current) => current ? { ...current, lessons, quiz } : current);
          }
        }
      } catch (error) {
        if (tabLoadRequestIdRef.current === requestId) {
          toast.error(error?.message || "Unable to load editor data.");
        }
      } finally {
        if (tabLoadRequestIdRef.current === requestId) {
          setIsActiveTabLoading(false);
        }
      }
    }

    loadActiveTabData();
    return undefined;
  }, [activeTab, detail?.module?.skillModuleId, refreshKey]);

  useEffect(() => {
    if (!isLoading || detail) {
      setShowLoadingState(false);
      return undefined;
    }

    const timer = window.setTimeout(() => setShowLoadingState(true), 180);
    return () => window.clearTimeout(timer);
  }, [isLoading, detail]);

  useEffect(() => {
    if (!hasUnsavedChanges) return undefined;

    const handleBeforeUnload = (event) => {
      event.preventDefault();
      event.returnValue = "";
    };

    window.addEventListener("beforeunload", handleBeforeUnload);
    return () => window.removeEventListener("beforeunload", handleBeforeUnload);
  }, [hasUnsavedChanges]);

  const refreshPublishReadiness = async (moduleId = activeModuleId) => {
    if (!moduleId) return null;

    try {
      const readiness = await contentManagerLearningModuleApi.getPublishReadiness(moduleId);

      setDetail((current) =>
        current
          ? {
              ...current,
              publishReadiness: readiness,
            }
          : current,
      );

      return readiness;
    } catch {
      return null;
    }
  };

  const handleOverviewSaved = (result) => {
    const updatedModule = getModuleFromMutationResult(result);
    clearDirtyState("overview");

    if (!updatedModule?.skillModuleId) {
      reload();
      return;
    }

    setResolvedModuleId(updatedModule.skillModuleId);
    setDetail((current) =>
      current
        ? {
            ...current,
            module: {
              ...current.module,
              ...updatedModule,
            },
          }
        : createEditorDetail(updatedModule),
    );

    void refreshPublishReadiness(updatedModule.skillModuleId);

    const nextRouteSegment = getLearningModuleRouteSegment(updatedModule);

    if (nextRouteSegment && nextRouteSegment !== moduleId) {
      const queryString = searchParams.toString();

      navigate(
        `/content/learning-modules/${nextRouteSegment}/edit${queryString ? `?${queryString}` : ""}`,
        { replace: true },
      );
    }
  };

  const refreshDetailSilently = async () => {
    if (!activeModuleId) return;

    try {
      const [lessons, readiness] = await Promise.all([
        contentManagerLearningModuleApi.getModuleLessons(activeModuleId, { force: true }),
        contentManagerLearningModuleApi.getPublishReadiness(activeModuleId),
      ]);

      setDetail((current) =>
        current
          ? {
              ...current,
              lessons,
              publishReadiness: readiness,
            }
          : current,
      );
    } catch {
      // Polling is best-effort.
    }
  };

  const handlePublish = async () => {
    try {
      setIsPublishing(true);

      if (!activeModuleId) {
        toast.error("Learning module was not loaded yet.");
        return;
      }

      const result = await contentManagerLearningModuleApi.publishModule(activeModuleId);

      if (result?.readiness?.canPublish === false) {
        toast.error(result.readiness.errors?.[0] || "Module is not ready to publish.");
        setIsPublishDialogOpen(false);
        reload();
        return;
      }

      setIsPublishDialogOpen(false);
      toast.success("Module published.");
      navigate("/content/learning-modules?status=published");
    } catch (error) {
      toast.error(error?.message || "Unable to publish module.");
    } finally {
      setIsPublishing(false);
    }
  };

  return {
    detail,
    module,
    activeModuleId,
    activeTab,
    isLoading,
    isActiveTabLoading,
    showLoadingState,
    isPublishing,
    isPublishDialogOpen,
    isDiscardDialogOpen: Boolean(pendingNavigation),
    reload,
    selectTab,
    leaveEditor,
    discardChangesAndContinue,
    cancelDiscard: () => setPendingNavigation(null),
    openPublishDialog: () => setIsPublishDialogOpen(true),
    closePublishDialog: () => setIsPublishDialogOpen(false),
    handlePublish,
    handleOverviewSaved,
    refreshDetailSilently,
    setDirtyState,
  };
}
