namespace RoadmapPlatform.Application.DTOs.Storage;

public sealed record StoredFileDto(
    string ObjectPath,
    string? Url,
    long SizeBytes,
    string? ContentHash);
