namespace RoadmapPlatform.Infrastructure.Services.MarketPulse;

public static class MarketPulseBusinessTime
{
    public const string DefaultTimezone = "Asia/Ho_Chi_Minh";

    public static DateOnly GetBusinessDate(
        DateTimeOffset timestamp,
        string? timezoneId = DefaultTimezone)
    {
        var timezone = ResolveTimezone(timezoneId);
        var localTimestamp = TimeZoneInfo.ConvertTime(timestamp.ToUniversalTime(), timezone);
        return DateOnly.FromDateTime(localTimestamp.DateTime);
    }

    public static DateOnly GetBusinessDate(
        DateTime timestamp,
        string? timezoneId = DefaultTimezone)
    {
        var utcTimestamp = timestamp.Kind switch
        {
            DateTimeKind.Utc => timestamp,
            DateTimeKind.Local => timestamp.ToUniversalTime(),
            _ => DateTime.SpecifyKind(timestamp, DateTimeKind.Utc)
        };
        return GetBusinessDate(new DateTimeOffset(utcTimestamp), timezoneId);
    }

    public static (DateTime StartUtc, DateTime EndUtc) GetBusinessDayUtcRange(
        DateOnly businessDate,
        string? timezoneId = DefaultTimezone)
    {
        var timezone = ResolveTimezone(timezoneId);
        var localStart = DateTime.SpecifyKind(
            businessDate.ToDateTime(TimeOnly.MinValue),
            DateTimeKind.Unspecified);
        var localEnd = localStart.AddDays(1);

        return (
            TimeZoneInfo.ConvertTimeToUtc(localStart, timezone),
            TimeZoneInfo.ConvertTimeToUtc(localEnd, timezone));
    }

    public static bool IsReliablePostDate(string? confidence) =>
        string.Equals(confidence, "exact", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(confidence, "relative", StringComparison.OrdinalIgnoreCase);

    public static string NormalizePostDateConfidence(string? confidence)
    {
        var normalized = confidence?.Trim().ToLowerInvariant();
        return normalized is "exact" or "relative" or "unknown"
            ? normalized
            : "unknown";
    }

    private static TimeZoneInfo ResolveTimezone(string? timezoneId)
    {
        var configuredId = string.IsNullOrWhiteSpace(timezoneId)
            ? DefaultTimezone
            : timezoneId.Trim();

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(configuredId);
        }
        catch (TimeZoneNotFoundException) when (
            string.Equals(configuredId, DefaultTimezone, StringComparison.OrdinalIgnoreCase))
        {
            return TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        }
        catch (InvalidTimeZoneException exception)
        {
            throw new InvalidOperationException(
                $"Market Pulse business timezone '{configuredId}' is invalid.",
                exception);
        }
        catch (TimeZoneNotFoundException exception)
        {
            throw new InvalidOperationException(
                $"Market Pulse business timezone '{configuredId}' was not found.",
                exception);
        }
    }
}
