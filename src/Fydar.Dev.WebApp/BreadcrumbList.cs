using System.Collections;

namespace Fydar.Dev.WebApp;

public class BreadcrumbList : IReadOnlyList<BreadcrumbListItem>
{
    private readonly List<BreadcrumbListItem> items = [];

    public BreadcrumbListItem this[int index] => items[index];
    public int Count => items.Count;

    public void Add(BreadcrumbListItem item)
    {
        items.Add(item);
    }

    public IEnumerator<BreadcrumbListItem> GetEnumerator()
    {
        return items.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return items.GetEnumerator();
    }
}
