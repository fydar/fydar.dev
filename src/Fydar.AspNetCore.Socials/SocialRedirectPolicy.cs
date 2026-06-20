using Fydar.AspNetCore.OpenGraph;
using System;

namespace Fydar.AspNetCore.Socials;

public class SocialRedirectPolicy
{
	public required string Destination { get; set; } = string.Empty;
	public Action<IMetadataFactory>? Metadata { get; set; }
}
