using OrchardCore.ABTesting.Workflows.Activities;
using OrchardCore.Workflows.ViewModels;

namespace OrchardCore.ABTesting.Workflows.ViewModels;

public class ABTestWinnerDetectedEventViewModel : ActivityViewModel<ABTestWinnerDetectedEvent>
{
    public ABTestWinnerDetectedEventViewModel()
    {
    }

    public ABTestWinnerDetectedEventViewModel(ABTestWinnerDetectedEvent activity)
    {
        Activity = activity;
    }
}
