namespace Fydar.AspNetCore.OpenGraph.Internal;

internal sealed class MetadataImageBuilder : IMetadataImageBuilder
{
	public string Url { get; set; } = string.Empty;
	public string Alt { get; set; } = string.Empty;
}
