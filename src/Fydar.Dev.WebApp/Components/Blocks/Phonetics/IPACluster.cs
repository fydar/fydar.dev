namespace Fydar.Dev.WebApp.Components.Blocks.Phonetics;

public class IPACluster
{
    public static IPACluster PrimaryStress { get; } = new()
    {
        Symbol = "ˈ",
        SymbolName = "Vertical Stroke (Superior)",
        SoundDescription = "Primary Stress Mark",
        Hint = "/ˈ/: primary stress follows"
    };

    public static IPACluster F { get; } = new()
    {
        Symbol = "f",
        SymbolName = "Lower-Case F",
        SoundDescription = "Voiceless labiodental fricative",
        Hint = "/f/: 'f' in 'find'",
        Examples =
        [
            "(f)ind",
            "lea(f)",
        ]
    };

    public static IPACluster Y { get; } = new()
    {
        Symbol = "aɪ",
        SymbolName = "Lower-Case A and Small Capital I",
        SoundDescription = "Open front unrounded vowel",
        Hint = "/aɪ/: 'ie' in 'pie'",
        Examples =
        [
            "p(ie)"
        ]
    };

    public static IPACluster Eir { get; } = new()
    {
        Symbol = "ɛ",
        SymbolName = "Epsilon",
        SoundDescription = "Open-mid front unrounded vowel",
        Hint = "/ɛ/: 'e' in 'bed'",
        Examples =
        [
            "b(e)d"
        ]
    };

    public static IPACluster Syllabification { get; } = new()
    {
        Symbol = ".",
        SymbolName = "Period",
        SoundDescription = "Syllabification",
        Hint = "/./: syllable break"
    };

    public static IPACluster D { get; } = new()
    {
        Symbol = "d",
        SymbolName = "Lower-Case D",
        SoundDescription = "Voiced alveolar plosive",
        Hint = "/d/: 'd' in 'dye'",
        Examples =
        [
            "(d)ye",
            "car(d)",
            "la(dd)er"
        ]
    };

    public static IPACluster AR { get; } = new()
    {
        Symbol = "ɑː",
        SymbolName = "Script A and Length Mark",
        SoundDescription = "Open back unrounded vowel",
        Hint = "/ɑː/: 'a' in 'father'",
        Examples =
        [
            "f(a)ther",
            "dr(a)ma",
            "sp(a)",
        ]
    };

    public static IPACluster R { get; } = new()
    {
        Symbol = "ɹ̠",
        SymbolName = "Turned R and Underbar",
        SoundDescription = "Voiced postalveolar approximant",
        Hint = "/ɹ̠/: 'r' in 'red'",
        Examples =
        [
            "(r)ed",
            "(wr)ite",
            "(rh)inoceros",
        ]
    };

    public static IPACluster Phi { get; } = new()
    {
        Symbol = "θ",
        SymbolName = "Theta",
        SoundDescription = "Voiceless dental fricative",
        Hint = "/θ/: 'th' in 'thin'",
        Examples =
        [
            "(th)in",
            "(th)ree",
            "(th)igh",
        ]
    };

    public static IPACluster AspiratedStop { get; } = new()
    {
        Symbol = "pʰ",
        SymbolName = "Lower-Case P and Superscript Lower-Case H",
        SoundDescription = "Aspirated Stop",
        Hint = "/p/: 'p' in 'pat'",
        Examples =
        [
            "(p)at",
        ]
    };

    public string SoundDescription { get; set; }
    public string SymbolName { get; set; }
    public string Symbol { get; set; }
    public string Hint { get; set; }
    public IPAExampleWord[] Examples { get; set; } = Array.Empty<IPAExampleWord>();

    public IPACluster()
    {
    }

    public IPACluster(string symbol, params string[] example)
    {
        Symbol = symbol;
        Examples = [.. example.Select(e => new IPAExampleWord(e))];
    }
}
