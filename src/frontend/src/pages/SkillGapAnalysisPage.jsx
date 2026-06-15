import {
  SkillGapErrorMessage,
  SkillGapInsightPanel,
  SkillGapPageHeader,
  SkillGapResultStep,
  SkillGapRoleStep,
  SkillGapSkillsStep,
  SkillGapStepper,
} from "../components/skillGap";
import { useSkillGapAnalysis } from "../hooks/useSkillGapAnalysis";

export default function SkillGapAnalysisPage() {
  const {
    roles,
    selectedRole,
    groups,
    selectedSkillSlugs,
    result,
    step,
    isRolesLoading,
    isGroupsLoading,
    isAnalyzing,
    error,
    actions,
  } = useSkillGapAnalysis();

  return (
    <main className="tm-page min-h-screen bg-[#F7F1E8] px-4 py-8 sm:px-6 lg:px-8">
      <div className="mx-auto max-w-[1200px]">
        <SkillGapPageHeader />

        <div className="mb-6">
          <SkillGapStepper currentStep={step} />
        </div>

        <SkillGapErrorMessage>{error}</SkillGapErrorMessage>

        <div className={`grid gap-6 ${step === 3 ? "lg:grid-cols-1" : "lg:grid-cols-[minmax(0,1fr)_360px]"}`}>
          <div className="min-w-0">
            {step === 1 && (
              <SkillGapRoleStep
                roles={roles}
                selectedRole={selectedRole}
                isLoading={isRolesLoading}
                isLoadingGroups={isGroupsLoading}
                onSelectRole={actions.selectRole}
                onNext={actions.loadGroups}
              />
            )}

            {step === 2 && (
              <SkillGapSkillsStep
                role={selectedRole}
                groups={groups}
                selectedSkillSlugs={selectedSkillSlugs}
                isLoading={isGroupsLoading}
                isAnalyzing={isAnalyzing}
                onToggleSkill={actions.toggleSkill}
                onBack={actions.goToRoleStep}
                onAnalyze={actions.analyze}
              />
            )}

            {step === 3 && result && (
              <SkillGapResultStep
                result={result}
                selectedSkillCount={selectedSkillSlugs.length}
                onBack={actions.goToSkillStep}
                onReset={actions.reset}
              />
            )}
          </div>

          {step < 3 && (
            <SkillGapInsightPanel
              step={step}
              roles={roles}
              selectedRole={selectedRole}
              groups={groups}
              selectedSkillSlugs={selectedSkillSlugs}
              result={result}
            />
          )}
        </div>
      </div>
    </main>
  );
}
