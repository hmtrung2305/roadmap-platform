using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using RoadmapPlatform.Api.Constants;
using RoadmapPlatform.Application.DTOs.MarketPulse;
using RoadmapPlatform.Application.Interfaces.MarketPulse;
using RoadmapPlatform.Infrastructure.Configurations;

namespace RoadmapPlatform.Api.Controllers.MarketPulse;

/// <summary>
/// Receives authenticated machine-to-machine Market Pulse refresh operations.
/// </summary>
[ApiController]
[AllowAnonymous]
[EnableRateLimiting(RateLimitPolicyNames.AdminMutation)]
[Route("api/internal/market-pulse")]
public sealed class InternalMarketPulseController(
    IMarketPulseService marketPulseService,
    IOptions<MarketPulseSettings> options) : ControllerBase
{
    private const string ApiKeyHeaderName = "X-Market-Pulse-Key";

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(MarketPulseRefreshResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        if (!IsAuthorized())
        {
            return Unauthorized();
        }

        var result = await marketPulseService.RefreshAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPost("ingest")]
    [ProducesResponseType(typeof(MarketPulseRefreshResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Ingest(
        [FromBody] MarketPulseIngestRequestDto? request,
        CancellationToken cancellationToken)
    {
        if (!IsAuthorized())
        {
            return Unauthorized();
        }

        if (request is null || request.Postings.Count == 0)
        {
            return BadRequest("At least one posting is required.");
        }

        try
        {
            var result = await marketPulseService.IngestAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(MarketPulseApiEnvelopeDto<object>.Failure(
                "UNSUPPORTED_MARKET_PULSE_SOURCE",
                exception.Message));
        }
    }

    private bool IsAuthorized()
    {
        var configuredKey = options.Value.InternalApiKey;
        if (string.IsNullOrWhiteSpace(configuredKey) || configuredKey.Trim().Length < 16)
        {
            return false;
        }

        if (!Request.Headers.TryGetValue(ApiKeyHeaderName, out var providedValues))
        {
            return false;
        }

        var providedKey = providedValues.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(providedKey))
        {
            return false;
        }

        return FixedTimeEquals(configuredKey.Trim(), providedKey.Trim());
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        var leftBytes = Encoding.UTF8.GetBytes(left);
        var rightBytes = Encoding.UTF8.GetBytes(right);
        return leftBytes.Length == rightBytes.Length &&
            CryptographicOperations.FixedTimeEquals(leftBytes, rightBytes);
    }
}
