using Microsoft.AspNetCore.Components;

namespace Fydar.Dev.WebApp.Toolkit.Icons;

public abstract class Icon : ComponentBase
{
    [Parameter]
    public required string Alt { get; set; }

    public virtual string Identifier { get; }

    public virtual string Title { get; }
}
