import { useEffect, useState } from "react";
import { Loader2 } from "lucide-react";
import { useLocation, useNavigate, useParams } from "react-router-dom";
import { skillGapApi } from "../api/skillGapApi";
import {
  SkillGapErrorMessage,
  SkillGapHistoryPanel,
  SkillGapInsightPanel,
  SkillGapPageHeader,
  SkillGapResultStep,
  SkillGapRoleStep,
  SkillGapSkillsStep,
  SkillGapStepper,
} from "../features/skillGap/components";
import { useSkillGapAnalysis } from "../features/skillGap/hooks/useSkillGapAnalysis";
import { normalizeSkillGapResult } from "../features/skillGap/utils/skillGapUtils";
import { getFriendlyApiErrorMessage } from "../utils/apiErrorUtils";

function SurfaceTabs({ activeTab, onChange }) {
  const tabs = [
    { id: "analysis", label: "New analysis" },
    { id: "history", label: "History" },
  ];

  return (
    <div className="mb-6 flex flex-wrap items-center justify-center gap-2">
      {tabs.map((tab) => {
        const active = activeTab === tab.id || (tab.id === "history" && activeTab === "historyDetail");

        return (
          <button
            key={tab.id}
            type="button"
            onClick={() => onChange(tab.id)}
            className={`rounded-full border px-4 py-2 text-sm font-extrabold transition ${
              active
                ? "border-[#2FA084] bg-[#6FCF97]/24 text-[#1F6F5F] shadow-sm"
                : "border-[#B9D8CC] bg-white text-slate-600 hover:border-[#2FA084] hover:bg-[#F7F1E8] hover:text-[#1F6F5F]"
            }`}
          >
            {tab.label}
          </button>
        );
      })}
    </div>
  );
}

export default function SkillGapAnalysisPage() {
  const location = useLocation();
  const navigate = useNavigate();
  const { historyId = "" } = useParams();
  const [activeTab, setActiveTab] = useState("analysis");
  const [historyResult, setHistoryResult] = useState(null);
  const [isHistoryDetailLoading, setIsHistoryDetailLoading] = useState(false);
  const [historyDetailError, setHistoryDetailError] = useState("");
  const {
    roles,
    roadmaps,
    selectedRole,
    selectedRoadmap,
    categories,
    selectedSkillIds,
    result,
    step,
    isRolesLoading,
    isRoadmapsLoading,
    isAssessmentLoading,
    isAnalyzing,
    error,
    actions,
  } = useSkillGapAnalysis();

  useEffect(() => {
    let cancelled = false;

    if (!historyId) {
      const requestedTab = new URLSearchParams(location.search).get("tab");
      setActiveTab(requestedTab === "history" ? "history" : "analysis");
      setHistoryResult(null);
      setIsHistoryDetailLoading(false);
      setHistoryDetailError("");
      return () => {
        cancelled = true;
      };
    }

    setActiveTab("historyDetail");
    setHistoryResult(null);
    setIsHistoryDetailLoading(true);
    setHistoryDetailError("");

    skillGapApi.getHistoryDetail(historyId)
      .then((detail) => {
        if (!cancelled) {
          setHistoryResult(normalizeSkillGapResult(detail));
        }
      })
      .catch((requestError) => {
        if (!cancelled) {
          setHistoryDetailError(
            getFriendlyApiErrorMessage(requestError, "Unable to open this history item."),
          );
        }
      })
      .finally(() => {
        if (!cancelled) {
          setIsHistoryDetailLoading(false);
        }
      });

    return () => {
      cancelled = true;
    };
  }, [historyId, location.search]);

  const handleSelectRole = async (role) => {
    actions.selectRole(role);
    await actions.loadRoadmaps({ role });
  };

  const handleChangeTab = (tabId) => {
    navigate(tabId === "history" ? "/skill-gap?tab=history" : "/skill-gap");
  };

  const handleViewHistoryResult = (savedHistoryId) => {
    navigate(`/skill-gap/history/${encodeURIComponent(savedHistoryId)}`);
  };

  const handleResetFromHistory = () => {
    actions.reset();
    navigate("/skill-gap");
  };

  return (
    <main className="tm-page min-h-screen bg-[#F7F1E8] px-4 py-8 sm:px-6 lg:px-8">
      <div className="mx-auto max-w-300">
        <SkillGapPageHeader />
        <SurfaceTabs activeTab={activeTab} onChange={handleChangeTab} />

        {activeTab === "history" ? (
          <SkillGapHistoryPanel onViewResult={handleViewHistoryResult} />
        ) : activeTab === "historyDetail" ? (
          isHistoryDetailLoading ? (
            <section className="grid place-items-center rounded-3xl border border-[#B9D8CC]/80 bg-white/95 py-16 text-sm font-bold text-slate-600 shadow-sm">
              <Loader2 className="mb-3 animate-spin text-[#2FA084]" size={26} />
              Loading saved analysis...
            </section>
          ) : historyResult ? (
            <SkillGapResultStep
              result={historyResult}
              canUpdateSelection={false}
              isHistoryView
              onBackToHistory={() => navigate("/skill-gap?tab=history")}
              onReset={handleResetFromHistory}
            />
          ) : (
            <section className="rounded-3xl border border-red-200 bg-red-50 p-6 text-center shadow-sm">
              <p className="text-sm font-bold text-red-700">
                {historyDetailError || "Unable to open this history item."}
              </p>
              <button
                type="button"
                onClick={() => navigate("/skill-gap?tab=history")}
                className="mt-4 rounded-lg border border-red-200 bg-white px-4 py-2 text-sm font-extrabold text-red-700"
              >
                Back to history
              </button>
            </section>
          )
        ) : (
          <>
            <div className="mb-6">
              <SkillGapStepper currentStep={step} />
            </div>

            <SkillGapErrorMessage>{error}</SkillGapErrorMessage>

            <div className={`grid gap-6 ${step === 3 ? "lg:grid-cols-1" : "lg:grid-cols-[minmax(0,1fr)_360px]"}`}>
              <div className="min-w-0">
                {step === 1 && (
                  <SkillGapRoleStep
                    roles={roles}
                    roadmaps={roadmaps}
                    selectedRole={selectedRole}
                    selectedRoadmap={selectedRoadmap}
                    isLoading={isRolesLoading}
                    isLoadingRoadmaps={isRoadmapsLoading}
                    isLoadingAssessment={isAssessmentLoading}
                    onSelectRole={handleSelectRole}
                    onSelectRoadmap={actions.selectRoadmap}
                    onNext={actions.loadAssessment}
                  />
                )}

                {step === 2 && (
                  <SkillGapSkillsStep
                    role={selectedRole}
                    roadmap={selectedRoadmap}
                    categories={categories}
                    selectedSkillIds={selectedSkillIds}
                    isLoading={isAssessmentLoading}
                    isAnalyzing={isAnalyzing}
                    onToggleSkill={actions.toggleSkill}
                    onBack={actions.goToRoleStep}
                    onAnalyze={actions.analyze}
                  />
                )}

                {step === 3 && result && (
                  <SkillGapResultStep
                    result={result}
                    canUpdateSelection={Boolean(selectedRole && selectedRoadmap)}
                    onBack={actions.goToSkillStep}
                    onReset={actions.reset}
                  />
                )}
              </div>

              {step < 3 && (
                <SkillGapInsightPanel
                  step={step}
                  selectedRole={selectedRole}
                  selectedRoadmap={selectedRoadmap}
                  categories={categories}
                  selectedSkillIds={selectedSkillIds}
                  result={result}
                />
              )}
            </div>
          </>
        )}
      </div>
    </main>
  );
}
