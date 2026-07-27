namespace Fydar.Dev.WebApp.Components.Blocks;

internal static class IssueCardStateHelper
{
    public static string ToClass(IssueCardState state)
    {
        return state switch
        {
            IssueCardState.Completed => "completed",
            IssueCardState.NotPlanned => "not-planned",
            _ => "open",
        };
    }
}
