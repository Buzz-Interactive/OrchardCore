namespace OrchardCore.ABTesting.Models;

/// <summary>
/// Represents the type of goal being tracked in an A/B test.
/// </summary>
public enum GoalType
{
    /// <summary>
    /// No goal configured for this test.
    /// </summary>
    None = 0,

    /// <summary>
    /// Track clicks on a button or link identified by CSS selector.
    /// </summary>
    ButtonLinkClick = 1,

    /// <summary>
    /// Track form submissions identified by CSS selector or form ID.
    /// </summary>
    FormSubmission = 2,

    /// <summary>
    /// Track when user scrolls past a certain percentage of the page.
    /// </summary>
    ScrollPercentage = 3,

    /// <summary>
    /// Track a custom JavaScript event.
    /// </summary>
    CustomEvent = 4,

    /// <summary>
    /// Track time spent on page (active time only, pauses when tab is hidden).
    /// </summary>
    TimeOnPage = 5,
}
