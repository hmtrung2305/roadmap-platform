using System;
using System.Collections.Generic;

namespace RoadmapPlatform.Infrastructure.Entities;

public partial class Repository
{
    public Guid RepositoryId { get; set; }

    public Guid UserId { get; set; }

    public long GithubRepoId { get; set; }

    public string Name { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string HtmlUrl { get; set; } = null!;

    public string? Description { get; set; }

    public string? PrimaryLanguage { get; set; }

    public int Stars { get; set; }

    public int Forks { get; set; }

    public bool IsPrivate { get; set; }

    public bool IsSelectedForPortfolio { get; set; }

    public DateTime? GithubCreatedAt { get; set; }

    public DateTime? GithubUpdatedAt { get; set; }

    public DateTime SyncedAt { get; set; }

    public virtual ICollection<RepoInsight> RepoInsights { get; set; } = new List<RepoInsight>();

    public virtual User User { get; set; } = null!;
}
