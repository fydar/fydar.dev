using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Fydar.Dev.WebApp.Components.Blocks.Graphs;

public partial class Block_NodeGraph : ComponentBase
{
    [Inject]
    public IJSRuntime JSRuntime { get; set; } = default!;

    public required string PositionPixelsX { get; set; }
    public required string PositionPixelsY { get; set; }

    protected override Task OnInitializedAsync()
    {
        return base.OnInitializedAsync();
    }
}
