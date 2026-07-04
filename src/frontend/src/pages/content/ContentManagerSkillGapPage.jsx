import { useCallback, useEffect, useMemo, useState } from "react";
import {
  ArrowDown,
  ArrowUp,
  CheckCircle2,
  Loader2,
  Map,
  RefreshCw,
  Save,
  SlidersHorizontal,
} from "lucide-react";
import { skillGapApi } from "../../api/skillGapApi";
import { getFriendlyApiErrorMessage } from "../../utils/apiErrorUtils";
import {
  normalizeAssessmentResponse,
  normalizeRoadmapOptions,
  toArray,
} from "../../features/skillGap/utils/skillGapUtils";

function orderCategories(categories) {
  return toArray(categories).map((category, index) => ({
    ...category,
    displayOrder: index + 1,
  }));
}

function swapItems(items, fromIndex, toIndex) {
  if (toIndex < 0 || toIndex >= items.length) return items;

  const next = [...items];
  const [item] = next.splice(fromIndex, 1);
  next.splice(toIndex, 0, item);
  return orderCategories(next);
}

function EmptyState({ title, description }) {
  return (
    <div className="rounded-2xl border border-dashed border-[#B9D8CC] bg-[#F7F1E8]/70 p-8 text-center">
      <p className="text-sm font-extrabold text-[#18332D]">{title}</p>
      <p className="mt-1 text-xs font-semibold text-slate-500">{description}</p>
    </div>
  );
}

