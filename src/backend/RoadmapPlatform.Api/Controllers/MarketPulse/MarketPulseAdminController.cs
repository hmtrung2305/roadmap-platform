using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.RateLimiting;
using RoadmapPlatform.Api.Authorization;
using RoadmapPlatform.Api.Constants;
using RoadmapPlatform.Application.Constants;
using RoadmapPlatform.Application.DTOs.MarketPulse;
using RoadmapPlatform.Application.Interfaces.MarketPulse;
using RoadmapPlatform.Infrastructure.Services.MarketPulse;

namespace RoadmapPlatform.Api.Controllers.MarketPulse;

[ApiController]
[Route("api/market-pulse/admin")]
[RequirePermission(PermissionConstant.MARKET_PULSE_MANAGE_ANY)]
public sealed class MarketPulseAdminController(
    IMarketPulseService marketPulseService,
    IMarketPulseAdminService adminService,
    IJobsApiHealthService jobsApiHealthService,
    TopCvJobsApiClient topCvClient) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        var dashboard = await adminService.GetDashboardAsync(cancellationToken);
        return Ok(MarketPulseApiEnvelopeDto<MarketPulseAdminDashboardDto>.Success(dashboard));
    }

    [HttpPost("refresh-operations")]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    public async Task<IActionResult> CreateRefreshOperation(CancellationToken cancellationToken)
    {
        try
        {
            var operation = await adminService.CreateRefreshOperationAsync(cancellationToken);
            return Accepted(MarketPulseApiEnvelopeDto<MarketPulseRefreshOperationDto>.Success(operation));
        }
        catch (MarketPulseRefreshOperationConflictException conflict)
        {
            return Conflict(MarketPulseApiEnvelopeDto<MarketPulseRefreshOperationDto>.Failure(
                "MARKET_PULSE_REFRESH_RUNNING",
                conflict.Message,
                conflict.CurrentOperation));
        }
    }

    [HttpGet("refresh-operations/current")]
    public async Task<IActionResult> GetCurrentRefreshOperation(CancellationToken cancellationToken)
    {
        var operation = await adminService.GetCurrentRefreshOperationAsync(cancellationToken);
        return Ok(MarketPulseApiEnvelopeDto<MarketPulseRefreshOperationDto?>.Success(operation));
    }

    [HttpGet("refresh-operations/{operationId:guid}")]
    public async Task<IActionResult> GetRefreshOperation(
        Guid operationId,
        CancellationToken cancellationToken)
    {
        var operation = await adminService.GetRefreshOperationAsync(operationId, cancellationToken);
        return operation is null
            ? NotFound(MarketPulseApiEnvelopeDto<object>.Failure(
                "MARKET_PULSE_REFRESH_NOT_FOUND",
                "Refresh operation was not found."))
            : Ok(MarketPulseApiEnvelopeDto<MarketPulseRefreshOperationDto>.Success(operation));
    }

    [HttpPost("history-sync")]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    public async Task<IActionResult> SyncHistory(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] MarketPulseHistorySyncRequestDto? request,
        CancellationToken cancellationToken)
    {
        var result = await marketPulseService.SyncPublicationHistoryAsync(
            request,
            cancellationToken);
        return Ok(MarketPulseApiEnvelopeDto<MarketPulseRefreshResultDto>.Success(result));
    }

    [HttpPost("import-latest")]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    public async Task<IActionResult> ImportLatest(
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] MarketPulseRefreshRequestDto? request,
        CancellationToken cancellationToken)
    {
        var result = await marketPulseService.RefreshAsync(request, cancellationToken);
        return Ok(MarketPulseApiEnvelopeDto<MarketPulseRefreshResultDto>.Success(result));
    }

    [HttpGet("import-runs")]
    public Task<IActionResult> GetImportRuns(
        [FromQuery] string? status = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default) =>
        GetCrawlRuns(status, "topcv", from, to, limit, cancellationToken);

    [HttpGet("failures")]
    public async Task<IActionResult> GetFailures(
        [FromQuery] string? status = "open",
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var failures = await adminService.GetFailureGroupsAsync(
            new MarketPulseAdminQueryDto
            {
                Status = status,
                Source = "topcv",
                From = from,
                To = to,
                Limit = limit
            },
            cancellationToken);
        return Ok(MarketPulseApiEnvelopeDto<MarketPulseFailureGroupsDto>.Success(failures));
    }

    [HttpPost("failures/retry")]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    public async Task<IActionResult> RetryFailures(
        [FromBody] MarketPulseBulkActionRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var importFailures = await adminService.RetryFailedItemsAsync(
                request.ResolveImportIds(),
                cancellationToken);
            var crawlerFailures = await topCvClient.UpdateCrawlerFailuresAsync(
                request.ResolveCrawlerIds(),
                "retry",
                cancellationToken);
            return Ok(MarketPulseApiEnvelopeDto<MarketPulseFailureGroupsDto>.Success(new MarketPulseFailureGroupsDto
            {
                CrawlerFailures = crawlerFailures,
                ImportFailures = importFailures
            }));
        }
        catch (InvalidOperationException exception) when (
            exception.Message.Contains("already running", StringComparison.OrdinalIgnoreCase))
        {
            return MarketPulseRefreshRunningResult(exception.Message);
        }
    }

    [HttpPost("failures/ignore")]
    [EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
    public async Task<IActionResult> IgnoreFailures(
        [FromBody] MarketPulseBulkActionRequestDto request,
        CancellationToken cancellationToken)
    {
        var importFailures = await adminService.IgnoreFailedItemsAsync(
            request.ResolveImportIds(),
            cancellationToken);
        var crawlerFailures = await topCvClient.UpdateCrawlerFailuresAsync(
            request.ResolveCrawlerIds(),
            "ignore",
            cancellationToken);
        return Ok(MarketPulseApiEnvelopeDto<MarketPulseFailureGroupsDto>.Success(new MarketPulseFailureGroupsDto
        {
            CrawlerFailures = crawlerFailures,
            ImportFailures = importFailures
        }));
    }

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
        if (!IsSupportedSource(source))
        {
            return UnsupportedSourceResult(source);
        }

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
        if (!IsSupportedSource(source))
        {
            return UnsupportedSourceResult(source);
        }

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
        catch (InvalidOperationException exception) when (
            exception.Message.Contains("already running", StringComparison.OrdinalIgnoreCase))
        {
            return MarketPulseRefreshRunningResult(exception.Message);
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
        catch (InvalidOperationException exception) when (
            exception.Message.Contains("already running", StringComparison.OrdinalIgnoreCase))
        {
            return MarketPulseRefreshRunningResult(exception.Message);
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
            "Market Pulse database objects are out of date. Apply the TopCV-only consolidated migration 039 to the same PostgreSQL database used by the backend, then restart the backend.",
            new
            {
                migrations = new[]
                {
                    "database/migrations/024-market-pulse-admin-ops.sql",
                    "database/migrations/037-market-pulse-option-a-schema-cleanup.sql",
                    "database/migrations/038-market-pulse-expanded-jobs-api.sql",
                    "database/migrations/039-market-pulse-topcv-consolidated.sql"
                },
                hint = "Run the migration against the same DATABASE_URL used by the Render backend.",
                providerMessage = exception.Message
            }))
        {
            StatusCode = StatusCodes.Status503ServiceUnavailable
        };
    }

    private static ObjectResult MarketPulseRefreshRunningResult(string message) => new (
        MarketPulseApiEnvelopeDto<object>.Failure(
            "MARKET_PULSE_REFRESH_RUNNING",
            message))
    {
        StatusCode = StatusCodes.Status409Conflict
    };

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

    private static bool IsSupportedSource(string? source) =>
        string.IsNullOrWhiteSpace(source) ||
        string.Equals(source.Trim(), "topcv", StringComparison.OrdinalIgnoreCase);

    private static BadRequestObjectResult UnsupportedSourceResult(string? source) => new (
        MarketPulseApiEnvelopeDto<object>.Failure(
            "UNSUPPORTED_MARKET_PULSE_SOURCE",
            $"Market Pulse only supports source='topcv'; received '{source}'."));
}
