using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.RateLimiting;
using RoadmapPlatform.Api.Authorization;
using RoadmapPlatform.Api.Constants;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.DTOs.MarketPulse;
using RoadmapPlatform.Application.Interfaces.MarketPulse;

namespace RoadmapPlatform.Api.Controllers.MarketPulse;

[ApiController]
[Route("api/market-pulse/admin")]
[RequirePermission(PermissionConstant.MARKET_PULSE_MANAGE_ANY)]
public sealed class MarketPulseAdminController(
    IMarketPulseService marketPulseService,
    IMarketPulseAdminService adminService,
    IJobsApiHealthService jobsApiHealthService) : ControllerBase
{
    [HttpPost("refresh")]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    [ProducesResponseType(typeof(MarketPulseApiEnvelopeDto<MarketPulseRefreshResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Refresh(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] MarketPulseRefreshRequestDto? request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await marketPulseService.RefreshAsync(request, cancellationToken);
            return Ok(MarketPulseApiEnvelopeDto<MarketPulseRefreshResultDto>.Success(result));
        }
        catch (InvalidOperationException ex) when (
            ex.Message.Contains("already running", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCode(
                StatusCodes.Status409Conflict,
                MarketPulseApiEnvelopeDto<object>.Failure(
                    "MARKET_PULSE_REFRESH_RUNNING",
                    ex.Message));
        }
        catch (Exception ex) when (IsMarketPulseAdminSchemaMissing(ex))
        {
            return MarketPulseAdminSchemaMissingResult(ex);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                MarketPulseApiEnvelopeDto<object>.Failure(
                    "MARKET_PULSE_REFRESH_FAILED",
                    "Manual refresh failed. Check crawl run history and failed items for details.",
                    new { message = ex.Message }));
        }
    }

    [HttpGet("crawl-runs")]
    [ProducesResponseType(typeof(MarketPulseApiEnvelopeDto<IReadOnlyList<MarketPulseCrawlRunDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCrawlRuns(
        [FromQuery] string? status = null,
        [FromQuery] string? source = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var runs = await adminService.GetCrawlRunsAsync(
                new MarketPulseAdminQueryDto
                {
                    Status = status,
                    Source = source,
                    From = from,
                    To = to,
                    Limit = limit
                },
                cancellationToken);

            return Ok(MarketPulseApiEnvelopeDto<IReadOnlyList<MarketPulseCrawlRunDto>>.Success(runs));
        }
        catch (Exception ex) when (IsMarketPulseAdminSchemaMissing(ex))
        {
            return MarketPulseAdminSchemaMissingResult(ex);
        }
    }

    [HttpGet("failed-items")]
    [ProducesResponseType(typeof(MarketPulseApiEnvelopeDto<IReadOnlyList<MarketPulseFailedItemDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFailedItems(
        [FromQuery] string? status = null,
        [FromQuery] string? source = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var items = await adminService.GetFailedItemsAsync(
                new MarketPulseAdminQueryDto
                {
                    Status = status,
                    Source = source,
                    From = from,
                    To = to,
                    Limit = limit
                },
                cancellationToken);

            return Ok(MarketPulseApiEnvelopeDto<IReadOnlyList<MarketPulseFailedItemDto>>.Success(items));
        }
        catch (Exception ex) when (IsMarketPulseAdminSchemaMissing(ex))
        {
            return MarketPulseAdminSchemaMissingResult(ex);
        }
    }

    [HttpPost("failed-items/{failedItemId:guid}/retry")]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    public async Task<IActionResult> RetryFailedItem(
        Guid failedItemId,
        CancellationToken cancellationToken)
    {
        try
        {
            var items = await adminService.RetryFailedItemsAsync([failedItemId], cancellationToken);
            return Ok(MarketPulseApiEnvelopeDto<IReadOnlyList<MarketPulseFailedItemDto>>.Success(items));
        }
        catch (Exception ex) when (IsMarketPulseAdminSchemaMissing(ex))
        {
            return MarketPulseAdminSchemaMissingResult(ex);
        }
    }

    [HttpPost("failed-items/retry")]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    public async Task<IActionResult> RetryFailedItems(
        [FromBody] MarketPulseBulkActionRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var items = await adminService.RetryFailedItemsAsync(request.FailedItemIds, cancellationToken);
            return Ok(MarketPulseApiEnvelopeDto<IReadOnlyList<MarketPulseFailedItemDto>>.Success(items));
        }
        catch (Exception ex) when (IsMarketPulseAdminSchemaMissing(ex))
        {
            return MarketPulseAdminSchemaMissingResult(ex);
        }
    }

    [HttpPost("failed-items/{failedItemId:guid}/ignore")]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    public async Task<IActionResult> IgnoreFailedItem(
        Guid failedItemId,
        CancellationToken cancellationToken)
    {
        try
        {
            var items = await adminService.IgnoreFailedItemsAsync([failedItemId], cancellationToken);
            return Ok(MarketPulseApiEnvelopeDto<IReadOnlyList<MarketPulseFailedItemDto>>.Success(items));
        }
        catch (Exception ex) when (IsMarketPulseAdminSchemaMissing(ex))
        {
            return MarketPulseAdminSchemaMissingResult(ex);
        }
    }

    [HttpPost("failed-items/ignore")]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    public async Task<IActionResult> IgnoreFailedItems(
        [FromBody] MarketPulseBulkActionRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var items = await adminService.IgnoreFailedItemsAsync(request.FailedItemIds, cancellationToken);
            return Ok(MarketPulseApiEnvelopeDto<IReadOnlyList<MarketPulseFailedItemDto>>.Success(items));
        }
        catch (Exception ex) when (IsMarketPulseAdminSchemaMissing(ex))
        {
            return MarketPulseAdminSchemaMissingResult(ex);
        }
    }

    [HttpGet("classifier/categories")]
    public async Task<IActionResult> GetClassifierCategories(CancellationToken cancellationToken)
    {
        try
        {
            var categories = await adminService.GetCategoriesAsync(cancellationToken);
            return Ok(MarketPulseApiEnvelopeDto<IReadOnlyList<string>>.Success(categories));
        }
        catch (Exception ex) when (IsMarketPulseAdminSchemaMissing(ex))
        {
            return MarketPulseAdminSchemaMissingResult(ex);
        }
    }

    [HttpGet("classifier/mappings")]
    public async Task<IActionResult> GetClassifierMappings(CancellationToken cancellationToken)
    {
        try
        {
            var mappings = await adminService.GetClassifierMappingsAsync(cancellationToken);
            return Ok(MarketPulseApiEnvelopeDto<IReadOnlyList<MarketPulseClassifierMappingDto>>.Success(mappings));
        }
        catch (Exception ex) when (IsMarketPulseAdminSchemaMissing(ex))
        {
            return MarketPulseAdminSchemaMissingResult(ex);
        }
    }

    [HttpPost("classifier/mappings")]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    public async Task<IActionResult> CreateClassifierMapping(
        [FromBody] MarketPulseClassifierMappingRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var mapping = await adminService.CreateClassifierMappingAsync(request, cancellationToken);
            return Ok(MarketPulseApiEnvelopeDto<MarketPulseClassifierMappingDto>.Success(mapping));
        }
        catch (Exception ex) when (IsMarketPulseAdminSchemaMissing(ex))
        {
            return MarketPulseAdminSchemaMissingResult(ex);
        }
    }

    [HttpPut("classifier/mappings/{mappingId:guid}")]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    public async Task<IActionResult> UpdateClassifierMapping(
        Guid mappingId,
        [FromBody] MarketPulseClassifierMappingRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var mapping = await adminService.UpdateClassifierMappingAsync(mappingId, request, cancellationToken);
            return Ok(MarketPulseApiEnvelopeDto<MarketPulseClassifierMappingDto>.Success(mapping));
        }
        catch (Exception ex) when (IsMarketPulseAdminSchemaMissing(ex))
        {
            return MarketPulseAdminSchemaMissingResult(ex);
        }
    }

    [HttpDelete("classifier/mappings/{mappingId:guid}")]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    public async Task<IActionResult> DeleteClassifierMapping(
        Guid mappingId,
        CancellationToken cancellationToken)
    {
        try
        {
            await adminService.DeleteClassifierMappingAsync(mappingId, cancellationToken);
            return Ok(MarketPulseApiEnvelopeDto<object>.Success(new { deleted = true }));
        }
        catch (Exception ex) when (IsMarketPulseAdminSchemaMissing(ex))
        {
            return MarketPulseAdminSchemaMissingResult(ex);
        }
    }

    [HttpPost("classifier/test")]
    public async Task<IActionResult> TestClassifier(
        [FromBody] MarketPulseClassifierTestRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await adminService.TestClassifierAsync(request, cancellationToken);
            return Ok(MarketPulseApiEnvelopeDto<MarketPulseClassifierTestResultDto>.Success(result));
        }
        catch (Exception ex) when (IsMarketPulseAdminSchemaMissing(ex))
        {
            return MarketPulseAdminSchemaMissingResult(ex);
        }
    }

    [HttpGet("source-health")]
    public async Task<IActionResult> GetSourceHealth(CancellationToken cancellationToken)
    {
        try
        {
            var health = await adminService.GetSourceHealthAsync(cancellationToken);
            return Ok(MarketPulseApiEnvelopeDto<IReadOnlyList<MarketPulseSourceHealthDto>>.Success(health));
        }
        catch (Exception ex) when (IsMarketPulseAdminSchemaMissing(ex))
        {
            return MarketPulseAdminSchemaMissingResult(ex);
        }
    }

    [HttpGet("external-source-health")]
    [ProducesResponseType(
        typeof(MarketPulseApiEnvelopeDto<MarketPulseExternalSourceHealthDto>),
        StatusCodes.Status200OK)]
    public async Task<IActionResult> GetExternalSourceHealth(CancellationToken cancellationToken)
    {
        var health = await jobsApiHealthService.GetHealthAsync(cancellationToken);
        return Ok(MarketPulseApiEnvelopeDto<MarketPulseExternalSourceHealthDto>.Success(health));
    }

    private static ObjectResult MarketPulseAdminSchemaMissingResult(Exception exception)
    {
        return new ObjectResult(MarketPulseApiEnvelopeDto<object>.Failure(
            "MARKET_PULSE_ADMIN_SCHEMA_MISSING",
            "Market Pulse database objects or legacy data are out of date. Apply migrations 024 and 037 through 040 to the same PostgreSQL database used by the backend, then restart the backend.",
            new
            {
                migrations = new[]
                {
                    "database/migrations/024-market-pulse-admin-ops.sql",
                    "database/migrations/037-market-pulse-option-a-schema-cleanup.sql",
                    "database/migrations/038-market-pulse-expanded-jobs-api.sql",
                    "database/migrations/039-market-pulse-relative-date-observations.sql",
                    "database/migrations/040-market-pulse-post-date-confidence-integrity.sql"
                },
                hint = "Run the migration against the same DATABASE_URL used by the Render backend.",
                providerMessage = exception.Message
            }))
        {
            StatusCode = StatusCodes.Status503ServiceUnavailable
        };
    }

    private static bool IsMarketPulseAdminSchemaMissing(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            var typeName = current.GetType().FullName;
            var sqlState = current.GetType().GetProperty("SqlState")?.GetValue(current)?.ToString();

            if (typeName == "Npgsql.PostgresException" &&
                (sqlState == "42P01" || sqlState == "42703" || sqlState == "42P07"))
            {
                return true;
            }

            if (current.Message.Contains(
                "Column 'post_date_confidence' is null",
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
