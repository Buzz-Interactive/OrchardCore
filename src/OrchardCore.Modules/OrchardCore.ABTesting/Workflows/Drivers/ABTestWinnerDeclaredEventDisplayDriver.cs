using OrchardCore.ABTesting.Workflows.Activities;
using OrchardCore.ABTesting.Workflows.ViewModels;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Workflows.Display;

namespace OrchardCore.ABTesting.Workflows.Drivers;

public sealed class ABTestWinnerDeclaredEventDisplayDriver : ActivityDisplayDriver<ABTestWinnerDeclaredEvent, ABTestWinnerDeclaredEventViewModel>
{
    protected override void EditActivity(ABTestWinnerDeclaredEvent source, ABTestWinnerDeclaredEventViewModel model)
    {
        model.Activity = source;
    }

    public override Task<IDisplayResult> DisplayAsync(ABTestWinnerDeclaredEvent activity, BuildDisplayContext context)
    {
        return CombineAsync(
            Shape("ABTestWinnerDeclaredEvent_Fields_Thumbnail", new ABTestWinnerDeclaredEventViewModel(activity)).Location("Thumbnail", "Content"),
            Shape("ABTestWinnerDeclaredEvent_Fields_Design", new ABTestWinnerDeclaredEventViewModel(activity)).Location("Design", "Content")
        );
    }
}
