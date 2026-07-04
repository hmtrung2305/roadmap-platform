import { useEffect } from "react";
import { useSkillGapStore } from "../../../stores/useSkillGapStore";

export function useSkillGapAnalysis() {
  const roles = useSkillGapStore((state) => state.roles);
  const roadmaps = useSkillGapStore((state) => state.roadmaps);
  const selectedRole = useSkillGapStore((state) => state.selectedRole);
  const selectedRoadmap = useSkillGapStore((state) => state.selectedRoadmap);
  const assessment = useSkillGapStore((state) => state.assessment);
  const categories = useSkillGapStore((state) => state.categories);
  const selectedSkillIds = useSkillGapStore((state) => state.selectedSkillIds);
  const result = useSkillGapStore((state) => state.result);
  const step = useSkillGapStore((state) => state.step);
  const isRolesLoading = useSkillGapStore((state) => state.isRolesLoading);
  const isRoadmapsLoading = useSkillGapStore((state) => state.isRoadmapsLoading);
  const isAssessmentLoading = useSkillGapStore((state) => state.isAssessmentLoading);
  const isAnalyzing = useSkillGapStore((state) => state.isAnalyzing);
  const error = useSkillGapStore((state) => state.error);

  const analyze = useSkillGapStore((state) => state.analyze);
  const goToRoleStep = useSkillGapStore((state) => state.goToRoleStep);
  const goToSkillStep = useSkillGapStore((state) => state.goToSkillStep);
  const loadAssessment = useSkillGapStore((state) => state.loadAssessment);
  const loadRoadmaps = useSkillGapStore((state) => state.loadRoadmaps);
  const loadRoles = useSkillGapStore((state) => state.loadRoles);
  const reset = useSkillGapStore((state) => state.reset);
  const selectRoadmap = useSkillGapStore((state) => state.selectRoadmap);
  const selectRole = useSkillGapStore((state) => state.selectRole);
  const showHistoryResult = useSkillGapStore((state) => state.showHistoryResult);
  const toggleSkill = useSkillGapStore((state) => state.toggleSkill);

  useEffect(() => {
    loadRoles();
  }, [loadRoles]);

  return {
    roles,
    roadmaps,
    selectedRole,
    selectedRoadmap,
    assessment,
    categories,
    selectedSkillIds,
    result,
    step,
    isRolesLoading,
    isRoadmapsLoading,
    isAssessmentLoading,
    isAnalyzing,
    error,
    actions: {
      analyze,
      goToRoleStep,
      goToSkillStep,
      loadAssessment,
      loadRoadmaps,
      reset,
      selectRoadmap,
      selectRole,
      showHistoryResult,
      toggleSkill,
    },
  };
}
