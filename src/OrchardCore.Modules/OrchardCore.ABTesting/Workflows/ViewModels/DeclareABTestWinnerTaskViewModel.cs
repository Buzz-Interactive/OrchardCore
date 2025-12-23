using OrchardCore.ABTesting.Workflows.Activities;
using OrchardCore.Workflows.ViewModels;

namespace OrchardCore.ABTesting.Workflows.ViewModels;

public class DeclareABTestWinnerTaskViewModel : ActivityViewModel<DeclareABTestWinnerTask>
{
    public string TestIdExpression { get; set; }

    public string Winner { get; set; }
}
