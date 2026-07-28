namespace Fydar.Dev.WebApp.Components.Blocks.Phonetics;

public readonly struct IPAExampleWord
{
    public string Prefix { get; }
    public string Sample { get; }
    public string Suffix { get; }

    public IPAExampleWord(string template)
    {
        int indexOpen = template.IndexOf('(');
        if (indexOpen < 0)
        {
            // No parentheses: treat whole template as Sample
            Prefix = "";
            Sample = template.Trim();
            Suffix = "";
            return;
        }

        int indexClose = template.IndexOf(')', indexOpen + 1);
        if (indexClose <= indexOpen)
        {
            // Malformed parentheses: fallback to treating whole string as Sample
            Prefix = "";
            Sample = template.Trim();
            Suffix = "";
            return;
        }

        string rawPrefix = template[..indexOpen];
        string rawSample = template.Substring(indexOpen + 1, indexClose - indexOpen - 1);
        string rawSuffix = template[(indexClose + 1)..];

        Prefix = rawPrefix.Trim();
        Sample = rawSample.Trim();
        Suffix = rawSuffix.Trim();
    }

    public static implicit operator IPAExampleWord(string template)
    {
        return new IPAExampleWord(template);
    }
}
