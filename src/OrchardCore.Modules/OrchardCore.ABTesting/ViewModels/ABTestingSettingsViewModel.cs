using System.ComponentModel.DataAnnotations;

namespace OrchardCore.ABTesting.ViewModels;

/// <summary>
/// View model for A/B Testing settings form.
/// </summary>
public class ABTestingSettingsViewModel
{
    /// <summary>
    /// Minimum number of impressions required per variant before statistical analysis is performed.
    /// </summary>
    [Required]
    [Range(30, 500, ErrorMessage = "Minimum sample size must be between 30 and 500.")]
    public int MinimumSampleSize { get; set; } = 50;

    /// <summary>
    /// The confidence level required to declare a winner (90, 95, or 99).
    /// </summary>
    [Required]
    public int ConfidenceThreshold { get; set; } = 90;
}
