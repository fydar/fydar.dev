using Microsoft.AspNetCore.Components;

namespace Fydar.Dev.WebApp.Components;

internal sealed class HeadFragmentRegistry
{
	private readonly List<RenderFragment> _fragments = [];
	private HeadFragmentOutlet? _outlet;

	public IReadOnlyList<RenderFragment> Fragments => _fragments;

	internal void Subscribe(HeadFragmentOutlet outlet)
	{
		_outlet = outlet;
	}

	internal void Add(RenderFragment fragment)
	{
		_fragments.Add(fragment);
		_outlet?.Notify();
	}

	internal void Remove(RenderFragment fragment)
	{
		_fragments.Remove(fragment);
	}
}
