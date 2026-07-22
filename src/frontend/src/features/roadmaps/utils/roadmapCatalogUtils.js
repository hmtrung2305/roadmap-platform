export function matchesRoadmapCatalogSearch(roadmap, query) {
  const searchValue = String(query || "").trim().toLowerCase();

  if (!searchValue) return true;

  return [roadmap?.title, roadmap?.careerRole?.name]
    .filter(Boolean)
    .some((value) => String(value).toLowerCase().includes(searchValue));
}

export function buildRoadmapCatalogSearchParams(currentParams, value) {
  const nextParams = new URLSearchParams(currentParams);
  const inputValue = String(value ?? "");

  if (inputValue.trim()) {
    nextParams.set("q", inputValue);
    nextParams.delete("role");
  } else {
    nextParams.delete("q");
    nextParams.delete("role");
  }

  return nextParams;
}
