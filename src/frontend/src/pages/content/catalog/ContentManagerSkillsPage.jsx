/* eslint-disable react-hooks/set-state-in-effect */
import { useCallback, useEffect, useMemo, useState } from "react";
import { Edit3, Loader2, Plus, RefreshCw, Search, Tag } from "lucide-react";
import { toast } from "react-toastify";

import { contentSkillCatalogApi } from "../../../api/contentCatalogApi";
import AppSelect from "../../../components/common/AppSelect";
import { skillApi } from "../../../api/skillApi";
import {
  ModuleBadge,
  ModuleButton,
  ModuleCard,
  ModuleEmptyState,
  ModuleField,
  ModulePageShell,
  inputClass,
} from "../../../features/learningModules/components/learningModuleUi";
import SkillFormModal from "../../../features/contentCatalog/SkillFormModal";
import { getFriendlyApiErrorMessage } from "../../../utils/apiErrorUtils";
import { PERMISSIONS } from "../../../constants/permissions";
import { hasPermission } from "../../../utils/authorizationUtils";
import { useAuthStore } from "../../../stores/useAuthStore";

const pageSize = 10;

export default function ContentManagerSkillsPage() {
  const user = useAuthStore((state) => state.user);
  const canCreateSkill = hasPermission(user, PERMISSIONS.SKILL_CREATE_CATALOG);
  const canUpdateSkill = hasPermission(user, PERMISSIONS.SKILL_UPDATE_CATALOG);

  const [items, setItems] = useState([]);
  const [categories, setCategories] = useState([]);
  const [search, setSearch] = useState("");
  const [category, setCategory] = useState("");
  const [offset, setOffset] = useState(0);
  const [totalCount, setTotalCount] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState("");
  const [modalError, setModalError] = useState("");
  const [editingSkill, setEditingSkill] = useState(null);
  const [isCreateOpen, setIsCreateOpen] = useState(false);

  const page = Math.floor(offset / pageSize) + 1;
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

  const loadCategories = useCallback(async () => {
    try {
      const result = await skillApi.getCategories();
      setCategories(Array.isArray(result) ? result : []);
    } catch {
      setCategories([]);
    }
  }, []);

  const loadSkills = useCallback(async () => {
    try {
      setIsLoading(true);
      setError("");
      const result = await contentSkillCatalogApi.searchSkills({
        search,
        category,
        limit: pageSize,
        offset,
      });
      setItems(result.items);
      setTotalCount(result.totalCount);
    } catch (loadError) {
      setItems([]);
      setTotalCount(0);
      setError(getFriendlyApiErrorMessage(loadError, "Unable to load skills."));
    } finally {
      setIsLoading(false);
    }
  }, [category, offset, search]);

  useEffect(() => {
    loadCategories();
  }, [loadCategories]);

  useEffect(() => {
    loadSkills();
  }, [loadSkills]);

  const canGoPrev = offset > 0;
  const canGoNext = offset + pageSize < totalCount;

  const submitCreate = async (payload) => {
    try {
      setIsSaving(true);
      setModalError("");
      await contentSkillCatalogApi.createSkill(payload);
      toast.success("Skill created.");
      setIsCreateOpen(false);
      await Promise.all([loadSkills(), loadCategories()]);
    } catch (saveError) {
      setModalError(getFriendlyApiErrorMessage(saveError, "Unable to create skill."));
    } finally {
      setIsSaving(false);
    }
  };

  const submitUpdate = async (payload) => {
    if (!editingSkill?.skillId) return;

    try {
      setIsSaving(true);
      setModalError("");
      await contentSkillCatalogApi.updateSkill(editingSkill.skillId, payload);
      toast.success("Skill saved.");
      setEditingSkill(null);
      await Promise.all([loadSkills(), loadCategories()]);
    } catch (saveError) {
      setModalError(getFriendlyApiErrorMessage(saveError, "Unable to save skill."));
    } finally {
      setIsSaving(false);
    }
  };

  const categoryFilterOptions = useMemo(
    () => [
      { value: "", label: "All categories" },
      ...categories.filter(Boolean).map((item) => ({ value: item, label: item })),
    ],
    [categories],
  );

  return (
    <ModulePageShell compact>
      <div className="space-y-5">
        <section className="rounded-xl border border-[#B9D8CC] bg-white p-5 shadow-sm">
          <div className="flex flex-wrap items-center justify-between gap-4">
            <div className="flex min-w-0 items-center gap-3">
              <div className="grid h-11 w-11 shrink-0 place-items-center rounded-lg bg-[#6FCF97]/20 text-[#1F6F5F]">
                <Tag size={22} />
              </div>
              <div className="min-w-0">
                <p className="text-xs font-extrabold uppercase tracking-[0.18em] text-[#1F6F5F]">
                  Skills catalog
                </p>
                <h1 className="text-2xl font-extrabold text-[#18332D]">Manage skills</h1>
              </div>
            </div>
            {canCreateSkill && (
              <ModuleButton onClick={() => {
                setModalError("");
                setIsCreateOpen(true);
              }}>
                <Plus size={14} /> Create skill
              </ModuleButton>
            )}
          </div>
        </section>

        <ModuleCard className="p-4">
          <div className="grid gap-3 lg:grid-cols-[1fr_240px_auto]">
            <ModuleField label="Search">
              <div className="relative">
                <Search size={16} className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
                <input
                  type="search"
                  value={search}
                  onChange={(event) => {
                    setSearch(event.target.value);
                    setOffset(0);
                  }}
                  className={`${inputClass} pl-9`}
                  placeholder="Search name, category, description"
                />
              </div>
            </ModuleField>
            <ModuleField label="Category">
              <AppSelect
                value={category}
                options={categoryFilterOptions}
                onChange={(value) => {
                  setCategory(value);
                  setOffset(0);
                }}
                ariaLabel="Filter skills by category"
              />
            </ModuleField>
            <div className="flex items-end">
              <ModuleButton variant="secondary" onClick={loadSkills} disabled={isLoading}>
                {isLoading ? <Loader2 size={14} className="animate-spin" /> : <RefreshCw size={14} />}
                Refresh
              </ModuleButton>
            </div>
          </div>
        </ModuleCard>

        {error && <ModuleCard className="border-red-200 bg-red-50 p-4 text-sm font-bold text-red-700">{error}</ModuleCard>}

        <ModuleCard className="overflow-hidden">
          {isLoading ? (
            <div className="flex items-center justify-center gap-2 p-10 text-sm font-bold text-slate-600">
              <Loader2 size={16} className="animate-spin text-[#1F6F5F]" /> Loading skills...
            </div>
          ) : items.length === 0 ? (
            <div className="p-4">
              <ModuleEmptyState
                title="No skills found"
                action={canCreateSkill ? (
                  <ModuleButton onClick={() => {
                    setModalError("");
                    setIsCreateOpen(true);
                  }}>
                    <Plus size={14} /> Create skill
                  </ModuleButton>
                ) : null}
              />
            </div>
          ) : (
            <div className="divide-y divide-[#B9D8CC]/70">
              {items.map((skill) => (
                <article key={skill.skillId} className="flex flex-wrap items-center justify-between gap-3 p-4">
                  <div className="min-w-0">
                    <div className="flex flex-wrap items-center gap-2">
                      <h2 className="text-sm font-extrabold text-[#18332D]">{skill.name}</h2>
                      {skill.category && <ModuleBadge tone="green">{skill.category}</ModuleBadge>}
                      {skill.usageCount > 0 && <ModuleBadge tone="slate">Used {skill.usageCount}</ModuleBadge>}
                    </div>
                    {skill.description && (
                      <p className="mt-1 line-clamp-2 text-sm font-semibold leading-6 text-slate-600">{skill.description}</p>
                    )}
                  </div>
                  <ModuleButton
                    variant="secondary"
                    onClick={() => {
                      setModalError("");
                      setEditingSkill(skill);
                    }}
                    disabled={!canUpdateSkill || !skill.canEdit}
                    title={skill.canEdit ? "Edit skill" : "Used skills cannot be edited here"}
                  >
                    <Edit3 size={14} /> Edit
                  </ModuleButton>
                </article>
              ))}
            </div>
          )}

          <div className="flex flex-wrap items-center justify-between gap-3 border-t border-[#B9D8CC]/70 p-4 text-xs font-bold text-slate-600">
            <span>Page {page} of {totalPages}</span>
            <div className="flex gap-2">
              <ModuleButton variant="secondary" onClick={() => setOffset((current) => Math.max(current - pageSize, 0))} disabled={!canGoPrev || isLoading}>Previous</ModuleButton>
              <ModuleButton variant="secondary" onClick={() => setOffset((current) => current + pageSize)} disabled={!canGoNext || isLoading}>Next</ModuleButton>
            </div>
          </div>
        </ModuleCard>
      </div>

      <SkillFormModal
        isOpen={canCreateSkill && isCreateOpen}
        categories={categories}
        isSaving={isSaving}
        error={modalError}
        onClose={() => setIsCreateOpen(false)}
        onSubmit={submitCreate}
      />
      <SkillFormModal
        isOpen={canUpdateSkill && Boolean(editingSkill)}
        skill={editingSkill}
        categories={categories}
        isSaving={isSaving}
        error={modalError}
        onClose={() => setEditingSkill(null)}
        onSubmit={submitUpdate}
      />
    </ModulePageShell>
  );
}
