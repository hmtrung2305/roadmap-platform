export function getRepositoryId(repo) {
  return repo?.repositoryId || repo?.id || repo?.repoId || repo?.repoName || repo?.name;
}

export function getRepoName(repo) {
  return repo?.repoName || repo?.name || repo?.repositoryName || "Untitled repository";
}

export function getRepoOwner(repo, username) {
  return repo?.owner || repo?.fullName?.split("/")[0] || username || "github";
}

export function getRepoDescription(repo) {
  return (
    repo?.summary ||
    repo?.description ||
    repo?.objective ||
    "Add a short summary of what this project proves."
  );
}

export function getInitiallySelectedIds(repositories) {
  return repositories
    .filter((repo) => repo?.isSelectedForPortfolio || repo?.isSelected || repo?.isFeatured)
    .map(getRepositoryId)
    .filter(Boolean);
}

export function getPortfolioBio(portfolio) {
  return (
    portfolio?.bio ||
    portfolio?.about ||
    portfolio?.summary ||
    portfolio?.profile?.about ||
    "No bio yet. Update your profile details to show a short introduction here."
  );
}
