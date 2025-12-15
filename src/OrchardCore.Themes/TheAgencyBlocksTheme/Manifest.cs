using OrchardCore.DisplayManagement.Manifest;
using OrchardCore.Modules.Manifest;

[assembly: Theme(
    Name = "The Agency Blocks Theme",
    Author = ManifestConstants.OrchardCoreTeam,
    Website = ManifestConstants.OrchardCoreWebsite,
    Version = ManifestConstants.OrchardCoreVersion,
    Description = "A theme adapted for agency websites with Blocks editor support.",
    Tags = ["Bootstrap", "Landing page", "Liquid", "Blocks"],
    BaseTheme = "TheAgencyTheme"
)]