export default function ContentManagerSkillGapPage() {
  const [roadmaps, setRoadmaps] = useState([]);
  const [selectedRoadmapId, setSelectedRoadmapId] = useState("");
  const [configuration, setConfiguration] = useState(null);
  const [categories, setCategories] = useState([]);
  const [isLoadingRoadmaps, setIsLoadingRoadmaps] = useState(true);
  const [isLoadingCategories, setIsLoadingCategories] = useState(false);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  const selectedRoadmap = useMemo(
    () => roadmaps.find((roadmap) => roadmap.roadmapId === selectedRoadmapId) || null,
    [roadmaps, selectedRoadmapId],
  );

  const totalSkills = useMemo(
    () => categories.reduce((sum, category) => sum + toArray(category.skills).length, 0),
    [categories],
  );

  const loadRoadmaps = useCallback(async () => {
    setIsLoadingRoadmaps(true);
    setError("");
    setSuccess("");

    try {
      const response = await skillGapApi.getMyPublishedRoadmaps();
      const normalizedRoadmaps = normalizeRoadmapOptions(response);
      setRoadmaps(normalizedRoadmaps);

      if (!selectedRoadmapId && normalizedRoadmaps.length > 0) {
        setSelectedRoadmapId(normalizedRoadmaps[0].roadmapId);
      }
    } catch (err) {
      setError(getFriendlyApiErrorMessage(err, "Unable to load your published roadmaps."));
    } finally {
      setIsLoadingRoadmaps(false);
    }
  }, [selectedRoadmapId]);

  const loadCategories = useCallback(async (roadmapId) => {
    if (!roadmapId) {
      setConfiguration(null);
      setCategories([]);
      return;
    }

    setIsLoadingCategories(true);
    setError("");
    setSuccess("");

    try {
      const response = await skillGapApi.getRoadmapCategories(roadmapId);
      const normalizedConfiguration = normalizeAssessmentResponse(response);
      setConfiguration(normalizedConfiguration);
      setCategories(orderCategories(normalizedConfiguration?.categories || []));
    } catch (err) {
      setConfiguration(null);
      setCategories([]);
      setError(getFriendlyApiErrorMessage(err, "Unable to load category configuration for this roadmap."));
    } finally {
      setIsLoadingCategories(false);
    }
  }, []);

  useEffect(() => {
    loadRoadmaps();
  }, [loadRoadmaps]);

  useEffect(() => {
    loadCategories(selectedRoadmapId);
  }, [loadCategories, selectedRoadmapId]);

  const handleSelectRoadmap = (roadmapId) => {
    setSelectedRoadmapId(roadmapId);
  };

  const handleMoveCategory = (index, direction) => {
    setCategories((current) => swapItems(current, index, index + direction));
    setSuccess("");
  };

  const handleSave = async () => {
    if (!selectedRoadmapId || categories.length === 0) return;

    setIsSaving(true);
    setError("");
    setSuccess("");

    try {
      const payload = categories.map((category, index) => ({
        categoryName: category.categoryName,
        displayOrder: index + 1,
      }));

      await skillGapApi.updateRoadmapCategories({
        roadmapId: selectedRoadmapId,
        categories: payload,
      });

      setCategories(orderCategories(payload.map((item) => ({
        ...categories.find((category) => category.categoryName === item.categoryName),
        ...item,
      }))));
      setSuccess("Category order saved successfully.");
      await loadCategories(selectedRoadmapId);
    } catch (err) {
      setError(getFriendlyApiErrorMessage(err, "Unable to save category order."));
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <main className="min-h-[calc(100vh-4rem)] bg-[#F7F1E8] px-4 py-7 sm:px-6 lg:px-8">
      <div className="mx-auto max-w-[1320px]">
        <div className="mb-6 rounded-3xl border border-[#B9D8CC]/80 bg-white/95 p-6 shadow-sm">
          <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
            <div>
              <p className="inline-flex items-center gap-2 rounded-full border border-[#B9D8CC] bg-[#EAF7F1] px-3 py-1 text-xs font-extrabold uppercase tracking-[0.18em] text-[#1F6F5F]">
                <SlidersHorizontal size={14} /> Skill gap configuration
              </p>
              <h1 className="mt-4 text-3xl font-black tracking-tight text-[#18332D]">
                Manage roadmap skill categories
              </h1>
              <p className="mt-2 max-w-3xl text-sm font-semibold leading-6 text-slate-600">
                Choose one of your published roadmaps, review the generated skill categories, and reorder how they appear in learner assessment and analysis results.
              </p>
            </div>

            <button
              type="button"
              onClick={loadRoadmaps}
              disabled={isLoadingRoadmaps || isLoadingCategories || isSaving}
              className="inline-flex items-center justify-center gap-2 rounded-lg border border-[#B9D8CC] bg-white px-4 py-2.5 text-sm font-extrabold text-[#18332D] transition hover:border-[#2FA084] hover:bg-[#F7F1E8] disabled:cursor-not-allowed disabled:opacity-60"
            >
              {isLoadingRoadmaps ? <Loader2 className="animate-spin" size={16} /> : <RefreshCw size={16} />}
              Refresh
            </button>
          </div>
        </div>

        {error && (
          <div className="mb-5 rounded-xl border border-red-200 bg-red-50 px-4 py-3 text-sm font-bold text-red-700">
            {error}
          </div>
        )}

        {success && (
          <div className="mb-5 flex items-center gap-2 rounded-xl border border-[#B9D8CC] bg-[#EAF7F1] px-4 py-3 text-sm font-bold text-[#1F6F5F]">
            <CheckCircle2 size={17} /> {success}
          </div>
        )}

        <div className="grid gap-6 lg:grid-cols-[360px_minmax(0,1fr)]">
          <aside className="rounded-3xl border border-[#B9D8CC]/80 bg-white/95 p-5 shadow-sm">
            <div className="mb-4 flex items-center gap-2 text-sm font-extrabold text-[#18332D]">
              <Map size={17} className="text-[#2FA084]" /> Your published roadmaps
            </div>

            {isLoadingRoadmaps ? (
              <div className="grid place-items-center rounded-2xl border border-dashed border-[#B9D8CC] bg-[#F7F1E8]/70 py-12 text-sm font-bold text-slate-500">
                <Loader2 className="mb-2 animate-spin text-[#2FA084]" size={24} /> Loading roadmaps...
              </div>
            ) : roadmaps.length === 0 ? (
              <EmptyState
                title="No published roadmaps"
                description="Publish a roadmap first. Category configuration is generated from the skills attached to roadmap nodes."
              />
            ) : (
              <div className="space-y-2">
                {roadmaps.map((roadmap) => {
                  const active = selectedRoadmapId === roadmap.roadmapId;

                  return (
                    <button
                      key={roadmap.roadmapId}
                      type="button"
                      onClick={() => handleSelectRoadmap(roadmap.roadmapId)}
                      className={`w-full rounded-2xl border px-4 py-3 text-left transition hover:-translate-y-0.5 ${
                        active
                          ? "border-[#2FA084] bg-[#6FCF97]/20 shadow-sm"
                          : "border-slate-200 bg-white hover:border-[#2FA084] hover:bg-[#EAF7F1]/70"
                      }`}
                    >
                      <p className="text-sm font-black text-[#18332D]">{roadmap.roadmapName || roadmap.title}</p>
                      <p className="mt-1 text-xs font-semibold text-slate-500">
                        {roadmap.careerRoleName || "Career role"} · Version {roadmap.roadmapVersionNumber || roadmap.versionNumber || "—"}
                      </p>
                    </button>
                  );
                })}
              </div>
            )}
          </aside>

          <section className="rounded-3xl border border-[#B9D8CC]/80 bg-white/95 p-5 shadow-sm sm:p-6">
            {!selectedRoadmap ? (
              <EmptyState title="Select a roadmap" description="Category configuration will appear here." />
            ) : isLoadingCategories ? (
              <div className="grid place-items-center rounded-2xl border border-dashed border-[#B9D8CC] bg-[#F7F1E8]/70 py-16 text-sm font-bold text-slate-500">
                <Loader2 className="mb-2 animate-spin text-[#2FA084]" size={24} /> Loading categories...
              </div>
            ) : (
              <>
                <div className="flex flex-col gap-4 border-b border-[#E4ECE8] pb-5 lg:flex-row lg:items-start lg:justify-between">
                  <div>
                    <h2 className="text-2xl font-black text-[#18332D]">
                      {configuration?.roadmapName || selectedRoadmap.roadmapName || selectedRoadmap.title}
                    </h2>
                    <p className="mt-1 text-sm font-semibold text-slate-600">
                      {configuration?.careerRoleName || selectedRoadmap.careerRoleName || "Career role"}
                      {configuration?.authorName ? ` · by ${configuration.authorName}` : ""}
                    </p>
                    <p className="mt-2 text-xs font-bold text-slate-500">
                      {categories.length} categories · {totalSkills} skills
                    </p>
                  </div>

                  <button
                    type="button"
                    onClick={handleSave}
                    disabled={isSaving || categories.length === 0}
                    className="inline-flex items-center justify-center gap-2 rounded-lg bg-[#2FA084] px-5 py-2.5 text-sm font-extrabold text-white shadow-sm transition hover:-translate-y-0.5 hover:bg-[#1F6F5F] disabled:cursor-not-allowed disabled:bg-slate-300 disabled:text-slate-600 disabled:hover:translate-y-0"
                  >
                    {isSaving ? <Loader2 className="animate-spin" size={16} /> : <Save size={16} />}
                    Save order
                  </button>
                </div>

                {categories.length === 0 ? (
                  <div className="mt-6">
                    <EmptyState
                      title="No category configuration"
                      description="The selected published roadmap does not have generated categories yet, or it has no skills with category values."
                    />
                  </div>
                ) : (
                  <div className="mt-6 space-y-3">
                    {categories.map((category, index) => (
                      <article key={category.categoryName} className="rounded-2xl border border-[#B9D8CC]/80 bg-white shadow-sm">
                        <div className="flex flex-col gap-4 p-4 lg:flex-row lg:items-start lg:justify-between">
                          <div className="min-w-0">
                            <div className="flex flex-wrap items-center gap-2">
                              <span className="grid h-8 w-8 place-items-center rounded-full bg-[#2FA084] text-sm font-black text-white">
                                {index + 1}
                              </span>
                              <h3 className="text-base font-black text-[#18332D]">{category.categoryName}</h3>
                              <span className="rounded-full border border-[#B9D8CC] bg-[#EAF7F1] px-3 py-1 text-xs font-extrabold text-[#1F6F5F]">
                                {toArray(category.skills).length} skills
                              </span>
                            </div>

                            <div className="mt-3 flex flex-wrap gap-2">
                              {toArray(category.skills).length === 0 ? (
                                <span className="rounded-full border border-slate-200 bg-slate-50 px-3 py-1.5 text-xs font-bold text-slate-500">
                                  No skills in this category
                                </span>
                              ) : (
                                toArray(category.skills).map((skill) => (
                                  <span key={skill.skillId} className="rounded-full border border-slate-200 bg-slate-50 px-3 py-1.5 text-xs font-bold text-slate-600">
                                    {skill.skillName}
                                  </span>
                                ))
                              )}
                            </div>
                          </div>

                          <div className="flex shrink-0 gap-2">
                            <button
                              type="button"
                              onClick={() => handleMoveCategory(index, -1)}
                              disabled={index === 0 || isSaving}
                              className="inline-flex items-center justify-center gap-1 rounded-lg border border-[#B9D8CC] bg-white px-3 py-2 text-xs font-extrabold text-[#18332D] transition hover:border-[#2FA084] hover:bg-[#F7F1E8] disabled:cursor-not-allowed disabled:opacity-40"
                            >
                              <ArrowUp size={14} /> Up
                            </button>
                            <button
                              type="button"
                              onClick={() => handleMoveCategory(index, 1)}
                              disabled={index === categories.length - 1 || isSaving}
                              className="inline-flex items-center justify-center gap-1 rounded-lg border border-[#B9D8CC] bg-white px-3 py-2 text-xs font-extrabold text-[#18332D] transition hover:border-[#2FA084] hover:bg-[#F7F1E8] disabled:cursor-not-allowed disabled:opacity-40"
                            >
                              <ArrowDown size={14} /> Down
                            </button>
                          </div>
                        </div>
                      </article>
                    ))}
                  </div>
                )}
              </>
            )}
          </section>
        </div>
      </div>
    </main>
  );
}
