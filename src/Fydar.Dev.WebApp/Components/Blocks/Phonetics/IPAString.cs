namespace Fydar.Dev.WebApp.Components.Blocks.Phonetics;

public class IPAString
{
    public IPACluster[] Clusters { get; set; }

    public IPAString(params IPACluster[] clusters)
    {
        Clusters = clusters;
    }
}
