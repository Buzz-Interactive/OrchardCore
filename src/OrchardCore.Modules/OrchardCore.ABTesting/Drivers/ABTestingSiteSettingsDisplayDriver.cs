using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using OrchardCore.ABTesting.Settings;
using OrchardCore.ABTesting.ViewModels;
using OrchardCore.DisplayManagement.Entities;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Settings;

namespace OrchardCore.ABTesting.Drivers;

public sealed class ABTestingSiteSettingsDisplayDriver : SiteDisplayDriver<ABTestingSettings>
{
    public const string GroupId = "abtesting";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuthorizationService _authorizationService;

    public ABTestingSiteSettingsDisplayDriver(
        IHttpContextAccessor httpContextAccessor,
        IAuthorizationService authorizationService)
    {
        _httpContextAccessor = httpContextAccessor;
        _authorizationService = authorizationService;
    }

    protected override string SettingsGroupId
        => GroupId;

    public override async Task<IDisplayResult> EditAsync(ISite site, ABTestingSettings settings, BuildEditorContext context)
    {
        var user = _httpContextAccessor.HttpContext?.User;

        if (!await _authorizationService.AuthorizeAsync(user, Permissions.ManageABTests))
        {
            return null;
        }

        return Initialize<ABTestingSettingsViewModel>("ABTestingSettings_Edit", model =>
        {
            model.DisplayAllContentTypes = settings.DisplayAllContentTypes;
            model.AllowedContentTypes = settings.AllowedContentTypes ?? [];
            model.MinimumSampleSizeLimit = settings.MinimumSampleSizeLimit;
            model.EnableBotDetection = settings.EnableBotDetection;
            model.BotUserAgentPatternsText = settings.BotUserAgentPatterns != null
                ? string.Join("\n", settings.BotUserAgentPatterns)
                : string.Join("\n", ABTestingSettings.DefaultBotPatterns);
        }).Location("Content:3")
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

        await context.Updater.TryUpdateModelAsync(model, Prefix,
            m => m.DisplayAllContentTypes,
            m => m.AllowedContentTypes,
            m => m.MinimumSampleSizeLimit,
            m => m.EnableBotDetection,
            m => m.BotUserAgentPatternsText);

        settings.DisplayAllContentTypes = model.DisplayAllContentTypes;
        settings.AllowedContentTypes = model.AllowedContentTypes ?? [];
        settings.MinimumSampleSizeLimit = Math.Clamp(model.MinimumSampleSizeLimit, 10, 100);
        settings.EnableBotDetection = model.EnableBotDetection;
        settings.BotUserAgentPatterns = ParsePatterns(model.BotUserAgentPatternsText);

        return await EditAsync(site, settings, context);
    }

    private static string[] ParsePatterns(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return ABTestingSettings.DefaultBotPatterns;
        }

        return text
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrEmpty(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
