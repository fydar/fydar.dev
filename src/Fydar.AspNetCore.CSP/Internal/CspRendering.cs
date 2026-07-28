using System.Collections.Generic;

namespace Fydar.AspNetCore.CSP.Internal;

internal class CspRendering
{
    public Dictionary<string, SortedSet<string>> PolicyMap { get; set; } = [];
}
