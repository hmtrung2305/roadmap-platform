/* eslint-disable react-hooks/set-state-in-effect */
import { useCallback, useEffect, useState } from "react";
import { BookOpenText, Edit3, ExternalLink, Loader2, Plus, RefreshCw, Search } from "lucide-react";
import { toast } from "react-toastify";

import { contentLearningResourceCatalogApi } from "../../../api/contentCatalogApi";
import AppSelect from "../../../components/common/AppSelect";
import {
  ModuleBadge,
  ModuleButton,
  ModuleCard,
  ModuleEmptyState,
  ModuleField,
  ModulePageShell,
  inputClass,
} from "../../../features/learningModules/components/learningModuleUi";
import LearningResourceFormModal from "../../../features/contentCatalog/LearningResourceFormModal";
import {
  resourceDifficultyOptions,
  resourceTypeOptions,
} from "../../../features/contentCatalog/catalogConstants";
import { getFriendlyApiErrorMessage } from "../../../utils/apiErrorUtils";
import { PERMISSIONS } from "../../../constants/permissions";
import { hasPermission } from "../../../utils/authorizationUtils";
import { useAuthStore } from "../../../stores/useAuthStore";

const pageSize = 10;

function getTypeLabel(value) {
  return resourceTypeOptions.find((option) => option.value === value)?.label || value || "Resource";
}

