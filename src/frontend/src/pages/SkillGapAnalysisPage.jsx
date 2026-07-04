import { useState } from "react";
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
  const [activeTab, setActiveTab] = useState("analysis");
  const [historyResult, setHistoryResult] = useState(null);
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

  const handleSelectRole = async (role) => {
    actions.selectRole(role);
    await actions.loadRoadmaps({ role });
  };

  const handleChangeTab = (tabId) => {
    setActiveTab(tabId);
    if (tabId !== "historyDetail") {
      setHistoryResult(null);
    }
  };

  const handleViewHistoryResult = (savedResult) => {
    setHistoryResult(savedResult);
    setActiveTab("historyDetail");
  };

  const handleResetFromHistory = () => {
    setHistoryResult(null);
    setActiveTab("analysis");
    actions.reset();
  };

  return (
    <main className="tm-page min-h-screen bg-[#F7F1E8] px-4 py-8 sm:px-6 lg:px-8">
      <div className="mx-auto max-w-300">
        <SkillGapPageHeader />
        <SurfaceTabs activeTab={activeTab} onChange={handleChangeTab} />

        {activeTab === "history" ? (
          <SkillGapHistoryPanel onViewResult={handleViewHistoryResult} />
        ) : activeTab === "historyDetail" && historyResult ? (
          <SkillGapResultStep
            result={historyResult}
            canUpdateSelection={false}
            isHistoryView
            onBackToHistory={() => setActiveTab("history")}
            onReset={handleResetFromHistory}
          />
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
