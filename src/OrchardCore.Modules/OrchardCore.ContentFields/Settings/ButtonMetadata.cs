namespace OrchardCore.ContentFields.Settings;

/// <summary>
/// Metadata for a toolbar button type.
/// </summary>
public record ButtonMetadata(
    string Type,
    string DisplayName,
    string Icon,
    string Category,
    bool RequiresValue,
    string[] AllowedValues = null
);
