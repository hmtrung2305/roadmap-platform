/* eslint-disable react-hooks/set-state-in-effect */
import { useEffect, useMemo, useState } from "react";
import {
  AlertTriangle,
  Check,
  CheckCircle2,
  Layers3,
  Loader2,
  RefreshCw,
  Save,
  Search,
  SlidersHorizontal,
  Target,
} from "lucide-react";
import { toast } from "react-toastify";

import { skillGapApi } from "../../api/skillGapApi";
import {
  ModuleBadge,
  ModuleButton,
  ModuleCard,
  ModuleEmptyState,
  ModulePageShell,
  inputClass,
} from "../../components/learningModules/learningModuleUi";
import { getFriendlyApiErrorMessage } from "../../utils/apiErrorUtils";
import { getAssessmentLevelStyle, normalizeCareerRoles, normalizeId, toArray } from "../../components/skillGap/skillGapUtils";

function normalizeAdminLevel(level) {
  const levelId = level?.levelId ?? level?.LevelId;
  const levelName = level?.levelName ?? level?.LevelName ?? level?.name ?? level?.Name ?? "Assessment level";
  const slug = level?.slug ?? level?.Slug ?? level?.levelSlug ?? level?.LevelSlug ?? "";

  return {
    ...level,
    levelId,
    id: levelId,
    levelName,
    name: levelName,
    slug: String(slug || "").trim(),
    groupCount: Number(level?.groupCount ?? level?.GroupCount ?? 0),
  };
}

function normalizeAdminGroup(group) {
  const groupId = normalizeId(group?.groupId ?? group?.GroupId ?? group?.id ?? group?.Id);

  return {
    ...group,
    groupId,
    groupName: group?.groupName ?? group?.GroupName ?? "Unnamed group",
    groupSlug: group?.groupSlug ?? group?.GroupSlug ?? "",
    phaseName: group?.phaseName ?? group?.PhaseName ?? "Unassigned phase",
    sortOrder: Number(group?.sortOrder ?? group?.SortOrder ?? 0),
    selected: Boolean(group?.selected ?? group?.Selected),
  };
}

function HeaderCard() {
  return (
    <section className="rounded-xl border border-[#B9D8CC] bg-white p-5 shadow-sm">
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div className="flex items-center gap-3">
          <div className="grid h-11 w-11 place-items-center rounded-lg bg-[#6FCF97]/20 text-[#1F6F5F]">
            <SlidersHorizontal size={22} />
          </div>
          <div>
            <p className="text-xs font-extrabold uppercase tracking-[0.18em] text-[#1F6F5F]">
              Skill Gap
            </p>
            <h1 className="text-2xl font-extrabold text-[#18332D]">
              Assessment level groups
            </h1>
          </div>
        </div>

        <ModuleBadge tone="green" className="px-3 py-1">
          Content Manager
        </ModuleBadge>
      </div>
    </section>
  );
}

function HiddenScrollbarStyles() {
  return (
    <style>{`
      .skill-gap-admin-hidden-scrollbar {
        scrollbar-width: none;
        -ms-overflow-style: none;
      }

      .skill-gap-admin-hidden-scrollbar::-webkit-scrollbar {
        display: none;
      }
    `}</style>
  );
}

function StatCard({ icon: Icon, label, value }) {
  return (
    <ModuleCard className="p-4">
      <div className="flex items-start gap-3">
        <div className="grid h-10 w-10 shrink-0 place-items-center rounded-lg bg-[#6FCF97]/18 text-[#1F6F5F]">
          <Icon size={18} />
        </div>
        <div className="min-w-0">
          <div className="text-2xl font-extrabold leading-tight text-[#18332D]">{value}</div>
          <div className="mt-1 text-xs font-extrabold uppercase tracking-wide text-slate-500">{label}</div>
        </div>
      </div>
    </ModuleCard>
  );
}

function groupByPhase(groups) {
  return groups.reduce((acc, group) => {
    const phaseName = group.phaseName || "Unassigned phase";
    if (!acc.has(phaseName)) acc.set(phaseName, []);
    acc.get(phaseName).push(group);
    return acc;
  }, new Map());
}

