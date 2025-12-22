using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrchardCore.ABTesting.Models;
using OrchardCore.ABTesting.Services;
using OrchardCore.ABTesting.ViewModels;

namespace OrchardCore.ABTesting.Controllers;

/// <summary>
/// API controller for A/B testing operations.
/// </summary>
[Route("api/abtest")]
[ApiController]
[IgnoreAntiforgeryToken]
[AllowAnonymous]
public class ABTestApiController : ControllerBase
{
    private readonly ITrackingService _trackingService;
    private readonly IABTestLookupService _lookupService;
    private readonly IBotDetectionService _botDetectionService;

    public ABTestApiController(
        ITrackingService trackingService,
        IABTestLookupService lookupService,
        IBotDetectionService botDetectionService)
    {
        _trackingService = trackingService;
        _lookupService = lookupService;
        _botDetectionService = botDetectionService;
    }

    /// <summary>
    /// Records an impression for an A/B test variant.
    /// </summary>
    /// <param name="model">The impression data.</param>
    /// <returns>OK if successful, NotFound if test doesn't exist.</returns>
    [HttpPost]
    [Route("impression")]
    public async Task<IActionResult> RecordImpression([FromBody] ImpressionModel model)
    {
        if (model == null || string.IsNullOrEmpty(model.TestId) || string.IsNullOrEmpty(model.Variant))
        {
            return BadRequest();
        }

        // Skip tracking for bot traffic
        var userAgent = Request.Headers.UserAgent.ToString();
        if (_botDetectionService.IsBot(userAgent))
        {
            return Ok();
        }

        // Validate the test exists and is active
        var testInfo = await _lookupService.GetTestByIdAsync(model.TestId);
        if (testInfo == null)
        {
            return NotFound();
        }

        // Parse variant
        if (!Enum.TryParse<ABVariant>(model.Variant, ignoreCase: true, out var variant))
        {
            return BadRequest("Invalid variant. Must be 'A' or 'B'.");
        }

        // Record the impression
        await _trackingService.RecordImpressionAsync(model.TestId, variant);

        return Ok();
    }

    /// <summary>
    /// Records a goal conversion for an A/B test variant.
    /// </summary>
    /// <param name="model">The conversion data.</param>
    /// <returns>OK if successful, NotFound if test doesn't exist.</returns>
    [HttpPost]
    [Route("conversion")]
    public async Task<IActionResult> RecordConversion([FromBody] ConversionModel model)
    {
        if (model == null || string.IsNullOrEmpty(model.TestId) || string.IsNullOrEmpty(model.Variant))
        {
            return BadRequest();
        }

        // Skip tracking for bot traffic
        var userAgent = Request.Headers.UserAgent.ToString();
        if (_botDetectionService.IsBot(userAgent))
        {
            return Ok();
        }

        // Validate the test exists
        var testInfo = await _lookupService.GetTestByIdAsync(model.TestId);
        if (testInfo == null)
        {
            return NotFound();
        }

        // Parse variant
        if (!Enum.TryParse<ABVariant>(model.Variant, ignoreCase: true, out var variant))
        {
            return BadRequest("Invalid variant. Must be 'A' or 'B'.");
        }

        // Record the conversion
        await _trackingService.RecordConversionAsync(model.TestId, variant);

        return Ok();
    }
}
