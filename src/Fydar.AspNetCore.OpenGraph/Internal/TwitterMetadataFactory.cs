using System;

namespace Fydar.AspNetCore.OpenGraph.Internal;

internal sealed class TwitterMetadataFactory : ITwitterMetadataFactory
{
	public string? Title { get; private set; }
	public string? Description { get; private set; }
	public string? CanonicalUrl { get; private set; }
	public MetadataImageBuilder? Image { get; private set; }

	public void UseTitle(string title)
	{
		Title = title;
	}

	public void UseDescription(string description)
	{
		Description = description;
	}

	public void UseCanonicalUrl(string url)
	{
		CanonicalUrl = url;
	}

	public void UseImage(Action<IMetadataImageBuilder> configure)
	{
		var builder = new MetadataImageBuilder();
		configure(builder);
		Image = builder;
	}
}
