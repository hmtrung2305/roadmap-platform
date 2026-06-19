import { useEffect, useRef, useState } from "react";
import { useLocation, useNavigate, useParams, useSearchParams } from "react-router-dom";
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

export default function useLearningModuleEditor() {
  const { moduleSlug } = useParams();
  const navigate = useNavigate();
  const location = useLocation();
  const [searchParams, setSearchParams] = useSearchParams();
  const routeStateModuleId = location.state?.moduleId || null;
  const resolvedModuleIdRef = useRef(routeStateModuleId);

  const [detail, setDetail] = useState(null);
  const [activeTab, setActiveTab] = useState(
    () => getValidEditorTab(searchParams.get("tab")) || "overview",
  );
  const [isLoading, setIsLoading] = useState(true);
  const [showLoadingState, setShowLoadingState] = useState(false);
  const [isPublishing, setIsPublishing] = useState(false);
  const [isPublishDialogOpen, setIsPublishDialogOpen] = useState(false);
  const [refreshKey, setRefreshKey] = useState(0);
  const [resolvedModuleId, setResolvedModuleId] = useState(routeStateModuleId);
  const [pendingNavigation, setPendingNavigation] = useState(null);
  const loadRequestIdRef = useRef(0);

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
    if (!routeStateModuleId) return;

    resolvedModuleIdRef.current = routeStateModuleId;
    setResolvedModuleId(routeStateModuleId);
  }, [routeStateModuleId]);

  useEffect(() => {
    const requestId = loadRequestIdRef.current + 1;
    loadRequestIdRef.current = requestId;

    async function loadDetail() {
      try {
        setIsLoading(true);
        const knownModuleId = routeStateModuleId || resolvedModuleIdRef.current;
        const moduleId = await contentManagerLearningModuleApi.resolveModuleIdFromRoute(
          moduleSlug,
          knownModuleId,
        );
        const data = await contentManagerLearningModuleApi.getModule(moduleId, {
          force: refreshKey > 0,
        });

        if (loadRequestIdRef.current === requestId) {
          resolvedModuleIdRef.current = moduleId;
          setResolvedModuleId(moduleId);
          setDetail(data);
        }
      } catch (error) {
        if (loadRequestIdRef.current === requestId) {
          setResolvedModuleId(null);
          toast.error(error?.message || "Unable to load module.");
        }
      } finally {
        if (loadRequestIdRef.current === requestId) setIsLoading(false);
      }
    }

    loadDetail();
  }, [moduleSlug, refreshKey, routeStateModuleId]);

  useEffect(() => {
    const nextTab = getValidEditorTab(searchParams.get("tab")) || "overview";
    setActiveTab((current) => current === nextTab ? current : nextTab);
  }, [searchParams]);

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

    resolvedModuleIdRef.current = updatedModule.skillModuleId;
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
        : current,
    );

    void refreshPublishReadiness(updatedModule.skillModuleId);

    const nextRouteSegment = getLearningModuleRouteSegment(updatedModule);

    if (nextRouteSegment && nextRouteSegment !== moduleSlug) {
      const queryString = searchParams.toString();

      navigate(
        `/content/learning-modules/${nextRouteSegment}/edit${queryString ? `?${queryString}` : ""}`,
        {
          replace: true,
          state: { moduleId: updatedModule.skillModuleId },
        },
      );
    }
  };

  const refreshDetailSilently = async () => {
    if (!activeModuleId) return;

    try {
      const data = await contentManagerLearningModuleApi.getModule(activeModuleId, {
        force: true,
      });
      setDetail(data);
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
