namespace RoadmapPlatform.Application.DTOs.LearningModules;

public sealed class LearningModuleUploadedFileDto
{
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = "text/markdown";
    public long Length { get; init; }
    public Stream Content { get; init; } = Stream.Null;
}
