import { useEffect, useState } from "react";
import { skillGapApi } from "../api/skillGapApi";
import {
  normalizeAssessmentGroups,
  normalizeSkillGapResult,
} from "../components/skillGap/skillGapUtils";

export function useSkillGapAnalysis() {
  const [roles, setRoles] = useState([]);
  const [selectedRole, setSelectedRole] = useState(null);
  const [groups, setGroups] = useState([]);
  const [selectedSkillSlugs, setSelectedSkillSlugs] = useState([]);
  const [result, setResult] = useState(null);
  const [step, setStep] = useState(1);
  const [isRolesLoading, setIsRolesLoading] = useState(true);
  const [isGroupsLoading, setIsGroupsLoading] = useState(false);
  const [isAnalyzing, setIsAnalyzing] = useState(false);
  const [error, setError] = useState("");

  useEffect(() => {
    let ignore = false;

    async function loadRoles() {
      setIsRolesLoading(true);
      setError("");

      try {
        const data = await skillGapApi.getCareerRoles();
        if (!ignore) setRoles(data);
      } catch (err) {
        if (!ignore) setError(err?.message || "Unable to load career roles.");
      } finally {
        if (!ignore) setIsRolesLoading(false);
      }
    }

    loadRoles();

    return () => {
      ignore = true;
    };
  }, []);

  const selectRole = (role) => {
    setSelectedRole(role);
    setGroups([]);
    setSelectedSkillSlugs([]);
    setResult(null);
    setError("");
  };

  const loadGroups = async () => {
    if (!selectedRole?.slug) return;

    setIsGroupsLoading(true);
    setError("");

    try {
      const response = await skillGapApi.getAssessmentSkills(selectedRole.slug);
      setGroups(normalizeAssessmentGroups(response));
      setSelectedSkillSlugs([]);
      setResult(null);
      setStep(2);
    } catch (err) {
      setError(err?.message || "Unable to load assessment skills for this role.");
    } finally {
      setIsGroupsLoading(false);
    }
  };

  const toggleSkill = (slug) => {
    if (!slug) return;

    setSelectedSkillSlugs((current) => {
      if (current.includes(slug)) {
        return current.filter((item) => item !== slug);
      }

      return [...current, slug];
    });
  };

  const analyze = async () => {
    if (!selectedRole?.slug) return;

    setIsAnalyzing(true);
    setError("");

    try {
      const response = await skillGapApi.analyzeSkillGap({
        careerRoleSlug: selectedRole.slug,
        selectedSkillSlugs,
      });

      setResult(normalizeSkillGapResult(response));
      setStep(3);
    } catch (err) {
      setError(err?.message || "Unable to analyze your skill gap.");
    } finally {
      setIsAnalyzing(false);
    }
  };

  const reset = () => {
    setSelectedRole(null);
    setGroups([]);
    setSelectedSkillSlugs([]);
    setResult(null);
    setError("");
    setStep(1);
  };

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
      goToRoleStep: () => setStep(1),
      goToSkillStep: () => setStep(2),
      loadGroups,
      reset,
      selectRole,
      toggleSkill,
    },
  };
}
