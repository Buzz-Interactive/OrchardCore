using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "A/B Testing",
    Author = ManifestConstants.OrchardCoreTeam,
    Website = ManifestConstants.OrchardCoreWebsite,
    Version = ManifestConstants.OrchardCoreVersion
)]

[assembly: Feature(
    Id = "OrchardCore.ABTesting",
    Name = "A/B Testing",
    Description = "Create A/B tests to compare content variants and measure their effectiveness.",
    Dependencies = ["OrchardCore.ContentTypes", "OrchardCore.Contents", "OrchardCore.ContentFields"],
    Category = "Content Management"
)]

[assembly: Feature(
    Id = "OrchardCore.ABTesting.Workflows",
    Name = "A/B Testing Workflows",
    Description = "Provides workflow activities for A/B testing, including events for winner detection and tasks for declaring winners.",
    Dependencies = ["OrchardCore.ABTesting", "OrchardCore.Workflows"],
    Category = "Content Management"
)]