export default function ContentManagerLearningResourcesPage() {
  const user = useAuthStore((state) => state.user);
  const canCreateResource = hasPermission(user, PERMISSIONS.LEARNING_RESOURCE_CREATE_CATALOG);
  const canUpdateResource = hasPermission(user, PERMISSIONS.LEARNING_RESOURCE_UPDATE_CATALOG);

  const [items, setItems] = useState([]);
  const [search, setSearch] = useState("");
  const [resourceType, setResourceType] = useState("");
  const [difficultyLevel, setDifficultyLevel] = useState("");
  const [offset, setOffset] = useState(0);
  const [totalCount, setTotalCount] = useState(0);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState("");
  const [modalError, setModalError] = useState("");
  const [editingResource, setEditingResource] = useState(null);
  const [isCreateOpen, setIsCreateOpen] = useState(false);

  const page = Math.floor(offset / pageSize) + 1;
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));

  const loadResources = useCallback(async () => {
    try {
      setIsLoading(true);
      setError("");
      const result = await contentLearningResourceCatalogApi.searchResources({
        search,
        resourceType,
        difficultyLevel,
        limit: pageSize,
        offset,
      });
      setItems(result.items);
      setTotalCount(result.totalCount);
    } catch (loadError) {
      setItems([]);
      setTotalCount(0);
      setError(getFriendlyApiErrorMessage(loadError, "Unable to load resources."));
    } finally {
      setIsLoading(false);
    }
  }, [difficultyLevel, offset, resourceType, search]);

  useEffect(() => {
    loadResources();
  }, [loadResources]);

  const canGoPrev = offset > 0;
  const canGoNext = offset + pageSize < totalCount;

  const submitCreate = async (payload) => {
    try {
      setIsSaving(true);
      setModalError("");
      await contentLearningResourceCatalogApi.createResource(payload);
      toast.success("Resource created.");
      setIsCreateOpen(false);
      await loadResources();
    } catch (saveError) {
      setModalError(getFriendlyApiErrorMessage(saveError, "Unable to create resource."));
    } finally {
      setIsSaving(false);
    }
  };

  const submitUpdate = async (payload) => {
    if (!editingResource?.resourceId) return;

    try {
      setIsSaving(true);
      setModalError("");
      await contentLearningResourceCatalogApi.updateResource(editingResource.resourceId, payload);
      toast.success("Resource saved.");
      setEditingResource(null);
      await loadResources();
    } catch (saveError) {
      setModalError(getFriendlyApiErrorMessage(saveError, "Unable to save resource."));
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <ModulePageShell compact>
      <div className="space-y-5">
        <section className="rounded-xl border border-[#B9D8CC] bg-white p-5 shadow-sm">
          <div className="flex flex-wrap items-center justify-between gap-4">
            <div className="flex min-w-0 items-center gap-3">
              <div className="grid h-11 w-11 shrink-0 place-items-center rounded-lg bg-[#6FCF97]/20 text-[#1F6F5F]">
                <BookOpenText size={22} />
              </div>
              <div className="min-w-0">
                <p className="text-xs font-extrabold uppercase tracking-[0.18em] text-[#1F6F5F]">
                  Learning resources catalog
                </p>
                <h1 className="text-2xl font-extrabold text-[#18332D]">Manage resources</h1>
              </div>
            </div>
            {canCreateResource && (
              <ModuleButton onClick={() => {
                setModalError("");
                setIsCreateOpen(true);
              }}>
                <Plus size={14} /> Create resource
              </ModuleButton>
            )}
          </div>
        </section>

        <ModuleCard className="p-4">
          <div className="grid gap-3 xl:grid-cols-[1fr_210px_210px_auto]">
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
                  placeholder="Search title, URL, provider, description"
                />
              </div>
            </ModuleField>
            <ModuleField label="Type">
              <AppSelect
                value={resourceType}
                options={[{ value: "", label: "All types" }, ...resourceTypeOptions]}
                onChange={(value) => {
                  setResourceType(value);
                  setOffset(0);
                }}
                ariaLabel="Filter resources by type"
              />
            </ModuleField>
            <ModuleField label="Difficulty">
              <AppSelect
                value={difficultyLevel}
                options={resourceDifficultyOptions.map((option) => (
                  option.value ? option : { ...option, label: "All difficulties" }
                ))}
                onChange={(value) => {
                  setDifficultyLevel(value);
                  setOffset(0);
                }}
                ariaLabel="Filter resources by difficulty"
              />
            </ModuleField>
            <div className="flex items-end">
              <ModuleButton variant="secondary" onClick={loadResources} disabled={isLoading}>
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
              <Loader2 size={16} className="animate-spin text-[#1F6F5F]" /> Loading resources...
            </div>
          ) : items.length === 0 ? (
            <div className="p-4">
              <ModuleEmptyState
                title="No resources found"
                action={canCreateResource ? (
                  <ModuleButton onClick={() => {
                    setModalError("");
                    setIsCreateOpen(true);
                  }}>
                    <Plus size={14} /> Create resource
                  </ModuleButton>
                ) : null}
              />
            </div>
          ) : (
            <div className="divide-y divide-[#B9D8CC]/70">
              {items.map((resource) => (
                <article key={resource.resourceId} className="flex flex-wrap items-center justify-between gap-3 p-4">
                  <div className="min-w-0 flex-1">
                    <div className="flex flex-wrap items-center gap-2">
                      <h2 className="min-w-0 truncate text-sm font-extrabold text-[#18332D]">{resource.title}</h2>
                      <ModuleBadge tone="green">{getTypeLabel(resource.resourceType)}</ModuleBadge>
                      {resource.difficultyLevel && <ModuleBadge tone="blue">{resource.difficultyLevel}</ModuleBadge>}
                      {resource.nodeMappingCount > 0 && <ModuleBadge tone="slate">Used {resource.nodeMappingCount}</ModuleBadge>}
                    </div>
                    <div className="mt-1 flex min-w-0 items-center gap-2 text-xs font-semibold text-slate-600">
                      <span className="truncate">{resource.provider || "No provider"}</span>
                      <span>·</span>
                      <a
                        href={resource.url}
                        target="_blank"
                        rel="noreferrer"
                        className="inline-flex min-w-0 items-center gap-1 truncate text-[#1F6F5F] hover:underline"
                      >
                        <span className="truncate">{resource.url}</span>
                        <ExternalLink size={12} className="shrink-0" />
                      </a>
                    </div>
                    {resource.description && (
                      <p className="mt-1 line-clamp-2 text-sm font-semibold leading-6 text-slate-600">{resource.description}</p>
                    )}
                  </div>
                  {canUpdateResource && (
                    <ModuleButton
                      variant="secondary"
                      onClick={() => {
                        setModalError("");
                        setEditingResource(resource);
                      }}
                    >
                      <Edit3 size={14} /> Edit
                    </ModuleButton>
                  )}
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

      <LearningResourceFormModal
        isOpen={canCreateResource && isCreateOpen}
        isSaving={isSaving}
        error={modalError}
        onClose={() => setIsCreateOpen(false)}
        onSubmit={submitCreate}
      />
      <LearningResourceFormModal
        isOpen={canUpdateResource && Boolean(editingResource)}
        resource={editingResource}
        isSaving={isSaving}
        error={modalError}
        onClose={() => setEditingResource(null)}
        onSubmit={submitUpdate}
      />
    </ModulePageShell>
  );
}
