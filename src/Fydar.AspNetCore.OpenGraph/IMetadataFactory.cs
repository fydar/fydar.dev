using System;

namespace Fydar.AspNetCore.OpenGraph;

public interface IMetadataFactory
{
    public void UseTitle(string title);
    public void UseDescription(string description);
    public void UseCanonicalUrl(string url);
    public void UseImage(Action<IMetadataImageBuilder> configure);
}
