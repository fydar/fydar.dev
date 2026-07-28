using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Fydar.Dev.WebApp.Components.Blocks.Graphs;

public partial class Node : ComponentBase
{
    private bool isDragging = false;
    private double dragStartPositionX;
    private double dragStartPositionY;

    protected override Task OnInitializedAsync()
    {
        return base.OnInitializedAsync();
    }

    private void Node_OnMouseMove(MouseEventArgs mouseEventArgs)
    {
        if (isDragging)
        {
            X = (int)Math.Round((mouseEventArgs.ClientX - dragStartPositionX) / 20.0);
            Y = (int)Math.Round((mouseEventArgs.ClientY - dragStartPositionY) / 20.0);
            StateHasChanged();
        }
    }

    private void Node_OnMouseDown(MouseEventArgs mouseEventArgs)
    {
        isDragging = true;
        dragStartPositionX = mouseEventArgs.ClientX - (X * 20);
        dragStartPositionY = mouseEventArgs.ClientY - (Y * 20);
    }

    private void Node_OnMouseUp(MouseEventArgs mouseEventArgs)
    {
        isDragging = false;
    }
}
