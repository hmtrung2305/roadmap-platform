namespace RoadmapPlatform.Application.DTOs.MarketPulse;

public sealed class MarketPulseApiEnvelopeDto<T>
{
    public bool Ok { get; init; }

    public T? Data { get; init; }

    public MarketPulseApiErrorDto? Error { get; init; }

    public static MarketPulseApiEnvelopeDto<T> Success(T data) => new()
    {
        Ok = true,
        Data = data,
        Error = null
    };

    public static MarketPulseApiEnvelopeDto<T> Failure(
        string code,
        string message,
        object? details = null) => new()
    {
        Ok = false,
        Data = default,
        Error = new MarketPulseApiErrorDto
        {
            Code = code,
            Message = message,
            Details = details
        }
    };
}

public sealed class MarketPulseApiErrorDto
{
    public string Code { get; init; } = "MARKET_PULSE_ERROR";

    public string Message { get; init; } = string.Empty;

    public object? Details { get; init; }
}
