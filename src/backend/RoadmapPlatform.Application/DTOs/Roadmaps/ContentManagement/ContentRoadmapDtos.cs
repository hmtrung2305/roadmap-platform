using System.Text.Json;
using RoadmapPlatform.Application.DTOs.Roadmaps;

namespace RoadmapPlatform.Application.DTOs.Roadmaps.ContentManagement;

public sealed class ContentRoadmapListQueryDto
{
    public string? Status { get; set; }
    public string? Search { get; set; }
    public string? Sort { get; set; } = "updated_desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class ContentRoadmapListResultDto
{
    public List<ContentRoadmapSummaryDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
    public ContentRoadmapStatusCountsDto StatusCounts { get; set; } = new();
}

public sealed class ContentRoadmapStatusCountsDto
{
    public int Draft { get; set; }
    public int PendingReview { get; set; }
    public int ChangesRequested { get; set; }
    public int Published { get; set; }
    public int Archived { get; set; }
}

public sealed class ContentRoadmapSummaryDto
{
    public Guid RoadmapId { get; set; }
    public Guid RoadmapVersionId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Visibility { get; set; } = string.Empty;
    public int VersionNumber { get; set; }
    public int MajorVersion { get; set; }
    public int MinorVersion { get; set; }
    public int PatchVersion { get; set; }
    public string VersionLabel { get; set; } = string.Empty;
    public string ReleaseType { get; set; } = string.Empty;
    public Guid? CreatedFromVersionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? EstimatedTotalHours { get; set; }
    public int NodeCount { get; set; }
    public int TrackableNodeCount { get; set; }
    public int ResourceMappingCount { get; set; }
    public int SkillMappingCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public CareerRoleDto CareerRole { get; set; } = new();
    public ContentRoadmapReviewEventDto? LatestReviewEvent { get; set; }
    public List<ContentRoadmapReviewEventDto> ReviewEvents { get; set; } = [];
}

public sealed class ContentRoadmapDetailDto
{
    public Guid RoadmapId { get; set; }
    public Guid RoadmapVersionId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Visibility { get; set; } = string.Empty;
    public int VersionNumber { get; set; }
    public int MajorVersion { get; set; }
    public int MinorVersion { get; set; }
    public int PatchVersion { get; set; }
    public string VersionLabel { get; set; } = string.Empty;
    public string ReleaseType { get; set; } = string.Empty;
    public Guid? CreatedFromVersionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? EstimatedTotalHours { get; set; }
    public string LayoutDirection { get; set; } = string.Empty;
    public string? LayoutAlgorithm { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
    public CareerRoleDto CareerRole { get; set; } = new();
    public List<ContentRoadmapVersionSummaryDto> Versions { get; set; } = [];
    public List<ContentRoadmapNodeDto> Nodes { get; set; } = [];
    public List<ContentRoadmapEdgeDto> Edges { get; set; } = [];
    public int NodeCount { get; set; }
    public int TrackableNodeCount { get; set; }
    public int ResourceMappingCount { get; set; }
    public int SkillMappingCount { get; set; }
    public ContentRoadmapReviewEventDto? LatestReviewEvent { get; set; }
    public List<ContentRoadmapReviewEventDto> ReviewEvents { get; set; } = [];
}

public sealed class ContentRoadmapReviewEventDto
{
    public Guid RoadmapVersionReviewEventId { get; set; }
    public Guid RoadmapVersionId { get; set; }
    public Guid? ActorUserId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public sealed class ContentRoadmapVersionSummaryDto
{
    public Guid RoadmapVersionId { get; set; }
    public int VersionNumber { get; set; }
    public int MajorVersion { get; set; }
    public int MinorVersion { get; set; }
    public int PatchVersion { get; set; }
    public string VersionLabel { get; set; } = string.Empty;
    public string ReleaseType { get; set; } = string.Empty;
    public Guid? CreatedFromVersionId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
}

public sealed class ContentRoadmapNodeDto
{
    public Guid RoadmapNodeId { get; set; }
    public Guid? ParentNodeId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string NodeType { get; set; } = string.Empty;
    public string? CheckpointType { get; set; }
    public string? SelectionType { get; set; }
    public int? RequiredCount { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int OrderIndex { get; set; }
    public string LayoutRole { get; set; } = string.Empty;
    public int? EstimatedHours { get; set; }
    public string? DifficultyLevel { get; set; }
    public bool IsRequired { get; set; }
    public bool IsTrackable { get; set; }
    public JsonElement? Metadata { get; set; }
    public List<string> LearningOutcomes { get; set; } = [];
    public List<string> CompletionCriteria { get; set; } = [];
    public List<SkillDto> Skills { get; set; } = [];
    public List<LearningResourceDto> Resources { get; set; } = [];
}


public sealed class ContentRoadmapEdgeDto
{
    public Guid RoadmapEdgeId { get; set; }
    public Guid RoadmapVersionId { get; set; }
    public Guid FromNodeId { get; set; }
    public Guid ToNodeId { get; set; }
    public string EdgeType { get; set; } = string.Empty;
    public string DependencyType { get; set; } = string.Empty;
    public string? Condition { get; set; }
}

public sealed class CreateRoadmapRequestDto
{
    public Guid CareerRoleId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? EstimatedTotalHours { get; set; }
}

public sealed class UpdateRoadmapVersionMetadataRequestDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? EstimatedTotalHours { get; set; }
}

public sealed class UpdateRoadmapNodeMetadataRequestDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? EstimatedHours { get; set; }
    public string? DifficultyLevel { get; set; }
    public List<string>? LearningOutcomes { get; set; }
    public List<string>? CompletionCriteria { get; set; }
    public UpdateRoadmapNodeGuideRequestDto? Guide { get; set; }
}

public sealed class UpdateRoadmapNodeGuideRequestDto
{
    public string? Focus { get; set; }
    public List<string>? Practice { get; set; }
    public string? WhatToBuild { get; set; }
    public List<string>? BuildSteps { get; set; }
    public string? ReviewFocus { get; set; }
    public List<string>? ReviewCriteria { get; set; }
    public string? WhenToChoose { get; set; }
    public string? ChoiceGuidance { get; set; }
    public List<string>? SelectionNotes { get; set; }
    public string? PhaseFocus { get; set; }
    public List<string>? Milestones { get; set; }
    public string? Purpose { get; set; }
}


public sealed class CloneRoadmapVersionDraftRequestDto
{
    public string? Title { get; set; }
}

public sealed class SubmitRoadmapVersionReviewRequestDto
{
    public string ChangeLog { get; set; } = string.Empty;
}

public sealed class RejectRoadmapVersionReviewRequestDto
{
    public string Reason { get; set; } = string.Empty;
}

public sealed class CreateRoadmapNodeRequestDto
{
    public string NodeType { get; set; } = string.Empty;
    public Guid? ParentNodeId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? EstimatedHours { get; set; }
    public string? DifficultyLevel { get; set; }
    public string? CheckpointType { get; set; }
    public string? LayoutRole { get; set; }
    public bool? IsRequired { get; set; }
    public string? Position { get; set; }
    public Guid? ReferenceNodeId { get; set; }
}

public sealed class MoveRoadmapNodeRequestDto
{
    public string Direction { get; set; } = string.Empty;
}


public sealed class UpdateRoadmapNodeGroupRuleRequestDto
{
    public string SelectionType { get; set; } = string.Empty;
    public int? RequiredCount { get; set; }
}

public sealed class UpdateRoadmapNodeRequirementRequestDto
{
    public bool IsRequired { get; set; }
}

public sealed class ContentRoadmapStructureMutationResultDto
{
    public ContentRoadmapDetailDto Roadmap { get; set; } = new();
    public Guid? FocusNodeId { get; set; }
}

public sealed class ContentRoadmapValidationResultDto
{
    public bool IsValid { get; set; }
    public List<ContentRoadmapValidationItemDto> Errors { get; set; } = [];
    public List<ContentRoadmapValidationItemDto> Warnings { get; set; } = [];
}

public sealed class ContentRoadmapValidationItemDto
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public Guid? RoadmapNodeId { get; set; }
    public string? NodeTitle { get; set; }
}

public sealed class AddRoadmapNodeResourceRequestDto
{
    public Guid LearningResourceId { get; set; }
}

public sealed class AddRoadmapNodeSkillRequestDto
{
    public Guid SkillId { get; set; }
}

public sealed class ContentLearningResourceSearchResultDto
{
    public Guid ResourceId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ResourceType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Provider { get; set; }
    public string? DifficultyLevel { get; set; }
    public string LanguageCode { get; set; } = string.Empty;
    public string VerificationStatus { get; set; } = string.Empty;
}
