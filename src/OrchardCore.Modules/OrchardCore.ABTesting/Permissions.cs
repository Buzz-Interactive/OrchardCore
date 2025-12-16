using OrchardCore.Security.Permissions;

namespace OrchardCore.ABTesting;

public sealed class Permissions : IPermissionProvider
{
    public static readonly Permission ManageABTests = new("ManageABTests", "Manage A/B Tests");

    private readonly IEnumerable<Permission> _allPermissions =
    [
        ManageABTests,
    ];

    public Task<IEnumerable<Permission>> GetPermissionsAsync()
        => Task.FromResult(_allPermissions);

    public IEnumerable<PermissionStereotype> GetDefaultStereotypes() =>
    [
        new PermissionStereotype
        {
            Name = OrchardCoreConstants.Roles.Administrator,
            Permissions = _allPermissions,
        },
        new PermissionStereotype
        {
            Name = OrchardCoreConstants.Roles.Editor,
            Permissions = _allPermissions,
        },
    ];
}
