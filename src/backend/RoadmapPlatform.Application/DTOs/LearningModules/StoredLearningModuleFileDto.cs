namespace RoadmapPlatform.Application.DTOs.LearningModules;

public sealed record StoredLearningModuleFileDto(
    string ObjectPath,
    string? Url,
    long SizeBytes,
    string? ContentHash);
