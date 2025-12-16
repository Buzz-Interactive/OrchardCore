using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using OrchardCore.ContentManagement;
using OrchardCore.ContentManagement.Display.ContentDisplay;
using OrchardCore.ContentManagement.Display.ViewModels;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.Views;

namespace OrchardCore.ABTesting.Drivers;

/// <summary>
/// Display driver that adds the "View Results" action to the ABTest content item's Actions menu.
/// </summary>
public sealed class ABTestResultsDisplayDriver : ContentDisplayDriver
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuthorizationService _authorizationService;

    public ABTestResultsDisplayDriver(
        IHttpContextAccessor httpContextAccessor,
        IAuthorizationService authorizationService)
    {
        _httpContextAccessor = httpContextAccessor;
        _authorizationService = authorizationService;
    }

    public override IDisplayResult Display(ContentItem contentItem, BuildDisplayContext context)
    {
        // Only show for ABTest content type
        if (contentItem.ContentType != "ABTest")
        {
            return null;
        }

        return Shape("ABTest_ViewResults_SummaryAdmin", new ContentItemViewModel(contentItem))
            .Location("SummaryAdmin", "ActionsMenu:5")
            .RenderWhen(() => AuthorizeAsync());
    }

    private async Task<bool> AuthorizeAsync()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user == null)
        {
            return false;
        }

        return await _authorizationService.AuthorizeAsync(user, Permissions.ManageABTests);
    }
}
