using OrchardCore.ABTesting.Workflows.Activities;
using OrchardCore.Workflows.ViewModels;

namespace OrchardCore.ABTesting.Workflows.ViewModels;

public class ABTestWinnerDeclaredEventViewModel : ActivityViewModel<ABTestWinnerDeclaredEvent>
{
    public ABTestWinnerDeclaredEventViewModel()
    {
    }

    public ABTestWinnerDeclaredEventViewModel(ABTestWinnerDeclaredEvent activity)
    {
        Activity = activity;
    }
}
