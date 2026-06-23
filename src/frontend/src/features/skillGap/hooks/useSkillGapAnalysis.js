import { useEffect } from "react";
import { useSkillGapStore } from "../../../stores/useSkillGapStore";

export function useSkillGapAnalysis() {
  const roles = useSkillGapStore((state) => state.roles);
  const levels = useSkillGapStore((state) => state.levels);
  const selectedRole = useSkillGapStore((state) => state.selectedRole);
  const selectedLevel = useSkillGapStore((state) => state.selectedLevel);
  const groups = useSkillGapStore((state) => state.groups);
  const selectedNodeIds = useSkillGapStore((state) => state.selectedNodeIds);
  const result = useSkillGapStore((state) => state.result);
  const step = useSkillGapStore((state) => state.step);
  const isRolesLoading = useSkillGapStore((state) => state.isRolesLoading);
  const isLevelsLoading = useSkillGapStore((state) => state.isLevelsLoading);
  const isGroupsLoading = useSkillGapStore((state) => state.isGroupsLoading);
  const isAnalyzing = useSkillGapStore((state) => state.isAnalyzing);
  const error = useSkillGapStore((state) => state.error);

  const analyze = useSkillGapStore((state) => state.analyze);
  const goToRoleStep = useSkillGapStore((state) => state.goToRoleStep);
  const goToSkillStep = useSkillGapStore((state) => state.goToSkillStep);
  const loadGroups = useSkillGapStore((state) => state.loadGroups);
  const loadLevels = useSkillGapStore((state) => state.loadLevels);
  const loadRoles = useSkillGapStore((state) => state.loadRoles);
  const reset = useSkillGapStore((state) => state.reset);
  const selectLevel = useSkillGapStore((state) => state.selectLevel);
  const selectRole = useSkillGapStore((state) => state.selectRole);
  const showHistoryResult = useSkillGapStore((state) => state.showHistoryResult);
  const toggleSkill = useSkillGapStore((state) => state.toggleSkill);

  useEffect(() => {
    loadRoles();
  }, [loadRoles]);

  return {
    roles,
    levels,
    selectedRole,
    selectedLevel,
    groups,
    selectedNodeIds,
    result,
    step,
    isRolesLoading,
    isLevelsLoading,
    isGroupsLoading,
    isAnalyzing,
    error,
    actions: {
      analyze,
      goToRoleStep,
      goToSkillStep,
      loadGroups,
      loadLevels,
      reset,
      selectLevel,
      selectRole,
      showHistoryResult,
      toggleSkill,
    },
  };
}
