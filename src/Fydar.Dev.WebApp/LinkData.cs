using System.Collections;
using System.Text;
using System.Text.Json;

namespace Fydar.Dev.WebApp;

public class LinkData : IReadOnlyList<ILinkDataElement>
{
	private readonly List<ILinkDataElement> elements = [];

	public ILinkDataElement this[int index] => elements[index];

	public int Count => elements.Count;

	public void Add(ILinkDataElement element)
	{
		elements.Add(element);
	}

	/// <inheritdoc/>
	public IEnumerator<ILinkDataElement> GetEnumerator()
	{
		return elements.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return elements.GetEnumerator();
	}

	public string ToJson()
	{
		var options = new JsonWriterOptions
		{
			Indented = false
		};

		using var stream = new MemoryStream();
		using var writer = new Utf8JsonWriter(stream, options);

		writer.WriteStartArray();
		foreach (var linkDataElement in elements)
		{
			if (linkDataElement is LinkDataBreadcrumbList breadcrumbList)
			{
				writer.WriteStartObject();
				writer.WriteString("@context", "https://schema.org");
				writer.WriteString("@type", "BreadcrumbList");
				writer.WriteString("name", string.Join(" > ", breadcrumbList.Select(b => b.Name).Where(n => !string.IsNullOrWhiteSpace(n))));
				writer.WritePropertyName("itemListElement");
				writer.WriteStartArray();
				for (int j = 0; j < breadcrumbList.Count; j++)
				{
					var breadcrumb = breadcrumbList[j];

					writer.WriteStartObject();
					writer.WriteString("@type", "ListItem");
					writer.WriteNumber("position", j + 1);
					writer.WriteString("name", breadcrumb.Name);

					if (!string.IsNullOrWhiteSpace(breadcrumb.Item))
					{
						writer.WriteString("item", breadcrumb.Item);
					}
					writer.WriteEndObject();
				}
				writer.WriteEndArray();
				writer.WriteEndObject();
			}
		}
		writer.WriteEndArray();

		writer.Flush();

		string json = Encoding.UTF8.GetString(stream.ToArray());
		return json;
	}
}

public interface ILinkDataElement
{

}

public class LinkDataBreadcrumbList : ILinkDataElement, IReadOnlyList<LinkDataBreadcrumbListItem>
{
	private readonly List<LinkDataBreadcrumbListItem> elements = [];

	public LinkDataBreadcrumbListItem this[int index] => elements[index];

	public int Count => elements.Count;

	public void Add(LinkDataBreadcrumbListItem element)
	{
		elements.Add(element);
	}

	/// <inheritdoc/>
	public IEnumerator<LinkDataBreadcrumbListItem> GetEnumerator()
	{
		return elements.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return elements.GetEnumerator();
	}
}

public class LinkDataBreadcrumbListItem
{
	public string Name { get; set; }
	public string Item { get; set; }

	public LinkDataBreadcrumbListItem()
	{
		Name = string.Empty;
		Item = string.Empty;
	}

	public LinkDataBreadcrumbListItem(string name)
	{
		Name = name;
		Item = string.Empty;
	}

	public LinkDataBreadcrumbListItem(string name, string item)
	{
		Name = name;
		Item = item;
	}
}

