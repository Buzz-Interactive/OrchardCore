using OrchardCore.ABTesting.Workflows.Activities;
using OrchardCore.ABTesting.Workflows.ViewModels;
using OrchardCore.DisplayManagement.Handlers;
using OrchardCore.DisplayManagement.Views;
using OrchardCore.Workflows.Display;
using OrchardCore.Workflows.Models;

namespace OrchardCore.ABTesting.Workflows.Drivers;

public sealed class DeclareABTestWinnerTaskDisplayDriver : ActivityDisplayDriver<DeclareABTestWinnerTask, DeclareABTestWinnerTaskViewModel>
{
    protected override void EditActivity(DeclareABTestWinnerTask source, DeclareABTestWinnerTaskViewModel target)
    {
        target.Activity = source;
        target.TestIdExpression = source.TestId.Expression;
        target.Winner = source.Winner;
    }

    protected override void UpdateActivity(DeclareABTestWinnerTaskViewModel model, DeclareABTestWinnerTask activity)
    {
        activity.TestId = new WorkflowExpression<string>(model.TestIdExpression);
        activity.Winner = model.Winner;
    }

    public override Task<IDisplayResult> DisplayAsync(DeclareABTestWinnerTask activity, BuildDisplayContext context)
    {
        return CombineAsync(
            Shape("DeclareABTestWinnerTask_Fields_Thumbnail", new DeclareABTestWinnerTaskViewModel
            {
                Activity = activity,
                TestIdExpression = activity.TestId.Expression,
                Winner = activity.Winner,
            }).Location("Thumbnail", "Content"),
            Shape("DeclareABTestWinnerTask_Fields_Design", new DeclareABTestWinnerTaskViewModel
            {
                Activity = activity,
                TestIdExpression = activity.TestId.Expression,
                Winner = activity.Winner,
            }).Location("Design", "Content")
        );
    }
}
