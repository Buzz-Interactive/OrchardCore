using OrchardCore.ABTesting.Workflows.Activities;
using OrchardCore.ABTesting.Workflows.ViewModels;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Workflows.Display;

namespace OrchardCore.ABTesting.Workflows.Drivers;

public sealed class ABTestWinnerDetectedEventDisplayDriver : ActivityDisplayDriver<ABTestWinnerDetectedEvent, ABTestWinnerDetectedEventViewModel>
{
    protected override void EditActivity(ABTestWinnerDetectedEvent source, ABTestWinnerDetectedEventViewModel model)
    {
        model.Activity = source;
    }

    public override Task<IDisplayResult> DisplayAsync(ABTestWinnerDetectedEvent activity, BuildDisplayContext context)
    {
        return CombineAsync(
            Shape("ABTestWinnerDetectedEvent_Fields_Thumbnail", new ABTestWinnerDetectedEventViewModel(activity)).Location("Thumbnail", "Content"),
            Shape("ABTestWinnerDetectedEvent_Fields_Design", new ABTestWinnerDetectedEventViewModel(activity)).Location("Design", "Content")
        );
    }
}
