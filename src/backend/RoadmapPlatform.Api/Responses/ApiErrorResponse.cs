namespace RoadmapPlatform.Api.Responses;

public sealed class ApiErrorResponse
{
    public required string Code { get; init; }

    public required string Message { get; init; }

    public required int Status { get; init; }

    public object? Details { get; init; }

    public IReadOnlyDictionary<string, string[]>? Errors { get; init; }

    public int? RetryAfterSeconds { get; init; }

    public object? CreditStatus { get; init; }

    public string? TraceId { get; init; }
}