export default function ContentManagerSkillGapPage() {
  const [roles, setRoles] = useState([]);
  const [levels, setLevels] = useState([]);
  const [groups, setGroups] = useState([]);
  const [selectedRoleSlug, setSelectedRoleSlug] = useState("");
  const [selectedLevelSlug, setSelectedLevelSlug] = useState("");
  const [selectedGroupIds, setSelectedGroupIds] = useState([]);
  const [searchTerm, setSearchTerm] = useState("");
  const [isLoadingRoles, setIsLoadingRoles] = useState(true);
  const [isLoadingLevels, setIsLoadingLevels] = useState(false);
  const [isLoadingGroups, setIsLoadingGroups] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState("");

  const selectedRole = useMemo(
    () => roles.find((role) => role.slug === selectedRoleSlug) || null,
    [roles, selectedRoleSlug],
  );
  const selectedLevel = useMemo(
    () => levels.find((level) => level.slug === selectedLevelSlug) || null,
    [levels, selectedLevelSlug],
  );
  const selectedSet = useMemo(() => new Set(selectedGroupIds), [selectedGroupIds]);

  const filteredGroups = useMemo(() => {
    const keyword = searchTerm.trim().toLowerCase();
    const orderedGroups = groups.slice().sort((a, b) => a.sortOrder - b.sortOrder);

    if (!keyword) return orderedGroups;

    return orderedGroups.filter((group) => {
      return [group.groupName, group.phaseName, group.groupSlug]
        .filter(Boolean)
        .some((value) => String(value).toLowerCase().includes(keyword));
    });
  }, [groups, searchTerm]);

  const phaseGroups = useMemo(() => Array.from(groupByPhase(filteredGroups).entries()), [filteredGroups]);
  const selectedVisibleGroupIds = filteredGroups.filter((group) => selectedSet.has(group.groupId)).map((group) => group.groupId);
  const dirtySelectedCount = selectedGroupIds.length;

  const fetchRoles = async () => {
    try {
      setIsLoadingRoles(true);
      setError("");
      const roleList = normalizeCareerRoles(await skillGapApi.getCareerRoles());
      setRoles(roleList);
      setSelectedRoleSlug((current) => current || roleList[0]?.slug || "");
    } catch (fetchError) {
      setError(getFriendlyApiErrorMessage(fetchError, "Unable to load career roles."));
    } finally {
      setIsLoadingRoles(false);
    }
  };

  useEffect(() => {
    fetchRoles();
  }, []);

  useEffect(() => {
    if (!selectedRoleSlug) {
      setLevels([]);
      setSelectedLevelSlug("");
      setGroups([]);
      return;
    }

    let isActive = true;

    async function fetchLevels() {
      try {
        setIsLoadingLevels(true);
        setError("");
        const levelList = toArray(await skillGapApi.getAdminAssessmentLevels(selectedRoleSlug))
          .map(normalizeAdminLevel)
          .filter((level) => level.slug);

        if (!isActive) return;

        setLevels(levelList);
        setSelectedLevelSlug((current) => {
          if (levelList.some((level) => level.slug === current)) return current;
          return levelList[0]?.slug || "";
        });
      } catch (fetchError) {
        if (!isActive) return;
        setLevels([]);
        setSelectedLevelSlug("");
        setGroups([]);
        setError(getFriendlyApiErrorMessage(fetchError, "Unable to load assessment levels."));
      } finally {
        if (isActive) setIsLoadingLevels(false);
      }
    }

    fetchLevels();

    return () => {
      isActive = false;
    };
  }, [selectedRoleSlug]);

  const fetchGroups = async () => {
    if (!selectedRoleSlug || !selectedLevelSlug) {
      setGroups([]);
      setSelectedGroupIds([]);
      return;
    }

    try {
      setIsLoadingGroups(true);
      setError("");
      const response = await skillGapApi.getAdminGroupsByLevel(selectedRoleSlug, selectedLevelSlug);
      const groupList = toArray(response?.groups ?? response?.Groups)
        .map(normalizeAdminGroup)
        .filter((group) => group.groupId)
        .sort((a, b) => a.sortOrder - b.sortOrder);

      setGroups(groupList);
      setSelectedGroupIds(groupList.filter((group) => group.selected).map((group) => group.groupId));
    } catch (fetchError) {
      setGroups([]);
      setSelectedGroupIds([]);
      setError(getFriendlyApiErrorMessage(fetchError, "Unable to load groups for this level."));
    } finally {
      setIsLoadingGroups(false);
    }
  };

  useEffect(() => {
    fetchGroups();
  }, [selectedRoleSlug, selectedLevelSlug]);

  const toggleGroup = (groupId) => {
    setSelectedGroupIds((current) => (
      current.includes(groupId)
        ? current.filter((item) => item !== groupId)
        : [...current, groupId]
    ));
  };

  const selectAllVisible = () => {
    const visibleIds = filteredGroups.map((group) => group.groupId);
    setSelectedGroupIds((current) => Array.from(new Set([...current, ...visibleIds])));
  };

  const clearVisible = () => {
    const visibleIds = new Set(filteredGroups.map((group) => group.groupId));
    setSelectedGroupIds((current) => current.filter((groupId) => !visibleIds.has(groupId)));
  };

  const saveGroups = async () => {
    if (!selectedRoleSlug || !selectedLevelSlug) return;

    try {
      setIsSaving(true);
      setError("");
      await skillGapApi.updateAdminGroupsByLevel({
        careerRoleSlug: selectedRoleSlug,
        levelSlug: selectedLevelSlug,
        groupIds: selectedGroupIds,
      });
      toast.success("Skill gap level groups updated.");
      await Promise.all([
        skillGapApi.getAdminAssessmentLevels(selectedRoleSlug).then((data) => {
          const levelList = toArray(data).map(normalizeAdminLevel).filter((level) => level.slug);
          setLevels(levelList);
        }),
        fetchGroups(),
      ]);
    } catch (saveError) {
      setError(getFriendlyApiErrorMessage(saveError, "Unable to save group configuration."));
    } finally {
      setIsSaving(false);
    }
  };

  const isBusy = isLoadingRoles || isLoadingLevels || isLoadingGroups;

  return (
    <ModulePageShell compact>
      <HiddenScrollbarStyles />
      <div className="space-y-5">
        <HeaderCard />

        {error && (
          <ModuleCard className="border-red-200 bg-red-50 p-4 text-sm font-bold text-red-700">
            <div className="flex items-start gap-2">
              <AlertTriangle size={17} className="mt-0.5 shrink-0" />
              <span>{error}</span>
            </div>
          </ModuleCard>
        )}

        <div className="grid gap-3 sm:grid-cols-3">
          <StatCard icon={Target} label="Career roles" value={roles.length || "—"} />
          <StatCard icon={Layers3} label="Levels" value={levels.length || "—"} />
          <StatCard icon={CheckCircle2} label="Selected groups" value={`${dirtySelectedCount}/${groups.length || 0}`} />
        </div>

        {isLoadingRoles ? (
          <ModuleCard className="grid min-h-64 place-items-center p-8 text-sm font-bold text-slate-600">
            <div className="flex items-center gap-2">
              <Loader2 className="animate-spin text-[#2FA084]" size={18} />
              Loading skill gap configuration...
            </div>
          </ModuleCard>
        ) : roles.length === 0 ? (
          <ModuleEmptyState title="No career roles found">
            Career roles must exist before content managers can configure skill gap assessment levels.
          </ModuleEmptyState>
        ) : (
          <div className="grid items-stretch gap-5 xl:grid-cols-[340px_minmax(0,1fr)]">
            <ModuleCard className="flex h-300 flex-col overflow-hidden p-4">
              <div className="mb-3 text-xs font-extrabold uppercase tracking-wide text-slate-500">
                Career role
              </div>
              <div className="skill-gap-admin-hidden-scrollbar grid min-h-0 flex-1 content-start gap-2 overflow-y-auto pr-1">
                {roles.map((role) => {
                  const active = role.slug === selectedRoleSlug;

                  return (
                    <button
                      key={role.careerRoleId || role.slug}
                      type="button"
                      onClick={() => setSelectedRoleSlug(role.slug)}
                      className={`flex items-center justify-between gap-3 rounded-lg border px-3 py-2.5 text-left text-sm font-extrabold transition ${
                        active
                          ? "border-[#2FA084] bg-[#6FCF97]/20 text-[#1F6F5F]"
                          : "border-slate-200 bg-white text-slate-700 hover:border-[#2FA084] hover:bg-[#F7F1E8]"
                      }`}
                    >
                      <span className="min-w-0 truncate">{role.name}</span>
                      {active && <Check size={15} className="shrink-0" />}
                    </button>
                  );
                })}
              </div>
            </ModuleCard>

            <div className="flex h-300 min-h-0 flex-col gap-5">
              <ModuleCard className="shrink-0 p-4">
                <div className="flex flex-col gap-3 lg:flex-row lg:items-start lg:justify-between">
                  <div>
                    <p className="text-xs font-extrabold uppercase tracking-wide text-[#1F6F5F]">
                      {selectedRole?.name || "Selected role"}
                    </p>
                    <h2 className="mt-1 text-xl font-extrabold text-[#18332D]">
                      Configure groups by assessment level
                    </h2>
                  </div>

                  <div className="flex flex-wrap gap-2">
                    <ModuleButton variant="secondary" onClick={fetchGroups} disabled={isBusy || !selectedLevelSlug}>
                      <RefreshCw size={14} /> Reload
                    </ModuleButton>
                    <ModuleButton onClick={saveGroups} disabled={isBusy || isSaving || !selectedLevelSlug}>
                      {isSaving ? <Loader2 className="animate-spin" size={14} /> : <Save size={14} />}
                      Save
                    </ModuleButton>
                  </div>
                </div>

                <div className="mt-5 grid gap-3 sm:grid-cols-3">
                  {isLoadingLevels ? (
                    <div className="col-span-full flex items-center gap-2 rounded-xl border border-dashed border-[#B9D8CC] bg-[#F7F1E8]/60 p-4 text-sm font-bold text-slate-600">
                      <Loader2 className="animate-spin text-[#2FA084]" size={17} />
                      Loading levels...
                    </div>
                  ) : levels.length === 0 ? (
                    <div className="col-span-full rounded-xl border border-dashed border-[#B9D8CC] bg-[#F7F1E8]/60 p-4 text-sm font-bold text-slate-600">
                      No levels are configured for this role.
                    </div>
                  ) : levels.map((level) => {
                    const active = level.slug === selectedLevelSlug;
                    const style = getAssessmentLevelStyle(level.slug);

                    return (
                      <button
                        key={level.levelId || level.slug}
                        type="button"
                        onClick={() => setSelectedLevelSlug(level.slug)}
                        className={`rounded-2xl border p-4 text-left transition hover:-translate-y-0.5 hover:shadow-sm ${
                          active
                            ? `${style.panel} border-[#2FA084] shadow-sm`
                            : "border-[#B9D8CC]/80 bg-white hover:border-[#2FA084]"
                        }`}
                      >
                        <div className="flex items-start justify-between gap-2">
                          <span className={`rounded-full border px-2.5 py-1 text-[11px] font-extrabold ${style.badge}`}>
                            {level.levelName}
                          </span>
                          {active && <Check size={15} className="text-[#1F6F5F]" />}
                        </div>
                        <p className="mt-3 text-xs font-bold text-slate-600">
                          {level.groupCount} selected groups
                        </p>
                      </button>
                    );
                  })}
                </div>
              </ModuleCard>

              <ModuleCard className="flex min-h-0 flex-1 flex-col overflow-hidden">
                <div className="border-b border-[#B9D8CC]/70 p-4">
                  <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
                    <div>
                      <h3 className="text-sm font-extrabold uppercase tracking-wide text-[#18332D]">
                        Select roadmap choice groups
                      </h3>
                      <p className="mt-1 text-xs font-extrabold text-[#1F6F5F]">
                        {selectedVisibleGroupIds.length}/{filteredGroups.length} visible groups selected
                      </p>
                    </div>

                    <div className="flex flex-col gap-2 sm:flex-row sm:items-center">
                      <div className="relative min-w-60">
                        <Search size={15} className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
                        <input
                          value={searchTerm}
                          onChange={(event) => setSearchTerm(event.target.value)}
                          placeholder="Search group or phase"
                          className={`${inputClass} pl-9`}
                        />
                      </div>
                      <ModuleButton variant="secondary" onClick={selectAllVisible} disabled={isBusy || filteredGroups.length === 0}>
                        Select visible
                      </ModuleButton>
                      <ModuleButton variant="ghost" onClick={clearVisible} disabled={isBusy || filteredGroups.length === 0}>
                        Clear visible
                      </ModuleButton>
                    </div>
                  </div>
                </div>

                {isLoadingGroups ? (
                  <div className="grid min-h-72 place-items-center p-8 text-sm font-bold text-slate-600">
                    <div className="flex items-center gap-2">
                      <Loader2 className="animate-spin text-[#2FA084]" size={18} />
                      Loading groups...
                    </div>
                  </div>
                ) : groups.length === 0 ? (
                  <div className="p-5">
                    <ModuleEmptyState title="No groups available">
                      This role needs a published roadmap with choice groups before it can be configured.
                    </ModuleEmptyState>
                  </div>
                ) : filteredGroups.length === 0 ? (
                  <div className="p-5">
                    <ModuleEmptyState title="No matching groups">
                      Try another keyword or clear the search box.
                    </ModuleEmptyState>
                  </div>
                ) : (
                  <div className="skill-gap-admin-hidden-scrollbar grid min-h-0 flex-1 content-start gap-4 overflow-y-auto p-4">
                    {phaseGroups.map(([phaseName, phaseItems]) => (
                      <section key={phaseName} className="rounded-2xl border border-slate-200 bg-slate-50/60 p-3">
                        <div className="mb-3 flex flex-wrap items-center justify-between gap-2">
                          <div className="text-xs font-extrabold uppercase tracking-wide text-slate-500">
                            {phaseName}
                          </div>
                          <ModuleBadge tone="slate">
                            {phaseItems.filter((group) => selectedSet.has(group.groupId)).length}/{phaseItems.length} selected
                          </ModuleBadge>
                        </div>

                        <div className="grid gap-2 md:grid-cols-2">
                          {phaseItems.map((group) => {
                            const checked = selectedSet.has(group.groupId);

                            return (
                              <button
                                key={group.groupId}
                                type="button"
                                onClick={() => toggleGroup(group.groupId)}
                                className={`flex items-start gap-3 rounded-xl border p-3 text-left transition hover:-translate-y-0.5 hover:shadow-sm ${
                                  checked
                                    ? "border-[#2FA084] bg-[#6FCF97]/18"
                                    : "border-slate-200 bg-white hover:border-[#B9D8CC]"
                                }`}
                              >
                                <span className={`mt-0.5 grid h-5 w-5 shrink-0 place-items-center rounded-md border ${checked ? "border-[#2FA084] bg-[#2FA084] text-white" : "border-slate-300 bg-white text-transparent"}`}>
                                  <Check size={13} />
                                </span>
                                <span className="min-w-0 flex-1">
                                  <span className="block text-sm font-extrabold text-[#18332D]">
                                    {group.groupName}
                                  </span>
                                  <span className="mt-1 block truncate text-xs font-semibold text-slate-500">
                                    {group.groupSlug || "choice group"}
                                  </span>
                                </span>
                              </button>
                            );
                          })}
                        </div>
                      </section>
                    ))}
                  </div>
                )}
              </ModuleCard>
            </div>
          </div>
        )}
      </div>
    </ModulePageShell>
  );
}
