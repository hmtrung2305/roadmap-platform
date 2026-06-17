import { useEffect } from "react";
import { useSkillGapStore } from "../stores/useSkillGapStore";

export function useSkillGapAnalysis() {
  const roles = useSkillGapStore((state) => state.roles);
  const selectedRole = useSkillGapStore((state) => state.selectedRole);
  const groups = useSkillGapStore((state) => state.groups);
  const selectedSkillSlugs = useSkillGapStore((state) => state.selectedSkillSlugs);
  const result = useSkillGapStore((state) => state.result);
  const step = useSkillGapStore((state) => state.step);
  const isRolesLoading = useSkillGapStore((state) => state.isRolesLoading);
  const isGroupsLoading = useSkillGapStore((state) => state.isGroupsLoading);
  const isAnalyzing = useSkillGapStore((state) => state.isAnalyzing);
  const error = useSkillGapStore((state) => state.error);

  const analyze = useSkillGapStore((state) => state.analyze);
  const goToRoleStep = useSkillGapStore((state) => state.goToRoleStep);
  const goToSkillStep = useSkillGapStore((state) => state.goToSkillStep);
  const loadGroups = useSkillGapStore((state) => state.loadGroups);
  const loadRoles = useSkillGapStore((state) => state.loadRoles);
  const reset = useSkillGapStore((state) => state.reset);
  const selectRole = useSkillGapStore((state) => state.selectRole);
  const toggleSkill = useSkillGapStore((state) => state.toggleSkill);

  useEffect(() => {
    loadRoles();
  }, [loadRoles]);

  return {
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
    actions: {
      analyze,
      goToRoleStep,
      goToSkillStep,
      loadGroups,
      reset,
      selectRole,
      toggleSkill,
    },
  };
}
