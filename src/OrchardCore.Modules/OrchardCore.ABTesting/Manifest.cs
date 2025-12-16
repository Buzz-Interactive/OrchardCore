using OrchardCore.Modules.Manifest;

[assembly: Module(
    Name = "A/B Testing",
    Author = ManifestConstants.OrchardCoreTeam,
    Website = ManifestConstants.OrchardCoreWebsite,
    Version = ManifestConstants.OrchardCoreVersion,
    Description = "Create A/B tests to compare content variants and measure their effectiveness.",
    Dependencies = ["OrchardCore.ContentTypes", "OrchardCore.Contents", "OrchardCore.ContentFields"],
    Category = "Content Management"
)]
