namespace Fydar.AspNetCore.OpenGraph;

public interface IMetadataImageBuilder
{
	public string Url { get; set; }
	public string Alt { get; set; }
}
