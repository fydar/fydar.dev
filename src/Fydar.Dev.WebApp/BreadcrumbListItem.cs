namespace Fydar.Dev.WebApp;

public class BreadcrumbListItem
{
	public string Name { get; set; }
	public string Href { get; set; }

	public BreadcrumbListItem(string name, string href = "")
	{
		Name = name;
		Href = href;
	}
}
