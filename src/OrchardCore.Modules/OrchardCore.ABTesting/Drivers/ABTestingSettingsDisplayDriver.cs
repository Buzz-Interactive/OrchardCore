using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using OrchardCore.ABTesting.Settings;
using OrchardCore.ABTesting.ViewModels;
using OrchardCore.DisplayManagement.Entities;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Settings;

namespace OrchardCore.ABTesting.Drivers;

public sealed class ABTestingSettingsDisplayDriver : SiteDisplayDriver<ABTestingSettings>
{
    protected override string SettingsGroupId
        => ABTestingSettingsGroup.Id;

    private readonly IAuthorizationService _authorizationService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ABTestingSettingsDisplayDriver(
        IAuthorizationService authorizationService,
        IHttpContextAccessor httpContextAccessor)
    {
        _authorizationService = authorizationService;
        _httpContextAccessor = httpContextAccessor;
    }

    public override async Task<IDisplayResult> EditAsync(ISite site, ABTestingSettings settings, BuildEditorContext context)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (!await _authorizationService.AuthorizeAsync(user, Permissions.ManageABTests))
        {
            return null;
        }

        return Initialize<ABTestingSettingsViewModel>("ABTestingSettings_Edit", model =>
        {
            model.MinimumSampleSize = settings.MinimumSampleSize;
            model.ConfidenceThreshold = settings.ConfidenceThreshold;
        }).Location("Content:5")
        .OnGroup(SettingsGroupId);
    }

    public override async Task<IDisplayResult> UpdateAsync(ISite site, ABTestingSettings settings, UpdateEditorContext context)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (!await _authorizationService.AuthorizeAsync(user, Permissions.ManageABTests))
        {
            return null;
        }

        var model = new ABTestingSettingsViewModel();
        await context.Updater.TryUpdateModelAsync(model, Prefix);

        if (context.Updater.ModelState.IsValid)
        {
            settings.MinimumSampleSize = model.MinimumSampleSize;
            settings.ConfidenceThreshold = model.ConfidenceThreshold;
        }

        return await EditAsync(site, settings, context);
    }
}
